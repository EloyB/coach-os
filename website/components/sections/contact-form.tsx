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
import { Textarea } from "@/components/ui/textarea";
import { contactSchema, type ContactValues } from "@/lib/schemas";
import { CTA_SECTION } from "@/content/cta";

export function ContactForm() {
  const router = useRouter();
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const form = useForm<ContactValues>({
    resolver: zodResolver(contactSchema),
    defaultValues: {
      name: "",
      organization: "",
      email: "",
      message: "",
      website: "",
    },
  });

  async function onSubmit(values: ContactValues) {
    setSubmitting(true);
    setError(null);
    try {
      const res = await fetch("/api/contact", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(values),
      });
      if (!res.ok) throw new Error("submit failed");
      router.push("/bedankt?type=contact");
    } catch {
      setError("Versturen lukte niet — probeer het opnieuw.");
      setSubmitting(false);
    }
  }

  const fields = CTA_SECTION.contact.fields;

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
        <input
          type="text"
          autoComplete="off"
          tabIndex={-1}
          className="sr-only"
          aria-hidden="true"
          {...form.register("website")}
        />

        <div className="grid gap-4 md:grid-cols-2">
          <FormField
            control={form.control}
            name="name"
            render={({ field }) => (
              <FormItem>
                <FormLabel>{fields.name}</FormLabel>
                <FormControl>
                  <Input {...field} autoComplete="name" />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
          <FormField
            control={form.control}
            name="organization"
            render={({ field }) => (
              <FormItem>
                <FormLabel>{fields.organization}</FormLabel>
                <FormControl>
                  <Input {...field} autoComplete="organization" />
                </FormControl>
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
          name="message"
          render={({ field }) => (
            <FormItem>
              <FormLabel>{fields.message}</FormLabel>
              <FormControl>
                <Textarea {...field} rows={5} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        {error && <p className="text-sm text-urgent">{error}</p>}

        <button
          type="submit"
          disabled={submitting}
          className="inline-flex h-11 items-center gap-2 rounded-md bg-ink px-5 text-sm font-semibold text-white transition-colors hover:bg-ink/90 disabled:opacity-50"
        >
          {submitting ? "Versturen…" : CTA_SECTION.contact.submit}
          <ArrowRight className="h-4 w-4" />
        </button>
      </form>
    </Form>
  );
}
