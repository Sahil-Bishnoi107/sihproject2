"""Offline entity extraction: regexes for Indian statutory identifiers and common bid-document facts."""

import re

from app.models import ExtractedField
from app.parsing import AMOUNT_PATTERN, parse_amount, parse_date

GSTIN = re.compile(r"\b\d{2}[A-Z]{5}\d{4}[A-Z][1-9A-Z]Z[0-9A-Z]\b")
CIN = re.compile(r"\b[LU]\d{5}[A-Z]{2}\d{4}[A-Z]{3}\d{6}\b")
UDYAM = re.compile(r"\bUDYAM-[A-Z]{2}-\d{2}-\d{7}\b")
PAN_LABELLED = re.compile(r"\bPAN\b\s*(?:No\.?|Number)?\s*[:\-]?\s*([A-Z]{5}\d{4}[A-Z])\b")
EPFO = re.compile(r"\b(?:EPFO?|PF)\s*(?:Establishment\s*)?(?:Code|ID|Reg(?:istration)?\.?\s*No\.?)\s*[:\-]?\s*([A-Za-z]{5}\d{7,10})\b",
                  re.IGNORECASE)
TURNOVER_YEAR = re.compile(r"(?:FY\s*)?(20\d{2})\s*[-–/]\s*(\d{2,4})[^\d\n]{0,40}?" + AMOUNT_PATTERN, re.IGNORECASE)
TURNOVER_AVG = re.compile(r"average\s+(?:annual\s+)?turnover[^\n]{0,80}?" + AMOUNT_PATTERN, re.IGNORECASE)
EXPERIENCE = (
    re.compile(r"(\d{1,2})\s*\+?\s*years?\s+of\s+(?:\w+\s+)?experience", re.IGNORECASE),
    re.compile(r"experience\s+of\s+(\d{1,2})\s*\+?\s*years?", re.IGNORECASE),
)
ISO_9001 = re.compile(r"ISO\s*9001", re.IGNORECASE)
CERT_NUMBER = re.compile(r"Certificate\s*(?:No\.?|Number)\s*[:\-]?\s*([A-Za-z0-9][A-Za-z0-9\-/]{2,})", re.IGNORECASE)
EXPIRY = re.compile(
    r"(?:Valid\s*(?:Until|Till|Upto|up\s*to)|Expiry\s*(?:Date)?|Expires\s*(?:on)?|Date\s+of\s+Expiry)\s*[:\-]?\s*"
    r"(\d{1,2}[/\-.]\d{1,2}[/\-.]\d{4}|\d{4}-\d{2}-\d{2}|\d{1,2}\s+[A-Za-z]{3,9},?\s+\d{4})",
    re.IGNORECASE)
OEM = re.compile(r"(?:OEM|Manufacturer'?s?)\s+Authori[sz]ation|Authori[sz]ation\s+(?:Form|Certificate|Letter)", re.IGNORECASE)
EMD = re.compile(r"\bEMD\b|Earnest\s+Money", re.IGNORECASE)
PAYMENT = re.compile(r"\b(?:paid|deposited|remitted|UTR|transaction|receipt)\b", re.IGNORECASE)


class _Collector:
    """Keeps the best (highest-confidence, then earliest) value per key."""

    def __init__(self) -> None:
        self.fields: dict[str, ExtractedField] = {}

    def add(self, key: str, value: str, confidence: float, page: int) -> None:
        current = self.fields.get(key)
        if current is None or confidence > current.confidence:
            self.fields[key] = ExtractedField(key, value, confidence, page)


def extract_entities(pages: list[str]) -> list[ExtractedField]:
    out = _Collector()
    turnover_by_year: dict[int, tuple[int, int]] = {}

    for index, text in enumerate(pages, start=1):
        if not text or not text.strip():
            continue

        for m in GSTIN.finditer(text):
            out.add("gstin", m.group(0), 0.95, index)
            out.add("pan", m.group(0)[2:12], 0.6, index)  # a GSTIN embeds the holder's PAN
        if m := PAN_LABELLED.search(text):
            out.add("pan", m.group(1), 0.9, index)
        if m := CIN.search(text):
            out.add("cin", m.group(0), 0.95, index)
        if m := UDYAM.search(text):
            out.add("udyam", m.group(0), 0.95, index)
        if m := EPFO.search(text):
            out.add("epfo", m.group(1).upper(), 0.85, index)

        for m in TURNOVER_YEAR.finditer(text):
            start, end = int(m.group(1)), m.group(2)
            year = int(end) if len(end) == 4 else (start // 100) * 100 + int(end)
            amount = parse_amount(m.group(3), m.group(4))
            if amount is not None:
                turnover_by_year[year] = (amount, index)
                out.add(f"turnover.fy{year}", str(amount), 0.85, index)
        if m := TURNOVER_AVG.search(text):
            amount = parse_amount(m.group(1), m.group(2))
            if amount is not None:
                out.add("turnover.avg3y", str(amount), 0.85, index)

        for pattern in EXPERIENCE:
            if m := pattern.search(text):
                out.add("experience.years", m.group(1), 0.75, index)
                break

        if ISO_9001.search(text):
            if m := CERT_NUMBER.search(text):
                out.add("iso9001.number", m.group(1).upper(), 0.85, index)
            if (m := EXPIRY.search(text)) and (iso := parse_date(m.group(1))):
                out.add("iso9001.expiry", iso, 0.85, index)

        if OEM.search(text):
            out.add("oem.authorization", "yes", 0.7, index)

        for line in text.splitlines():
            if EMD.search(line) and PAYMENT.search(line):
                out.add("emd.paid", "yes", 0.7, index)
                break

    # Derive the three-year average when the document lists yearly turnover but no average.
    if "turnover.avg3y" not in out.fields and len(turnover_by_year) >= 3:
        latest = sorted(turnover_by_year)[-3:]
        average = sum(turnover_by_year[y][0] for y in latest) // 3
        out.add("turnover.avg3y", str(average), 0.8, turnover_by_year[latest[-1]][1])

    return list(out.fields.values())
