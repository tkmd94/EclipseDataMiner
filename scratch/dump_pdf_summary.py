import fitz

doc = fitz.open("release/EclipseDataMiner_v3.0.0_Manual.pdf")
print(f"Total pages: {len(doc)}")

for i in range(len(doc)):
    page = doc[i]
    text = page.get_text()
    first_few_lines = [line.strip() for line in text.split('\n') if line.strip()][:5]
    print(f"--- Page {i+1} ---")
    print(" | ".join(first_few_lines))
