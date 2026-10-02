from app.heuristics.entities import extract_entities

GST_CERT = """Government of India - Form GST REG-06
Registration Certificate
GSTIN: 27AAECS1234K1Z5
Legal Name: Shakti Enterprises Pvt Ltd
"""

FINANCIALS = """Certificate from Chartered Accountant
Annual turnover of Shakti Enterprises Pvt Ltd
FY 2021-22   Rs. 45 Lakh
FY 2022-23   Rs. 55 Lakh
FY 2023-24   Rs. 80 Lakh
"""

ISO = """ISO 9001:2015 Certificate
Certificate No: QMS-IN-2291
Valid Until: 31/12/2027
"""

MISC = """Udyam Registration Number: UDYAM-MH-26-0012345
CIN: U72900MH2019PTC123456
PAN No: AAECS1234K
EPFO Establishment Code: MHBAN0012345000
The bidder has 7 years of experience in supply of IT hardware.
Manufacturer's Authorization Form (OEM authorisation) enclosed.
EMD of Rs 2,00,000 paid vide UTR 8812345.
"""


def fields(pages):
    return {f.key: f for f in extract_entities(pages)}


def test_gstin_and_derived_pan():
    got = fields([GST_CERT])
    assert got["gstin"].value == "27AAECS1234K1Z5"
    assert got["gstin"].page == 1
    assert got["pan"].value == "AAECS1234K"
    assert got["pan"].confidence < got["gstin"].confidence


def test_turnover_years_and_three_year_average():
    got = fields([FINANCIALS])
    assert got["turnover.fy2022"].value == "4500000"
    assert got["turnover.fy2024"].value == "8000000"
    assert got["turnover.avg3y"].value == "6000000"


def test_iso_certificate_number_and_expiry():
    got = fields([ISO])
    assert got["iso9001.number"].value == "QMS-IN-2291"
    assert got["iso9001.expiry"].value == "2027-12-31"


def test_other_identifiers_and_flags():
    got = fields(["page one", MISC])
    assert got["udyam"].value == "UDYAM-MH-26-0012345"
    assert got["udyam"].page == 2
    assert got["cin"].value == "U72900MH2019PTC123456"
    assert got["pan"].value == "AAECS1234K"
    assert got["epfo"].value == "MHBAN0012345000"
    assert got["experience.years"].value == "7"
    assert got["oem.authorization"].value == "yes"
    assert got["emd.paid"].value == "yes"


def test_empty_text_yields_nothing():
    assert extract_entities(["", "   "]) == []
