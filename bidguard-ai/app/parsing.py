"""Parsing helpers for amounts and dates as written in Indian procurement documents."""

from datetime import datetime

_MULTIPLIERS = {
    "lakh": 100_000, "lakhs": 100_000, "lac": 100_000, "lacs": 100_000,
    "crore": 10_000_000, "crores": 10_000_000, "cr": 10_000_000,
}

_DATE_FORMATS = ("%d/%m/%Y", "%d-%m-%Y", "%d.%m.%Y", "%Y-%m-%d", "%d %b %Y", "%d %B %Y", "%d-%b-%Y", "%d %b, %Y", "%d %B, %Y")

# Regex fragment for a rupee amount: currency marker, number, optional Lakh/Crore unit.
AMOUNT_PATTERN = r"(?:Rs\.?|INR|₹)\s*([\d,]+(?:\.\d+)?)\s*(Lakhs?|Lacs?|Crores?|Cr\b)?"


def parse_amount(number: str, unit: str | None) -> int | None:
    """'50' + 'Lakh' -> 5000000; '60,00,000' -> 6000000."""
    try:
        value = float(number.replace(",", "").strip())
    except ValueError:
        return None
    multiplier = _MULTIPLIERS.get((unit or "").strip().lower().rstrip("."), 1)
    return int(round(value * multiplier))


def parse_date(text: str) -> str | None:
    """Normalise a written date to ISO yyyy-mm-dd, or None when unrecognised."""
    cleaned = " ".join(text.strip().split())
    for fmt in _DATE_FORMATS:
        try:
            return datetime.strptime(cleaned, fmt).date().isoformat()
        except ValueError:
            continue
    return None
