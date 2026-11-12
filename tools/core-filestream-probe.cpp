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
 HMODULE ogg=LoadLibraryW(L"ogg.dll");
 printf("OGG=%p err=%lu\n",ogg,GetLastError());
 HMODULE core=GetModuleHandleW(L"Core.dll");
 HMODULE shim=GetModuleHandleW(L"SmartCapture.dll");
 HMODULE npk=GetModuleHandleW(L"npkscrypt.dll");
 printf("NPK_LOADED=%p\\n",npk);
 typedef void (__stdcall *FlushFn)(void);
 FlushFn flush=shim?(FlushFn)GetProcAddress(shim,"SmartCapture_Flush"):nullptr;
 printf("CORE=%p\n",core);
 if(!core)return 2;
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
