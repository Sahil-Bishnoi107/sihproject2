"""PDF / image text extraction. Native text layer first; Tesseract OCR only for pages without one."""

import functools
import io
import logging
from dataclasses import dataclass, field

import fitz  # PyMuPDF

log = logging.getLogger(__name__)

_MIN_NATIVE_CHARS = 20
_IMAGE_TYPES = {".png": "png", ".jpg": "jpeg", ".jpeg": "jpeg"}


class UnreadableDocument(Exception):
    pass


@functools.cache
def tesseract_available() -> bool:
    try:
        import pytesseract
        pytesseract.get_tesseract_version()
        return True
    except Exception:  # binary missing or not on PATH
        return False


@dataclass
class DocumentText:
    pages: list[str]
    ocr_used: bool
    _doc: fitz.Document = field(repr=False)
    _ocr_pages: set[int] = field(default_factory=set, repr=False)

    def locate(self, page: int | None, value: str) -> list[float] | None:
        """Bounding box [x0, y0, x1, y1] of the value on its page, for evidence deep-links (text-layer pages only)."""
        if page is None or page < 1 or page > len(self._doc) or page in self._ocr_pages:
            return None
        hits = self._doc[page - 1].search_for(value)
        if not hits:
            return None
        r = hits[0]
        return [round(r.x0, 1), round(r.y0, 1), round(r.x1, 1), round(r.y1, 1)]

    def close(self) -> None:
        self._doc.close()


def extract_text(data: bytes, filename: str) -> DocumentText:
    suffix = "." + filename.rsplit(".", 1)[-1].lower() if "." in filename else ""
    try:
        doc = fitz.open(stream=data, filetype=_IMAGE_TYPES.get(suffix, "pdf"))
    except Exception as exc:
        raise UnreadableDocument(f"Cannot open {filename}: {exc}") from exc

    pages: list[str] = []
    ocr_pages: set[int] = set()
    for number, page in enumerate(doc, start=1):
        text = page.get_text("text") if doc.is_pdf else ""
        if len(text.strip()) < _MIN_NATIVE_CHARS:
            ocr_text = _ocr(page)
            if ocr_text is not None:
                text = ocr_text
                ocr_pages.add(number)
        pages.append(text)

    return DocumentText(pages, bool(ocr_pages), doc, ocr_pages)


def _ocr(page: fitz.Page) -> str | None:
    if not tesseract_available():
        log.warning("Page %s has no text layer and Tesseract is not installed; skipping OCR", page.number + 1)
        return None
    import pytesseract
    from PIL import Image

    pix = page.get_pixmap(dpi=200)
    image = Image.open(io.BytesIO(pix.tobytes("png")))
    return pytesseract.image_to_string(image, lang="eng")
