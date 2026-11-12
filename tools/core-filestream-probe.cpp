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
 return stream?0:4;
}
