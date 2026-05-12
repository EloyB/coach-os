"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useTranslations } from "next-intl";
import * as z from "zod";
import axios from "axios";

import { createAdmin } from "@/lib/api/super-admin";
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
  organizationName: z.string().min(1).max(200),
  firstName: z.string().min(1).max(100),
  lastName: z.string().min(1).max(100),
  email: z.email(),
  isEarlyBird: z.boolean(),
});
type FormValues = z.infer<typeof schema>;

export default function NewAdminPage() {
  const t = useTranslations("superAdmin");
  const router = useRouter();
  const [errors, setErrors] = useState<string[]>([]);
  const [submitting, setSubmitting] = useState(false);

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      organizationName: "",
      firstName: "",
      lastName: "",
      email: "",
      isEarlyBird: false,
    },
  });

  async function onSubmit(values: FormValues) {
    setSubmitting(true);
    setErrors([]);
    try {
      await createAdmin(values);
      router.push("/super-admin/admins");
    } catch (error) {
      if (axios.isAxiosError(error) && error.response?.data) {
        const data = error.response.data;
        if (Array.isArray(data)) setErrors(data);
        else if (typeof data === "string") setErrors([data]);
        else setErrors([t("createError")]);
      } else {
        setErrors([t("createError")]);
      }
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="max-w-xl space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-ink">{t("createTitle")}</h1>
        <p className="text-sm text-ink-3 mt-1">{t("createSubtitle")}</p>
      </div>

      {errors.length > 0 && (
        <div className="rounded-lg bg-red-50 border border-red-100 px-4 py-3">
          {errors.map((err, i) => (
            <p key={i} className="text-sm text-red-700">{err}</p>
          ))}
        </div>
      )}

      <Form {...form}>
        <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
          <FormField
            control={form.control}
            name="organizationName"
            render={({ field }) => (
              <FormItem>
                <FormLabel>{t("fieldOrganization")}</FormLabel>
                <FormControl>
                  <Input {...field} placeholder="TC Brederode" />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
          <div className="grid grid-cols-2 gap-3">
            <FormField
              control={form.control}
              name="firstName"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t("fieldFirstName")}</FormLabel>
                  <FormControl><Input {...field} /></FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="lastName"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t("fieldLastName")}</FormLabel>
                  <FormControl><Input {...field} /></FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
          </div>
          <FormField
            control={form.control}
            name="email"
            render={({ field }) => (
              <FormItem>
                <FormLabel>{t("fieldEmail")}</FormLabel>
                <FormControl><Input {...field} type="email" /></FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
          <FormField
            control={form.control}
            name="isEarlyBird"
            render={({ field }) => (
              <FormItem className="flex items-center gap-2 space-y-0">
                <FormControl>
                  <input
                    type="checkbox"
                    checked={field.value}
                    onChange={(e) => field.onChange(e.target.checked)}
                    className="h-4 w-4"
                  />
                </FormControl>
                <FormLabel className="!mt-0">{t("fieldEarlyBird")}</FormLabel>
              </FormItem>
            )}
          />

          <div className="flex gap-2 pt-2">
            <Button type="submit" disabled={submitting} className="bg-tennis-green hover:bg-tennis-green/90 text-white">
              {submitting ? t("submitting") : t("createSubmit")}
            </Button>
            <Button type="button" variant="outline" onClick={() => router.back()}>
              {t("cancel")}
            </Button>
          </div>
        </form>
      </Form>
    </div>
  );
}
