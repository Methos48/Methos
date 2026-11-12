using System;
using System.IO;
using System.Runtime.InteropServices;
internal static class Probe {
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern IntPtr LoadLibraryW(string p);
 [DllImport("kernel32.dll",CharSet=CharSet.Ansi,SetLastError=true)] static extern IntPtr GetProcAddress(IntPtr h,string n);
 [UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate bool InitFn();
 [UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void FlushFn();
 static int Main() {
  Console.WriteLine("PID="+System.Diagnostics.Process.GetCurrentProcess().Id+" BITNESS="+(IntPtr.Size*8));
  IntPtr shim=LoadLibraryW(Path.Combine(Environment.CurrentDirectory,"SmartCapture.dll"));
  if(shim==IntPtr.Zero){Console.WriteLine("SHIM_LOAD_FAIL "+Marshal.GetLastWin32Error());return 2;}
  var init=(InitFn)Marshal.GetDelegateForFunctionPointer(GetProcAddress(shim,"SmartCapture_Initialize"),typeof(InitFn));
  var flush=(FlushFn)Marshal.GetDelegateForFunctionPointer(GetProcAddress(shim,"SmartCapture_Flush"),typeof(FlushFn));
  IntPtr engine=LoadLibraryW(Path.Combine(Environment.CurrentDirectory,"engine.dll"));
  Console.WriteLine("ENGINE_LOAD="+(engine!=IntPtr.Zero)+" ERROR="+Marshal.GetLastWin32Error());
  IntPtr ogg=LoadLibraryW(Path.Combine(Environment.CurrentDirectory,"ogg.dll"));
  Console.WriteLine("OGG_LOAD="+(ogg!=IntPtr.Zero)+" ERROR="+Marshal.GetLastWin32Error());
  bool patched=init(); Console.WriteLine("IAT_PATCHED="+patched);
  byte[] bytes=File.ReadAllBytes(Path.Combine(Environment.CurrentDirectory,"SysString-e.dat")); flush();
  Console.WriteLine("NORMAL_READ="+bytes.Length+" CAPTURE_EXISTS="+File.Exists("smartcrypt-capture.bin"));
  if(File.Exists("smartcrypt-capture.bin")){byte[] cap=File.ReadAllBytes("smartcrypt-capture.bin");using(var sha=System.Security.Cryptography.SHA256.Create())Console.WriteLine("CAPTURE_BYTES="+cap.Length+" SHA256="+BitConverter.ToString(sha.ComputeHash(cap)).Replace("-","").ToLowerInvariant());Console.WriteLine("CAPTURE_HEADER="+BitConverter.ToString(cap,0,Math.Min(16,cap.Length)));}
  return 0;
 }}