/* 
 * File: PropertyDefinitionConversion.cs
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



using ILSpy.GetPrigIndirectionStubSetting.Common;
using Mono.Cecil;
using System.Collections.Generic;

namespace ILSpy.GetPrigIndirectionStubSetting.Plugin
{
    class PropertyDefinitionConversion : ReflectionMethodIdConversion
    {
        PropertyDefinition m_propDef;

        public PropertyDefinitionConversion(PropertyDefinition propDef)
        {
            m_propDef = propDef;
        }

        public override IEnumerable<ReflectionMethodId> Perform()
        {
            var methodIds = new List<ReflectionMethodId>();

            if (m_propDef.GetMethod != null)
            {
                var assemblyLocation = m_propDef.GetMethod.DeclaringType.Module.FullyQualifiedName;
                var typeDefToken = m_propDef.DeclaringType.MetadataToken.ToInt32();
                var methodDefToken = m_propDef.GetMethod.MetadataToken.ToInt32();
                methodIds.Add(new ReflectionMethodId(assemblyLocation, typeDefToken, methodDefToken));
            }

            if (m_propDef.SetMethod != null)
            {
                var assemblyLocation = m_propDef.SetMethod.DeclaringType.Module.FullyQualifiedName;
                var typeDefToken = m_propDef.DeclaringType.MetadataToken.ToInt32();
                var methodDefToken = m_propDef.SetMethod.MetadataToken.ToInt32();
                methodIds.Add(new ReflectionMethodId(assemblyLocation, typeDefToken, methodDefToken));
            }

            return methodIds;
        }
    }
}
