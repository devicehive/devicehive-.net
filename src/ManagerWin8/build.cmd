C:\WINDOWS\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe DeviceHive.ManagerWin8.sln /p:Configuration=Release /clp:Verbosity=Quiet
rmdir ..\..\Bin\ManagerWin8 /s /q
xcopy DeviceHive.ManagerWin8\bin\Release ..\..\bin\ManagerWin8 /e /y /i