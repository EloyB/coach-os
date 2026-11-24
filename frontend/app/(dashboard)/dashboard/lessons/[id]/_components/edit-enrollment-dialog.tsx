"use client";

import { useForm, useWatch } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import * as z from "zod";
import { toast } from "sonner";
import { useTranslations } from "next-intl";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from "@/components/ui/dialog";
import { FieldError } from "@/components/forms/field-error";
import { inputClass, btnPrimary, btnSecondary } from "@/lib/styles";
import { getLessonSeriesEnrollments, updateBasicEnrollment } from "@/lib/api/enrollments";
import { getLessonSeriesById } from "@/lib/api/lessonSeries";
import { MaxGroupSizeField } from "@/components/forms/max-group-size-field";
import type { LessonSeriesEnrollmentDto } from "@/lib/api/enrollments";
import { getLessonSeriePrices } from "@/lib/api/lessonSeriePrices";

const basicEnrollmentSchema = z.object({
  studentName: z.string().min(1, "Naam is verplicht"),
  contactEmail: z.string().email("Ongeldig e-mailadres"),
  studentEmail: z.string().email("Ongeldig e-mailadres").or(z.literal("")),
  studentPhone: z.string(),
  dateOfBirth: z.string().min(1, "Geboortedatum is verplicht"),
  maxGroupSize: z.number().int().min(1).max(4),
  selectedPriceOptionId: z.string().optional(),
});

type BasicEnrollmentFormValues = z.infer<typeof basicEnrollmentSchema>;

