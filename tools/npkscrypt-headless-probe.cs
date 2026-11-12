using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

internal static class Probe
{
    [DllImport("kernel32.dll", CharSet=CharSet.Unicode, ExactSpelling=true, SetLastError=true)]
    static extern IntPtr LoadLibraryW(string path);
    [DllImport("kernel32.dll", CharSet=CharSet.Unicode, ExactSpelling=true, SetLastError=true)]
    static extern bool SetDllDirectoryW(string path);
    [DllImport("kernel32.dll", CharSet=CharSet.Unicode, ExactSpelling=true, SetLastError=true)]
    static extern IntPtr CreateFileW(string name, uint access, uint share, IntPtr security, uint creation, uint flags, IntPtr template);
    [DllImport("kernel32.dll", ExactSpelling=true, SetLastError=true)]
    static extern bool ReadFile(IntPtr file, byte[] buffer, uint count, out uint read, IntPtr overlapped);
    [DllImport("kernel32.dll", ExactSpelling=true, SetLastError=true)]
    static extern bool CloseHandle(IntPtr handle);
    static string Hash(byte[] b) { using (var s=SHA256.Create()) return BitConverter.ToString(s.ComputeHash(b)).Replace("-","").ToLowerInvariant(); }
    static bool Load(string name) {
        string path=Path.Combine(Environment.CurrentDirectory,name);
        if(!File.Exists(path)){Console.WriteLine("MISSING "+name);return false;}
        IntPtr p=LoadLibraryW(path);
        Console.WriteLine("LOAD "+name+"="+(p!=IntPtr.Zero?"OK":"FAIL")+" error="+Marshal.GetLastWin32Error());
        return p!=IntPtr.Zero;
    }
    static int Main(string[] args) {
        string mode=args.Length==0?"npk":args[0].ToLowerInvariant();
        Console.WriteLine("MODE="+mode+" PID="+System.Diagnostics.Process.GetCurrentProcess().Id);
        SetDllDirectoryW(Environment.CurrentDirectory);
        if((mode=="npk"||mode=="all")&&!Load("npkscrypt.dll"))return 10;
        if((mode=="ogg"||mode=="all")&&!Load("ogg.dll"))return 12;
        if(mode=="engine"||mode=="all"){Load("Core.dll");if(!Load("engine.dll"))return 13;}
        IntPtr h=CreateFileW(Path.Combine(Environment.CurrentDirectory,"SysString-e.dat"),0x80000000,1,IntPtr.Zero,3,0x80,IntPtr.Zero);
        if(h==new IntPtr(-1)){Console.WriteLine("CREATEFILE FAIL "+Marshal.GetLastWin32Error());return 11;}
        using(var ms=new MemoryStream()){
            byte[] buf=new byte[65536];uint n;
            while(ReadFile(h,buf,(uint)buf.Length,out n,IntPtr.Zero)&&n>0)ms.Write(buf,0,(int)n);
            CloseHandle(h);byte[] data=ms.ToArray();string output="read-"+mode+".bin";File.WriteAllBytes(output,data);
            Console.WriteLine("READ bytes="+data.Length+" sha256="+Hash(data)+" header="+BitConverter.ToString(data,0,Math.Min(16,data.Length)));
        }
        return 0;
    }
}