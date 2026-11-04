from docx import Document
from docx.oxml.ns import qn
from PIL import Image, ImageDraw, ImageFont
import os, io

path = r"D:\freebikeapk\AI文档\VR全景视频自行车沉浸式健身制作教程(1).docx"
outdir = r"D:\freebikeapk\_docx_media"
os.makedirs(outdir, exist_ok=True)
doc = Document(path)

# Build ordered body paragraph -> image list
items = []
img_no = 0
for p_idx, p in enumerate(doc.paragraphs):
    blips = p._element.findall('.//' + qn('a:blip'))
    for blip in blips:
        rid = blip.get(qn('r:embed'))
        if not rid:
            continue
        part = doc.part.related_parts[rid]
        img_no += 1
        ext = part.partname.ext
        fn = f"{img_no:02d}{ext}"
        fp = os.path.join(outdir, fn)
        with open(fp, "wb") as f:
            f.write(part.blob)
        alt = ""
        docPr = p._element.find('.//' + qn('wp:docPr'))
        if docPr is not None:
            alt = docPr.get('descr') or docPr.get('name') or ""
        # caption = nearest previous non-empty text
        prev = ""
        for j in range(p_idx - 1, -1, -1):
            if doc.paragraphs[j].text.strip():
                prev = doc.paragraphs[j].text.strip()
                break
        items.append((img_no, fn, p_idx, alt, prev))

for it in items:
    print(it)

# contact sheet
thumb_w = 640
cols = 4
pad = 30
label_h = 90
rows = (len(items) + cols - 1) // cols
cell_w = thumb_w + pad
cell_h = 480 + label_h
sheet = Image.new("RGB", (cols * cell_w, rows * cell_h), "white")
draw = ImageDraw.Draw(sheet)
try:
    font = ImageFont.truetype("C:/Windows/Fonts/msyh.ttc", 20)
except Exception:
    font = ImageFont.load_default()

for idx, (n, fn, p_idx, alt, prev) in enumerate(items):
    im = Image.open(os.path.join(outdir, fn)).convert("RGB")
    im.thumbnail((thumb_w, 460))
    x = (idx % cols) * cell_w + pad // 2
    y = (idx // cols) * cell_h + 10
    sheet.paste(im, (x, y))
    label = f"IMG{n:02d} p{p_idx}"
    draw.text((x, y + 465), label, fill="black", font=font)
    # wrap caption
    cap = prev[:38]
    draw.text((x, y + 490), cap, fill="#333333", font=font)

sheet_path = r"D:\freebikeapk\_docx_media\contact_sheet.jpg"
sheet.save(sheet_path, quality=90)
print("SHEET", sheet_path, sheet.size)
