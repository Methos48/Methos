#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <stdio.h>
typedef HANDLE (WINAPI *PFN_NpkCreateFileW)(LPCWSTR,DWORD,DWORD,LPSECURITY_ATTRIBUTES,DWORD,DWORD,HANDLE);
typedef BOOL (WINAPI *PFN_NpkReadFile)(HANDLE,LPVOID,DWORD,LPDWORD,LPOVERLAPPED);

int main() {
    SetErrorMode(SEM_FAILCRITICALERRORS | SEM_NOGPFAULTERRORBOX);
    SetDllDirectoryW(L".");
    HMODULE ogg = LoadLibraryW(L"ogg.dll");
    if (!ogg) {
        printf("HOST_OGG_LOAD_FAILED=%lu\n", GetLastError());
        return 10;
    }
    printf("HOST_OGG_LOADED=%p\n", ogg);
    Sleep(500);

    HANDLE file = CreateFileW(
        L"SysString-e.dat",
        GENERIC_READ,
        FILE_SHARE_READ | FILE_SHARE_WRITE,
        NULL,
        OPEN_EXISTING,
        FILE_ATTRIBUTE_NORMAL,
        NULL
    );
    if (file == INVALID_HANDLE_VALUE) {
        printf("HOST_TARGET_OPEN_FAILED=%lu\n", GetLastError());
        return 11;
    }

    LARGE_INTEGER size = {};
    GetFileSizeEx(file, &size);
    printf("HOST_TARGET_SIZE=%lld\n", size.QuadPart);

    BYTE buffer[65536];
    unsigned long long total = 0;
    for (;;) {
        DWORD received = 0;
        if (!ReadFile(file, buffer, sizeof(buffer), &received, NULL)) {
            printf("HOST_READ_FAILED=%lu\n", GetLastError());
            break;
        }
        if (received == 0) break;
        total += received;
    }
    printf("HOST_READ_BYTES=%llu\n", total);
    CloseHandle(file);

    HMODULE npk = GetModuleHandleA("npkscrypt.dll");
    printf("HOST_NPK_MODULE=%p\n", npk);
    if (npk) {
        PFN_NpkCreateFileW npkCreate = (PFN_NpkCreateFileW)((BYTE*)npk + 0x119A7C);
        PFN_NpkReadFile npkRead = (PFN_NpkReadFile)((BYTE*)npk + 0x119C6C);
        HANDLE file2 = npkCreate(
            L"SysString-e.dat",
            GENERIC_READ,
            FILE_SHARE_READ | FILE_SHARE_WRITE,
            NULL,
            OPEN_EXISTING,
            FILE_ATTRIBUTE_NORMAL,
            NULL
        );
        if (file2 != INVALID_HANDLE_VALUE) {
            LARGE_INTEGER zero = {};
            SetFilePointerEx(file2, zero, NULL, FILE_BEGIN);
            unsigned long long npkTotal = 0;
            DWORD firstRead = 0;
            if (npkRead(file2, buffer, 0x400, &firstRead, NULL)) {
                npkTotal += firstRead;
                printf("HOST_NPK_FIRST_READ_REQUEST=1024 BYTES=%lu\n", firstRead);
            } else {
                printf("HOST_NPK_FIRST_READ_FAILED=%lu\n", GetLastError());
            }
            for (;;) {
                DWORD received = 0;
                if (!npkRead(file2, buffer, sizeof(buffer), &received, NULL)) {
                    printf("HOST_NPK_READ_FAILED=%lu\n", GetLastError());
                    break;
                }
                if (received == 0) break;
                npkTotal += received;
            }
            printf("HOST_NPK_READ_BYTES=%llu\n", npkTotal);
            CloseHandle(file2);
        } else {
            printf("HOST_NPK_TARGET_OPEN_FAILED=%lu\n", GetLastError());
        }
    }
    Sleep(1000);
    return total ? 0 : 12;
}
