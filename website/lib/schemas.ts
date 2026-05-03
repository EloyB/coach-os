import { z } from "zod";

export const contactSchema = z.object({
  name: z.string().min(2, "Naam is verplicht").max(100),
  organization: z.string().max(100).optional().or(z.literal("")),
  email: z.string().email("Ongeldig e-mailadres"),
  message: z.string().min(10, "Vertel iets meer").max(2000),
  website: z.string().max(200).optional().or(z.literal("")),
});

export const waitlistSchema = z.object({
  email: z.string().email("Ongeldig e-mailadres"),
  role: z.enum(["club", "coach", "anders"]).optional(),
  website: z.string().max(200).optional().or(z.literal("")),
});

export type ContactValues = z.infer<typeof contactSchema>;
export type WaitlistValues = z.infer<typeof waitlistSchema>;