export function EditEnrollmentDialog({
  enrollment,
  seriesId,
  open,
  onOpenChange,
}: {
  enrollment: LessonSeriesEnrollmentDto;
  seriesId: string;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}) {
  const t = useTranslations("enrollmentsTable");
  const queryClient = useQueryClient();

  const { data: priceOptions = [] } = useQuery({
    queryKey: ["lessonSeriePrices", seriesId],
    queryFn: () => getLessonSeriePrices(seriesId),
  });

  // Prijsoptie is vergrendeld zodra er betaald/bevestigd is of een betaling loopt.
  const priceLocked =
    enrollment.status === "Confirmed" ||
    enrollment.status === "PendingPayment" ||
    enrollment.status === "Cancelled";
  const inGroup = enrollment.enrollmentGroupId !== null;

  // Keuzes voor het maximum: van de eigen groepsgrootte tot wat de reeks toelaat.
  const { data: series } = useQuery({
    queryKey: ["lessonSeries", seriesId],
    queryFn: () => getLessonSeriesById(seriesId),
    enabled: open,
  });
  const { data: seriesEnrollments } = useQuery({
    queryKey: ["enrollments", seriesId],
    queryFn: () => getLessonSeriesEnrollments(seriesId),
    enabled: open && inGroup,
  });
  const ownSize = inGroup
    ? Math.max(
        1,
        (seriesEnrollments ?? []).filter(
          (e) =>
            e.enrollmentGroupId === enrollment.enrollmentGroupId &&
            (e.status === "Confirmed" || e.status === "Pending" || e.status === "PendingPayment"),
        ).length,
      )
    : 1;
  const maxGroupSizeLimit = Math.max(series?.maxGroupSize ?? 4, ownSize, enrollment.maxGroupSize);

  const form = useForm<BasicEnrollmentFormValues>({
    resolver: zodResolver(basicEnrollmentSchema),
    values: {
      studentName: enrollment.studentName,
      contactEmail: enrollment.contactEmail,
      studentEmail: enrollment.studentEmail ?? "",
      studentPhone: enrollment.studentPhone ?? "",
      dateOfBirth: enrollment.dateOfBirth ?? "",
      maxGroupSize: enrollment.maxGroupSize,
      selectedPriceOptionId: enrollment.selectedPriceOptionId ?? "",
    },
  });

  const maxGroupSizeValue = useWatch({ control: form.control, name: "maxGroupSize" });

  const mutation = useMutation({
    mutationFn: (values: BasicEnrollmentFormValues) =>
      updateBasicEnrollment(seriesId, enrollment.id, {
        studentName: values.studentName,
        contactEmail: values.contactEmail,
        studentEmail: values.studentEmail?.trim() ? values.studentEmail : null,
        studentPhone: values.studentPhone?.trim() ? values.studentPhone : null,
        dateOfBirth: values.dateOfBirth,
        maxGroupSize: values.maxGroupSize,
        selectedPriceOptionId: values.selectedPriceOptionId || undefined,
      }),
    onSuccess: () => {
      toast.success(t("toastUpdated"));
      queryClient.invalidateQueries({ queryKey: ["enrollments", seriesId] });
      queryClient.invalidateQueries({ queryKey: ["planning", seriesId] });
      onOpenChange(false);
    },
  });

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        onClick={(e) => e.stopPropagation()}
        className="sm:max-w-lg"
        // Voorkom dat Radix bij openen het eerste veld autofocust — op gsm klapt
        // dan meteen het toetsenbord/dropdown open.
        onOpenAutoFocus={(e) => e.preventDefault()}
      >
        <DialogHeader>
          <DialogTitle>Inschrijving aanpassen</DialogTitle>
          <DialogDescription>
            Wijzig enkel basisgegevens. Betaling en planning blijven ongewijzigd.
          </DialogDescription>
        </DialogHeader>
        <p className="text-xs text-gray-500">
          Ingeschreven op {new Date(enrollment.enrolledAt).toLocaleDateString("nl-BE")}
        </p>
        <form
          onSubmit={form.handleSubmit((values) => mutation.mutate(values))}
          className="space-y-4"
        >
          <div className="grid gap-3 sm:grid-cols-2">
            <div className="sm:col-span-2">
              <label className="mb-1 block text-xs font-medium text-gray-600">
                Naam deelnemer
              </label>
              <input className={inputClass} {...form.register("studentName")} />
              <FieldError message={form.formState.errors.studentName?.message} />
            </div>
            <div>
              <label className="mb-1 block text-xs font-medium text-gray-600">
                Contact e-mail
              </label>
              <input type="email" className={inputClass} {...form.register("contactEmail")} />
              <FieldError message={form.formState.errors.contactEmail?.message} />
            </div>
            <div>
              <label className="mb-1 block text-xs font-medium text-gray-600">
                Eigen e-mail deelnemer
              </label>
              <input type="email" className={inputClass} {...form.register("studentEmail")} />
              <FieldError message={form.formState.errors.studentEmail?.message} />
            </div>
            <div>
              <label className="mb-1 block text-xs font-medium text-gray-600">Telefoon</label>
              <input className={inputClass} {...form.register("studentPhone")} />
              <FieldError message={form.formState.errors.studentPhone?.message} />
            </div>
            <div>
              <label className="mb-1 block text-xs font-medium text-gray-600">
                Geboortedatum
              </label>
              <input type="date" className={inputClass + " appearance-none min-w-0"} {...form.register("dateOfBirth")} />
              <FieldError message={form.formState.errors.dateOfBirth?.message} />
            </div>
          </div>
          {priceOptions.length > 0 && (
            <div>
              <label className="mb-1 block text-xs font-medium text-gray-600">
                {t("priceOptionLabel")}
              </label>
              {priceLocked ? (
                <div className="rounded-md border border-gray-200 bg-gray-50 px-3 py-2 text-sm text-gray-600">
                  {priceOptions.find((o) => o.id === enrollment.selectedPriceOptionId)?.label
                    ?? t("priceOptionNone")}
                  <div className="mt-1 text-xs text-gray-400">{t("priceOptionLocked")}</div>
                </div>
              ) : (
                <>
                  <select
                    {...form.register("selectedPriceOptionId")}
                    className={inputClass}
                  >
                    <option value="">{t("priceOptionNone")}</option>
                    {priceOptions
                      .slice()
                      .sort((a, b) => a.sortOrder - b.sortOrder)
                      .map((o) => (
                        <option key={o.id} value={o.id}>
                          {o.label} — €{o.totalPrice}
                        </option>
                      ))}
                  </select>
                  {inGroup && (
                    <p className="mt-1 text-xs text-gray-400">{t("priceOptionGroupHint")}</p>
                  )}
                </>
              )}
            </div>
          )}
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">
              {t("maxGroupSizeEdit")}
            </label>
            <MaxGroupSizeField
              name={`edit-max-group-size-${enrollment.id}`}
              value={maxGroupSizeValue}
              onChange={(value) => form.setValue("maxGroupSize", value, { shouldDirty: true })}
              min={ownSize}
              max={maxGroupSizeLimit}
            />
            <p className="mt-1 text-xs text-gray-400">
              {inGroup ? `${t("maxGroupSizeGroupHint")} ` : ""}
              {t("maxGroupSizeLowerHint")}
            </p>
          </div>
          <p className="rounded-lg bg-amber-50 px-3 py-2 text-xs text-amber-700">
            Deze wijziging past geen betaalstatus, betalingsbedrag of planningstoewijzing aan.
          </p>
          <div className="flex justify-end gap-2 pt-2">
            <button
              type="button"
              onClick={() => onOpenChange(false)}
              className={btnSecondary}
            >
              Annuleren
            </button>
            <button
              type="submit"
              disabled={mutation.isPending}
              className={btnPrimary}
            >
              {mutation.isPending ? "Opslaan…" : "Opslaan"}
            </button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}
