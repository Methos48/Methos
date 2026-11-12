#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <stdio.h>
typedef void* (__cdecl *PFN_NpkCreateReader)(LPCWSTR,DWORD,void*);

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
        printf("HOST_NPK_CREATE_READER=%p ORIGINAL=%p\\n", npkCreateReader, originalCreateReader);
        if (originalCreateReader) {
            void* archive = npkCreateReader(L"SysString-e.dat", 0, NULL);
            printf("HOST_NPK_ARCHIVE=%p\\n", archive);
            if (archive) {
                void** vtable = *(void***)archive;
                printf("HOST_NPK_ARCHIVE_VTABLE=%p\\n", vtable);
                for (int slot = 0; slot < 8; ++slot)
                    printf("HOST_NPK_ARCHIVE_VT[%d]=%p\\n", slot, vtable[slot]);
            }
        } else {
            printf("HOST_NPK_CREATE_READER_UNAVAILABLE\\n");
        }
    }
    Sleep(1000);
    return total ? 0 : 12;
}
