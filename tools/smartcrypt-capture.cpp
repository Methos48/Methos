#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <stdint.h>
#include <stdio.h>
#include <string.h>
#include <wchar.h>
#include <stdarg.h>
#include <intrin.h>
#include "MinHook.h"
typedef HANDLE (WINAPI *PFN_CreateFileW)(LPCWSTR,DWORD,DWORD,LPSECURITY_ATTRIBUTES,DWORD,DWORD,HANDLE);
typedef BOOL (WINAPI *PFN_ReadFile)(HANDLE,LPVOID,DWORD,LPDWORD,LPOVERLAPPED);
typedef FARPROC (WINAPI *PFN_GetProcAddress)(HMODULE,LPCSTR);
typedef HMODULE (WINAPI *PFN_LoadLibraryA)(LPCSTR);
typedef BOOL (WINAPI *PFN_VirtualProtect)(LPVOID,SIZE_T,DWORD,PDWORD);
typedef HANDLE (WINAPI *PFN_CreateThread)(LPSECURITY_ATTRIBUTES,SIZE_T,LPTHREAD_START_ROUTINE,LPVOID,DWORD,LPDWORD);
typedef DWORD (WINAPI *PFN_GetModuleFileNameW)(HMODULE,LPWSTR,DWORD);
typedef HMODULE (WINAPI *PFN_GetModuleHandleA)(LPCSTR);
typedef DWORD (WINAPI *PFN_GetModuleFileNameA)(HMODULE,LPSTR,DWORD);
typedef HLOCAL (WINAPI *PFN_LocalAlloc)(UINT,SIZE_T);
typedef HLOCAL (WINAPI *PFN_LocalFree)(HLOCAL);
typedef BOOL (WINAPI *PFN_InitCriticalSection)(LPCRITICAL_SECTION,DWORD);
static PFN_CreateFileW realCreateFileW=NULL; static PFN_ReadFile realReadFile=NULL;
static PFN_GetProcAddress realGetProcAddress=NULL; static PFN_LoadLibraryA realLoadLibraryA=NULL;
static PFN_VirtualProtect realVirtualProtect=NULL; static PFN_CreateThread realCreateThread=NULL;
static PFN_GetModuleFileNameW realGetModuleFileNameW=NULL;
static PFN_GetModuleHandleA realGetModuleHandleA=NULL;
static PFN_GetModuleFileNameA realGetModuleFileNameA=NULL;
static PFN_LocalAlloc realLocalAlloc=NULL; static PFN_LocalFree realLocalFree=NULL;
static PFN_InitCriticalSection realInitCriticalSection=NULL;
static HANDLE targetHandle=INVALID_HANDLE_VALUE,outputHandle=INVALID_HANDLE_VALUE;
static CRITICAL_SECTION lock; static LONG initialized=0; static LONGLONG maxEnd=0;
static HMODULE engineModule=NULL; static BOOL hookReady=FALSE;
static char traceBuffer[1<<20]; static LONG traceLength=0;
static void Trace(const char* fmt,...){
 if(!initialized)return; EnterCriticalSection(&lock);
 LONG at=traceLength; if(at<(LONG)sizeof(traceBuffer)-2){va_list ap;va_start(ap,fmt);int n=_vsnprintf_s(traceBuffer+at,sizeof(traceBuffer)-at,_TRUNCATE,fmt,ap);va_end(ap);if(n>0)InterlockedExchange(&traceLength,at+n);}
 LeaveCriticalSection(&lock);
}
static BOOL IsTarget(LPCWSTR s){if(!s)return FALSE;const wchar_t*p=wcsrchr(s,L'\\');p=p?p+1:s;return _wcsicmp(p,L"SysString-e.dat")==0;}
static HANDLE WINAPI HookCreateFileW(LPCWSTR n,DWORD a,DWORD s,LPSECURITY_ATTRIBUTES sa,DWORD c,DWORD f,HANDLE t){HANDLE h=realCreateFileW(n,a,s,sa,c,f,t);if(h!=INVALID_HANDLE_VALUE){EnterCriticalSection(&lock);if(IsTarget(n))targetHandle=h;LeaveCriticalSection(&lock);}return h;}
static BOOL WINAPI HookReadFile(HANDLE h,LPVOID b,DWORD req,LPDWORD got,LPOVERLAPPED ov){BOOL ok=realReadFile(h,b,req,got,ov);DWORD n=(ok&&got)?*got:0;if(ok&&h==targetHandle&&b&&n){LARGE_INTEGER pos;if(ov){pos.LowPart=ov->Offset;pos.HighPart=ov->OffsetHigh;}else{LARGE_INTEGER zero,cur;zero.QuadPart=0;cur.QuadPart=0;if(SetFilePointerEx(h,zero,&cur,FILE_CURRENT))cur.QuadPart-=n;pos=cur;}EnterCriticalSection(&lock);if(outputHandle==INVALID_HANDLE_VALUE)outputHandle=realCreateFileW(L"smartcrypt-capture.bin",GENERIC_WRITE,FILE_SHARE_READ,NULL,CREATE_ALWAYS,FILE_ATTRIBUTE_NORMAL,NULL);if(outputHandle!=INVALID_HANDLE_VALUE){SetFilePointerEx(outputHandle,pos,NULL,FILE_BEGIN);DWORD wrote=0;WriteFile(outputHandle,b,n,&wrote,NULL);LONGLONG end=pos.QuadPart+wrote;if(end>maxEnd)maxEnd=end;}LeaveCriticalSection(&lock);}return ok;}
static FARPROC WINAPI HookGetProcAddress(HMODULE m,LPCSTR n){FARPROC p=realGetProcAddress(m,n);if(n&&(uintptr_t)n>0xFFFF)Trace("GetProcAddress caller=%p module=%p name=%s result=%p\n",_ReturnAddress(),m,n,p);return p;}
static HMODULE WINAPI HookLoadLibraryA(LPCSTR n){HMODULE h=realLoadLibraryA(n);Trace("LoadLibraryA caller=%p name=%s result=%p\n",_ReturnAddress(),n?n:"(null)",h);return h;}
static BOOL WINAPI HookVirtualProtect(LPVOID a,SIZE_T s,DWORD p,PDWORD o){BOOL r=realVirtualProtect(a,s,p,o);Trace("VirtualProtect caller=%p address=%p size=%Ix protect=%08lX result=%d\n",_ReturnAddress(),a,s,p,r);return r;}
static HANDLE WINAPI HookCreateThread(LPSECURITY_ATTRIBUTES a,SIZE_T s,LPTHREAD_START_ROUTINE f,LPVOID p,DWORD flags,LPDWORD id){HANDLE h=realCreateThread(a,s,f,p,flags,id);Trace("CreateThread caller=%p start=%p param=%p result=%p\n",_ReturnAddress(),f,p,h);return h;}
static DWORD WINAPI HookGetModuleFileNameW(HMODULE m,LPWSTR b,DWORD n){DWORD r=realGetModuleFileNameW(m,b,n);if(r){char tmp[MAX_PATH*3]={};WideCharToMultiByte(CP_UTF8,0,b,-1,tmp,sizeof(tmp),NULL,NULL);Trace("GetModuleFileNameW caller=%p module=%p path=%s\n",_ReturnAddress(),m,tmp);}return r;}
static HMODULE WINAPI HookGetModuleHandleA(LPCSTR n){HMODULE h=realGetModuleHandleA(n);Trace("GetModuleHandleA caller=%p name=%s result=%p\\n",_ReturnAddress(),n?n:"(null)",h);return h;}
static DWORD WINAPI HookGetModuleFileNameA(HMODULE m,LPSTR b,DWORD n){DWORD r=realGetModuleFileNameA(m,b,n);if(r)Trace("GetModuleFileNameA caller=%p module=%p path=%s\\n",_ReturnAddress(),m,b);return r;}
static HLOCAL WINAPI HookLocalAlloc(UINT f,SIZE_T n){HLOCAL h=realLocalAlloc(f,n);Trace("LocalAlloc caller=%p flags=%X size=%Ix result=%p\\n",_ReturnAddress(),f,n,h);return h;}
static HLOCAL WINAPI HookLocalFree(HLOCAL h){Trace("LocalFree caller=%p handle=%p\\n",_ReturnAddress(),h);return realLocalFree(h);}
static BOOL WINAPI HookInitCriticalSection(LPCRITICAL_SECTION s,DWORD spin){BOOL r=realInitCriticalSection(s,spin);Trace("InitCriticalSection caller=%p spin=%lu result=%d\\n",_ReturnAddress(),spin,r);return r;}
extern "C" __declspec(dllexport) BOOL WINAPI SmartCapture_Initialize(void){
 if(InterlockedCompareExchange(&initialized,1,0)==0)InitializeCriticalSection(&lock);
 MH_STATUS init=MH_Initialize(); if(init!=MH_OK&&init!=MH_ERROR_ALREADY_INITIALIZED)return FALSE;
 MH_CreateHookApi(L"kernel32.dll","CreateFileW",(LPVOID)HookCreateFileW,(LPVOID*)&realCreateFileW);
 MH_CreateHookApi(L"kernel32.dll","ReadFile",(LPVOID)HookReadFile,(LPVOID*)&realReadFile);
 MH_CreateHookApi(L"kernel32.dll","GetProcAddress",(LPVOID)HookGetProcAddress,(LPVOID*)&realGetProcAddress);
 MH_CreateHookApi(L"kernel32.dll","LoadLibraryA",(LPVOID)HookLoadLibraryA,(LPVOID*)&realLoadLibraryA);
 MH_CreateHookApi(L"kernel32.dll","VirtualProtect",(LPVOID)HookVirtualProtect,(LPVOID*)&realVirtualProtect);
 MH_CreateHookApi(L"kernel32.dll","CreateThread",(LPVOID)HookCreateThread,(LPVOID*)&realCreateThread);
 MH_CreateHookApi(L"kernel32.dll","GetModuleFileNameW",(LPVOID)HookGetModuleFileNameW,(LPVOID*)&realGetModuleFileNameW);
 MH_CreateHookApi(L"kernel32.dll","GetModuleHandleA",(LPVOID)HookGetModuleHandleA,(LPVOID*)&realGetModuleHandleA);
 MH_CreateHookApi(L"kernel32.dll","GetModuleFileNameA",(LPVOID)HookGetModuleFileNameA,(LPVOID*)&realGetModuleFileNameA);
 MH_CreateHookApi(L"kernel32.dll","LocalAlloc",(LPVOID)HookLocalAlloc,(LPVOID*)&realLocalAlloc);
 MH_CreateHookApi(L"kernel32.dll","LocalFree",(LPVOID)HookLocalFree,(LPVOID*)&realLocalFree);
 MH_CreateHookApi(L"kernel32.dll","InitializeCriticalSectionAndSpinCount",(LPVOID)HookInitCriticalSection,(LPVOID*)&realInitCriticalSection);
 if(MH_EnableHook(MH_ALL_HOOKS)!=MH_OK)return FALSE;
 hookReady=TRUE; return TRUE;
}
extern "C" __declspec(dllexport) void WINAPI SmartCapture_Flush(void){if(!hookReady||!initialized)return;EnterCriticalSection(&lock);if(outputHandle!=INVALID_HANDLE_VALUE){LARGE_INTEGER end;end.QuadPart=maxEnd;SetFilePointerEx(outputHandle,end,NULL,FILE_BEGIN);SetEndOfFile(outputHandle);FlushFileBuffers(outputHandle);CloseHandle(outputHandle);outputHandle=INVALID_HANDLE_VALUE;}LeaveCriticalSection(&lock);}
extern "C" __declspec(dllexport) BOOL WINAPI SmartCapture_Status(void){return hookReady;}
extern "C" __declspec(dllexport) void WINAPI SmartCapture_DumpTrace(void){if(!initialized)return;HANDLE h=realCreateFileW(L"npkscrypt-api-trace.txt",GENERIC_WRITE,FILE_SHARE_READ,NULL,CREATE_ALWAYS,FILE_ATTRIBUTE_NORMAL,NULL);if(h!=INVALID_HANDLE_VALUE){DWORD n=(DWORD)traceLength,w=0;WriteFile(h,traceBuffer,n,&w,NULL);CloseHandle(h);}}
extern "C" __declspec(dllexport) int __stdcall load(void){Trace("SmartCapture load callback called\\n");return 0;}
BOOL WINAPI DllMain(HINSTANCE h,DWORD r,LPVOID p){if(r==DLL_PROCESS_ATTACH){DisableThreadLibraryCalls(h);engineModule=LoadLibraryA("engine.dll");}return TRUE;}
