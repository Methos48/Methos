from __future__ import annotations

import unittest

from unrealscript_functions import extract_functions, mask_comments


class UnrealScriptFunctionExtractionTests(unittest.TestCase):
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

    def test_masking_preserves_source_length_and_line_breaks(self):
        source = "a/* comment\nmore */b // tail\nc"
        masked = mask_comments(source)
        self.assertEqual(len(masked), len(source))
        self.assertEqual(masked.count("\n"), source.count("\n"))
        self.assertEqual(masked[0], "a")
        self.assertEqual(masked[-1], "c")


if __name__ == "__main__":
    unittest.main()
