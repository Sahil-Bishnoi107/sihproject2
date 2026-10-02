import { useEffect, useRef, useState } from "react";
import { documentsApi } from "../../api/endpoints";
import type { DocumentEvidence } from "../../api/types";
import { Button, ErrorState, Modal, Spinner } from "../ui";

/**
 * Evidence deep-link: renders the exact page the value was read from and outlines the value's bounding box
 * (PyMuPDF coordinates: PDF points, origin top-left).
 */
export function EvidenceViewer({ evidence, onClose }: { evidence: DocumentEvidence | null; onClose: () => void }) {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const [state, setState] = useState<{ loading: boolean; error?: unknown; scale: number; image?: string }>({ loading: true, scale: 1 });

  useEffect(() => {
    if (!evidence) return;
    let cancelled = false;
    let objectUrl: string | undefined;
    setState({ loading: true, scale: 1 });

    (async () => {
      try {
        const blob = await documentsApi.blob(evidence.documentId);
        if (!blob.type.includes("pdf")) {
          objectUrl = URL.createObjectURL(blob);
          if (!cancelled) setState({ loading: false, scale: 1, image: objectUrl });
          return;
        }
        const pdfjs = await import("pdfjs-dist");
        const worker = await import("pdfjs-dist/build/pdf.worker.min.mjs?url");
        pdfjs.GlobalWorkerOptions.workerSrc = worker.default;

        const pdf = await pdfjs.getDocument({ data: new Uint8Array(await blob.arrayBuffer()) }).promise;
        const page = await pdf.getPage(Math.min(Math.max(evidence.page ?? 1, 1), pdf.numPages));
        const scale = 760 / page.getViewport({ scale: 1 }).width;
        const viewport = page.getViewport({ scale });
        const canvas = canvasRef.current;
        if (!canvas || cancelled) return;

        const dpr = window.devicePixelRatio || 1;
        canvas.width = Math.floor(viewport.width * dpr);
        canvas.height = Math.floor(viewport.height * dpr);
        canvas.style.width = `${viewport.width}px`;
        canvas.style.height = `${viewport.height}px`;
        const ctx = canvas.getContext("2d")!;
        await page.render({ canvas, canvasContext: ctx, viewport, transform: dpr !== 1 ? [dpr, 0, 0, dpr, 0, 0] : undefined }).promise;
        if (!cancelled) setState({ loading: false, scale });
      } catch (error) {
        if (!cancelled) setState({ loading: false, scale: 1, error });
      }
    })();

    return () => {
      cancelled = true;
      if (objectUrl) URL.revokeObjectURL(objectUrl);
    };
  }, [evidence]);

  const box = evidence?.boundingBox;
  return (
    <Modal
      open={!!evidence}
      onClose={onClose}
      wide
      title={evidence ? `${evidence.fileName ?? "Document"} — page ${evidence.page ?? 1}` : ""}
      footer={
        evidence && (
          <Button variant="secondary" onClick={() => documentsApi.open(evidence.documentId, evidence.page)}>
            Open full document
          </Button>
        )
      }
    >
      {evidence && (
        <div className="mb-3 flex flex-wrap items-center gap-2 text-sm">
          <span className="text-ink-700">Extracted</span>
          <code className="rounded bg-ink-50 px-1.5 py-0.5 text-xs">{evidence.key}</code>
          <span className="text-ink-700">=</span>
          <span className="font-semibold">{evidence.value}</span>
          <span className="text-xs text-ink-700">({Math.round(evidence.confidence * 100)}% confidence)</span>
        </div>
      )}
      {state.loading && <Spinner label="Rendering page…" />}
      {state.error != null && <ErrorState error={state.error} />}
      {state.image && <img src={state.image} alt="Evidence document" className="max-w-full rounded border border-ink-100" />}
      <div className="relative mx-auto w-fit overflow-hidden rounded border border-ink-100" hidden={state.loading || !!state.error || !!state.image}>
        <canvas ref={canvasRef} />
        {box && box.length === 4 && (
          <div
            className="pointer-events-none absolute rounded-sm border-2 border-saffron-500 bg-saffron-500/20"
            style={{
              left: box[0] * state.scale - 3,
              top: box[1] * state.scale - 3,
              width: (box[2] - box[0]) * state.scale + 6,
              height: (box[3] - box[1]) * state.scale + 6,
            }}
          />
        )}
      </div>
      {!state.loading && !box && !state.error && (
        <p className="mt-3 text-xs text-ink-700">This value was read by OCR, so there is no exact position to highlight.</p>
      )}
    </Modal>
  );
}
