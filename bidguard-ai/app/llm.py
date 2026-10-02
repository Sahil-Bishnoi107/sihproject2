"""Optional LLM-backed extraction with strict JSON output. Returns None on any problem so callers fall back to heuristics.

The LLM only drafts rules and extracts values; BidGuard's deterministic rule engine makes every decision.
"""

import json
import logging
import os

import anthropic

from app.models import EXEMPTION_KINDS, RULE_CATEGORIES, DraftRule, ExtractedField

log = logging.getLogger(__name__)

MODEL = os.getenv("BIDGUARD_LLM_MODEL", "")

_KEYS_DOC = (
    "gstin, pan, cin, udyam, epfo, turnover.fy<YYYY> (fiscal year ending YYYY, rupees), turnover.avg3y (rupees), "
    "experience.years, iso9001.number, iso9001.expiry (YYYY-MM-DD), oem.authorization ('yes'), emd.paid ('yes')"
)

_RULES_SYSTEM = f"""You convert Government e-Marketplace (GeM) tender documents into machine-checkable compliance rules.
Return one rule per distinct bidder requirement (eligibility, financial, technical, document).
Each rule's expressionJson is a JSON object in this grammar:
- leaf: {{"field": <key>, "op": <op>, "value"?: <number|string|array>, "source"?: "GST"|"MCA"|"Udyam"|"EPFO"}}
- ops: exists, ==, !=, >=, <=, >, <, in, before, after, verified (verified needs source)
- combinators: {{"all": [...]}}, {{"any": [...]}}, {{"not": {{...}}}}
- "$bidEndDate", "$estimatedValue", "$emdAmount" refer to tender values.
Use only these field keys: {_KEYS_DOC}.
Convert amounts to rupees (1 Lakh = 100000, 1 Crore = 10000000).
Set exemptionKind to "Emd" for EMD requirements, "PriorExperienceTurnover" for prior experience or turnover requirements, else "None".
sourceClause quotes the tender sentence; sourcePage is its 1-based page number. Skip requirements you cannot express with these keys."""

_ENTITIES_SYSTEM = f"""You extract facts from an Indian bidder's supporting documents (certificates, financial statements, declarations).
Use only these keys: {_KEYS_DOC}.
Copy identifiers exactly as printed. Convert amounts to plain rupee integers. Dates as YYYY-MM-DD.
page is the 1-based page where the value appears; confidence is 0-1. Omit facts that are not present - never guess."""

_RULES_SCHEMA = {
    "type": "object",
    "properties": {
        "rules": {
            "type": "array",
            "items": {
                "type": "object",
                "properties": {
                    "code": {"type": "string"},
                    "title": {"type": "string"},
                    "category": {"type": "string", "enum": list(RULE_CATEGORIES)},
                    "isMandatory": {"type": "boolean"},
                    "weight": {"type": "number"},
                    "sourceClause": {"type": "string"},
                    "sourcePage": {"type": "integer"},
                    "exemptionKind": {"type": "string", "enum": ["None", *EXEMPTION_KINDS]},
                    "expressionJson": {"type": "string"},
                },
                "required": ["code", "title", "category", "isMandatory", "weight", "sourceClause", "sourcePage",
                             "exemptionKind", "expressionJson"],
                "additionalProperties": False,
            },
        }
    },
    "required": ["rules"],
    "additionalProperties": False,
}

_ENTITIES_SCHEMA = {
    "type": "object",
    "properties": {
        "fields": {
            "type": "array",
            "items": {
                "type": "object",
                "properties": {
                    "key": {"type": "string"},
                    "value": {"type": "string"},
                    "confidence": {"type": "number"},
                    "page": {"type": "integer"},
                },
                "required": ["key", "value", "confidence", "page"],
                "additionalProperties": False,
            },
        }
    },
    "required": ["fields"],
    "additionalProperties": False,
}


def enabled() -> bool:
    return bool(os.getenv("ANTHROPIC_API_KEY")) and bool(MODEL)


def _paginate(pages: list[str]) -> str:
    return "\n\n".join(f"=== Page {i} ===\n{text}" for i, text in enumerate(pages, start=1))


def _call(system: str, user: str, schema: dict) -> dict | None:
    try:
        response = anthropic.Anthropic().beta.messages.create(
            model=MODEL,
            max_tokens=16000,
            system=system,
            messages=[{"role": "user", "content": user}],
            output_config={"effort": "medium", "format": {"type": "json_schema", "schema": schema}},
            betas=["server-side-fallback-2026-07-01"],
            fallbacks="default",
        )
    except anthropic.APIConnectionError as exc:
        log.warning("LLM unreachable, using heuristics: %s", exc)
        return None
    except anthropic.APIStatusError as exc:
        log.warning("LLM request failed (%s), using heuristics: %s", exc.status_code, exc.message)
        return None

    if response.stop_reason in ("refusal", "max_tokens"):
        log.warning("LLM stopped with %s, using heuristics", response.stop_reason)
        return None
    text = next((b.text for b in response.content if b.type == "text"), None)
    try:
        return json.loads(text) if text else None
    except json.JSONDecodeError:
        log.warning("LLM returned invalid JSON, using heuristics")
        return None


def extract_rules(pages: list[str]) -> list[DraftRule] | None:
    data = _call(_RULES_SYSTEM, "Tender document:\n\n" + _paginate(pages), _RULES_SCHEMA)
    if data is None:
        return None
    rules = []
    for item in data.get("rules", []):
        try:
            expression = json.loads(item["expressionJson"])
        except (json.JSONDecodeError, TypeError):
            continue
        if not isinstance(expression, dict):
            continue
        kind = item.get("exemptionKind")
        rules.append(DraftRule(item["code"], item["title"], item["category"], bool(item["isMandatory"]),
                               max(float(item["weight"]), 1.0), expression, item.get("sourceClause"),
                               item.get("sourcePage"), None if kind in (None, "None") else kind))
    return rules


def extract_entities(pages: list[str], doc_type: str | None) -> list[ExtractedField] | None:
    hint = f"Document type hint: {doc_type}\n\n" if doc_type else ""
    data = _call(_ENTITIES_SYSTEM, hint + "Document:\n\n" + _paginate(pages), _ENTITIES_SCHEMA)
    if data is None:
        return None
    return [ExtractedField(f["key"], str(f["value"]), min(max(float(f["confidence"]), 0.0), 1.0), f.get("page"))
            for f in data.get("fields", []) if f.get("key") and f.get("value")]
