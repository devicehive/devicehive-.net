"c:\Program Files (x86)\MSBuild\12.0\Bin\MSBuild.exe" DeviceHive.WindowsManager.Universal.sln /p:Configuration=Release /clp:Verbosity=Quiet
rmdir ..\..\Bin\WindowsManager.Universal /s /q
xcopy DeviceHive.WindowsManager.Universal\bin\Release ..\..\bin\WindowsManager.Universal /e /y /i

"c:\Program Files (x86)\MSBuild\12.0\Bin\MSBuild.exe" DeviceHive.WindowsManager.sln /p:Configuration=Release /clp:Verbosity=Quiet
rmdir ..\..\Bin\WindowsManager /s /q
xcopy DeviceHive.WindowsManager\bin\Release ..\..\bin\WindowsManager /e /y /i