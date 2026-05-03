"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { ArrowRight } from "lucide-react";
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { waitlistSchema, type WaitlistValues } from "@/lib/schemas";
import { CTA_SECTION } from "@/content/cta";
import { cn } from "@/lib/utils";

const ROLES: { value: "club" | "coach" | "anders"; label: string }[] = [
  { value: "club", label: CTA_SECTION.waitlist.fields.roleOptions.club },
  { value: "coach", label: CTA_SECTION.waitlist.fields.roleOptions.coach },
  { value: "anders", label: CTA_SECTION.waitlist.fields.roleOptions.anders },
];

export function WaitlistForm() {
  const router = useRouter();
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const form = useForm<WaitlistValues>({
    resolver: zodResolver(waitlistSchema),
    defaultValues: {
      email: "",
      role: undefined,
      website: "",
    },
  });

  const selectedRole = form.watch("role");

  async function onSubmit(values: WaitlistValues) {
    setSubmitting(true);
    setError(null);
    try {
      const res = await fetch("/api/waitlist", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(values),
      });
      if (!res.ok) throw new Error("submit failed");
      router.push("/bedankt?type=waitlist");
    } catch {
      setError("Inschrijven lukte niet — probeer het opnieuw.");
      setSubmitting(false);
    }
  }

  const fields = CTA_SECTION.waitlist.fields;

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-5">
        <input
          type="text"
          autoComplete="off"
          tabIndex={-1}
          className="sr-only"
          aria-hidden="true"
          {...form.register("website")}
        />

        <FormField
          control={form.control}
          name="email"
          render={({ field }) => (
            <FormItem>
              <FormLabel>{fields.email}</FormLabel>
              <FormControl>
                <Input {...field} type="email" autoComplete="email" />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="role"
          render={() => (
            <FormItem>
              <FormLabel>{fields.role}</FormLabel>
              <div className="flex flex-wrap gap-2">
                {ROLES.map((role) => {
                  const active = selectedRole === role.value;
                  return (
                    <button
                      key={role.value}
                      type="button"
                      onClick={() => form.setValue("role", role.value)}
                      className={cn(
                        "h-10 rounded-md border px-4 text-sm font-medium transition-colors",
                        active
                          ? "border-ink bg-ink text-white"
                          : "border-rule bg-paper text-ink-2 hover:border-ink/40",
                      )}
                    >
                      {role.label}
                    </button>
                  );
                })}
              </div>
              <FormMessage />
            </FormItem>
          )}
        />

        {error && <p className="text-sm text-urgent">{error}</p>}

        <button
          type="submit"
          disabled={submitting}
          className="inline-flex h-11 items-center gap-2 rounded-md bg-tennis-lime px-5 text-sm font-semibold text-ink transition-colors hover:bg-tennis-lime/90 disabled:opacity-50"
        >
          {submitting ? "Inschrijven…" : CTA_SECTION.waitlist.submit}
          <ArrowRight className="h-4 w-4" />
        </button>
      </form>
    </Form>
  );
}
