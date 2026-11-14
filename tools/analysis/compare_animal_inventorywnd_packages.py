from __future__ import annotations

import hashlib
import json
import re
import sys
from difflib import SequenceMatcher
from pathlib import Path

FUNCTION = re.compile(
    r"(?im)^\s*function\s+(?:(?:[A-Za-z_]\w*)\s+)?([A-Za-z_]\w*)\s*\(([^)]*)\)\s*\{"
)
ANONYMOUS = re.compile(r"(?:function\d+|MethosRecoveredName_\d+)", re.I)


def read_source(path: Path) -> str:
    raw = path.read_bytes()
    if raw.startswith((bytes.fromhex("FFFE"), bytes.fromhex("FEFF"))):
        return raw.decode("utf-16")
    return raw.decode("utf-8-sig", errors="replace")


def extract(source: str) -> list[dict]:
    result = []
    for match in FUNCTION.finditer(source):
        start = source.find("{", match.start(), match.end())
        depth, quote, escaped = 0, None, False
        for index in range(start, len(source)):
            char = source[index]
            if quote:
                if escaped:
                    escaped = False
                elif char == "\\":
                    escaped = True
                elif char == quote:
                    quote = None
            elif char in ("'", '"'):
                quote = char
            elif char == "{":
                depth += 1
            elif char == "}":
                depth -= 1
                if depth == 0:
                    result.append({
                        "name": match.group(1),
                        "params": match.group(2).strip(),
                        "signature": match.group(0).split("{")[0].strip(),
                        "body": source[start + 1:index],
                    })
                    break
    return result


def tokens(source: str) -> list[str]:
    source = re.sub(r"/\*.*?\*/|//[^\r\n]*", " ", source, flags=re.S)
    return re.findall(
        r'"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'|[A-Za-z_]\w*|\d+|==|!=|<=|>=|&&|\|\||\S',
        source,
    )


def normalized_hash(source: str) -> str:
    normalized = " ".join(tokens(source))
    return hashlib.sha256(normalized.encode("utf-8")).hexdigest()


def parameter_count(params: str) -> int:
    return len([part for part in params.split(",") if part.strip()])


def main() -> int:
    if len(sys.argv) != 5:
        raise SystemExit(
            "Usage: compare_animal_inventorywnd_packages.py "
            "<animal.uc> <berserker.uc> <silent-dark.uc> <output.json>"
        )

    animal_path, berserker_path, silent_path, output_path = map(Path, sys.argv[1:])
    animal = extract(read_source(animal_path))
    berserker = extract(read_source(berserker_path))
    silent = extract(read_source(silent_path))

    berserker_by_name: dict[str, list[dict]] = {}
    for function in berserker:
        berserker_by_name.setdefault(function["name"].lower(), []).append(function)

    cross_build = []
    for function in animal:
        matches = berserker_by_name.get(function["name"].lower(), [])
        if not matches:
            continue
        source_hash = normalized_hash(function["body"])
        ranked = []
        for candidate in matches:
            candidate_hash = normalized_hash(candidate["body"])
            ranked.append({
                "signature": candidate["signature"],
                "body_exact_after_comment_whitespace_normalization": source_hash == candidate_hash,
                "animal_body_sha256": source_hash,
                "berserker_body_sha256": candidate_hash,
            })
        cross_build.append({
            "animal_signature": function["signature"],
            "berserker_matches": ranked,
        })

    silent_candidates = []
    animal_names = {function["name"].lower() for function in animal}
    for function in animal:
        if not ANONYMOUS.fullmatch(function["name"]):
            continue
        source_tokens = tokens(function["body"])
        ranked = []
        for candidate in silent:
            candidate_tokens = tokens(candidate["body"])
            score = (
                SequenceMatcher(None, source_tokens, candidate_tokens, autojunk=False).ratio()
                if source_tokens and candidate_tokens else 0.0
            )
            same_params = parameter_count(function["params"]) == parameter_count(candidate["params"])
            conflict = (
                candidate["name"].lower() in animal_names
                and candidate["name"].lower() != function["name"].lower()
            )
            ranked.append({
                "candidate": candidate["name"],
                "signature": candidate["signature"],
                "token_similarity": round(score, 4),
                "same_parameter_count": same_params,
                "name_already_exists_in_animal_inventorywnd": conflict,
            })
        ranked.sort(
            key=lambda item: (item["token_similarity"], item["same_parameter_count"]),
            reverse=True,
        )
        silent_candidates.append({
            "animal_function": function["name"],
            "animal_signature": function["signature"],
            "body_token_count": len(source_tokens),
            "silent_dark_candidates": ranked[:5],
        })

    payload = {
        "animal_source": str(animal_path),
        "berserker_source": str(berserker_path),
        "silent_dark_source": str(silent_path),
        "animal_function_count": len(animal),
        "berserker_function_count": len(berserker),
        "silent_dark_function_count": len(silent),
        "method": (
            "Same-name body hashes after comment/whitespace normalization; "
            "anonymous Animal functions ranked against Silent Dark by token similarity. "
            "Similarity is a lead, not proof of original identifiers."
        ),
        "animal_vs_berserker_same_name": cross_build,
        "animal_anonymous_vs_silent_dark": silent_candidates,
    }
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")

    exact = sum(
        1
        for entry in cross_build
        for candidate in entry["berserker_matches"]
        if candidate["body_exact_after_comment_whitespace_normalization"]
    )
    print(
        "ANIMAL_INVENTORYWND_PACKAGE_COMPARE_OK "
        f"animal={len(animal)} berserker={len(berserker)} silent_dark={len(silent)} "
        f"same_name_exact={exact} anonymous={len(silent_candidates)}"
    )
    for entry in silent_candidates:
        best = entry["silent_dark_candidates"][0] if entry["silent_dark_candidates"] else {}
        print(
            f"ANIMAL={entry['animal_function']} "
            f"SILENT_BEST={best.get('candidate', 'none')} "
            f"SCORE={best.get('token_similarity', 0)} "
            f"NAME_CONFLICT={best.get('name_already_exists_in_animal_inventorywnd', False)}"
        )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
