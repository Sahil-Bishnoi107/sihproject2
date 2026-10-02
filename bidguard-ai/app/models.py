"""Plain result types shared by the heuristic and LLM extractors."""

from dataclasses import dataclass, field
from typing import Any

# Canonical fact keys shared with the .NET rule engine (FactSetBuilder) and tender rules.
FIELD_KEYS = (
    "gstin", "pan", "cin", "udyam", "epfo",
    "turnover.fyYYYY", "turnover.avg3y", "experience.years",
    "iso9001.number", "iso9001.expiry", "oem.authorization", "emd.paid",
)

RULE_CATEGORIES = ("Eligibility", "Technical", "Financial", "Document")
EXEMPTION_KINDS = ("Emd", "PriorExperienceTurnover")


@dataclass
class ExtractedField:
    key: str
    value: str
    confidence: float
    page: int | None
    bbox: list[float] | None = None

    def to_json(self) -> dict[str, Any]:
        return {"key": self.key, "value": self.value, "confidence": round(self.confidence, 3),
                "page": self.page, "bbox": self.bbox}


@dataclass
class DraftRule:
    code: str
    title: str
    category: str
    is_mandatory: bool
    weight: float
    expression: dict[str, Any]
    source_clause: str | None = None
    source_page: int | None = None
    exemption_kind: str | None = None
    extra: dict[str, Any] = field(default_factory=dict)

    def to_json(self) -> dict[str, Any]:
        return {
            "code": self.code, "title": self.title, "category": self.category, "isMandatory": self.is_mandatory,
            "weight": self.weight, "sourceClause": self.source_clause, "sourcePage": self.source_page,
            "exemptionKind": self.exemption_kind, "expression": self.expression,
        }
