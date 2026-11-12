using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
internal static class Probe {
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode,ExactSpelling=true,SetLastError=true)] static extern IntPtr LoadLibraryW(string p);
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode,ExactSpelling=true,SetLastError=true)] static extern bool SetDllDirectoryW(string p);
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode,ExactSpelling=true,SetLastError=true)] static extern IntPtr CreateFileW(string n,uint a,uint s,IntPtr sa,uint c,uint f,IntPtr t);
 [DllImport("kernel32.dll",ExactSpelling=true,SetLastError=true)] static extern bool ReadFile(IntPtr h,byte[] b,uint c,out uint n,IntPtr o);
 [DllImport("kernel32.dll",ExactSpelling=true,SetLastError=true)] static extern bool CloseHandle(IntPtr h);
 static bool Load(string n){string p=Path.Combine(Environment.CurrentDirectory,n);if(!File.Exists(p)){Console.WriteLine("MISSING "+n);return false;}IntPtr h=LoadLibraryW(p);Console.WriteLine("LOAD "+n+"="+(h!=IntPtr.Zero?"OK":"FAIL")+" error="+Marshal.GetLastWin32Error());return h!=IntPtr.Zero;}
 static string Hash(byte[] b){using(var s=SHA256.Create())return BitConverter.ToString(s.ComputeHash(b)).Replace("-","").ToLowerInvariant();}
 static int Main(string[] a){
  string mode=a.Length==0?"all":a[0].ToLowerInvariant(); Console.WriteLine("MODE="+mode+" PID="+System.Diagnostics.Process.GetCurrentProcess().Id); SetDllDirectoryW(Environment.CurrentDirectory);
  if(mode=="npk"||mode=="all")if(!Load("npkscrypt.dll"))return 10;
  if(mode=="ogg"||mode=="all")if(!Load("ogg.dll"))return 12;
  if(mode=="engine"||mode=="all"){if(!Load("Core.dll"))return 13;if(!Load("engine.dll"))return 14;if(File.Exists("l2ui.dll"))Load("l2ui.dll");}
  IntPtr hFile=CreateFileW(Path.Combine(Environment.CurrentDirectory,"SysString-e.dat"),0x80000000,1,IntPtr.Zero,3,0x80,IntPtr.Zero);
  if(hFile==new IntPtr(-1)){Console.WriteLine("CREATEFILE FAIL "+Marshal.GetLastWin32Error());return 15;}
  using(var ms=new MemoryStream()){byte[] b=new byte[65536];uint n;while(ReadFile(hFile,b,(uint)b.Length,out n,IntPtr.Zero)&&n>0)ms.Write(b,0,(int)n);CloseHandle(hFile);byte[] data=ms.ToArray();File.WriteAllBytes("read-"+mode+".bin",data);Console.WriteLine("READ bytes="+data.Length+" sha256="+Hash(data)+" header="+BitConverter.ToString(data,0,Math.Min(16,data.Length)));}
  return 0;
 }
}