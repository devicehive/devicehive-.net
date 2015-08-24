"../../tools/nuget.exe" restore

"c:\Program Files (x86)\MSBuild\12.0\Bin\MSBuild.exe" DeviceHive.Client.sln /p:Configuration=Release /clp:Verbosity=Quiet
rmdir ..\..\Bin\Client /s /q
xcopy DeviceHive.Client\bin\net45\Release ..\..\bin\Client\net45 /e /y /i
xcopy DeviceHive.Client\bin\portable\Release ..\..\bin\Client\portable /e /y /i
xcopy DeviceHive.Client.Universal\bin\ARM\Release ..\..\bin\Client\ARM /e /y /i