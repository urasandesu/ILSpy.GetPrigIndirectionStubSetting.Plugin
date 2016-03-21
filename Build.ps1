# 
# File: Build.ps1
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

[CmdletBinding(DefaultParametersetName = 'Package')]
param (
    [Parameter(ParameterSetName = 'Package')]
    [Switch]
    $Package, 

    [ValidateSet("", "Clean", "Rebuild")] 
    [string]
    $BuildTarget
)

trap {
    Write-Error ($Error[0] | Out-String)
    exit -1
}

try {
    msbuild /ver | Out-Null
} catch [System.Management.Automation.CommandNotFoundException] {
    Write-Error "You have to run this script in the Developer Command Prompt for VS2013 as Administrator."
    exit 392847384
}


try {
    cpack | Out-Null
} catch [System.Management.Automation.CommandNotFoundException] {
    Write-Error "You have to install Chocolatey. For more information, please see also README.md."
    exit -915763295
}


if (![string]::IsNullOrEmpty($BuildTarget)) {
    $buildTarget_ = ":" + $BuildTarget
}

switch ($PsCmdlet.ParameterSetName) {
    'Package' { 
        $curDir = $PWD.Path

        $solution = "ILSpy.GetPrigIndirectionStubSetting.Plugin.sln"
        $target = "/t:ILSpy_GetPrigIndirectionStubSetting_Plugin$buildTarget_"
        $configurations = "/p:Configuration=Release"
        $platforms = "/p:Platform=Any CPU"
        foreach ($configuration in $configurations) {
            foreach ($platform in $platforms) {
                Write-Verbose ("Solution: {0}" -f $solution)
                Write-Verbose ("Target: {0}" -f $target)
                Write-Verbose ("Configuration: {0}" -f $configuration)
                Write-Verbose ("Platform: {0}" -f $platform)
                msbuild $solution $target $configuration $platform /m
                if ($LASTEXITCODE -ne 0) {
                    exit $LASTEXITCODE
                }
            }
        }

        if ($BuildTarget -ne "Clean") {
            Push-Location ([System.IO.Path]::Combine($curDir, 'Chocolatey'))
            [System.Environment]::CurrentDirectory = $PWD
			cpack 'ILSpy.GetPrigIndirectionStubSetting.Plugin.nuspec'
			Pop-Location
        }
    }
}
