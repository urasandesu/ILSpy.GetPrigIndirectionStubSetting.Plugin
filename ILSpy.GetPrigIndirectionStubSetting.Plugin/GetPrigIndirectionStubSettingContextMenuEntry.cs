/* 
 * File: GetPrigIndirectionStubSettingContextMenuEntry.cs
 * 
 * Author: Akira Sugiura (urasandesu@gmail.com)
 * 
 * 
 * Copyright (c) 2016 Akira Sugiura
 *  
 *  This software is MIT License.
 *  
 *  Permission is hereby granted, free of charge, to any person obtaining a copy
 *  of this software and associated documentation files (the "Software"), to deal
 *  in the Software without restriction, including without limitation the rights
 *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *  copies of the Software, and to permit persons to whom the Software is
 *  furnished to do so, subject to the following conditions:
 *  
 *  The above copyright notice and this permission notice shall be included in
 *  all copies or substantial portions of the Software.
 *  
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 *  THE SOFTWARE.
 */



using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TreeNodes;
using ICSharpCode.TreeView;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace ILSpy.GetPrigIndirectionStubSetting.Plugin
{
    [ExportContextMenuEntry(Header = "Get Prig Indirection Stub Setting", Category = "GetPrigIndirectionStubSetting")]
    public class GetPrigIndirectionStubSettingContextMenuEntry : IContextMenuEntry
    {
        public void Execute(TextViewContext context)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                var pkgDir = EnvironmentRepository.GetPackageFolder();
                if (string.IsNullOrEmpty(pkgDir))
                {
                    MessageBox.Show(Application.Current.MainWindow,
                                    "You haven't registered Prig yet.\r\n" +
                                    "Please install Prig from Visual Studio Gallery and register it: \r\n" +
                                    "In Visual Studio, choose the menu \"PRIG\" - \"Register Prig (Needs Restarting)\".",
                                    "ILSpy.GetPrigIndirectionStubSetting.Plugin",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Information);
                    return;
                }

                var methodIds = GetMethodIds(context.SelectedTreeNodes.Cast<IMemberTreeNode>().Select(_ => _.Member));
                var indStubSetting = GetIndirectionStubSetting(methodIds);
                if (indStubSetting == null)
                    return;

                Clipboard.SetText(indStubSetting);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        public bool IsEnabled(TextViewContext context)
        {
            return AreReplaceableNodesSelected(context);
        }

        public bool IsVisible(TextViewContext context)
        {
            return AreReplaceableNodesSelected(context);
        }

        static bool AreReplaceableNodesSelected(TextViewContext context)
        {
            if (context.SelectedTreeNodes == null)
                return false;

            return context.SelectedTreeNodes.All(IsReplaceableNodeSelected);
        }

        static bool IsReplaceableNodeSelected(SharpTreeNode node)
        {
            var memberNode = node as IMemberTreeNode;
            if (memberNode == null)
                return false;

            return memberNode.Member is MethodDefinition ||
                   memberNode.Member is PropertyDefinition ||
                   memberNode.Member is EventDefinition;
        }

        static IEnumerable<ReflectionMethodId> GetMethodIds(IEnumerable<MemberReference> members)
        {
            foreach (var conversion in members.Select(ReflectionMethodIdConversionFactory.New))
                foreach (var methodId in conversion.Perform())
                    yield return methodId;
        }

        static string GetIndirectionStubSetting(IEnumerable<ReflectionMethodId> methodIds)
        {
            var tempDomain = default(AppDomain);
            try
            {
                tempDomain = AppDomain.CreateDomain("TemporaryDomain");

                var impl = new GetIndirectionStubSettingImpl();
                impl.Parameter_methodIds = methodIds.ToArray();
                tempDomain.DoCallBack(impl.Invoke);

                return impl.Result;
            }
            finally
            {
                if (tempDomain != null)
                    AppDomain.Unload(tempDomain);
            }
        }

        class GetIndirectionStubSettingImpl : MarshalByRefObject
        {
            public ReflectionMethodId[] Parameter_methodIds { get; set; }

            public void Invoke()
            {
                var methods = ToReflectionMethod(Parameter_methodIds).ToArray();

                var initial = InitialSessionState.CreateDefault();
                initial.AuthorizationManager = new AuthorizationManager("MyShellId");
                var pkgDir = EnvironmentRepository.GetPackageFolder();
                initial.ImportPSModule(new[] { Path.Combine(EnvironmentRepository.GetPackageFolder(), @"tools\Urasandesu.Prig") });

                using (var runspace = RunspaceFactory.CreateRunspace(initial))
                {
                    runspace.Open();
                    runspace.SessionStateProxy.SetVariable("InputObject", methods);
                    var command = "$InputObject | Get-IndirectionStubSetting";
                    using (var pipeline = runspace.CreatePipeline(command, false))
                        Result = string.Join("\r\n", pipeline.Invoke().Select(_ => _.BaseObject).Cast<string>());
                }
            }

            public string Result { get; private set; }

            static IEnumerable<MethodBase> ToReflectionMethod(IEnumerable<ReflectionMethodId> methodIds)
            {
                foreach (var methodId in methodIds)
                {
                    var assembly = Assembly.LoadFrom(methodId.AssemblyLocation);
                    var type = GetTypes(assembly)[methodId.TypeDefToken];
                    yield return GetMethods(type)[methodId.MethodDefToken];
                }
            }

            static Dictionary<Assembly, Dictionary<int, Type>> ms_assemblyTypes = new Dictionary<Assembly, Dictionary<int, Type>>();
            static Dictionary<int, Type> GetTypes(Assembly assembly)
            {
                if (!ms_assemblyTypes.ContainsKey(assembly))
                    ms_assemblyTypes.Add(assembly, new Dictionary<int, Type>(assembly.GetTypes().ToDictionary(_ => _.MetadataToken)));
                return ms_assemblyTypes[assembly];
            }

            const BindingFlags AllDeclared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            static Dictionary<Type, Dictionary<int, MethodBase>> ms_typeMethods = new Dictionary<Type, Dictionary<int, MethodBase>>();
            static Dictionary<int, MethodBase> GetMethods(Type type)
            {
                if (!ms_typeMethods.ContainsKey(type))
                    ms_typeMethods.Add(type, new Dictionary<int, MethodBase>(type.GetMembers(AllDeclared).OfType<MethodBase>().ToDictionary(_ => _.MetadataToken)));
                return ms_typeMethods[type];
            }
        }
    }
}
