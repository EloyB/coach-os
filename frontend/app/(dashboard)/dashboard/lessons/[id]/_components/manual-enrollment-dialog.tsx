"use client";

import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { useTranslations } from "next-intl";
import { createManualEnrollment } from "@/lib/api/enrollments";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle,
} from "@/components/ui/dialog";

export function ManualEnrollmentDialog({
  seriesId, open, onOpenChange,
}: { seriesId: string; open: boolean; onOpenChange: (open: boolean) => void }) {
  const t = useTranslations("enrollmentsTable");
  const queryClient = useQueryClient();
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [phone, setPhone] = useState("");
  const [dateOfBirth, setDateOfBirth] = useState("");
  const mutation = useMutation({
    mutationFn: () => createManualEnrollment(seriesId, {
      studentName: name, contactEmail: email, studentPhone: phone || null, dateOfBirth,
    }),
    onSuccess: () => {
      toast.success(t("manualSuccess"));
      queryClient.invalidateQueries({ queryKey: ["enrollments", seriesId] });
      queryClient.invalidateQueries({ queryKey: ["lessonSeries", seriesId] });
      onOpenChange(false);
      setName(""); setEmail(""); setPhone(""); setDateOfBirth("");
    },
    onError: () => toast.error(t("manualError")),
  });

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{t("manualTitle")}</DialogTitle>
          <DialogDescription>{t("manualDescription")}</DialogDescription>
        </DialogHeader>
        <form className="space-y-4" onSubmit={(event) => { event.preventDefault(); mutation.mutate(); }}>
          <label className="block text-sm font-medium">{t("manualName")}
            <Input required value={name} onChange={(e) => setName(e.target.value)} className="mt-1" />
          </label>
          <label className="block text-sm font-medium">{t("manualEmail")}
            <Input required type="email" value={email} onChange={(e) => setEmail(e.target.value)} className="mt-1" />
          </label>
          <label className="block text-sm font-medium">{t("manualPhone")}
            <Input value={phone} onChange={(e) => setPhone(e.target.value)} className="mt-1" />
          </label>
          <label className="block text-sm font-medium">{t("manualBirthDate")}
            <Input required type="date" value={dateOfBirth} onChange={(e) => setDateOfBirth(e.target.value)} className="mt-1" />
          </label>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>{t("back")}</Button>
            <Button type="submit" disabled={mutation.isPending}>{t("manualSubmit")}</Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
