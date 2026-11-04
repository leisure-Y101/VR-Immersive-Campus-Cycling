# -*- coding: utf-8 -*-
from pathlib import Path
from docx import Document
from docx.oxml import OxmlElement
from docx.oxml.ns import qn

p = Path(r"D:\Users\admin\Desktop\FreeBike_工作报告_2026-10-29.docx")
doc = Document(p)

for table in doc.tables:
    for idx, row in enumerate(table.rows):
        trPr = row._tr.get_or_add_trPr()
        if trPr.find(qn("w:cantSplit")) is None:
            trPr.append(OxmlElement("w:cantSplit"))
        if idx == 0 and trPr.find(qn("w:tblHeader")) is None:
            trPr.append(OxmlElement("w:tblHeader"))

doc.save(p)
print("tables", len(doc.tables), "updated")
