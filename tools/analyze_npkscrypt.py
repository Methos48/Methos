import hashlib, math, re, zipfile
from collections import Counter
from pathlib import Path
import pefile
from capstone import Cs, CS_ARCH_X86, CS_MODE_32

out=Path("npkscrypt-analysis"); out.mkdir(exist_ok=True)
with zipfile.ZipFile("reference-data/client/lineage2/system.zip") as z:
    names=[n for n in z.namelist() if n.lower().endswith("/npkscrypt.dll") or n.lower()=="npkscrypt.dll"]
    if not names: raise SystemExit("npkscrypt.dll missing from system.zip")
    raw=z.read(names[0])
    (out/"npkscrypt.dll").write_bytes(raw)
    for n in z.namelist():
        if n.lower().endswith("/ogg.dll") or n.lower()=="ogg.dll":
            (out/"ogg.dll").write_bytes(z.read(n)); break
pe=pefile.PE(data=raw, fast_load=False)
lines=[]
def log(s=""): lines.append(str(s))
log("FILE npkscrypt.dll")
log(f"SIZE={len(raw)} SHA256={hashlib.sha256(raw).hexdigest()}")
log(f"MACHINE=0x{pe.FILE_HEADER.Machine:04X} ENTRY_RVA=0x{pe.OPTIONAL_HEADER.AddressOfEntryPoint:X} IMAGEBASE=0x{pe.OPTIONAL_HEADER.ImageBase:X}")
for s in pe.sections:
    data=s.get_data()
    freq=Counter(data)
    ent=-sum((n/len(data))*math.log2(n/len(data)) for n in freq.values()) if data else 0
    log(f"SECTION {s.Name.rstrip(bytes([0])).decode(errors='replace')} RVA=0x{s.VirtualAddress:X} VSIZE=0x{s.Misc_VirtualSize:X} RAW=0x{s.PointerToRawData:X} RSIZE=0x{s.SizeOfRawData:X} FLAGS=0x{s.Characteristics:X} ENTROPY={ent:.4f}")
if hasattr(pe,"DIRECTORY_ENTRY_IMPORT"):
    for d in pe.DIRECTORY_ENTRY_IMPORT:
        log("IMPORT "+d.dll.decode(errors="replace"))
        for i in d.imports[:300]:
            log(f"  {i.name.decode(errors='replace') if i.name else 'ORDINAL_'+str(i.ordinal)} IAT_RVA=0x{i.address-pe.OPTIONAL_HEADER.ImageBase:X}")
if hasattr(pe,"DIRECTORY_ENTRY_EXPORT"):
    for s in pe.DIRECTORY_ENTRY_EXPORT.symbols:
        log(f"EXPORT {s.name.decode(errors='replace') if s.name else 'ordinal'} RVA=0x{s.address:X} FORWARDER={s.forwarder}")
for m in re.finditer(rb"[ -~]{5,}",raw):
    v=m.group().decode("ascii","replace")
    if any(x in v.lower() for x in ["crypt","decrypt","encrypt","sysstring","lineage","nprotect","npk","file","read","write","error","smart","key","version"]):
        log(f"STRING FILEOFF=0x{m.start():X} {v[:300]}")
md=Cs(CS_ARCH_X86,CS_MODE_32)
with (out/"npkscrypt-disassembly.txt").open("w",encoding="utf-8") as f:
    f.write(f"; imagebase=0x{pe.OPTIONAL_HEADER.ImageBase:X} entry=0x{pe.OPTIONAL_HEADER.AddressOfEntryPoint:X}\n")
    for s in pe.sections:
        if not (s.Characteristics & 0x20000000): continue
        data=s.get_data()
        f.write(f"\n; SECTION {s.Name!r} RVA=0x{s.VirtualAddress:X} SIZE=0x{len(data):X}\n")
        for ins in md.disasm(data,s.VirtualAddress):
            f.write(f"{ins.address:08X}: {ins.bytes.hex(' '):<24} {ins.mnemonic:<8} {ins.op_str}\n")
(out/"report.txt").write_text("\n".join(lines),encoding="utf-8")
print("\n".join(lines))
print("DISASSEMBLY="+str(out/"npkscrypt-disassembly.txt"))
