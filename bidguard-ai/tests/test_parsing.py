from app.parsing import parse_amount, parse_date


def test_lakh_and_crore_amounts():
    assert parse_amount("50", "Lakh") == 5_000_000
    assert parse_amount("2", "Crore") == 20_000_000
    assert parse_amount("1.5", "Cr") == 15_000_000


def test_indian_digit_grouping_without_unit():
    assert parse_amount("60,00,000", None) == 6_000_000


def test_unparseable_amount_is_none():
    assert parse_amount("abc", None) is None


def test_dates_normalise_to_iso():
    assert parse_date("31/12/2027") == "2027-12-31"
    assert parse_date("31-12-2027") == "2027-12-31"
    assert parse_date("2027-12-31") == "2027-12-31"
    assert parse_date("31 Dec 2027") == "2027-12-31"
    assert parse_date("not a date") is None
