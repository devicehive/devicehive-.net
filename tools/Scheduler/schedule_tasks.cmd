@echo off
IF "%1"=="" GOTO USAGE

schtasks /create /tn DeviceHive /tr "%CD%\curl.exe %1/cron/RefreshDeviceStatus" /sc minute /mo 5 /ru System
GOTO DONE

:USAGE
ECHO Usage: schedule_tasks.bat [API_URL]
ECHO Example: schedule_tasks.bat http://localhost/api

:DONE
