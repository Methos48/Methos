#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <stdio.h>
#include <stdint.h>
typedef void* (__fastcall *PMalloc)(void*,void*,uint32_t,const wchar_t*);
typedef void* (__fastcall *PRealloc)(void*,void*,void*,uint32_t,const wchar_t*);
typedef void (__fastcall *PFree)(void*,void*,void*);
static void* __fastcall HeapMalloc(void*,void*,uint32_t n,const wchar_t*){return HeapAlloc(GetProcessHeap(),HEAP_ZERO_MEMORY,n?n:1);}
static void* __fastcall HeapRealloc(void*,void*,void* p,uint32_t n,const wchar_t*){if(!p)return HeapMalloc(nullptr,nullptr,n,nullptr);return HeapReAlloc(GetProcessHeap(),HEAP_ZERO_MEMORY,p,n?n:1);}
static void __fastcall AllocFree(void*,void*,void* p){if(p)HeapFree(GetProcessHeap(),0,p);}
int main(){
 HMODULE dsetup=LoadLibraryA("dsetup.dll");
 FARPROC dsetupOrdinal=dsetup?GetProcAddress(dsetup,(LPCSTR)0x67):nullptr;
 printf("DSETUP_MODULE=%p ORDINAL_67=%p\\n",dsetup,dsetupOrdinal);
 HMODULE ogg=LoadLibraryW(L"ogg.dll");
 printf("OGG=%p err=%lu\n",ogg,GetLastError());
 HMODULE core=GetModuleHandleW(L"Core.dll");
 HMODULE shim=GetModuleHandleW(L"SmartCapture.dll");
 HMODULE npk=GetModuleHandleW(L"npkscrypt.dll");
 typedef BOOL (__stdcall *CaptureStatusFn)(void);
 CaptureStatusFn captureStatus=shim?(CaptureStatusFn)GetProcAddress(shim,"SmartCapture_Status"):nullptr;
 BOOL captureReady=captureStatus?captureStatus():FALSE;
 printf("CAPTURE_HOOK_AUTO_INIT=%d\\n",captureReady);
 if(!captureReady)return 10;
 printf("NPK_LOADED=%p\\n",npk);
 if(npk){
  BYTE* base=(BYTE*)npk;
  IMAGE_DOS_HEADER* dos=(IMAGE_DOS_HEADER*)base;
  IMAGE_NT_HEADERS32* nt=(IMAGE_NT_HEADERS32*)(base+dos->e_lfanew);
  DWORD imageSize=nt->OptionalHeader.SizeOfImage;
  printf("NPK_IMAGE_BASE=%p SIZE=%08lX\\n",base,imageSize);
  printf("NPK_GLOBAL_51430=%08lX 51438=%08lX 51464=%08lX 51468=%08lX 5146C=%08lX 51470=%08lX 514F4=%08lX 514F8=%08lX 514E8=%08lX 514EC=%08lX\\n",
    *(DWORD*)(base+0x51430),*(DWORD*)(base+0x51438),*(DWORD*)(base+0x51464),*(DWORD*)(base+0x51468),*(DWORD*)(base+0x5146c),*(DWORD*)(base+0x51470),*(DWORD*)(base+0x514f4),*(DWORD*)(base+0x514f8),*(DWORD*)(base+0x514e8),*(DWORD*)(base+0x514ec));
  printf("NPK_WRAPPERS_OPEN=%p SEEK=%p CLOSE=%p READ=%p\\n",base+0x19A7C,base+0x19B5C,base+0x19C3C,base+0x19C6C);
  MEMORY_BASIC_INFORMATION mbi={};
  SIZE_T q=VirtualQuery((void*)0x02910000,&mbi,sizeof(mbi));
  printf("NPK_TRAMPOLINE_REGION_QUERY=%u BASE=%p SIZE=%p PROTECT=%08lX\\n",(unsigned)q,mbi.BaseAddress,(void*)mbi.RegionSize,mbi.Protect);
  if(q){BYTE tramp[0x300]={};SIZE_T got=0;if(ReadProcessMemory(GetCurrentProcess(),(void*)0x02910000,tramp,sizeof(tramp),&got)){HANDLE th=CreateFileW(L"npkscrypt-trampolines.bin",GENERIC_WRITE,0,nullptr,CREATE_ALWAYS,FILE_ATTRIBUTE_NORMAL,nullptr);DWORD tw=0;if(th!=INVALID_HANDLE_VALUE){WriteFile(th,tramp,(DWORD)got,&tw,nullptr);CloseHandle(th);}printf("NPK_TRAMPOLINE_DUMP=%lu\\n",(unsigned long)tw);}}
  HANDLE dump=CreateFileW(L"npkscrypt-memory.bin",GENERIC_WRITE,0,nullptr,CREATE_ALWAYS,FILE_ATTRIBUTE_NORMAL,nullptr);
  if(dump!=INVALID_HANDLE_VALUE){DWORD w=0;WriteFile(dump,base,imageSize,&w,nullptr);CloseHandle(dump);printf("NPK_IMAGE_DUMP=%lu\\n",w);}
 }

 typedef void (__stdcall *FlushFn)(void);
 FlushFn flush=shim?(FlushFn)GetProcAddress(shim,"SmartCapture_Flush"):nullptr;
 printf("CORE=%p\n",core);
 if(!core)return 2;
 printf("CORE_IAT_READFILE=%p CREATEFILEA=%p CREATEFILEW=%p CLOSEHANDLE=%p SETFILEPOINTER=%p\n",
   *(void**)((BYTE*)core+0x240570),*(void**)((BYTE*)core+0x240574),*(void**)((BYTE*)core+0x240578),*(void**)((BYTE*)core+0x24057c),*(void**)((BYTE*)core+0x2405f8));
 void** gMalloc=(void**)GetProcAddress(core,"?GMalloc@@3PAVFMalloc@@A");
 FARPROC initProc=GetProcAddress(core,"?Init@FFileStream@@SAPAV1@H@Z");
 printf("GMALLOC_EXPORT=%p FSTREAM_INIT=%p\n",gMalloc,initProc);
 if(!gMalloc||!initProc)return 3;
 static void* vtable[64]={};
 vtable[0]=(void*)HeapMalloc;
 vtable[1]=(void*)HeapRealloc;
 vtable[2]=(void*)AllocFree;
 static void* allocator[1];
 allocator[0]=vtable;
 *gMalloc=allocator;
 typedef void* (__cdecl *InitFn)(int);
 void* stream=((InitFn)initProc)(18);
 printf("FSTREAM_INIT_RESULT=%p\n",stream);
 if(!stream)return 4;
 FARPROC createProc=GetProcAddress(core,"?CreateStream@FFileStream@@QAEHPBGHHPAXW4EFileStreamType@@1@Z");
 printf("FSTREAM_CREATE=%p\n",createProc);
 if(!createProc)return 5;
 WIN32_FILE_ATTRIBUTE_DATA info={};
 if(!GetFileAttributesExW(L"SysString-e.dat",GetFileExInfoStandard,&info))return 6;
 uint64_t size=((uint64_t)info.nFileSizeHigh<<32)|info.nFileSizeLow;
 printf("TARGET_SIZE=%llu\\n",(unsigned long long)size);
 void* buffer=HeapAlloc(GetProcessHeap(),HEAP_ZERO_MEMORY,(SIZE_T)size);
 if(!buffer)return 7;
 typedef int (__thiscall *CreateStreamFn)(void*,const wchar_t*,int,int,void*,int,int);
 int result=((CreateStreamFn)createProc)(stream,L"SysString-e.dat",1,(int)size,buffer,0,0);
 printf("FSTREAM_CREATE_RESULT=%d\\n",result);
 HANDLE out=CreateFileW(L"filestream-read.bin",GENERIC_WRITE,0,nullptr,CREATE_ALWAYS,FILE_ATTRIBUTE_NORMAL,nullptr);
 DWORD wrote=0;
 if(out!=INVALID_HANDLE_VALUE){WriteFile(out,buffer,(DWORD)size,&wrote,nullptr);CloseHandle(out);}
 printf("FSTREAM_BUFFER_WRITTEN=%lu\\n",wrote);
 if(wrote>=16){unsigned char* b=(unsigned char*)buffer;printf("FSTREAM_HEADER=");for(int i=0;i<16;i++)printf("%02X%s",b[i],i==15?"\\n":"-");}
 HeapFree(GetProcessHeap(),0,buffer);
 if(flush)flush();
 typedef void (__stdcall *DumpFn)(void);
 DumpFn dump=shim?(DumpFn)GetProcAddress(shim,"SmartCapture_DumpTrace"):nullptr;
 if(dump)dump();
 WIN32_FILE_ATTRIBUTE_DATA capInfo={};
 if(GetFileAttributesExW(L"smartcrypt-capture.bin",GetFileExInfoStandard,&capInfo))
  printf("CAPTURE_SIZE=%lu\\n",capInfo.nFileSizeLow);
 return wrote?0:8;
}
