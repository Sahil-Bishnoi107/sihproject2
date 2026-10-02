import fitz
import pytest
from fastapi.testclient import TestClient

from app.main import app


@pytest.fixture(autouse=True)
def no_llm(monkeypatch):
    monkeypatch.delenv("ANTHROPIC_API_KEY", raising=False)


client = TestClient(app)


def make_pdf(*pages: str) -> bytes:
    doc = fitz.open()
    for text in pages:
        page = doc.new_page()
        page.insert_text((72, 72), text, fontsize=11)
    data = doc.tobytes()
    doc.close()
    return data


def test_health_reports_capabilities():
    body = client.get("/health").json()
    assert body["status"] == "ok"
    assert body["llm"] is False


def test_extract_entities_returns_camel_case_fields_with_bbox():
    pdf = make_pdf("GSTIN: 27AAECS1234K1Z5\nUDYAM-MH-26-0012345")
    res = client.post("/extract-entities", files={"file": ("gst.pdf", pdf, "application/pdf")},
                      data={"doc_type": "gst_certificate"})
    assert res.status_code == 200
    body = res.json()
    assert body["ocrUsed"] is False
    gstin = next(f for f in body["fields"] if f["key"] == "gstin")
    assert gstin["value"] == "27AAECS1234K1Z5"
    assert gstin["page"] == 1
    assert len(gstin["bbox"]) == 4


def test_extract_rules_returns_expression_objects():
    pdf = make_pdf("Minimum Average Annual Turnover should be Rs. 50 Lakh.\nValid GST registration required.")
    body = client.post("/extract-rules", files={"file": ("tender.pdf", pdf, "application/pdf")}).json()
    codes = {r["code"]: r for r in body["rules"]}
    assert codes["FIN-TURNOVER"]["expression"]["value"] == 5000000
    assert codes["FIN-TURNOVER"]["isMandatory"] is True
    assert "ELG-GST" in codes


def test_unreadable_file_is_a_422():
    res = client.post("/extract-entities", files={"file": ("x.pdf", b"not a pdf", "application/pdf")})
    assert res.status_code == 422
