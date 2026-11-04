"use client";

import { useState } from "react";
import { Check, Copy } from "lucide-react";

/** Mollie-referentie (tr_...) met kopieerknop, om de betaling in Mollie terug te vinden. */
export function MolliePaymentId({ id, copyLabel }: { id: string; copyLabel: string }) {
  const [copied, setCopied] = useState(false);

  function handleCopy() {
    navigator.clipboard.writeText(id);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  }

  return (
    <span className="inline-flex items-center gap-1.5">
      <code className="rounded bg-gray-50 px-1.5 py-0.5 font-mono text-xs">{id}</code>
      <button
        type="button"
        onClick={handleCopy}
        aria-label={copyLabel}
        title={copyLabel}
        className="rounded p-1 text-gray-400 transition-colors hover:bg-gray-50 hover:text-gray-600"
      >
        {copied ? <Check size={13} className="text-tennis-green" /> : <Copy size={13} />}
      </button>
    </span>
  );
}
