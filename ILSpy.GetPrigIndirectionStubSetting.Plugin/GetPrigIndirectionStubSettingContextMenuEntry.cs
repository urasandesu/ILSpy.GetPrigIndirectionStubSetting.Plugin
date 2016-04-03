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
using ILSpy.GetPrigIndirectionStubSetting.Common;
using Microsoft.PowerShell;
using Mono.Cecil;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Xml.Serialization;

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
            
            return context.SelectedTreeNodes.All(new ReplaceableNodesFinder().IsSatisfied);
        }

        class ReplaceableNodesFinder
        {
            readonly HashSet<string> m_assemblyFullNames = new HashSet<string>();

            public bool IsSatisfied(SharpTreeNode node)
            {
                var memberNode = node as IMemberTreeNode;
                if (memberNode == null)
                    return false;

                m_assemblyFullNames.Add(memberNode.Member.Module.Assembly.FullName);
                if (1 < m_assemblyFullNames.Count)
                    return false;

                return memberNode.Member is MethodDefinition ||
                       memberNode.Member is PropertyDefinition ||
                       memberNode.Member is EventDefinition;
            }
        }

        static IEnumerable<ReflectionMethodId> GetMethodIds(IEnumerable<MemberReference> members)
        {
            foreach (var conversion in members.Select(ReflectionMethodIdConversionFactory.New))
                foreach (var methodId in conversion.Perform())
                    yield return methodId;
        }

        static string GetIndirectionStubSetting(IEnumerable<ReflectionMethodId> methodIds)
        {
            var methodIdsPath = Path.GetTempFileName();
            using (var sw = new StreamWriter(methodIdsPath))
            {
                var serializer = new XmlSerializer(typeof(ReflectionMethodId[]));
                serializer.Serialize(sw, methodIds.ToArray());
            }

            var initial = InitialSessionState.CreateDefault();
            initial.AuthorizationManager = new AuthorizationManager("MyShellId");

            using (var runspace = RunspaceFactory.CreateRunspace(initial))
            {
                runspace.Open();
                runspace.SessionStateProxy.SetVariable("MethodIdsPath", methodIdsPath);

                var executingAsmDirPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var getPrigIndStubSettingPs1Path = Path.Combine(executingAsmDirPath, "Get-PrigIndirectionStubSetting.ps1");
                var command = new StringBuilder();
                command.AppendFormat("Set-ExecutionPolicy Bypass -Scope Process -Force\r\n", executingAsmDirPath);
                command.AppendFormat("cd \"{0}\"\r\n", executingAsmDirPath);
                command.AppendFormat("[System.Environment]::CurrentDirectory = $PWD\r\n", executingAsmDirPath);
                command.AppendFormat("& \"{0}\" -MethodIdsPath $MethodIdsPath", getPrigIndStubSettingPs1Path);
                using (var pipeline = runspace.CreatePipeline(command.ToString(), false))
                    return string.Join("\r\n", pipeline.Invoke().Select(_ => _.BaseObject).Cast<string>());
            }
        }
    }
}
