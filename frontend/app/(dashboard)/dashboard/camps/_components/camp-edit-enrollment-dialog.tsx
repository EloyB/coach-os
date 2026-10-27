"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { useTranslations } from "next-intl";
import {
  getCampForm,
  updateCampEnrollment,
  type CampEnrollmentDto,
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

interface CampEditEnrollmentDialogProps {
  campId: string;
  enrollment: CampEnrollmentDto;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function CampEditEnrollmentDialog({
  campId,
  enrollment,
  open,
  onOpenChange,
}: CampEditEnrollmentDialogProps) {
  const t = useTranslations("campEnrollmentsTable");

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        onClick={(e) => e.stopPropagation()}
        // Op gsm klapt anders meteen het toetsenbord open op het eerste veld.
        onOpenAutoFocus={(e) => e.preventDefault()}
      >
        <DialogHeader>
          <DialogTitle>{t("editTitle")}</DialogTitle>
          <DialogDescription>{t("editDescription")}</DialogDescription>
        </DialogHeader>
        {/* De inhoud wordt pas gemount wanneer de dialog open is, zodat de velden
            telkens vers uit de huidige inschrijving voorgevuld worden. */}
        <EditEnrollmentForm
          campId={campId}
          enrollment={enrollment}
          onClose={() => onOpenChange(false)}
        />
      </DialogContent>
    </Dialog>
  );
}

function EditEnrollmentForm({
  campId,
  enrollment,
  onClose,
}: {
  campId: string;
  enrollment: CampEnrollmentDto;
  onClose: () => void;
}) {
  const t = useTranslations("campEnrollmentsTable");
  const queryClient = useQueryClient();
  const [name, setName] = useState(enrollment.participantName);
  const [email, setEmail] = useState(enrollment.participantEmail);
  const [phone, setPhone] = useState(enrollment.participantPhone ?? "");
  const [priceId, setPriceId] = useState<string | null>(enrollment.campPriceId);
  const [packageId, setPackageId] = useState<string | null>(enrollment.campPackageId);
  const [answers, setAnswers] = useState<Record<string, string>>(() =>
    Object.fromEntries(
      enrollment.formResponses.map((r) => [r.fieldId, r.value]),
    ),
  );

  const { data: form } = useQuery({
    queryKey: ["campForm", campId],
    queryFn: () => getCampForm(campId),
  });

  const mutation = useMutation({
    mutationFn: () =>
      updateCampEnrollment(campId, enrollment.id, {
        participantName: name,
        participantEmail: email,
        participantPhone: phone.trim() ? phone : null,
        campPriceId: priceId,
        campPackageId: packageId,
        responses: Object.entries(answers)
          .filter(([, value]) => value.trim() !== "")
          .map(([campFormFieldId, value]) => ({ campFormFieldId, value })),
      }),
    onSuccess: () => {
      toast.success(t("toastUpdated"));
      queryClient.invalidateQueries({ queryKey: ["campEnrollments", campId] });
      queryClient.invalidateQueries({ queryKey: ["camp", campId] });
      onClose();
    },
    onError: (error) => {
      toast.error(getAxiosErrorMessages(error, t("editError")).join(" "));
    },
  });

  return (
    <form
      className="max-h-[60vh] space-y-4 overflow-y-auto"
      onSubmit={(event) => {
        event.preventDefault();
        mutation.mutate();
      }}
    >
      <p className="text-xs text-gray-500">
        {t("enrolledAt")}{" "}
        {new Date(enrollment.enrolledAt).toLocaleDateString("nl-BE")}
      </p>
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
        idPrefix={`edit_${enrollment.id}`}
        campId={campId}
        priceId={priceId}
        onPriceChange={setPriceId}
        packageId={packageId}
        onPackageChange={setPackageId}
      />
      <DialogFooter>
        <button type="button" onClick={onClose} className={btnSecondary}>
          {t("back")}
        </button>
        <button
          type="submit"
          disabled={mutation.isPending}
          className={btnPrimary}
        >
          {t("editSubmit")}
        </button>
      </DialogFooter>
    </form>
  );
}
