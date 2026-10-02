import json

from app.heuristics.rules import extract_rules

TENDER_PAGE_1 = """Bid Document - GEM/2026/B/5551234
Supply of 200 Laptops
Eligibility Criteria:
1. Minimum Average Annual Turnover of the bidder during the last three years should be Rs. 50 Lakh.
2. The bidder must have at least 3 years of experience in supply of similar goods.
3. The bidder must hold a valid GST registration (GSTIN).
"""

TENDER_PAGE_2 = """Technical Requirements:
4. The bidder shall possess a valid ISO 9001 certificate.
5. OEM Authorization certificate from the manufacturer is mandatory.
6. Earnest Money Deposit (EMD) of Rs 2,40,000 is required. MSEs registered under Udyam are exempted.
"""


def rules_by_code():
    return {r.code: r for r in extract_rules([TENDER_PAGE_1, TENDER_PAGE_2])}


def test_turnover_rule_with_amount_and_exemption():
    rule = rules_by_code()["FIN-TURNOVER"]
    assert rule.category == "Financial"
    assert rule.is_mandatory
    assert rule.exemption_kind == "PriorExperienceTurnover"
    assert rule.expression == {"field": "turnover.avg3y", "op": ">=", "value": 5000000}
    assert rule.source_page == 1
    assert "Turnover" in rule.source_clause


def test_experience_rule():
    rule = rules_by_code()["ELG-EXPERIENCE"]
    assert rule.expression == {"field": "experience.years", "op": ">=", "value": 3}


def test_gst_iso_oem_and_emd_rules():
    rules = rules_by_code()
    assert rules["ELG-GST"].expression["all"][1] == {"field": "gstin", "op": "verified", "source": "GST"}
    assert rules["TECH-ISO9001"].source_page == 2
    assert rules["TECH-OEM"].expression == {"field": "oem.authorization", "op": "exists"}
    assert rules["FIN-EMD"].exemption_kind == "Emd"


def test_expressions_are_json_serialisable():
    for rule in extract_rules([TENDER_PAGE_1, TENDER_PAGE_2]):
        json.dumps(rule.expression)


def test_clause_wrapped_across_lines_keeps_its_unit():
    page = ("1. Minimum Average Annual Turnover of the bidder during the last three years should be Rs.\n"
            "50 Lakh.\n2. The bidder must have at least 3 years of\nexperience in supply of similar goods.")
    rules = {r.code: r for r in extract_rules([page])}
    assert rules["FIN-TURNOVER"].expression["value"] == 5000000
    assert rules["ELG-EXPERIENCE"].expression["value"] == 3
    assert "50 Lakh" in rules["FIN-TURNOVER"].source_clause


def test_tender_without_known_clauses_gives_no_rules():
    assert extract_rules(["General terms and conditions apply."]) == []
