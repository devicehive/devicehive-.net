"../../tools/nuget.exe" restore

C:\WINDOWS\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe DeviceHive.Client.sln /p:Configuration=Release /clp:Verbosity=Quiet
rmdir ..\..\Bin\Client /s /q
xcopy DeviceHive.Client\bin\net45\Release ..\..\bin\Client\net45 /e /y /i
xcopy DeviceHive.Client\bin\portable\Release ..\..\bin\Client\portable /e /y /i