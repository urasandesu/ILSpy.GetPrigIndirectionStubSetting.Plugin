# ILSpy.GetPrigIndirectionStubSetting.Plugin
[ILSpy](https://github.com/icsharpcode/ILSpy) plugin supporting [Prig](https://github.com/urasandesu/Prig).



## INSTALLATION
Install Chocolatey in accordance with [the top page](https://chocolatey.org/). Then, run command prompt as Administrator, execute the following command: 
```dos
CMD C:\> cinst ilspy.getprigindirectionstubsetting.plugin -y
```



## USAGE
You can select the `Get Prig Indirection Stub Setting` menu when you right-click the method, property or event node(s) on ILSpy:  

![Get Prig Indirection Stub Setting menu](https://cdn.rawgit.com/urasandesu/ILSpy.GetPrigIndirectionStubSetting.Plugin/master/ILSpy.GetPrigIndirectionStubSetting.Plugin/Resources/Screenshot.png)

For example, if you select `Process.Start(string, string)` method like the above, you can get the [Indirection Stub Setting](https://github.com/urasandesu/Prig/wiki/Cheat-Sheet#-indirection-stub-setting) like the below to your clipboard: 
```xml
<add name="StartStringString" alias="StartStringString">
  <RuntimeMethodInfo xmlns:i="http://www.w3.org/2001/XMLSchema-instance" xmlns:x="http://www.w3.org/2001/XMLSchema" z:Id="1" z:FactoryType="MemberInfoSerializationHolder" z:Type="System.Reflection.MemberInfoSerializationHolder" z:Assembly="0" xmlns:z="http://schemas.microsoft.com/2003/10/Serialization/" xmlns="http://schemas.datacontract.org/2004/07/System.Reflection">
    <Name z:Id="2" z:Type="System.String" z:Assembly="0" xmlns="">Start</Name>
    <AssemblyName z:Id="3" z:Type="System.String" z:Assembly="0" xmlns="">System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</AssemblyName>
    <ClassName z:Id="4" z:Type="System.String" z:Assembly="0" xmlns="">System.Diagnostics.Process</ClassName>
    <Signature z:Id="5" z:Type="System.String" z:Assembly="0" xmlns="">System.Diagnostics.Process Start(System.String, System.String)</Signature>
    <Signature2 z:Id="6" z:Type="System.String" z:Assembly="0" xmlns="">System.Diagnostics.Process Start(System.String, System.String)</Signature2>
    <MemberType z:Id="7" z:Type="System.Int32" z:Assembly="0" xmlns="">8</MemberType>
    <GenericArguments i:nil="true" xmlns="" />
  </RuntimeMethodInfo>
</add>
```

After that, you just past it to the [Stub Settings File](https://github.com/urasandesu/Prig/wiki/Cheat-Sheet#-stub-settings-file).
