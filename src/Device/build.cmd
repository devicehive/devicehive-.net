"../../tools/nuget.exe" restore

"c:\Program Files (x86)\MSBuild\12.0\Bin\MSBuild.exe" DeviceHive.Device.sln /p:Configuration=Release /clp:Verbosity=Quiet
rmdir ..\..\Bin\Device /s /q
xcopy DeviceHive.Device\bin\Release ..\..\bin\Device /e /y /i
xcopy DeviceHive.Binary\bin\Release ..\..\bin\Device /e /y /i