from __future__ import annotations

import hashlib
import json
import re
import sys
from difflib import SequenceMatcher
from pathlib import Path

from unrealscript_functions import extract_functions as extract

ANONYMOUS = re.compile(r"(?:function\d+|MethosRecoveredName_\d+)", re.I)


def read_source(path: Path) -> str:
    raw = path.read_bytes()
    if raw.startswith((bytes.fromhex("FFFE"), bytes.fromhex("FEFF"))):
        return raw.decode("utf-16")
    return raw.decode("utf-8-sig", errors="replace")


def tokens(source: str) -> list[str]:
    source = re.sub(r"/\*.*?\*/|//[^\r\n]*", " ", source, flags=re.S)
    return re.findall(
        r'"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'|[A-Za-z_]\w*|\d+|==|!=|<=|>=|&&|\|\||\S',
        source,
    )


def event_routes(functions: list[dict]) -> dict[int, str]:
    on_event = next((item for item in functions if item["name"].lower() == "onevent"), None)
    if not on_event:
        return {}
    return {
        int(match.group(1)): match.group(2)
        for match in re.finditer(
            r"(?im)^\s*case\s+(\d+)\s*:\s*([A-Za-z_]\w*)\s*\(",
            on_event["body"],
        )
    }


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
    animal_events = event_routes(animal)
    berserker_events = event_routes(berserker)
    silent_events = event_routes(silent)
    event_correspondence = []
    for event_id, handler in sorted(animal_events.items()):
        if not ANONYMOUS.fullmatch(handler):
            continue
        silent_handler = silent_events.get(event_id)
        event_correspondence.append({
            "event_id": event_id,
            "animal_handler": handler,
            "berserker_handler": berserker_events.get(event_id),
            "silent_dark_handler": silent_handler,
            "candidate_name_conflicts_with_existing_animal_function": bool(
                silent_handler and silent_handler.lower() in animal_names
            ),
        })
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
        "animal_event_routes": {str(key): value for key, value in sorted(animal_events.items())},
        "berserker_event_routes": {str(key): value for key, value in sorted(berserker_events.items())},
        "silent_dark_event_routes": {str(key): value for key, value in sorted(silent_events.items())},
        "anonymous_event_correspondence": event_correspondence,
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
    for entry in event_correspondence:
        print(
            f"EVENT={entry['event_id']} ANIMAL={entry['animal_handler']} "
            f"SILENT={entry.get('silent_dark_handler')} "
            f"NAME_CONFLICT={entry['candidate_name_conflicts_with_existing_animal_function']}"
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
