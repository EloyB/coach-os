"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { useTranslations } from "next-intl";
import { addGroupMember, createManualEnrollment } from "@/lib/api/enrollments";
import { getLessonSeriesById } from "@/lib/api/lessonSeries";
import { MaxGroupSizeField } from "@/components/forms/max-group-size-field";
import { btnPrimary, btnSecondary } from "@/lib/styles";
import { Input } from "@/components/ui/input";
import {
  Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle,
} from "@/components/ui/dialog";

export function ManualEnrollmentDialog({
  seriesId, open, onOpenChange, groupId,
}: {
  seriesId: string;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  groupId?: string;
}) {
  const t = useTranslations("enrollmentsTable");
  const queryClient = useQueryClient();
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [phone, setPhone] = useState("");
  const [dateOfBirth, setDateOfBirth] = useState("");
  // Standaard solo (1): de admin kan een hoger maximum kiezen. Een nieuw lid van een groep erft dat van de groep.
  const [maxGroupSize, setMaxGroupSize] = useState(1);
  const { data: series } = useQuery({
    queryKey: ["lessonSeries", seriesId],
    queryFn: () => getLessonSeriesById(seriesId),
    enabled: open && !groupId,
  });
  const maxGroupSizeLimit = Math.max(series?.maxGroupSize ?? 1, 1);
  const mutation = useMutation({
    mutationFn: () => (groupId
      ? addGroupMember(seriesId, groupId, {
        studentName: name, contactEmail: email, studentPhone: phone.trim(), dateOfBirth,
      })
      : createManualEnrollment(seriesId, {
        studentName: name, contactEmail: email, studentPhone: phone.trim(), dateOfBirth, maxGroupSize,
      })),
    onSuccess: () => {
      toast.success(groupId ? t("addMemberSuccess") : t("manualSuccess"));
      queryClient.invalidateQueries({ queryKey: ["enrollments", seriesId] });
      queryClient.invalidateQueries({ queryKey: ["lessonSeries", seriesId] });
      if (groupId) {
        queryClient.invalidateQueries({ queryKey: ["planning", seriesId] });
      }
      onOpenChange(false);
      setName(""); setEmail(""); setPhone(""); setDateOfBirth(""); setMaxGroupSize(1);
    },
    onError: () => toast.error(t("manualError")),
  });

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{groupId ? t("addMemberTitle") : t("manualTitle")}</DialogTitle>
          <DialogDescription>{groupId ? t("addMemberDescription") : t("manualDescription")}</DialogDescription>
        </DialogHeader>
        <form className="space-y-4" onSubmit={(event) => { event.preventDefault(); mutation.mutate(); }}>
          <label className="block text-sm font-medium">{t("manualName")}
            <Input required value={name} onChange={(e) => setName(e.target.value)} className="mt-1" />
          </label>
          <label className="block text-sm font-medium">{t("manualEmail")}
            <Input required type="email" value={email} onChange={(e) => setEmail(e.target.value)} className="mt-1" />
          </label>
          <label className="block text-sm font-medium">{t("manualPhone")} *
            <Input required value={phone} onChange={(e) => setPhone(e.target.value)} className="mt-1" />
          </label>
          <label className="block text-sm font-medium">{t("manualBirthDate")}
            <Input required type="date" value={dateOfBirth} onChange={(e) => setDateOfBirth(e.target.value)} className="mt-1 appearance-none min-w-0" />
          </label>
          {!groupId && maxGroupSizeLimit > 1 && (
            <div>
              <div className="text-sm font-medium">{t("maxGroupSizeEdit")}</div>
              <div className="mt-1">
                <MaxGroupSizeField
                  name="manual-max-group-size"
                  value={maxGroupSize}
                  onChange={setMaxGroupSize}
                  min={1}
                  max={maxGroupSizeLimit}
                />
              </div>
            </div>
          )}
          <DialogFooter>
            <button type="button" onClick={() => onOpenChange(false)} className={btnSecondary}>{t("back")}</button>
            <button type="submit" disabled={mutation.isPending} className={btnPrimary}>{groupId ? t("addMemberSubmit") : t("manualSubmit")}</button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
