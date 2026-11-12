namespace MethosUEngine;

/// <summary>
/// Per-file SmartCrypt 1.5.2.2 state captured after npkscrypt.dll initializes it.
/// Key6 and Key16 are the final arrays at state offsets +2 and +8.
/// </summary>
public sealed class SmartCryptKeyProfile
{
    public byte[] Key6 { get; }
    public byte[] Key16 { get; }
    public byte Flag { get; }
    public byte Seed0 { get; }
    public byte Seed1 { get; }
    public uint GlobalKey { get; }
    public string Origin { get; }

    public bool UsesC5Variant => Flag != 0;

    public SmartCryptKeyProfile(ReadOnlySpan<byte> key6, ReadOnlySpan<byte> key16,
        byte flag, uint globalKey = 0, byte seed0 = 0, byte seed1 = 0,
        string origin = "runtime-capture")
    {
        if (key6.Length != 6) throw new ArgumentException("SmartCrypt key6 must contain exactly 6 bytes.", nameof(key6));
        if (key16.Length != 16) throw new ArgumentException("SmartCrypt key16 must contain exactly 16 bytes.", nameof(key16));
        Key6 = key6.ToArray();
        Key16 = key16.ToArray();
        Flag = flag;
        GlobalKey = globalKey;
        Seed0 = seed0;
        Seed1 = seed1;
        Origin = string.IsNullOrWhiteSpace(origin) ? "runtime-capture" : origin.Trim();
    }

    public static SmartCryptKeyProfile ParseLog(string text, string origin = "runtime-capture")
    {
        ArgumentNullException.ThrowIfNull(text);
        byte[] key6 = ParseHex(text, "KEY6", 6);
        byte[] key16 = ParseHex(text, "KEY16", 16);
        byte[] seed = ParseHex(text, "STATE_SEED", 2);
        byte[] flagBytes = ParseHex(text, "FLAG", 1);
        uint global = ParseUInt32Hex(text, "NPK_GLOBAL_KEY");
        return new SmartCryptKeyProfile(key6, key16, flagBytes[0], global,
            seed[0], seed[1], origin);
    }

    private static byte[] ParseHex(string text, string name, int expectedBytes)
    {
        string needle = name + "=";
        int start = text.IndexOf(needle, StringComparison.OrdinalIgnoreCase);
        if (start < 0) throw new InvalidDataException($"SmartCrypt profile is missing {name}.");
        start += needle.Length;
        int end = start;
        while (end < text.Length && Uri.IsHexDigit(text[end])) end++;
        if (end - start != expectedBytes * 2)
            throw new InvalidDataException($"SmartCrypt profile {name} must contain {expectedBytes * 2} hexadecimal characters.");
        try { return Convert.FromHexString(text.AsSpan(start, end - start)); }
        catch (FormatException ex) { throw new InvalidDataException($"SmartCrypt profile {name} is invalid.", ex); }
    }

    private static uint ParseUInt32Hex(string text, string name)
    {
        string needle = name + "=";
        int start = text.IndexOf(needle, StringComparison.OrdinalIgnoreCase);
        if (start < 0) throw new InvalidDataException($"SmartCrypt profile is missing {name}.");
        start += needle.Length;
        int end = start;
        while (end < text.Length && Uri.IsHexDigit(text[end])) end++;
        if (end - start != 8 || !uint.TryParse(text.AsSpan(start, 8),
            System.Globalization.NumberStyles.HexNumber, null, out uint value))
            throw new InvalidDataException($"SmartCrypt profile {name} is invalid.");
        return value;
    }
}

/// <summary>
/// SmartCrypt 1.5.2.2 NPK ReadFile transformation. The operation is symmetric XOR.
/// It requires the final per-handle key arrays captured from a matching client.
/// </summary>
public sealed class SmartCrypt1522Codec : IProtectionCodec
{
    private static readonly byte[] Magic = { 0xFF, 0xED, 0xFA, 0xCE };
    private readonly SmartCryptKeyProfile _profile;

    public SmartCrypt1522Codec(SmartCryptKeyProfile profile) =>
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));

    public string LayerId => "smartcrypt-1522";

    public static bool HasHeader(ReadOnlySpan<byte> data) =>
        data.Length >= Magic.Length && data[..Magic.Length].SequenceEqual(Magic);

    public byte[] Decode(ReadOnlyMemory<byte> input, string originalFileName,
        CancellationToken cancellationToken = default)
    {
        if (input.Length < 0x400 || !HasHeader(input.Span))
            throw new InvalidDataException("File is not a supported SmartCrypt 1.5.2.2 container.");
        byte[] output = input.ToArray();
        TransformInPlace(output, 0, cancellationToken);
        return output;
    }

    public byte[] Encode(ReadOnlyMemory<byte> input, string originalFileName,
        CancellationToken cancellationToken = default)
    {
        if (input.Length < 0x400)
            throw new InvalidDataException("SmartCrypt 1.5.2.2 requires a file of at least 0x400 bytes.");
        byte[] output = input.ToArray();
        TransformInPlace(output, 0, cancellationToken);
        if (!HasHeader(output))
            throw new InvalidDataException("SmartCrypt profile did not reproduce the original FF ED FA CE marker; refusing to emit a file.");
        return output;
    }

    public void TransformInPlace(Span<byte> data, uint absoluteFileOffset = 0,
        CancellationToken cancellationToken = default)
    {
        for (int i = 0; i < data.Length; i++)
        {
            if ((i & 0xFFFF) == 0) cancellationToken.ThrowIfCancellationRequested();
            uint position = checked(absoluteFileOffset + (uint)i);
            byte value = data[i];
            value ^= _profile.Key6[position % 6];
            value ^= _profile.Key16[position & 0x0F];
            uint salt = (position ^ (_profile.UsesC5Variant ? 0xC5u : 0x65u)) * 0x93u;
            value ^= (byte)salt;
            data[i] = value;
        }
    }
}
