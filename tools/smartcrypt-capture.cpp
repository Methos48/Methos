#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <stdint.h>
#include <stdio.h>
#include <string.h>
#include <wchar.h>
#include "MinHook.h"
typedef HANDLE (WINAPI *PFN_CreateFileW)(LPCWSTR,DWORD,DWORD,LPSECURITY_ATTRIBUTES,DWORD,DWORD,HANDLE);
typedef BOOL (WINAPI *PFN_ReadFile)(HANDLE,LPVOID,DWORD,LPDWORD,LPOVERLAPPED);
static PFN_CreateFileW realCreateFileW=NULL;
static PFN_ReadFile realReadFile=NULL;
static HANDLE targetHandle=INVALID_HANDLE_VALUE,outputHandle=INVALID_HANDLE_VALUE;
static CRITICAL_SECTION lock; static LONG initialized=0; static LONGLONG maxEnd=0;
static HMODULE engineModule=NULL; static BOOL hookReady=FALSE;
static BOOL IsTarget(LPCWSTR s){if(!s)return FALSE;const wchar_t*p=wcsrchr(s,L'\\');p=p?p+1:s;return _wcsicmp(p,L"SysString-e.dat")==0;}
static HANDLE WINAPI HookCreateFileW(LPCWSTR n,DWORD a,DWORD s,LPSECURITY_ATTRIBUTES sa,DWORD c,DWORD f,HANDLE t){HANDLE h=realCreateFileW(n,a,s,sa,c,f,t);if(h!=INVALID_HANDLE_VALUE){EnterCriticalSection(&lock);if(IsTarget(n))targetHandle=h;LeaveCriticalSection(&lock);}return h;}
static BOOL WINAPI HookReadFile(HANDLE h,LPVOID b,DWORD req,LPDWORD got,LPOVERLAPPED ov){BOOL ok=realReadFile(h,b,req,got,ov);DWORD n=(ok&&got)?*got:0;if(ok&&h==targetHandle&&b&&n){LARGE_INTEGER pos;if(ov){pos.LowPart=ov->Offset;pos.HighPart=ov->OffsetHigh;}else{LARGE_INTEGER zero,cur;zero.QuadPart=0;cur.QuadPart=0;if(SetFilePointerEx(h,zero,&cur,FILE_CURRENT))cur.QuadPart-=n;pos=cur;}EnterCriticalSection(&lock);if(outputHandle==INVALID_HANDLE_VALUE)outputHandle=realCreateFileW(L"smartcrypt-capture.bin",GENERIC_WRITE,FILE_SHARE_READ,NULL,CREATE_ALWAYS,FILE_ATTRIBUTE_NORMAL,NULL);if(outputHandle!=INVALID_HANDLE_VALUE){SetFilePointerEx(outputHandle,pos,NULL,FILE_BEGIN);DWORD wrote=0;WriteFile(outputHandle,b,n,&wrote,NULL);LONGLONG end=pos.QuadPart+wrote;if(end>maxEnd)maxEnd=end;}LeaveCriticalSection(&lock);}return ok;}
extern "C" __declspec(dllexport) BOOL WINAPI SmartCapture_Initialize(void){
 if(InterlockedCompareExchange(&initialized,1,0)==0)InitializeCriticalSection(&lock);
 MH_STATUS init=MH_Initialize(); if(init!=MH_OK&&init!=MH_ERROR_ALREADY_INITIALIZED)return FALSE;
 MH_STATUS c=MH_CreateHookApi(L"kernel32.dll","CreateFileW",(LPVOID)HookCreateFileW,(LPVOID*)&realCreateFileW);
 MH_STATUS r=MH_CreateHookApi(L"kernel32.dll","ReadFile",(LPVOID)HookReadFile,(LPVOID*)&realReadFile);
 if((c!=MH_OK&&c!=MH_ERROR_ALREADY_CREATED)||(r!=MH_OK&&r!=MH_ERROR_ALREADY_CREATED))return FALSE;
 if(MH_EnableHook(MH_ALL_HOOKS)!=MH_OK)return FALSE;
 hookReady=TRUE; return TRUE;
}
extern "C" __declspec(dllexport) void WINAPI SmartCapture_Flush(void){if(!hookReady||!initialized)return;EnterCriticalSection(&lock);if(outputHandle!=INVALID_HANDLE_VALUE){LARGE_INTEGER end;end.QuadPart=maxEnd;SetFilePointerEx(outputHandle,end,NULL,FILE_BEGIN);SetEndOfFile(outputHandle);FlushFileBuffers(outputHandle);CloseHandle(outputHandle);outputHandle=INVALID_HANDLE_VALUE;}LeaveCriticalSection(&lock);}
extern "C" __declspec(dllexport) BOOL WINAPI SmartCapture_Status(void){return hookReady;}
extern "C" __declspec(dllexport) int __cdecl load(void){return 0;}
BOOL WINAPI DllMain(HINSTANCE h,DWORD r,LPVOID p){
 if(r==DLL_PROCESS_ATTACH){
  DisableThreadLibraryCalls(h);
  engineModule=LoadLibraryA("engine.dll");
  if(!SmartCapture_Initialize())return FALSE;
 }
 return TRUE;
}