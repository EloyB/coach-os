"use client";

import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useTranslations } from "next-intl";
import Link from "next/link";
import * as z from "zod";

import { requestMagicLink } from "@/lib/api/auth";
import { CourtPattern } from "@/components/ui/court-pattern";
import { TennisBallIcon } from "@/components/ui/tennis-ball-icon";
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";

const schema = z.object({
  email: z.email(),
});

type FormValues = z.infer<typeof schema>;

export default function StudentLoginPage() {
  const t = useTranslations("studentAuth");
  const [isLoading, setIsLoading] = useState(false);
  const [sent, setSent] = useState(false);

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { email: "" },
  });

  async function onSubmit(data: FormValues) {
    setIsLoading(true);
    try {
      await requestMagicLink(data.email);
      setSent(true);
    } finally {
      setIsLoading(false);
    }
  }

  return (
    <div className="min-h-screen flex">
      <div className="hidden lg:flex lg:w-[52%] bg-tennis-green relative overflow-hidden flex-col justify-between p-14">
        <CourtPattern />
        <div className="relative z-10">
          <div className="flex items-center gap-3">
            <div className="w-9 h-9 rounded-full bg-tennis-lime flex items-center justify-center">
              <TennisBallIcon className="w-5 h-5" strokeWidth={2} />
            </div>
            <span className="text-white text-2xl font-bold tracking-tight">
              Coach<span className="text-tennis-lime">OS</span>
            </span>
          </div>
        </div>
        <div className="relative z-10">
          <p className="text-tennis-lime text-sm font-semibold tracking-widest uppercase mb-6 opacity-80">
            {t("studentArea")}
          </p>
          <h1 className="text-[3.25rem] font-extrabold text-white leading-[1.1] mb-6">
            {t("heroLine1")}
            <br />
            <span className="text-tennis-lime">{t("heroLine2")}</span>
          </h1>
          <p className="text-white/60 text-base max-w-xs leading-relaxed">
            {t("tagline")}
          </p>
        </div>
        <div className="relative z-10" />
      </div>

      <div className="w-full lg:w-[48%] flex items-center justify-center p-8 bg-[#FAFAF8]">
        <div className="w-full max-w-[360px]">
          <div className="flex items-center gap-2 mb-10 lg:hidden">
            <div className="w-7 h-7 rounded-full bg-tennis-green flex items-center justify-center">
              <TennisBallIcon
                className="w-4 h-4"
                strokeColor="#D0FF14"
                strokeWidth={2}
              />
            </div>
            <span className="text-tennis-green text-xl font-bold">
              Coach<span className="text-tennis-lime">OS</span>
            </span>
          </div>

          <div className="mb-8">
            <h2 className="text-[1.75rem] font-bold text-gray-900 mb-1 tracking-tight">
              {t("title")}
            </h2>
            <p className="text-gray-400 text-sm">{t("subtitle")}</p>
          </div>

          {sent ? (
            <div className="px-4 py-6 rounded-lg bg-tennis-lime/10 border border-tennis-lime/30">
              <p className="text-tennis-green font-semibold mb-1">
                {t("sentTitle")}
              </p>
              <p className="text-gray-600 text-sm">{t("sentBody")}</p>
            </div>
          ) : (
            <Form {...form}>
              <form
                onSubmit={form.handleSubmit(onSubmit)}
                className="space-y-5"
              >
                <FormField
                  control={form.control}
                  name="email"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel className="text-gray-600 text-sm font-medium">
                        {t("email")}
                      </FormLabel>
                      <FormControl>
                        <Input
                          {...field}
                          type="email"
                          autoComplete="email"
                          placeholder={t("emailPlaceholder")}
                          className="h-11 bg-white border-gray-200 rounded-lg"
                        />
                      </FormControl>
                      <FormMessage className="text-xs" />
                    </FormItem>
                  )}
                />

                <Button
                  type="submit"
                  disabled={isLoading}
                  className="w-full h-11 bg-tennis-green hover:bg-tennis-green/90 text-white font-semibold rounded-lg cursor-pointer"
                >
                  {isLoading ? t("loading") : t("submit")}
                </Button>
              </form>
            </Form>
          )}

          <p className="mt-7 text-center text-sm text-gray-400">
            {t("trainerOrAdmin")}{" "}
            <Link
              href="/login"
              className="text-tennis-green font-semibold hover:underline"
            >
              {t("trainerLogin")}
            </Link>
          </p>
        </div>
      </div>
    </div>
  );
}
