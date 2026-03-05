"use client";

import { use, useState } from "react";
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import * as z from "zod";
import axios from "axios";

import { acceptInvite } from "@/lib/api/trainers";
import { setToken, setAuthUser } from "@/lib/auth";
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

// ─── Schema ───────────────────────────────────────────────────────────────────

const schema = z
  .object({
    password: z.string().min(8, "Wachtwoord moet minimaal 8 karakters zijn"),
    confirmPassword: z.string().min(1, "Bevestig je wachtwoord"),
  })
  .refine((d) => d.password === d.confirmPassword, {
    message: "Wachtwoorden komen niet overeen",
    path: ["confirmPassword"],
  });

type FormValues = z.infer<typeof schema>;

function Spinner() {
  return (
    <svg className="animate-spin h-4 w-4" viewBox="0 0 24 24" fill="none" aria-hidden="true">
      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
      <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
    </svg>
  );
}

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function InvitePage({
  params,
}: {
  params: Promise<{ token: string }>;
}) {
  const { token } = use(params);
  const router = useRouter();
  const [isLoading, setIsLoading] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { password: "", confirmPassword: "" },
  });

  async function onSubmit(data: FormValues) {
    setIsLoading(true);
    setErrors([]);
    try {
      const response = await acceptInvite({ token, password: data.password });
      setToken(response.token);
      setAuthUser({
        userId: response.userId,
        email: response.email,
        firstName: response.firstName,
        lastName: response.lastName,
        organizationId: response.organizationId,
        role: response.role,
      });
      router.push("/dashboard");
    } catch (error) {
      if (axios.isAxiosError(error) && error.response?.data) {
        const d = error.response.data;
        setErrors(Array.isArray(d) ? d : [String(d)]);
      } else {
        setErrors(["Er ging iets mis. Probeer het opnieuw."]);
      }
    } finally {
      setIsLoading(false);
    }
  }

  return (
    <div className="min-h-screen flex">
      {/* Left panel — branding */}
      <div className="hidden lg:flex lg:w-[52%] bg-tennis-green relative overflow-hidden flex-col justify-between p-14">
        <CourtPattern />

        {/* Logo */}
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

        {/* Hero copy */}
        <div className="relative z-10">
          <p className="text-tennis-lime text-sm font-semibold tracking-widest uppercase mb-6 opacity-80">
            Trainer uitnodiging
          </p>
          <h1 className="text-[3.25rem] font-extrabold text-white leading-[1.1] mb-6">
            Welkom bij
            <br />
            <span className="text-tennis-lime">CoachOS</span>
          </h1>
          <p className="text-white/60 text-base max-w-xs leading-relaxed">
            Stel je wachtwoord in en begin direct met het beheren van je lessen.
          </p>
        </div>

        {/* Feature list */}
        <div className="relative z-10 space-y-4">
          {["Plan je lessen eenvoudig", "Beheer leerlingen en inschrijvingen", "Overzicht van al je lesreeksen"].map(
            (feature) => (
              <div key={feature} className="flex items-center gap-3">
                <div className="w-5 h-5 rounded-full bg-tennis-lime/20 flex items-center justify-center flex-shrink-0">
                  <div className="w-1.5 h-1.5 rounded-full bg-tennis-lime" />
                </div>
                <span className="text-white/70 text-sm">{feature}</span>
              </div>
            )
          )}
        </div>
      </div>

      {/* Right panel — form */}
      <div className="w-full lg:w-[48%] flex items-center justify-center p-8 bg-[#FAFAF8]">
        <div className="w-full max-w-[360px]">
          {/* Mobile logo */}
          <div className="flex items-center gap-2 mb-10 lg:hidden">
            <div className="w-7 h-7 rounded-full bg-tennis-green flex items-center justify-center">
              <TennisBallIcon className="w-4 h-4" strokeColor="#D0FF14" strokeWidth={2} />
            </div>
            <span className="text-tennis-green text-xl font-bold">
              Coach<span className="text-tennis-lime">OS</span>
            </span>
          </div>

          {/* Heading */}
          <div className="mb-8">
            <h2 className="text-[1.75rem] font-bold text-gray-900 mb-1 tracking-tight">
              Stel wachtwoord in
            </h2>
            <p className="text-gray-400 text-sm">
              Kies een wachtwoord om je account te activeren.
            </p>
          </div>

          {/* Error banner */}
          {errors.length > 0 && (
            <div className="mb-6 px-4 py-3 rounded-lg bg-red-50 border border-red-100">
              {errors.map((err, i) => (
                <p key={i} className="text-red-600 text-sm">
                  {err}
                </p>
              ))}
            </div>
          )}

          <Form {...form}>
            <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-5">
              <FormField
                control={form.control}
                name="password"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel className="text-gray-600 text-sm font-medium">
                      Wachtwoord
                    </FormLabel>
                    <FormControl>
                      <Input
                        {...field}
                        type="password"
                        autoComplete="new-password"
                        className="h-11 bg-white border-gray-200 rounded-lg text-gray-900 focus-visible:ring-1 focus-visible:ring-tennis-green focus-visible:border-tennis-green transition-colors"
                      />
                    </FormControl>
                    <FormMessage className="text-xs" />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="confirmPassword"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel className="text-gray-600 text-sm font-medium">
                      Bevestig wachtwoord
                    </FormLabel>
                    <FormControl>
                      <Input
                        {...field}
                        type="password"
                        autoComplete="new-password"
                        className="h-11 bg-white border-gray-200 rounded-lg text-gray-900 focus-visible:ring-1 focus-visible:ring-tennis-green focus-visible:border-tennis-green transition-colors"
                      />
                    </FormControl>
                    <FormMessage className="text-xs" />
                  </FormItem>
                )}
              />

              <Button
                type="submit"
                disabled={isLoading}
                className="w-full h-11 bg-tennis-green hover:bg-tennis-green/90 text-white font-semibold rounded-lg transition-colors mt-2 cursor-pointer"
              >
                {isLoading ? (
                  <span className="flex items-center gap-2">
                    <Spinner />
                    Activeren...
                  </span>
                ) : (
                  "Account activeren"
                )}
              </Button>
            </form>
          </Form>
        </div>
      </div>
    </div>
  );
}
