@echo off

schtasks /delete /tn "DeviceHive - Refresh Device Status" /f
schtasks /delete /tn "DeviceHive - Cleanup Database" /f
