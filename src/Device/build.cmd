C:\WINDOWS\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe DeviceHive.Device.sln /p:Configuration=Release /clp:Verbosity=Quiet
rmdir ..\..\Bin\Device /s /q
xcopy DeviceHive.Device\bin\Release ..\..\bin\Device /e /y /i