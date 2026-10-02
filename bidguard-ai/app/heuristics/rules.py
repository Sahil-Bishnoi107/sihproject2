"""Offline Compliance-as-Code drafting: recognise standard GeM eligibility clauses and emit rule expressions."""

import re
from collections.abc import Callable
from dataclasses import dataclass

from app.models import DraftRule
from app.parsing import AMOUNT_PATTERN, parse_amount

_AMOUNT = re.compile(AMOUNT_PATTERN, re.IGNORECASE)


@dataclass(frozen=True)
class _Clause:
    code: str
    matches: Callable[[str], re.Match | bool | None]
    build: Callable[[str], DraftRule | None]


def _turnover(line: str) -> DraftRule | None:
    m = _AMOUNT.search(line)
    amount = parse_amount(m.group(1), m.group(2)) if m else None
    if amount is None:
        return None
    return DraftRule("FIN-TURNOVER", "Minimum average annual turnover (last 3 years)", "Financial", True, 3,
                     {"field": "turnover.avg3y", "op": ">=", "value": amount}, exemption_kind="PriorExperienceTurnover")


_YEARS = re.compile(r"(\d{1,2})\s*(?:\(\w+\)\s*)?\+?\s*years?\s+(?:of\s+)?(?:\w+\s+){0,2}experience", re.IGNORECASE)


def _experience(line: str) -> DraftRule | None:
    m = _YEARS.search(line)
    if not m:
        return None
    years = int(m.group(1))
    return DraftRule("ELG-EXPERIENCE", f"At least {years} years of relevant experience", "Eligibility", True, 2,
                     {"field": "experience.years", "op": ">=", "value": years}, exemption_kind="PriorExperienceTurnover")


def _gst(_: str) -> DraftRule:
    return DraftRule("ELG-GST", "Valid GST registration, verified with GSTN", "Eligibility", True, 1,
                     {"all": [{"field": "gstin", "op": "exists"}, {"field": "gstin", "op": "verified", "source": "GST"}]})


def _iso(_: str) -> DraftRule:
    return DraftRule("TECH-ISO9001", "Valid ISO 9001 certificate through the bid end date", "Technical", True, 2,
                     {"all": [{"field": "iso9001.number", "op": "exists"},
                              {"field": "iso9001.expiry", "op": "after", "value": "$bidEndDate"}]})


def _oem(_: str) -> DraftRule:
    return DraftRule("TECH-OEM", "OEM / manufacturer authorization", "Technical", True, 1,
                     {"field": "oem.authorization", "op": "exists"})


def _emd(_: str) -> DraftRule:
    return DraftRule("FIN-EMD", "Earnest Money Deposit paid", "Financial", True, 1,
                     {"field": "emd.paid", "op": "exists"}, exemption_kind="Emd")


def _epfo(_: str) -> DraftRule:
    return DraftRule("ELG-EPFO", "EPFO registration verified", "Eligibility", False, 1,
                     {"field": "epfo", "op": "verified", "source": "EPFO"})


_CLAUSES = (
    _Clause("FIN-TURNOVER", lambda l: re.search(r"average\s+annual\s+(?:financial\s+)?turnover", l, re.IGNORECASE), _turnover),
    _Clause("ELG-EXPERIENCE", lambda l: _YEARS.search(l), _experience),
    _Clause("ELG-GST", lambda l: re.search(r"\bGST(?:IN)?\b", l) and re.search(r"registration|GSTIN", l, re.IGNORECASE), _gst),
    _Clause("TECH-ISO9001", lambda l: re.search(r"ISO\s*9001", l, re.IGNORECASE), _iso),
    _Clause("TECH-OEM", lambda l: re.search(r"\bOEM\b|Manufacturer", l, re.IGNORECASE)
            and re.search(r"Authori[sz]ation", l, re.IGNORECASE), _oem),
    _Clause("FIN-EMD", lambda l: re.search(r"\bEMD\b|Earnest\s+Money", l, re.IGNORECASE), _emd),
    _Clause("ELG-EPFO", lambda l: re.search(r"\bEPFO?\b|Provident\s+Fund", l, re.IGNORECASE), _epfo),
)


# Sentence boundary: end punctuation followed by a capital or digit, but not after abbreviations such as "Rs." or "No.".
_SENTENCE_END = re.compile(r"(?<!\bRs\.)(?<!\bNo\.)(?<!\bSr\.)(?<=[.!?;:])\s+(?=[A-Z0-9])")


def _clauses(text: str) -> list[str]:
    """PDF text wraps mid-sentence, so rejoin lines and split on sentence boundaries instead."""
    joined = re.sub(r"\s*\n\s*", " ", text or "").strip()
    return [c.strip() for c in _SENTENCE_END.split(joined) if c.strip()]


def extract_rules(pages: list[str]) -> list[DraftRule]:
    """First clause of each known kind wins; the sentence it came from becomes the rule's source clause."""
    found: dict[str, DraftRule] = {}
    for page_no, text in enumerate(pages, start=1):
        for line in _clauses(text):
            for clause in _CLAUSES:
                if clause.code in found or not clause.matches(line):
                    continue
                rule = clause.build(line)
                if rule is not None:
                    rule.source_clause = line[:300]
                    rule.source_page = page_no
                    found[clause.code] = rule
    return list(found.values())
