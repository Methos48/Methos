#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <stdio.h>
#include <stdarg.h>
#include <string.h>
typedef void* (__cdecl *PFN_NpkCreateReader)(LPCWSTR,DWORD,void*);
static void ProbeLog(const char* format, ...) {
    char line[2048] = {};
    va_list args;
    va_start(args, format);
    vsnprintf_s(line, sizeof(line), _TRUNCATE, format, args);
    va_end(args);
    HANDLE h = CreateFileA("smartcrypt-engine-probe.log", FILE_APPEND_DATA,
        FILE_SHARE_READ | FILE_SHARE_WRITE, NULL, OPEN_ALWAYS,
        FILE_ATTRIBUTE_NORMAL, NULL);
    if (h != INVALID_HANDLE_VALUE) {
        DWORD written = 0;
        WriteFile(h, line, (DWORD)strlen(line), &written, NULL);
        CloseHandle(h);
    }
}

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
        void* originalCreateReader = *(void**)((BYTE*)npk + 0x51434);
        PFN_NpkCreateReader npkCreateReader = (PFN_NpkCreateReader)((BYTE*)npk + 0x119A7C);
        ProbeLog("HOST_NPK_CREATE_READER=%p ORIGINAL=%p\\r\\n", npkCreateReader, originalCreateReader);
        if (originalCreateReader) {
            __try {
                void* archive = npkCreateReader(L"SysString-e.dat", 0, NULL);
                ProbeLog("HOST_NPK_ARCHIVE=%p\\r\\n", archive);
                if (archive) {
                    void** vtable = *(void***)archive;
                    ProbeLog("HOST_NPK_ARCHIVE_VTABLE=%p\\r\\n", vtable);
                    for (int slot = 0; slot < 8; ++slot)
                        ProbeLog("HOST_NPK_ARCHIVE_VT[%d]=%p\\r\\n", slot, vtable[slot]);
                }
            } __except(EXCEPTION_EXECUTE_HANDLER) {
                ProbeLog("HOST_NPK_CREATE_READER_EXCEPTION=0x%08lX\\r\\n", GetExceptionCode());
            }
        } else {
            ProbeLog("HOST_NPK_CREATE_READER_UNAVAILABLE\\r\\n");
        }
    }
    Sleep(1000);
    return total ? 0 : 12;
}
