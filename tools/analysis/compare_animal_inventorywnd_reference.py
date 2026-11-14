from __future__ import annotations
import json, re, sys
from difflib import SequenceMatcher
from pathlib import Path

if len(sys.argv) != 4:
    raise SystemExit("Usage: compare_animal_inventorywnd_reference.py <animal.uc> <reference.uc> <output.json>")

FUNC = re.compile(r"(?im)^\s*function\s+(?:(?:[A-Za-z_]\w*)\s+)?([A-Za-z_]\w*)\s*\(([^)]*)\)\s*\{")

def extract(source):
    result = []
    for m in FUNC.finditer(source):
        start = source.find("{", m.start(), m.end())
        depth, quote, escaped = 0, None, False
        for i in range(start, len(source)):
            ch = source[i]
            if quote:
                if escaped: escaped = False
                elif ch == "\\": escaped = True
                elif ch == quote: quote = None
            elif ch in ("'", '"'): quote = ch
            elif ch == "{": depth += 1
            elif ch == "}":
                depth -= 1
                if depth == 0:
                    result.append({"name": m.group(1), "params": m.group(2).strip(),
                                   "signature": m.group(0).split("{")[0].strip(),
                                   "body": source[start + 1:i]})
                    break
    return result

def lex(s):
    s = re.sub(r"/\*.*?\*/|//[^\r\n]*", " ", s, flags=re.S)
    return re.findall(r'"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'|[A-Za-z_]\w*|\d+|==|!=|<=|>=|&&|\|\||\S', s)

animal_path, ref_path, out_path = map(Path, sys.argv[1:])
animal = extract(animal_path.read_text(encoding="utf-8-sig"))
reference = extract(ref_path.read_text(encoding="utf-8-sig"))
anonymous = [f for f in animal if re.fullmatch(r"function\d+|MethosRecoveredName_\d+", f["name"], re.I)]
matches = []
for target in anonymous:
    a = lex(target["body"])
    ranked = []
    for candidate in reference:
        b = lex(candidate["body"])
        score = SequenceMatcher(None, a, b, autojunk=False).ratio() if a and b else 0.0
        ranked.append({"candidate": candidate["name"], "signature": candidate["signature"],
                       "token_similarity": round(score, 4),
                       "same_parameter_count": len([x for x in target["params"].split(",") if x.strip()]) == len([x for x in candidate["params"].split(",") if x.strip()])})
    ranked.sort(key=lambda x: (x["token_similarity"], x["same_parameter_count"]), reverse=True)
    matches.append({"animal_function": target["name"], "animal_signature": target["signature"],
                    "body_token_count": len(a), "candidates": ranked[:5]})
payload = {"animal_source": str(animal_path), "reference_source": str(ref_path),
           "animal_function_count": len(animal), "reference_function_count": len(reference),
           "anonymous_function_count": len(anonymous),
           "method": "Token similarity after stripping comments/whitespace. Leads only; never proof of original identifier.",
           "matches": matches}
out_path.parent.mkdir(parents=True, exist_ok=True)
out_path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")
print(f"ANIMAL_INVENTORYWND_REFERENCE_COMPARE_OK anonymous={len(anonymous)} reference_functions={len(reference)}")
for item in matches:
    best = item["candidates"][0] if item["candidates"] else {}
    print(f"ANIMAL={item['animal_function']} BEST={best.get('candidate','none')} SCORE={best.get('token_similarity',0)} PARAMS={best.get('same_parameter_count',False)}")
