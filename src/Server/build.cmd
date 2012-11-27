C:\WINDOWS\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe DeviceHive.Server.sln /p:Configuration=Release /clp:Verbosity=Quiet
C:\WINDOWS\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe DeviceHive.API\DeviceHive.API.csproj /t:ResolveReferences;_WPPCopyWebApplication /p:Configuration=Release /p:BuildingProject=true;CleanWebProjectOutputDir=True;WebProjectOutputDir=..\..\..\Bin\Server\Web /clp:Verbosity=Quiet
rmdir ..\..\Bin\Server\DBMigrator /s /q
xcopy DeviceHive.DBMigrator\bin\Release ..\..\bin\Server\DBMigrator /e /y /i