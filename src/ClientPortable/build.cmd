C:\WINDOWS\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe DeviceHive.Client.sln /p:Configuration=Release /clp:Verbosity=Quiet
rmdir ..\..\Bin\Client /s /q
xcopy DeviceHive.Client\bin\Release ..\..\bin\ClientPortable /e /y /i