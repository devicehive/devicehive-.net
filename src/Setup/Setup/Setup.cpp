#include "stdafx.h"
#include "Setup.h"

#define ERRMSG_ERROR        _T("DeviceHive")
#define ERRMSG_GETTEMPNAME  _T("Failed to get temporary file name.")
#define ERRMSG_WRITETEMP    _T("Failed to write to temporary file.")
#define ERRMSG_RUNPARAMS    _T("Failed to define installation folder.")
#define MSG_NEWLINE         _T("\r\n")
#define COMMAND_LINE        _T("msiexec /i %s %s")

void ShowMessage(LPCTSTR lpText, LPCTSTR lpCaption, DWORD error_code);

int APIENTRY _tWinMain(_In_ HINSTANCE hInstance,
    _In_opt_ HINSTANCE hPrevInstance,
    _In_ LPTSTR    lpCmdLine,
    _In_ int       nCmdShow)
{
    UNREFERENCED_PARAMETER(hPrevInstance);
    UNREFERENCED_PARAMETER(lpCmdLine);

    HRSRC msi = FindResource(hInstance, MAKEINTRESOURCE(IDR_MSI), _T("MSI"));
    HGLOBAL global = LoadResource(hInstance, msi);
    LPVOID msiAddress = LockResource(global);
    DWORD msiSize = SizeofResource(hInstance, msi);

    TCHAR lpPathBuffer[MAX_PATH] = { 0 };
    GetTempPath(MAX_PATH, lpPathBuffer);

    TCHAR szDeviceHiveSetupMsi[MAX_PATH] = { 0 };
    if (GetTempFileName(lpPathBuffer, _T("NEW"), 0, szDeviceHiveSetupMsi) == 0)
    {
        ShowMessage(ERRMSG_GETTEMPNAME, ERRMSG_ERROR, GetLastError());
        return 1;
    }

    HANDLE hTempFile = CreateFile((LPTSTR)szDeviceHiveSetupMsi, GENERIC_READ | GENERIC_WRITE, 0, NULL,
                                  CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);

    DWORD bytesWritten;
    TCHAR szTempName[MAX_PATH] = { 0 };

    WriteFile(hTempFile, msiAddress, msiSize, &bytesWritten, NULL);
    if (msiSize != bytesWritten)
    {
        CloseHandle(hTempFile);
        DeleteFile(szTempName);
        ShowMessage(ERRMSG_WRITETEMP, ERRMSG_ERROR, GetLastError());
        return 1;
    }

    CloseHandle(hTempFile);

    STARTUPINFO startInfo;
    memset(&startInfo, 0, sizeof(startInfo));
    startInfo.cb = sizeof(STARTUPINFO);
    PROCESS_INFORMATION procInfo;

    TCHAR cmdLine[MAX_PATH] = { 0 };
    swprintf_s(cmdLine, MAX_PATH, COMMAND_LINE, szDeviceHiveSetupMsi, lpCmdLine);

    if (CreateProcess(NULL, cmdLine, NULL, NULL, FALSE, 0, NULL, NULL, &startInfo, &procInfo) == FALSE)
    {
        DeleteFile(szTempName);
        ShowMessage(ERRMSG_RUNPARAMS, ERRMSG_ERROR, GetLastError());
        return 1;
    }

    return 0;
}

void ShowMessage(LPCTSTR lpText, LPCTSTR lpCaption, DWORD error_code)
{
    TCHAR message[MAX_PATH];
    _stprintf_s(message, MAX_PATH, _T("Error code: %d"), error_code);

    SIZE_T message_size = _tcslen(lpText) + _tcslen(MSG_NEWLINE) + _tcslen(message) + 1;
    TCHAR *lpMessage = new TCHAR[message_size];
    _tcscpy_s(lpMessage, message_size, lpText);
    _tcscat_s(lpMessage, message_size, MSG_NEWLINE);
    _tcscat_s(lpMessage, message_size, message);

    MessageBox(NULL, lpMessage, lpCaption, MB_OK | MB_ICONERROR);

    delete[] lpMessage;
}