using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

internal static class Probe
{
    [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)]
    static extern bool SetDllDirectory(string path);
    [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)]
    static extern IntPtr LoadLibrary(string path);
    [DllImport("kernel32.dll", CharSet=CharSet.Ansi, SetLastError=true)]
    static extern IntPtr GetProcAddress(IntPtr module, string name);
    [DllImport("kernel32.dll", SetLastError=true)]
    static extern IntPtr CreateFileW(string name, uint access, uint share, IntPtr security,
        uint creation, uint flags, IntPtr template);
    [DllImport("kernel32.dll", SetLastError=true)]
    static extern bool ReadFile(IntPtr file, byte[] buffer, uint count, out uint read, IntPtr overlapped);
    [DllImport("kernel32.dll", SetLastError=true)]
    static extern bool CloseHandle(IntPtr handle);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate int ExportFn();

    static void Log(string s) { Console.WriteLine(s); }
    static string Hash(byte[] b) {
        using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(b)).Replace("-", "").ToLowerInvariant();
    }

    static int Main(string[] args)
    {
        string mode = args.Length == 0 ? "npk" : args[0];
        string dir = Environment.CurrentDirectory;
        Log("MODE=" + mode);
        Log("PID=" + System.Diagnostics.Process.GetCurrentProcess().Id);
        SetDllDirectory(dir);

        if (mode == "stack") {
            IntPtr core = LoadLibrary(Path.Combine(dir, "Core.dll"));
            Log("LOAD_Core=" + (core != IntPtr.Zero) + " err=" + Marshal.GetLastWin32Error());
            IntPtr engine = LoadLibrary(Path.Combine(dir, "engine.dll"));
            Log("LOAD_engine=" + (engine != IntPtr.Zero) + " err=" + Marshal.GetLastWin32Error());
        }

        IntPtr npk = LoadLibrary(Path.Combine(dir, "npkscrypt.dll"));
        Log("LOAD_npkscrypt=" + (npk != IntPtr.Zero) + " err=" + Marshal.GetLastWin32Error());
        if (npk == IntPtr.Zero) return 10;

        IntPtr export = GetProcAddress(npk, "Akumu_ICQ_10050005");
        Log("EXPORT_Akumu=" + (export != IntPtr.Zero));
        if (export != IntPtr.Zero) {
            var fn = (ExportFn)Marshal.GetDelegateForFunctionPointer(export, typeof(ExportFn));
            Log("EXPORT_RESULT=" + fn());
        }

        if (mode == "stack") {
            IntPtr ogg = LoadLibrary(Path.Combine(dir, "ogg.dll"));
            Log("LOAD_ogg=" + (ogg != IntPtr.Zero) + " err=" + Marshal.GetLastWin32Error());
        }

        string input = Path.Combine(dir, "SysString-e.dat");
        IntPtr h = CreateFileW(input, 0x80000000, 1, IntPtr.Zero, 3, 0x80, IntPtr.Zero);
        Log("CREATEFILE=" + (h != new IntPtr(-1)) + " err=" + Marshal.GetLastWin32Error());
        if (h == new IntPtr(-1)) return 11;

        using (var ms = new MemoryStream()) {
            byte[] buf = new byte[65536];
            uint n;
            while (ReadFile(h, buf, (uint)buf.Length, out n, IntPtr.Zero) && n > 0)
                ms.Write(buf, 0, (int)n);
            CloseHandle(h);
            byte[] output = ms.ToArray();
            string outPath = Path.Combine(dir, "read-" + mode + ".bin");
            File.WriteAllBytes(outPath, output);
            Log("READ_BYTES=" + output.Length);
            Log("READ_SHA256=" + Hash(output));
            Log("READ_HEADER=" + BitConverter.ToString(output, 0, Math.Min(16, output.Length)));
            Log("OUTPUT=" + outPath);
        }
        return 0;
    }
}
