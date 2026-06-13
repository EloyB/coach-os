"use client";

import { useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { ChevronLeft } from "lucide-react";
import { createCamp, type CreateCampRequest } from "@/lib/api/camps";
import { getTennisClubs } from "@/lib/api/tennisClubs";
import { getTrainers } from "@/lib/api/trainers";
import { getAxiosErrorMessages } from "@/lib/utils/api-errors";
import { CampForm } from "../_components/camp-form";

export default function NewCampPage() {
  const t = useTranslations("camps");
  const router = useRouter();
  const queryClient = useQueryClient();
  const [errorMessages, setErrorMessages] = useState<string[]>([]);

  const { data: clubs = [] } = useQuery({
    queryKey: ["tennisClubs"],
    queryFn: getTennisClubs,
  });
  const { data: trainers = [] } = useQuery({
    queryKey: ["trainers"],
    queryFn: getTrainers,
  });

  const mutation = useMutation({
    mutationFn: (req: CreateCampRequest) => createCamp(req),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["camps"] });
      router.push("/dashboard/camps");
    },
    onError: (error) => {
      setErrorMessages(getAxiosErrorMessages(error, t("saveError")));
    },
  });

  return (
    <div className="max-w-3xl">
      <Link
        href="/dashboard/camps"
        className="inline-flex items-center gap-1 text-xs text-ink-3 hover:text-ink-2 mb-4"
      >
        <ChevronLeft size={14} />
        {t("back")}
      </Link>

      <div className="mb-5">
        <h1 className="text-lg font-bold text-ink tracking-tight">
          {t("createTitle")}
        </h1>
        <p className="text-sm text-ink-3 mt-0.5">{t("createSubtitle")}</p>
      </div>

      <CampForm
        clubs={clubs}
        trainers={trainers}
        onSubmit={(req) => {
          setErrorMessages([]);
          mutation.mutate(req);
        }}
        submitting={mutation.isPending}
        errorMessages={errorMessages}
      />
    </div>
  );
}
