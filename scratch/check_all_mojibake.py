import os
import fitz

def check_file_encoding(filepath):
    issues = []
    try:
        with open(filepath, 'rb') as f:
            raw = f.read()
    except Exception as e:
        return [f"Could not read file: {e}"]

    # Check for null bytes (UTF-16/binary)
    if b'\x00' in raw:
        issues.append("Contains NULL bytes (possibly UTF-16 or binary)")

    # Check decode as UTF-8
    try:
        text = raw.decode('utf-8')
    except UnicodeDecodeError as e:
        issues.append(f"UTF-8 decode error: {e}")
        try:
            text = raw.decode('cp932')
            issues.append("File is encoded in CP932/Shift-JIS instead of UTF-8!")
        except Exception:
            text = ""

    # Check for replacement char \ufffd
    if '\ufffd' in text:
        count = text.count('\ufffd')
        issues.append(f"Contains {count} replacement characters (\\ufffd)")

    # Common mojibake patterns from CP932 -> UTF-8 or UTF-8 -> CP932
    mojibake_indicators = ['縺', '繧', '繝', '・ｽ', 'ﾂ', 'ﾃ', '・']
    found_mojibake = [pat for pat in mojibake_indicators if pat in text]
    if found_mojibake:
        issues.append(f"Suspicious mojibake strings detected: {found_mojibake}")

    return issues

print("=== CHECKING MARKDOWN AND SOURCE FILES ===")
md_files = [
    "README.md",
    "docs/MANUAL.md",
    "docs/ARCHITECTURE.md",
    "docs/DESIGN_SPECIFICATION.md",
    "docs/COMMISSIONING.md",
    "docs/TROUBLESHOOTING.md",
    "docs/CONTRIBUTING.md",
    "docs/CHANGELOG.md",
    "docs/STANDARD_DEVELOPMENT_PLAN.md",
    "docs/compile_manual_pdf.py"
]

for mf in md_files:
    if os.path.exists(mf):
        issues = check_file_encoding(mf)
        if issues:
            print(f"[FAIL] {mf}:")
            for iss in issues:
                print(f"   - {iss}")
        else:
            print(f"[OK] {mf}")
    else:
        print(f"[NOT FOUND] {mf}")

print("\n=== CHECKING PDF PAGES FOR MOJIBAKE AND ISSUES ===")
pdf_path = "release/EclipseDataMiner_v3.0.0_Manual.pdf"
if os.path.exists(pdf_path):
    doc = fitz.open(pdf_path)
    print(f"PDF Total Pages: {len(doc)}")
    pdf_issues = []
    for page_idx, page in enumerate(doc):
        text = page.get_text()
        p_num = page_idx + 1
        if '\ufffd' in text:
            pdf_issues.append(f"Page {p_num}: Contains \\ufffd ({text.count(chr(0xfffd))} occurrences)")
        if '???' in text:
            pdf_issues.append(f"Page {p_num}: Contains '???'")
        if any(bad in text for bad in ['縺', '繧', '繝', '・ｽ', '&lt;div', '&lt;br']):
            pdf_issues.append(f"Page {p_num}: Contains mojibake characters or unrendered raw HTML tags")
    if pdf_issues:
        for pi in pdf_issues:
            print(f"   - {pi}")
    else:
        print("No obvious \\ufffd, mojibake, or unrendered HTML tags found in PDF text extraction.")
else:
    print(f"PDF not found at {pdf_path}")
