import sys
from pathlib import Path
import lief
src=Path(sys.argv[1]); out=Path(sys.argv[2])
binary=lief.PE.parse(str(src))
if binary is None: raise SystemExit("LIEF could not parse ogg.dll")
names=[x.name.lower() for x in binary.imports]
if "smartcapture.dll" not in names:
    imp=binary.add_import("SmartCapture.dll",0)
    imp.add_entry("load")
else:
    imp=next(x for x in binary.imports if x.name.lower()=="smartcapture.dll")
    if not any(e.name=="load" for e in imp.entries): imp.add_entry("load")
config=lief.PE.Builder.config_t()
config.imports=True
binary.write(str(out),config)
check=lief.PE.parse(str(out))
ordered=[x.name for x in check.imports]
print("IMPORT_ORDER="+ " -> ".join(ordered))
low=[x.lower() for x in ordered]
if "smartcapture.dll" not in low or "npkscrypt.dll" not in low:
    raise SystemExit("Required SmartCapture/npkscrypt import missing")
if low.index("smartcapture.dll") >= low.index("npkscrypt.dll"):
    raise SystemExit("SmartCapture must precede npkscrypt")
print("PASS: SmartCapture.dll!load precedes npkscrypt.dll")
