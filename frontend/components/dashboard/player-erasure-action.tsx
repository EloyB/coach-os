"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { Trash2 } from "lucide-react";
import { canEditEnrollment, isSuperAdmin } from "@/lib/auth";
import type { ErasureKind } from "@/lib/api/privacy";
import { PlayerErasureDialog } from "./player-erasure-dialog";

interface PlayerErasureActionProps {
  kind: ErasureKind;
  enrollmentId: string;
  /** Na een geslaagde verwijdering, bv. het detailvenster sluiten. */
  onErased: () => void;
}

/** Admin-only knop "Speler verwijderen" voor in een inschrijvingsdetail. */
export function PlayerErasureAction({ kind, enrollmentId, onErased }: PlayerErasureActionProps) {
  const t = useTranslations("playerErasure");
  const [allowed, setAllowed] = useState(false);
  const [open, setOpen] = useState(false);
  useEffect(() => {
    // Auth staat in localStorage: pas na mount lezen om hydration-mismatch te vermijden.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    setAllowed(canEditEnrollment() && !isSuperAdmin());
  }, []);

  if (!allowed) return null;
  return (
    <>
      <button
        type="button"
        onClick={() => setOpen(true)}
        className="inline-flex items-center gap-1.5 self-start text-xs font-medium text-red-600 hover:text-red-700 hover:underline"
      >
        <Trash2 size={13} />
        {t("action")}
      </button>
      <PlayerErasureDialog
        kind={kind}
        enrollmentId={enrollmentId}
        open={open}
        onOpenChange={setOpen}
        onErased={() => {
          setOpen(false);
          onErased();
        }}
      />
    </>
  );
}
