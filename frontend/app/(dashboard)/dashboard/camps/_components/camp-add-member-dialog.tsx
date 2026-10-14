"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { useTranslations } from "next-intl";
import { addCampGroupMember, getCampForm } from "@/lib/api/camps";
import { getAxiosErrorMessages } from "@/lib/utils/api-errors";
import { btnPrimary, btnSecondary } from "@/lib/styles";
import { Input } from "@/components/ui/input";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { CampFormFieldInputs } from "./camp-form-field-inputs";

interface CampAddMemberDialogProps {
  campId: string;
  groupId: string;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function CampAddMemberDialog({
  campId,
  groupId,
  open,
  onOpenChange,
}: CampAddMemberDialogProps) {
  const t = useTranslations("campEnrollmentsTable");
  const queryClient = useQueryClient();
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [phone, setPhone] = useState("");
  const [answers, setAnswers] = useState<Record<string, string>>({});

  const { data: form } = useQuery({
    queryKey: ["campForm", campId],
    queryFn: () => getCampForm(campId),
    enabled: open,
  });

  const mutation = useMutation({
    mutationFn: () =>
      addCampGroupMember(campId, groupId, {
        participantName: name,
        participantEmail: email,
        participantPhone: phone || null,
        responses: Object.entries(answers)
          .filter(([, value]) => value !== "")
          .map(([campFormFieldId, value]) => ({ campFormFieldId, value })),
      }),
    onSuccess: () => {
      toast.success(t("addMemberSuccess"));
      queryClient.invalidateQueries({ queryKey: ["campEnrollments", campId] });
      queryClient.invalidateQueries({ queryKey: ["camp", campId] });
      onOpenChange(false);
      setName("");
      setEmail("");
      setPhone("");
      setAnswers({});
    },
    onError: (error) => {
      toast.error(getAxiosErrorMessages(error, t("addMemberError")).join(" "));
    },
  });

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent onClick={(e) => e.stopPropagation()}>
        <DialogHeader>
          <DialogTitle>{t("addMemberTitle")}</DialogTitle>
          <DialogDescription>{t("addMemberDescription")}</DialogDescription>
        </DialogHeader>
        <form
          className="max-h-[60vh] space-y-4 overflow-y-auto"
          onSubmit={(event) => {
            event.preventDefault();
            mutation.mutate();
          }}
        >
          <label className="block text-sm font-medium">
            {t("memberName")}
            <Input
              required
              value={name}
              onChange={(e) => setName(e.target.value)}
              className="mt-1"
            />
          </label>
          <label className="block text-sm font-medium">
            {t("memberEmail")}
            <Input
              required
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className="mt-1"
            />
          </label>
          <label className="block text-sm font-medium">
            {t("memberPhone")}
            <Input
              value={phone}
              onChange={(e) => setPhone(e.target.value)}
              className="mt-1"
            />
          </label>
          {form && (
            <CampFormFieldInputs
              fields={form.fields}
              values={answers}
              onChange={(fieldId, value) =>
                setAnswers((prev) => ({ ...prev, [fieldId]: value }))
              }
              idPrefix={`member_${groupId}`}
            />
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
              {t("addMemberSubmit")}
            </button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
