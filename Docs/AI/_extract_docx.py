from docx import Document
from docx.table import Table
from docx.text.paragraph import Paragraph
from docx.oxml.ns import qn
import sys, os, io

buf = io.StringIO()
_stdout = sys.stdout
sys.stdout = buf

path = r"D:\freebikeapk\AI文档\VR全景视频自行车沉浸式健身制作教程(1).docx"
doc = Document(path)

print("=== CORE PROPERTIES ===")
cp = doc.core_properties
for attr in ["title","subject","author","last_modified_by","created","modified","revision","comments","keywords","category"]:
    try:
        print(f"{attr}: {getattr(cp, attr)}")
    except Exception as e:
        print(f"{attr}: ERR {e}")

print("\n=== BODY BLOCKS ===")
body = doc.element.body
idx = 0
for child in body.iterchildren():
    if child.tag == qn('w:p'):
        p = Paragraph(child, doc)
        text = p.text.strip()
        style = p.style.name if p.style is not None else ""
        if text:
            print(f"[P{idx}][{style}] {text}")
        else:
            print(f"[P{idx}][{style}] <empty>")
        idx += 1
    elif child.tag == qn('w:tbl'):
        t = Table(child, doc)
        print(f"[TABLE] rows={len(t.rows)} cols={len(t.columns)}")
        for r in t.rows:
            cells = []
            for c in r.cells:
                cells.append(c.text.replace("\n", " / ").strip())
            print(" | ".join(cells))
        idx += 1
    elif child.tag == qn('w:sectPr'):
        print("[SECTPR]")

print("\n=== INLINE SHAPES ===")
for i, shape in enumerate(doc.inline_shapes):
    print(i, shape.type, shape.width, shape.height)

print("\n=== MEDIA PARTS ===")
for part in doc.part.package.parts:
    ct = part.content_type
    if ct.startswith("image/") or ct.startswith("video/") or ct.startswith("audio/"):
        print(part.partname, ct, len(part.blob))

print("\n=== HEADERS/FOOTERS TEXT ===")
for si, section in enumerate(doc.sections):
    for name in ["header", "footer", "first_page_header", "first_page_footer", "even_page_header", "even_page_footer"]:
        try:
            obj = getattr(section, name)
            txt = "\n".join(p.text for p in obj.paragraphs if p.text.strip())
            if txt.strip():
                print(f"section{si} {name}: {txt}")
        except Exception:
            pass

print("\n=== COMMENTS ===")
try:
    from docx.oxml.ns import nsmap
    comments_part = None
    for rel in doc.part.rels.values():
        if "comments" in rel.reltype:
            comments_part = rel.target_part
    if comments_part is not None:
        print("comments part", comments_part.partname)
        print(comments_part.blob.decode("utf-8", errors="replace")[:5000])
    else:
        print("none")
except Exception as e:
    print("ERR", e)

sys.stdout = _stdout
with open(r"D:\freebikeapk\_docx_dump.txt", "w", encoding="utf-8") as f:
    f.write(buf.getvalue())

