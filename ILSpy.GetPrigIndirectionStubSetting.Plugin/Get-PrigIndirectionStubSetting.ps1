# 
# File: Get-PrigIndirectionStubSetting.ps1
# 
# Author: Akira Sugiura (urasandesu@gmail.com)
# 
# 
# Copyright (c) 2016 Akira Sugiura
#  
#  This software is MIT License.
#  
#  Permission is hereby granted, free of charge, to any person obtaining a copy
#  of this software and associated documentation files (the "Software"), to deal
#  in the Software without restriction, including without limitation the rights
#  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
#  copies of the Software, and to permit persons to whom the Software is
#  furnished to do so, subject to the following conditions:
#  
#  The above copyright notice and this permission notice shall be included in
#  all copies or substantial portions of the Software.
#  
#  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
#  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
#  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
#  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
#  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
#  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
#  THE SOFTWARE.
#

[CmdletBinding()]
param (
    [Parameter(Mandatory = $True)]
    [string]
    $MethodIdsPath, 

    [switch]
    $Core
)

Write-Verbose ('Method IDs Path          : {0}' -f $MethodIdsPath)



function InvokeCore {
    param (
        [Parameter(Mandatory = $True)]
        [string]
        $AssemblyLocation
    )

    [System.Reflection.ProcessorArchitecture]$osArch = 0
    $osArch = $(if ([System.IntPtr]::Size -eq 8) { 'Amd64' } else { 'X86' })
    Write-Verbose ('ILSpy running for: {0}' -f $osArch)

    [System.Reflection.ProcessorArchitecture]$asmArch = 0
    $asmName = prig dasm -assemblyfrom $AssemblyLocation | ConvertFrom-Csv
    $asmArch = $asmName.ProcessorArchitecture
    Write-Verbose ('Assembly Architecture: {0}' -f $asmArch)

    if ($osArch -eq 'X86') {
        if ($asmArch -match '(X86)|(MSIL)') {
            $powershell = [System.Environment]::ExpandEnvironmentVariables('%windir%\system32\WindowsPowerShell\v1.0\powershell.exe')
        }
    } elseif ($osArch -eq 'Amd64') {
        if ($asmArch -match '(Amd64)|(MSIL)') {
            $powershell = [System.Environment]::ExpandEnvironmentVariables('%windir%\system32\WindowsPowerShell\v1.0\powershell.exe')
        } elseif ($asmArch -eq 'X86') {
            $powershell = [System.Environment]::ExpandEnvironmentVariables('%windir%\SysWOW64\WindowsPowerShell\v1.0\powershell.exe')
        }
    }
    if ($null -eq $powershell) {
        New-Object System.NotSupportedException ('Running ILSpy for {0} and loading {1} assembly is not supported.' -f $osArch, $asmArch) 
    }
    Write-Verbose ('PowerShell: {0}' -f $powershell)

    $imageRuntimeVer = $asmName.ImageRuntimeVersion
    Write-Verbose ('Image Runtime Version: {0}' -f $imageRuntimeVer)

    $argList = '-NoLogo', '-NoProfile', '-File', ".\Get-PrigIndirectionStubSetting.ps1", """$MethodIdsPath""", "-Core"
    if ($imageRuntimeVer -eq 'v2.0.50727') {
        $argList = '-Version', '2' + $argList
    }
    if ($PSCmdlet.MyInvocation.BoundParameters["Verbose"].IsPresent) {
        $argList += '-Verbose'
    }
    Write-Verbose ('Argument List: {0}' -f ($argList -join ' '))

    $outputsPath = [System.IO.Path]::GetTempFileName()
    $errorsPath = [System.IO.Path]::GetTempFileName()
    $proc = Start-Process $powershell $argList -Wait -WindowStyle 'Hidden' -RedirectStandardOutput $outputsPath -RedirectStandardError $errorsPath -PassThru
    $proc.WaitForExit()
    $errors = Get-Content $errorsPath
    Remove-Item $errorsPath -ErrorAction SilentlyContinue
    if (0 -lt $errors.Length) {
        throw New-Object System.InvalidOperationException ($errors -join "`r`n")
    }
    Remove-Item $MethodIdsPath -ErrorAction SilentlyContinue
    Get-Content $outputsPath
    Remove-Item $outputsPath -ErrorAction SilentlyContinue
}



function GetIndirectionStubSettingCore {
    param (
        [Parameter(Mandatory = $True)]
        [ILSpy.GetPrigIndirectionStubSetting.Common.ReflectionMethodId[]]
        $MethodIds
    )

    $inputObject = New-Object 'System.Collections.Generic.List[System.Reflection.MethodBase]'
    [System.Reflection.Assembly]$asm = $null
    $assemblyTypes = New-Object "System.Collections.Generic.Dictionary[int, Type]"
    $typeMethods = New-Object "System.Collections.Generic.Dictionary[Type, [System.Collections.Generic.Dictionary[int, System.Reflection.MethodBase]]]"
    foreach ($methodId in $MethodIds) {
        if ($null -eq $asm) {
            $asm = [System.Reflection.Assembly]::LoadFrom($methodId.AssemblyLocation)
            foreach ($type in $asm.GetTypes()) {
                $assemblyTypes[$type.MetadataToken] = $type
            }
        }
        $type = $assemblyTypes[$methodId.TypeDefToken]
        if (!$typeMethods.ContainsKey($type)) {
            $typeMethods[$type] = [System.Collections.Generic.Dictionary[int, System.Reflection.MethodBase]](
                                        New-Object "System.Collections.Generic.Dictionary[int, System.Reflection.MethodBase]")
            $members = $type.GetMembers([System.Reflection.BindingFlags]'Public, NonPublic, Static, Instance, DeclaredOnly')
            foreach ($member in $members) {
                if ($member -isnot [System.Reflection.MethodBase]) { continue }
                $methods = $typeMethods[$type]
                $methods[$member.MetadataToken] = $member
            }
        }
        $methods = $typeMethods[$type]
        $inputObject.Add($methods[$methodId.MethodDefToken])
    }

    Import-Module ([System.IO.Path]::Combine($env:URASANDESU_PRIG_PACKAGE_FOLDER, 'tools\Urasandesu.Prig'))
    if ($Host.UI.RawUI) {
        $rawUI = $Host.UI.RawUI
        $oldSize = $rawUI.BufferSize
        $typeName = $oldSize.GetType().FullName
        $newSize = New-Object $typeName (1024, $oldSize.Height)
        $rawUI.BufferSize = $newSize
    }
    $inputObject | Get-IndirectionStubSetting
}


[void][System.Reflection.Assembly]::LoadFrom((dir .\ILSpy.GetPrigIndirectionStubSetting.Common.dll).FullName)
try {
    $sr = New-Object System.IO.StreamReader $MethodIdsPath
    $serializer = New-Object System.Xml.Serialization.XmlSerializer ([ILSpy.GetPrigIndirectionStubSetting.Common.ReflectionMethodId[]])
    $methodIds = @($serializer.Deserialize($sr))
} finally {
    if ($null -ne $sr) {
        $sr.Dispose()
    }
}

if ($Core) {
    GetIndirectionStubSettingCore $methodIds
} else {
    InvokeCore $methodIds[0].AssemblyLocation
}
