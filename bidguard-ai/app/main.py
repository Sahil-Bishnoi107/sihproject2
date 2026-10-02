"""bidguard-ai: stateless document-intelligence sidecar for the BidGuard .NET backend."""

import logging

from fastapi import FastAPI, File, Form, HTTPException, UploadFile

from app import llm
from app.heuristics.entities import extract_entities as heuristic_entities
from app.heuristics.rules import extract_rules as heuristic_rules
from app.text_extraction import UnreadableDocument, extract_text, tesseract_available

logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(name)s: %(message)s")
log = logging.getLogger("bidguard-ai")

MAX_BYTES = 25 * 1024 * 1024

app = FastAPI(title="BidGuard AI sidecar", version="1.0.0")


async def _read(file: UploadFile):
    data = await file.read()
    if not data:
        raise HTTPException(422, "Empty file.")
    if len(data) > MAX_BYTES:
        raise HTTPException(413, "File exceeds 25 MB.")
    try:
        return extract_text(data, file.filename or "document.pdf")
    except UnreadableDocument as exc:
        raise HTTPException(422, str(exc)) from exc


@app.get("/health")
def health() -> dict:
    return {"status": "ok", "llm": llm.enabled(), "llmModel": llm.MODEL if llm.enabled() else None, "ocr": tesseract_available()}


@app.post("/extract-rules")
async def extract_rules(file: UploadFile = File(...)) -> dict:
    doc = await _read(file)
    try:
        rules = llm.extract_rules(doc.pages) if llm.enabled() else None
        engine = "llm"
        if not rules:
            rules, engine = heuristic_rules(doc.pages), "heuristic"
        log.info("extract-rules %s: %d rule(s) via %s, ocr=%s", file.filename, len(rules), engine, doc.ocr_used)
        return {"engine": engine, "ocrUsed": doc.ocr_used, "rules": [r.to_json() for r in rules]}
    finally:
        doc.close()


@app.post("/extract-entities")
async def extract_entities(file: UploadFile = File(...), doc_type: str | None = Form(None)) -> dict:
    doc = await _read(file)
    try:
        fields = llm.extract_entities(doc.pages, doc_type) if llm.enabled() else None
        engine = "llm"
        if not fields:
            fields, engine = heuristic_entities(doc.pages), "heuristic"
        for f in fields:
            f.bbox = doc.locate(f.page, f.value)
        log.info("extract-entities %s: %d field(s) via %s, ocr=%s", file.filename, len(fields), engine, doc.ocr_used)
        return {"engine": engine, "ocrUsed": doc.ocr_used, "fields": [f.to_json() for f in fields]}
    finally:
        doc.close()
