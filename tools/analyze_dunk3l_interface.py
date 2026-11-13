from __future__ import annotations
import hashlib, math, re, subprocess, sys, tempfile, zlib, gzip, lzma, bz2
from collections import Counter
from pathlib import Path

ENGINE = Path(sys.argv[1])
FILES = [Path("reference-data/client/lineage2/interface.u"), Path("reference-data/client/lineage2/interface.xdat"), Path("reference-data/packages/dunk3l/Dunk3lFX_ByBALENCIAGA.u")]
PROTOCOLS = [0, 111, 121, 413]
UNREAL_MAGIC = bytes.fromhex("C1 83 2A 9E")

def entropy(data: bytes) -> float:
    if not data: return 0.0
    counts = Counter(data)
    return -sum((n/len(data))*math.log2(n/len(data)) for n in counts.values())

def summarize(data: bytes):
    return {
        "size": len(data),
        "sha256": hashlib.sha256(data).hexdigest(),
        "entropy": round(entropy(data), 5),
        "prefix": data[:48].hex(" "),
        "unreal_package_magic": data.startswith(UNREAL_MAGIC),
        "xdat_count_candidate": int.from_bytes(data[:4], "little") if len(data)>=4 else None,
        "xdat_count_plausible": 1 <= int.from_bytes(data[:4], "little") <= 10000 if len(data)>=4 else False,
    }

def report(label: str, data: bytes):
    print(f"\n[{label}]")
    print(summarize(data))
    for marker in (b"Lineage2Ver", "Lineage2Ver".encode("utf-16le"), UNREAL_MAGIC,
                   b"Unreal", b"Core", b"Engine", b"Class", b"Package",
                   b"SmartCrypt", b"Dunk3l", b"<?xml"):
        offsets=[]; start=0
        while True:
            pos=data.find(marker,start)
            if pos<0: break
            offsets.append(pos); start=pos+1
            if len(offsets)>=10: break
        if offsets: print("marker",repr(marker),"offsets",offsets)
    strings = re.findall(rb"[\x20-\x7e]{8,}", data)
    print("printable_strings_count:",len(strings))
    for item in strings[:12]:
        print("string:",item[:120].decode("ascii","replace"))

def try_decompressions(label: str, data: bytes):
    for off in range(min(64,len(data))):
        chunk=data[off:]
        for name,fn in (("zlib",zlib.decompress),("gzip",gzip.decompress),
                        ("lzma",lzma.decompress),("bz2",bz2.decompress)):
            try:
                out=fn(chunk)
                if out: print(f"[DECOMPRESS_OK] {label} offset={off} codec={name} result={summarize(out)}")
            except Exception:
                pass

def try_l2encdec(label: str, source: Path, data: bytes):
    for protocol in PROTOCOLS:
        with tempfile.TemporaryDirectory(prefix="dunk3l_probe_") as td:
            output=Path(td)/(source.name+f".p{protocol}.decoded")
            cmd=[str(ENGINE),"-c","decode"]
            if protocol: cmd += ["-p",str(protocol)]
            cmd += ["-o",str(output),str(source.resolve())]
            try:
                p=subprocess.run(cmd,capture_output=True,text=True,timeout=90,check=False)
                print(f"[L2ENCDEC] {label} protocol={protocol} rc={p.returncode}")
                if p.stdout.strip(): print("stdout:",p.stdout[-500:])
                if p.stderr.strip(): print("stderr:",p.stderr[-500:])
                if output.is_file():
                    decoded=output.read_bytes()
                    print("[L2ENCDEC_OUTPUT]",label,"protocol",protocol,
                          "same_as_input",decoded==data,
                          "equals_input_28_to_minus20",decoded==data[28:-20],
                          "equals_input_8_to_minus20",decoded==data[8:-20],
                          "summary",summarize(decoded))
                    if decoded.startswith(UNREAL_MAGIC) or (len(decoded)>=4 and 1<=int.from_bytes(decoded[:4],"little")<=10000):
                        report(label+f" plausible decode p{protocol}",decoded)
            except Exception as exc:
                print(f"[L2ENCDEC_ERROR] {label} protocol={protocol}: {type(exc).__name__}: {exc}")

for path in FILES:
    if not path.is_file():
        print("[MISSING]",path)
        continue
    raw=path.read_bytes()
    report(path.name,raw)
    try_decompressions(path.name,raw)
    try_l2encdec(path.name,path,raw)

print("\n[PAIRWISE_COMPARISON]")
present=[(p,p.read_bytes()) for p in FILES if p.is_file()]
for i,(pa,a) in enumerate(present):
    for pb,b in present[i+1:]:
        n=min(len(a),len(b))
        same=sum(x==y for x,y in zip(a[:n],b[:n]))
        print(pa.name,"vs",pb.name,"same_prefix_bytes",same,"of",n,
              "xor_first_32",bytes(x^y for x,y in zip(a[:32],b[:32])).hex(" "))
