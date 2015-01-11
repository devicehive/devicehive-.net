"../../tools/nuget.exe" restore

"c:\Program Files (x86)\MSBuild\12.0\Bin\MSBuild.exe" DeviceHive.Server.sln /p:Configuration=Release /clp:Verbosity=Quiet /p:NoWarn=1591 /p:DeployOnBuild=true /p:_PackageTempDir=..\..\..\bin\Server\Web

rmdir ..\..\bin\Server\DBMigrator /s /q
xcopy DeviceHive.DBMigrator\bin\Release ..\..\bin\Server\DBMigrator /e /y /i

rmdir ..\..\bin\Server\WebSockets.Host /s /q
xcopy DeviceHive.WebSockets.Host\bin\Release ..\..\bin\Server\WebSockets.Host /e /y /i

rmdir ..\..\bin\Server\WebSockets.API /s /q
xcopy DeviceHive.WebSockets.API\bin\Release ..\..\bin\Server\WebSockets.API /e /y /i