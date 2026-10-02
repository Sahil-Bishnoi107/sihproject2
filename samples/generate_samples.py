"""Generate demo PDFs for BidGuard: one GeM tender and two bids for the seeded bidder (Shakti Enterprises).

Run with the sidecar's virtualenv (needs PyMuPDF):  python samples/generate_samples.py
"""

from pathlib import Path

import fitz

OUT = Path(__file__).parent

TENDER = [
    """GeM Bid Document - Supply of 200 Laptops
Ministry of Example, Procurement Division

Eligibility Criteria:
1. Minimum Average Annual Turnover of the bidder during the last three years should be Rs. 50 Lakh.
2. The bidder must have at least 3 years of experience in supply of similar goods.
3. The bidder must hold a valid GST registration (GSTIN).""",
    """Technical and Commercial Requirements:
4. The bidder shall possess a valid ISO 9001 certificate.
5. OEM Authorization certificate from the manufacturer is mandatory.
6. Earnest Money Deposit (EMD) of Rs 2,40,000 is required. MSEs registered under Udyam are exempted.""",
]

BID_COMPLIANT = [
    """Form GST REG-06 - Registration Certificate
GSTIN: 27AAECS1234K1Z5
Legal Name: Shakti Enterprises Pvt Ltd
Udyam Registration Number: UDYAM-MH-26-0012345""",
    """Certificate from Chartered Accountant - Annual Turnover
FY 2021-22   Rs. 45 Lakh
FY 2022-23   Rs. 55 Lakh
FY 2023-24   Rs. 80 Lakh
The bidder has 7 years of experience in supply of IT hardware.""",
    """ISO 9001:2015 Certificate
Certificate No: QMS-IN-2291
Valid Until: 31/12/2027
Manufacturer's Authorization Form (OEM authorisation) from Dell India enclosed.""",
]

BID_NONCOMPLIANT = [
    """Form GST REG-06 - Registration Certificate
GSTIN: 27AAECS1234K1Z5
Legal Name: Shakti Enterprises Pvt Ltd""",
    """Certificate from Chartered Accountant - Annual Turnover
FY 2021-22   Rs. 20 Lakh
FY 2022-23   Rs. 25 Lakh
FY 2023-24   Rs. 30 Lakh
The bidder has 2 years of experience in supply of IT hardware.""",
    """ISO 9001:2015 Certificate
Certificate No: QMS-IN-1180
Valid Until: 30/06/2025""",
]


def write_pdf(name: str, pages: list[str]) -> Path:
    doc = fitz.open()
    for text in pages:
        page = doc.new_page()
        page.insert_textbox(fitz.Rect(56, 56, 540, 790), text, fontsize=11)
    path = OUT / name
    doc.save(path)
    doc.close()
    return path


if __name__ == "__main__":
    for name, pages in [("tender_sample.pdf", TENDER), ("bid_compliant.pdf", BID_COMPLIANT),
                        ("bid_noncompliant.pdf", BID_NONCOMPLIANT)]:
        print("wrote", write_pdf(name, pages))
