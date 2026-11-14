from __future__ import annotations

import re


FUNCTION = re.compile(
    r"(?im)^\s*function\s+(?:(?:[A-Za-z_]\w*)\s+)?([A-Za-z_]\w*)\s*\(([^)]*)\)\s*\{"
)


def mask_comments(source: str) -> str:
    """Replace comments with spaces while preserving offsets and newlines."""
    chars = list(source)
    i = 0
    state = "code"
    while i < len(source):
        ch = source[i]
        nxt = source[i + 1] if i + 1 < len(source) else ""
        if state == "code":
            if ch == "'":
                state = "single"
            elif ch == '"':
                state = "double"
            elif ch == "/" and nxt == "/":
                chars[i] = chars[i + 1] = " "
                i += 2
                state = "line"
                continue
            elif ch == "/" and nxt == "*":
                chars[i] = chars[i + 1] = " "
                i += 2
                state = "block"
                continue
        elif state in ("single", "double"):
            if ch == "\\":
                i += 2
                continue
            if (state == "single" and ch == "'") or (state == "double" and ch == '"'):
                state = "code"
        elif state == "line":
            if ch in "\r\n":
                state = "code"
            else:
                chars[i] = " "
        elif state == "block":
            if ch == "*" and nxt == "/":
                chars[i] = chars[i + 1] = " "
                i += 2
                state = "code"
                continue
            if ch not in "\r\n":
                chars[i] = " "
        i += 1
    return "".join(chars)



def mask_code(source: str) -> str:
    """Mask comments and string literals, preserving offsets and line breaks."""
    chars = list(mask_comments(source))
    scan = "".join(chars)
    state = "code"
    i = 0
    while i < len(scan):
        ch = scan[i]
        if state == "code":
            if ch in ("'", '"'):
                state = ch
                chars[i] = " "
        else:
            if ch == "\\":
                chars[i] = " "
                if i + 1 < len(scan) and scan[i + 1] not in "\r\n":
                    chars[i + 1] = " "
                i += 2
                continue
            if ch == state:
                chars[i] = " "
                state = "code"
            elif ch not in "\r\n":
                chars[i] = " "
        i += 1
    return "".join(chars)


def extract_functions(source: str) -> list[dict[str, str]]:
    masked = mask_comments(source)
    declaration_view = mask_code(source)
    result: list[dict[str, str]] = []
    for match in FUNCTION.finditer(declaration_view):
        opening = masked.find("{", match.start(), match.end())
        depth = 0
        quote = None
        escaped = False
        for index in range(opening, len(masked)):
            char = masked[index]
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
                        "signature": source[match.start():opening].strip(),
                        "body": source[opening + 1:index],
                    })
                    break
    return result



def lex_tokens(source: str) -> list[str]:
    """Tokenize code after masking comments without damaging quoted strings."""
    masked = mask_comments(source)
    return re.findall(
        r'"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'|[A-Za-z_]\w*|\d+|==|!=|<=|>=|&&|\|\||\S',
        masked,
    )
