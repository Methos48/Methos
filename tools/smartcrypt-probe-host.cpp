#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <stdio.h>

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
    Sleep(1000);
    return total ? 0 : 12;
}
