#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <stdint.h>
#include <stdio.h>
#include <string.h>
#include <wchar.h>
#include <stdarg.h>
#include <intrin.h>
#include <winternl.h>
#include "MinHook.h"
typedef HANDLE (WINAPI *PFN_CreateFileW)(LPCWSTR,DWORD,DWORD,LPSECURITY_ATTRIBUTES,DWORD,DWORD,HANDLE);
typedef HANDLE (WINAPI *PFN_CreateFileA)(LPCSTR,DWORD,DWORD,LPSECURITY_ATTRIBUTES,DWORD,DWORD,HANDLE);
typedef BOOL (WINAPI *PFN_ReadFile)(HANDLE,LPVOID,DWORD,LPDWORD,LPOVERLAPPED);
typedef BOOL (WINAPI *PFN_NpkReadFile)(HANDLE,LPVOID,DWORD,LPDWORD,LPOVERLAPPED);
typedef void* (__thiscall *PFN_NpkGetState)(HANDLE);
typedef NTSTATUS (NTAPI *PFN_NtReadFile)(HANDLE,HANDLE,PIO_APC_ROUTINE,PVOID,PIO_STATUS_BLOCK,PVOID,ULONG,PLARGE_INTEGER,PULONG);
typedef NTSTATUS (NTAPI *PFN_NtCreateFile)(PHANDLE,ACCESS_MASK,POBJECT_ATTRIBUTES,PIO_STATUS_BLOCK,PLARGE_INTEGER,ULONG,ULONG,ULONG,ULONG,PVOID,ULONG);
typedef NTSTATUS (NTAPI *PFN_NtOpenFile)(PHANDLE,ACCESS_MASK,POBJECT_ATTRIBUTES,PIO_STATUS_BLOCK,ULONG,ULONG);
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
static PFN_CreateFileW realCreateFileW=NULL; static PFN_CreateFileA realCreateFileA=NULL; static PFN_ReadFile realReadFile=NULL; static PFN_NpkReadFile realNpkReadFile=NULL; static PFN_NpkGetState realNpkGetState=NULL; static PFN_NtReadFile realNtReadFile=NULL; static PFN_NtCreateFile realNtCreateFile=NULL; static PFN_NtOpenFile realNtOpenFile=NULL;
static PFN_GetProcAddress realGetProcAddress=NULL; static PFN_LoadLibraryA realLoadLibraryA=NULL;
static PFN_VirtualProtect realVirtualProtect=NULL; static PFN_CreateThread realCreateThread=NULL;
static PFN_GetModuleFileNameW realGetModuleFileNameW=NULL;
static PFN_GetModuleHandleA realGetModuleHandleA=NULL;
static PFN_GetModuleFileNameA realGetModuleFileNameA=NULL;
static PFN_LocalAlloc realLocalAlloc=NULL; static PFN_LocalFree realLocalFree=NULL;
static PFN_InitCriticalSection realInitCriticalSection=NULL;
enum TargetKind { TARGET_NONE=0, TARGET_SYSSTRING=1, TARGET_INTERFACE_U=2, TARGET_INTERFACE_XDAT=3 };
static HANDLE targetHandles[4]={INVALID_HANDLE_VALUE,INVALID_HANDLE_VALUE,INVALID_HANDLE_VALUE,INVALID_HANDLE_VALUE};
static LONG keyDumped[4]={0,0,0,0};
static HANDLE outputHandles[4]={INVALID_HANDLE_VALUE,INVALID_HANDLE_VALUE,INVALID_HANDLE_VALUE,INVALID_HANDLE_VALUE};
static LONGLONG maxEnds[4]={0,0,0,0};
static volatile LONG openLogCount=0;
static CRITICAL_SECTION lock; static LONG initialized=0;
static HMODULE engineModule=NULL; static HMODULE npkModule=NULL; static BOOL hookReady=FALSE; static __declspec(thread) LONG inNpkRead=0;
static char traceBuffer[1<<20]; static LONG traceLength=0;
static HANDLE debugHandle=INVALID_HANDLE_VALUE;
static void DebugLog(const char* fmt,...){
 char line[768]={}; va_list ap; va_start(ap,fmt); _vsnprintf_s(line,sizeof(line),_TRUNCATE,fmt,ap); va_end(ap);
 if(debugHandle==INVALID_HANDLE_VALUE)debugHandle=CreateFileA("smartcapture-debug.log",FILE_APPEND_DATA,FILE_SHARE_READ|FILE_SHARE_WRITE,NULL,OPEN_ALWAYS,FILE_ATTRIBUTE_NORMAL,NULL);
 if(debugHandle!=INVALID_HANDLE_VALUE){DWORD n=0;WriteFile(debugHandle,line,(DWORD)strlen(line),&n,NULL);}
}
static void Trace(const char* fmt,...){
 if(!initialized)return; EnterCriticalSection(&lock);
 LONG at=traceLength; if(at<(LONG)sizeof(traceBuffer)-2){va_list ap;va_start(ap,fmt);int n=_vsnprintf_s(traceBuffer+at,sizeof(traceBuffer)-at,_TRUNCATE,fmt,ap);va_end(ap);if(n>0)InterlockedExchange(&traceLength,at+n);}
 LeaveCriticalSection(&lock);
}
static TargetKind GetTargetKindW(LPCWSTR s){
 if(!s)return TARGET_NONE;
 const wchar_t*p=wcsrchr(s,L'\\');p=p?p+1:s;
 if(_wcsicmp(p,L"SysString-e.dat")==0)return TARGET_SYSSTRING;
 if(_wcsicmp(p,L"Interface.u")==0)return TARGET_INTERFACE_U;
 if(_wcsicmp(p,L"Interface.xdat")==0)return TARGET_INTERFACE_XDAT;
 return TARGET_NONE;
}
static void SetTargetHandle(LPCWSTR path,HANDLE h,const char*api){
 TargetKind k=GetTargetKindW(path);
 if(k==TARGET_NONE||h==NULL||h==INVALID_HANDLE_VALUE)return;
 EnterCriticalSection(&lock);
 targetHandles[k]=h;
 InterlockedExchange(&keyDumped[k],0);
 char tmp[1024]={};WideCharToMultiByte(CP_UTF8,0,path,-1,tmp,sizeof(tmp),NULL,NULL);
 DebugLog("TARGET_OPEN kind=%d api=%s path=%s handle=%p\\r\\n",(int)k,api,tmp,h);
 LeaveCriticalSection(&lock);
}
static LONG npkStateLogged=0;
static void LogNpkStateOnce(void){
 if(InterlockedCompareExchange(&npkStateLogged,1,0)!=0)return;
 HMODULE n=GetModuleHandleA("npkscrypt.dll");
 HMODULE d=GetModuleHandleA("dsetup.dll");
 FARPROC ord=d?GetProcAddress(d,(LPCSTR)0x67):NULL;
 if(n){BYTE*b=(BYTE*)n;DebugLog("NPK_RUNTIME_STATE module=%p globals_51430=%08lX 51438=%08lX 51468=%08lX 5146C=%08lX dsetup=%p ordinal67=%p\\r\\n",n,*(DWORD*)(b+0x51430),*(DWORD*)(b+0x51438),*(DWORD*)(b+0x51468),*(DWORD*)(b+0x5146c),d,ord);}
 else DebugLog("NPK_RUNTIME_STATE module=NULL dsetup=%p ordinal67=%p\\r\\n",d,ord);
}
static void LogOpen(const char* api,LPCWSTR path,HANDLE h){LONG k=InterlockedIncrement(&openLogCount);if(k<=2000&&path){char tmp[2048]={};WideCharToMultiByte(CP_UTF8,0,path,-1,tmp,sizeof(tmp),NULL,NULL);DebugLog("OPEN_%s #%ld path=%s handle=%p\\r\\n",api,k,tmp,h);}}
static HANDLE WINAPI HookCreateFileW(LPCWSTR n,DWORD a,DWORD s,LPSECURITY_ATTRIBUTES sa,DWORD c,DWORD f,HANDLE t){HANDLE h=realCreateFileW(n,a,s,sa,c,f,t);LogOpen("CreateFileW",n,h);SetTargetHandle(n,h,"CreateFileW");return h;}
static HANDLE WINAPI HookCreateFileA(LPCSTR n,DWORD a,DWORD s,LPSECURITY_ATTRIBUTES sa,DWORD c,DWORD f,HANDLE t){HANDLE h=realCreateFileA(n,a,s,sa,c,f,t);if(n){WCHAR w[1024]={};MultiByteToWideChar(CP_ACP,0,n,-1,w,1024);LogOpen("CreateFileA",w,h);SetTargetHandle(w,h,"CreateFileA");}return h;}
static void CopyObjectPath(POBJECT_ATTRIBUTES oa,LPWSTR out,DWORD cap){if(!out||cap==0){return;}out[0]=0;if(!oa||!oa->ObjectName||!oa->ObjectName->Buffer)return;DWORD chars=oa->ObjectName->Length/sizeof(WCHAR);if(chars>=cap)chars=cap-1;memcpy(out,oa->ObjectName->Buffer,chars*sizeof(WCHAR));out[chars]=0;}
static void LogNativeOpen(const char* api,NTSTATUS st,POBJECT_ATTRIBUTES oa,HANDLE h){WCHAR wide[1024]={};CopyObjectPath(oa,wide,1024);LogOpen(api,wide,h);}
static NTSTATUS NTAPI HookNtCreateFile(PHANDLE out,ACCESS_MASK access,POBJECT_ATTRIBUTES oa,PIO_STATUS_BLOCK iosb,PLARGE_INTEGER alloc,ULONG attrs,ULONG share,ULONG disp,ULONG opts,PVOID ea,ULONG eaLen){NTSTATUS st=realNtCreateFile(out,access,oa,iosb,alloc,attrs,share,disp,opts,ea,eaLen);WCHAR path[1024]={};CopyObjectPath(oa,path,1024);HANDLE h=(st>=0&&out)?*out:NULL;LogNativeOpen("NTCREATEFILE",st,oa,h);if(st>=0&&out)SetTargetHandle(path,*out,"NtCreateFile");return st;}
static NTSTATUS NTAPI HookNtOpenFile(PHANDLE out,ACCESS_MASK access,POBJECT_ATTRIBUTES oa,PIO_STATUS_BLOCK iosb,ULONG share,ULONG opts){NTSTATUS st=realNtOpenFile(out,access,oa,iosb,share,opts);WCHAR path[1024]={};CopyObjectPath(oa,path,1024);HANDLE h=(st>=0&&out)?*out:NULL;LogNativeOpen("NTOPENFILE",st,oa,h);if(st>=0&&out)SetTargetHandle(path,*out,"NtOpenFile");return st;}
static void CaptureReadBuffer(HANDLE h,LPVOID b,DWORD n,const LARGE_INTEGER* explicitPos,const char* api){
 if(!b||!n)return;
 int kind=TARGET_NONE;
 for(int i=1;i<=3;i++)if(h==targetHandles[i]){kind=i;break;}
 if(kind==TARGET_NONE)return;
 LARGE_INTEGER pos={};
 if(explicitPos)pos=*explicitPos;
 else {LARGE_INTEGER zero,cur;zero.QuadPart=0;cur.QuadPart=0;if(SetFilePointerEx(h,zero,&cur,FILE_CURRENT))cur.QuadPart-=n;pos=cur;}
 EnterCriticalSection(&lock);
 const wchar_t* names[4]={L"",L"smartcrypt-capture.bin",L"smartcrypt-capture-interface.u.bin",L"smartcrypt-capture-interface.xdat.bin"};
 if(outputHandles[kind]==INVALID_HANDLE_VALUE)outputHandles[kind]=realCreateFileW(names[kind],GENERIC_WRITE,FILE_SHARE_READ,NULL,CREATE_ALWAYS,FILE_ATTRIBUTE_NORMAL,NULL);
 if(outputHandles[kind]!=INVALID_HANDLE_VALUE){SetFilePointerEx(outputHandles[kind],pos,NULL,FILE_BEGIN);DWORD wrote=0;WriteFile(outputHandles[kind],b,n,&wrote,NULL);LONGLONG end=pos.QuadPart+wrote;if(end>maxEnds[kind])maxEnds[kind]=end;Trace("CAPTURE_KIND=%d_%s offset=%I64d bytes=%lu\\n",kind,api,pos.QuadPart,wrote);DebugLog("TARGET_READ kind=%d api=%s offset=%I64d bytes=%lu caller=%p\\r\\n",kind,api,pos.QuadPart,wrote,_ReturnAddress());}
 LeaveCriticalSection(&lock);
}
static BOOL WINAPI HookReadFile(HANDLE h,LPVOID b,DWORD req,LPDWORD got,LPOVERLAPPED ov){
 BOOL ok=realReadFile(h,b,req,got,ov);LogNpkStateOnce();DWORD n=(ok&&got)?*got:0;
 if(ok&&n&&inNpkRead==0){Trace("READFILE_CALLER=%p\\n",_ReturnAddress());LARGE_INTEGER pos,*pp=NULL;if(ov){pos.LowPart=ov->Offset;pos.HighPart=ov->OffsetHigh;pp=&pos;}CaptureReadBuffer(h,b,n,pp,"ReadFile");}
 return ok;
}
static void DumpNpkKeyState(HANDLE h){
 if(!realNpkGetState)return;
 int kind=TARGET_NONE;
 EnterCriticalSection(&lock);for(int i=1;i<=3;i++)if(h==targetHandles[i]){kind=i;break;}LeaveCriticalSection(&lock);
 if(kind==TARGET_NONE||keyDumped[kind])return;
 void* state=realNpkGetState(h);
 if(!state){DebugLog("NPK_KEY_STATE kind=%d handle=%p state=NULL\\r\\n",kind,h);return;}
 if(InterlockedCompareExchange(&keyDumped[kind],1,0)!=0)return;
 BYTE* s=(BYTE*)state;char k6[32]={},k16[64]={};
 for(int i=0;i<6;i++)sprintf_s(k6+i*2,3,"%02X",s[2+i]);
 for(int i=0;i<16;i++)sprintf_s(k16+i*2,3,"%02X",s[8+i]);
 DWORD globalKey=npkModule?*(DWORD*)((BYTE*)npkModule+0x51430):0;
 DWORD globalFlag=npkModule?*(DWORD*)((BYTE*)npkModule+0x51468):0;
 BYTE seed[24]={};seed[0]=s[0];seed[1]=s[1];
 BYTE globalBytes[4]={(BYTE)globalKey,(BYTE)(globalKey>>8),(BYTE)(globalKey>>16),(BYTE)(globalKey>>24)};
 for(int i=0;i<6;i++){BYTE v=s[2+i];if((s[1]&1)&&globalFlag)v^=globalBytes[i&3];seed[2+i]=(BYTE)(v^(BYTE)(s[0]+1));}
 for(int i=0;i<16;i++)seed[8+i]=(BYTE)(s[8+i]^s[2+(i%6)]);
 char seedHex[64]={};for(int i=0;i<24;i++)sprintf_s(seedHex+i*2,3,"%02X",seed[i]);
 DWORD version=s[0]==1?111:(s[0]==2?121:(s[0]==3?413:0));
 DebugLog("NPK_KEY_STATE kind=%d handle=%p state=%p global=%08lX globalFlag=%08lX seed=%02X%02X seedBlock=%s key6=%s key16=%s flag=%02X version=%lu\\r\\n",kind,h,state,globalKey,globalFlag,s[0],s[1],seedHex,k6,k16,s[0x18],version);
 const char* files[4]={"","smartcrypt-key-profile-sysstring.txt","smartcrypt-key-profile-interface-u.txt","smartcrypt-key-profile-interface-xdat.txt"};
 HANDLE out=CreateFileA(files[kind],GENERIC_WRITE,FILE_SHARE_READ,NULL,CREATE_ALWAYS,FILE_ATTRIBUTE_NORMAL,NULL);
 if(out!=INVALID_HANDLE_VALUE){char line[768]={};int n=sprintf_s(line,sizeof(line),"NPK_GLOBAL_KEY=%08lX\\r\\nSTATE_SEED=%02X%02X\\r\\nSEED_BLOCK=%s\\r\\nMARKER=FFEDFACE\\r\\nLINEAGE2_VERSION=%08lX\\r\\nKEY6=%s\\r\\nKEY16=%s\\r\\nFLAG=%02X\\r\\n",globalKey,s[0],s[1],seedHex,version,k6,k16,s[0x18]);DWORD wrote=0;WriteFile(out,line,(DWORD)n,&wrote,NULL);FlushFileBuffers(out);CloseHandle(out);}
}
static BOOL WINAPI HookNpkReadFile(HANDLE h,LPVOID b,DWORD req,LPDWORD got,LPOVERLAPPED ov){
 inNpkRead++;
 BOOL ok=realNpkReadFile(h,b,req,got,ov);
 inNpkRead--;
 LogNpkStateOnce();
 DWORD n=(ok&&got)?*got:0;
 if(ok&&n){LARGE_INTEGER pos,*pp=NULL;if(ov){pos.LowPart=ov->Offset;pos.HighPart=ov->OffsetHigh;pp=&pos;}CaptureReadBuffer(h,b,n,pp,"NpkReadFilePostTransform");DumpNpkKeyState(h);}
 return ok;
}
static BOOL InstallNpkReadHook(void){
 npkModule=GetModuleHandleA("npkscrypt.dll");
 if(!npkModule){DebugLog("NPK_DIRECT_HOOK module=NULL\\r\\n");return FALSE;}
 BYTE* readAddress=(BYTE*)npkModule+0x119C6C;
 BYTE* stateAddress=(BYTE*)npkModule+0x11983C;
 const BYTE readSig[]={0x55,0x8B,0xEC,0x83,0xEC,0x08,0x53,0x8B,0x5D,0x08,0x56,0x8B,0x75,0x10,0x57};
 const BYTE stateSig[]={0x55,0x8B,0xEC,0x83,0xEC,0x08,0x83,0x3D,0x38,0x14,0x05,0x10,0x00};
 if(memcmp(readAddress,readSig,sizeof(readSig))!=0){DebugLog("NPK_DIRECT_HOOK read signature mismatch module=%p address=%p\\r\\n",npkModule,readAddress);return FALSE;}
 if(memcmp(stateAddress,stateSig,sizeof(stateSig))==0)realNpkGetState=(PFN_NpkGetState)stateAddress;
 else {realNpkGetState=NULL;DebugLog("NPK_GET_STATE signature mismatch address=%p\\r\\n",stateAddress);}
 MH_STATUS st=MH_CreateHook(readAddress,(LPVOID)HookNpkReadFile,(LPVOID*)&realNpkReadFile);
 if(st==MH_OK||st==MH_ERROR_ALREADY_CREATED){MH_STATUS en=MH_EnableHook(readAddress);DebugLog("NPK_DIRECT_HOOK module=%p read=%p state=%p create=%d enable=%d\\r\\n",npkModule,readAddress,stateAddress,(int)st,(int)en);return en==MH_OK||en==MH_ERROR_ENABLED;}
 DebugLog("NPK_DIRECT_HOOK create failed module=%p read=%p status=%d\\r\\n",npkModule,readAddress,(int)st);return FALSE;
}
static DWORD WINAPI InstallNpkReadHookThread(LPVOID){
 for(int i=0;i<30;i++){if(InstallNpkReadHook())return 0;Sleep(100);}
 DebugLog("NPK_DIRECT_HOOK failed after retries\\r\\n");return 1;
}
static NTSTATUS NTAPI HookNtReadFile(HANDLE h,HANDLE ev,PIO_APC_ROUTINE apc,PVOID apcCtx,PIO_STATUS_BLOCK iosb,PVOID b,ULONG req,PLARGE_INTEGER offset,PULONG key){
 NTSTATUS status=realNtReadFile(h,ev,apc,apcCtx,iosb,b,req,offset,key);
 if(status>=0&&iosb&&iosb->Information&&inNpkRead==0){Trace("NTREADFILE_CALLER=%p\\n",_ReturnAddress());LARGE_INTEGER pos,*pp=NULL;if(offset){pos=*offset;pp=&pos;}CaptureReadBuffer(h,b,(DWORD)iosb->Information,pp,"NtReadFile");}
 return status;
}
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
extern "C" __declspec(dllexport) BOOL WINAPI SmartCapture_TraceInitialize(void){
 if(InterlockedCompareExchange(&initialized,1,0)==0)InitializeCriticalSection(&lock);
 MH_STATUS init=MH_Initialize(); if(init!=MH_OK&&init!=MH_ERROR_ALREADY_INITIALIZED)return FALSE;
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
 return MH_EnableHook(MH_ALL_HOOKS)==MH_OK;
}
extern "C" __declspec(dllexport) BOOL WINAPI SmartCapture_Initialize(void){
 if(!initialized&&!SmartCapture_TraceInitialize())return FALSE;
 MH_CreateHookApi(L"kernel32.dll","CreateFileW",(LPVOID)HookCreateFileW,(LPVOID*)&realCreateFileW);
 MH_CreateHookApi(L"kernel32.dll","CreateFileA",(LPVOID)HookCreateFileA,(LPVOID*)&realCreateFileA);
 MH_CreateHookApi(L"kernel32.dll","ReadFile",(LPVOID)HookReadFile,(LPVOID*)&realReadFile);
 MH_CreateHookApi(L"ntdll.dll","NtReadFile",(LPVOID)HookNtReadFile,(LPVOID*)&realNtReadFile);
 MH_CreateHookApi(L"ntdll.dll","NtCreateFile",(LPVOID)HookNtCreateFile,(LPVOID*)&realNtCreateFile);
 MH_CreateHookApi(L"ntdll.dll","NtOpenFile",(LPVOID)HookNtOpenFile,(LPVOID*)&realNtOpenFile);
 if(MH_EnableHook(MH_ALL_HOOKS)!=MH_OK)return FALSE;
 hookReady=TRUE; return TRUE;
}
extern "C" __declspec(dllexport) void WINAPI SmartCapture_Flush(void){if(!hookReady||!initialized)return;EnterCriticalSection(&lock);for(int i=1;i<=3;i++){if(outputHandles[i]!=INVALID_HANDLE_VALUE){LARGE_INTEGER end;end.QuadPart=maxEnds[i];SetFilePointerEx(outputHandles[i],end,NULL,FILE_BEGIN);SetEndOfFile(outputHandles[i]);FlushFileBuffers(outputHandles[i]);CloseHandle(outputHandles[i]);outputHandles[i]=INVALID_HANDLE_VALUE;}}LeaveCriticalSection(&lock);}
extern "C" __declspec(dllexport) BOOL WINAPI SmartCapture_Status(void){return hookReady;}
extern "C" __declspec(dllexport) void WINAPI SmartCapture_DumpTrace(void){if(!initialized)return;HANDLE h=realCreateFileW(L"npkscrypt-api-trace.txt",GENERIC_WRITE,FILE_SHARE_READ,NULL,CREATE_ALWAYS,FILE_ATTRIBUTE_NORMAL,NULL);if(h!=INVALID_HANDLE_VALUE){DWORD n=(DWORD)traceLength,w=0;WriteFile(h,traceBuffer,n,&w,NULL);CloseHandle(h);}}
extern "C" __declspec(dllexport) int __stdcall load(void){Trace("SmartCapture load callback called\\n");return 0;}
BOOL WINAPI DllMain(HINSTANCE h,DWORD r,LPVOID p){if(r==DLL_PROCESS_ATTACH){DisableThreadLibraryCalls(h);DebugLog("DLL_PROCESS_ATTACH\\r\\n");HMODULE npkBefore=GetModuleHandleA("npkscrypt.dll");DebugLog("NPK_BEFORE_ENGINE=%p\\r\\n",npkBefore);engineModule=LoadLibraryA("engine.dll");DebugLog("ENGINE_LOAD=%p\\r\\n",engineModule);if(!engineModule)return FALSE;npkModule=GetModuleHandleA("npkscrypt.dll");if(!npkModule)npkModule=LoadLibraryA("npkscrypt.dll");DebugLog("NPK_PRELOAD=%p\\r\\n",npkModule);BOOL ok=SmartCapture_Initialize();DebugLog("SMARTCAPTURE_INITIALIZE=%d\\r\\n",ok);if(!ok)return FALSE;if(realCreateThread){HANDLE th=realCreateThread(NULL,0,InstallNpkReadHookThread,NULL,0,NULL);if(th)CloseHandle(th);}}else if(r==DLL_PROCESS_DETACH){DebugLog("DLL_PROCESS_DETACH\\r\\n");SmartCapture_Flush();SmartCapture_DumpTrace();}return TRUE;}
