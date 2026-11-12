using System;
using System.IO;
using System.Runtime.InteropServices;
internal static class Probe {
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern IntPtr LoadLibraryW(string p);
 [DllImport("kernel32.dll",CharSet=CharSet.Ansi,SetLastError=true)] static extern IntPtr GetProcAddress(IntPtr h,string n);
 [UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate bool InitFn();
 [UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void FlushFn();
 static IntPtr Load(string name) {
  IntPtr h=LoadLibraryW(Path.Combine(Environment.CurrentDirectory,name));
  Console.WriteLine(name.ToUpperInvariant()+"_LOAD="+(h!=IntPtr.Zero)+" ERROR="+(h==IntPtr.Zero?Marshal.GetLastWin32Error():0));
  return h;
 }
 static int Main() {
  Console.WriteLine("PID="+System.Diagnostics.Process.GetCurrentProcess().Id+" BITNESS="+(IntPtr.Size*8));
  IntPtr shim=Load("SmartCapture.dll");
  if(shim==IntPtr.Zero)return 2;
  var init=(InitFn)Marshal.GetDelegateForFunctionPointer(GetProcAddress(shim,"SmartCapture_Initialize"),typeof(InitFn));
  var flush=(FlushFn)Marshal.GetDelegateForFunctionPointer(GetProcAddress(shim,"SmartCapture_Flush"),typeof(FlushFn));
  bool hooked=init(); Console.WriteLine("HOOK_INIT="+hooked);
  IntPtr vorbis=Load("vorbisfile.dll");
  IntPtr ifc=Load("ifc23.dll");
  IntPtr core=Load("Core.dll");
  IntPtr engine=Load("engine.dll");
  IntPtr ogg=Load("ogg.dll");
  byte[] bytes=File.ReadAllBytes(Path.Combine(Environment.CurrentDirectory,"SysString-e.dat")); flush();
  Console.WriteLine("NORMAL_READ="+bytes.Length+" CAPTURE_EXISTS="+File.Exists("smartcrypt-capture.bin"));
  if(File.Exists("smartcrypt-capture.bin")){byte[] cap=File.ReadAllBytes("smartcrypt-capture.bin");using(var sha=System.Security.Cryptography.SHA256.Create())Console.WriteLine("CAPTURE_BYTES="+cap.Length+" SHA256="+BitConverter.ToString(sha.ComputeHash(cap)).Replace("-","").ToLowerInvariant());Console.WriteLine("CAPTURE_HEADER="+BitConverter.ToString(cap,0,Math.Min(16,cap.Length)));}
  return 0;
 }
}
