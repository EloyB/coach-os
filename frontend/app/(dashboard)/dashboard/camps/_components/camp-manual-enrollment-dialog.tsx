"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { useTranslations } from "next-intl";
import {
  createManualCampEnrollment,
  getCampById,
  getCampForm,
} from "@/lib/api/camps";
import { getAxiosErrorMessages } from "@/lib/utils/api-errors";
import { btnPrimary, btnSecondary } from "@/lib/styles";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { CampParticipantFields } from "./camp-participant-fields";

interface CampManualEnrollmentDialogProps {
  campId: string;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function CampManualEnrollmentDialog({
  campId,
  open,
  onOpenChange,
}: CampManualEnrollmentDialogProps) {
  const t = useTranslations("campEnrollmentsTable");
  const queryClient = useQueryClient();
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [phone, setPhone] = useState("");
  const [answers, setAnswers] = useState<Record<string, string>>({});
  const [paid, setPaid] = useState(false);

  const { data: form } = useQuery({
    queryKey: ["campForm", campId],
    queryFn: () => getCampForm(campId),
    enabled: open,
  });
  // Zelfde query als de detailpagina: meestal al in de cache.
  const { data: camp } = useQuery({
    queryKey: ["camp", campId],
    queryFn: () => getCampById(campId),
    enabled: open,
  });
  const isPaidCamp = (camp?.price ?? 0) > 0;

  const mutation = useMutation({
    mutationFn: () =>
      createManualCampEnrollment(campId, {
        participantName: name,
        participantEmail: email,
        participantPhone: phone || null,
        paid: isPaidCamp && paid,
        responses: Object.entries(answers)
          .filter(([, value]) => value !== "")
          .map(([campFormFieldId, value]) => ({ campFormFieldId, value })),
      }),
    onSuccess: () => {
      toast.success(t("manualSuccess"));
      queryClient.invalidateQueries({ queryKey: ["campEnrollments", campId] });
      queryClient.invalidateQueries({ queryKey: ["camp", campId] });
      onOpenChange(false);
      setName("");
      setEmail("");
      setPhone("");
      setAnswers({});
      setPaid(false);
    },
    onError: (error) => {
      toast.error(getAxiosErrorMessages(error, t("manualError")).join(" "));
    },
  });

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        onClick={(e) => e.stopPropagation()}
        onOpenAutoFocus={(e) => e.preventDefault()}
      >
        <DialogHeader>
          <DialogTitle>{t("manualTitle")}</DialogTitle>
          <DialogDescription>{t("manualDescription")}</DialogDescription>
        </DialogHeader>
        <form
          className="max-h-[60vh] space-y-4 overflow-y-auto"
          onSubmit={(event) => {
            event.preventDefault();
            mutation.mutate();
          }}
        >
          <CampParticipantFields
            values={{ name, email, phone }}
            onChange={(patch) => {
              if (patch.name !== undefined) setName(patch.name);
              if (patch.email !== undefined) setEmail(patch.email);
              if (patch.phone !== undefined) setPhone(patch.phone);
            }}
            form={form}
            answers={answers}
            onAnswerChange={(fieldId, value) =>
              setAnswers((prev) => ({ ...prev, [fieldId]: value }))
            }
            idPrefix="manual"
          />
          {isPaidCamp && (
            <fieldset className="space-y-1.5">
              <legend className="text-sm font-medium">
                {t("manualPayment")}
              </legend>
              <label className="flex cursor-pointer items-center gap-2">
                <input
                  type="radio"
                  name="manual_payment"
                  checked={!paid}
                  onChange={() => setPaid(false)}
                  className="h-4 w-4 text-tennis-green focus:ring-tennis-green"
                />
                <span className="text-sm text-gray-700">
                  {t("manualPaymentPending")}
                </span>
              </label>
              <label className="flex cursor-pointer items-center gap-2">
                <input
                  type="radio"
                  name="manual_payment"
                  checked={paid}
                  onChange={() => setPaid(true)}
                  className="h-4 w-4 text-tennis-green focus:ring-tennis-green"
                />
                <span className="text-sm text-gray-700">
                  {t("manualPaymentReceived")}
                </span>
              </label>
            </fieldset>
          )}
          <DialogFooter>
            <button
              type="button"
              onClick={() => onOpenChange(false)}
              className={btnSecondary}
            >
              {t("back")}
            </button>
            <button
              type="submit"
              disabled={mutation.isPending}
              className={btnPrimary}
            >
              {t("manualSubmit")}
            </button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
