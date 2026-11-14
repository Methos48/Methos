from __future__ import annotations

import unittest

from unrealscript_functions import decode_source_bytes, extract_event_routes, extract_functions, lex_tokens, mask_comments


class UnrealScriptFunctionExtractionTests(unittest.TestCase):
    def test_source_decoder_supports_utf8_and_utf8_bom(self):
        self.assertEqual(decode_source_bytes("café".encode("utf-8")), "café")
        self.assertEqual(decode_source_bytes(b"\xef\xbb\xbf" + "café".encode("utf-8")), "café")

    def test_source_decoder_supports_utf16_bom_little_and_big_endian(self):
        text = "function Café() {}"
        self.assertEqual(decode_source_bytes(b"\xff\xfe" + text.encode("utf-16-le")), text)
        self.assertEqual(decode_source_bytes(b"\xfe\xff" + text.encode("utf-16-be")), text)

    def test_source_decoder_rejects_invalid_utf8_instead_of_replacing_bytes(self):
        with self.assertRaises(UnicodeDecodeError):
            decode_source_bytes(b"function Real() { \xff }")

    def test_ignores_function_declarations_and_braces_in_comments(self):
        source = """// function Fake() { }
function Real()
{
    /* } function AlsoFake() { */
    return;
}
"""
        functions = extract_functions(source)
        self.assertEqual([item["name"] for item in functions], ["Real"])

    def test_ignores_fake_declarations_inside_multiline_strings(self):
        source = """local string S;
S = "literal start
function Fake() {
    case 999: FakeHandler();
}
literal end";
function Real()
{
    return;
}
"""
        functions = extract_functions(source)
        self.assertEqual([item["name"] for item in functions], ["Real"])

    def test_event_routes_ignore_comments_and_multiline_literals(self):
        source = """switch(EventNum)
{
    case 100: RealHandler();
    /*
    case 200: CommentHandler();
    */
    local string S;
    S = "literal start
case 300: LiteralHandler();
literal end";
}
"""
        self.assertEqual(extract_event_routes(source), {100: "RealHandler"})

    def test_event_routes_reject_duplicate_event_ids(self):
        source = """case 100: FirstHandler();
case 100: SecondHandler();
"""
        with self.assertRaisesRegex(ValueError, "Duplicate event case 100"):
            extract_event_routes(source)

    def test_preserves_comment_markers_inside_strings(self):
        source = '''function Real()
{
    local string S;
    S = "http://example.invalid/{x}"; // trailing comment
}
'''
        functions = extract_functions(source)
        self.assertEqual(len(functions), 1)
        self.assertIn('http://example.invalid/{x}', functions[0]["body"])

    def test_tokenizer_preserves_comment_markers_inside_strings(self):
        tokens = lex_tokens('value = "http://host/path/*literal*/"; // real comment')
        self.assertIn('"http://host/path/*literal*/"', tokens)
        self.assertNotIn("comment", tokens)

    def test_escaped_quotes_and_unbalanced_braces_inside_strings(self):
        source = r'''function Real()
{
    local string S;
    S = "escaped \" } // still text /* still text */";
    S = 'single quote with // and }';
}
'''
        functions = extract_functions(source)
        self.assertEqual(len(functions), 1)
        tokens = lex_tokens(functions[0]["body"])
        self.assertIn('"escaped \" } // still text /* still text */"', tokens)
        self.assertIn("'single quote with // and }'", tokens)

    def test_masking_preserves_source_length_and_line_breaks(self):
        source = "a/* comment\nmore */b // tail\nc"
        masked = mask_comments(source)
        self.assertEqual(len(masked), len(source))
        self.assertEqual(masked.count("\n"), source.count("\n"))
        self.assertEqual(masked[0], "a")
        self.assertEqual(masked[-1], "c")


if __name__ == "__main__":
    unittest.main()
