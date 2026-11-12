using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
internal static class Probe {
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern IntPtr LoadLibraryW(string p);
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] static extern IntPtr GetModuleHandleW(string p);
 [DllImport("kernel32.dll",CharSet=CharSet.Ansi,SetLastError=true)] static extern IntPtr GetProcAddress(IntPtr h,string n);
 [UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void FlushFn();
 [UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate bool StatusFn();
 static int Main() {
  Console.WriteLine("PID="+System.Diagnostics.Process.GetCurrentProcess().Id+" BITNESS="+(IntPtr.Size*8));
  IntPtr ogg=LoadLibraryW(Path.Combine(Environment.CurrentDirectory,"ogg.dll"));
  Console.WriteLine("OGG_LOAD="+(ogg!=IntPtr.Zero)+" ERROR="+(ogg==IntPtr.Zero?Marshal.GetLastWin32Error():0));
  IntPtr shim=GetModuleHandleW("SmartCapture.dll");
  IntPtr engine=GetModuleHandleW("engine.dll");
  IntPtr core=GetModuleHandleW("Core.dll");
  Console.WriteLine("SHIM_IMPORTED="+(shim!=IntPtr.Zero)+" ENGINE_LOADED_BY_SHIM="+(engine!=IntPtr.Zero)+" CORE_LOADED="+(core!=IntPtr.Zero));
  if(ogg==IntPtr.Zero||shim==IntPtr.Zero)return 2;
  var status=(StatusFn)Marshal.GetDelegateForFunctionPointer(GetProcAddress(shim,"SmartCapture_Status"),typeof(StatusFn));
  var flush=(FlushFn)Marshal.GetDelegateForFunctionPointer(GetProcAddress(shim,"SmartCapture_Flush"),typeof(FlushFn));
  Console.WriteLine("HOOK_READY="+status());
  byte[] bytes=File.ReadAllBytes(Path.Combine(Environment.CurrentDirectory,"SysString-e.dat"));
  flush();
  Console.WriteLine("NORMAL_READ="+bytes.Length+" CAPTURE_EXISTS="+File.Exists("smartcrypt-capture.bin"));
  if(File.Exists("smartcrypt-capture.bin")) {
   byte[] cap=File.ReadAllBytes("smartcrypt-capture.bin");
   using(var sha=SHA256.Create())Console.WriteLine("CAPTURE_BYTES="+cap.Length+" SHA256="+BitConverter.ToString(sha.ComputeHash(cap)).Replace("-","").ToLowerInvariant());
   Console.WriteLine("CAPTURE_HEADER="+BitConverter.ToString(cap,0,Math.Min(16,cap.Length)));
  }
  return 0;
 }
}