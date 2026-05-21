import { PRICING_VISIBLE } from "@/content/pricing";

export interface NavLink {
  href: string;
  label: string;
}

export const NAV_LINKS: NavLink[] = [
  { href: "/#voor-wie", label: "Voor wie" },
  { href: "/#hoe-het-werkt", label: "Hoe het werkt" },
  { href: "/#features", label: "Features" },
  // { href: "/lessen", label: "Lessen" },
  ...(PRICING_VISIBLE ? [{ href: "/prijzen", label: "Prijzen" }] : []),
  { href: "/#faq", label: "FAQ" },
];

export const NAV_CTA = {
  primary: { label: "Boek een demo", href: "#contact" },
  login: { label: "Inloggen", href: "/login" },
} as const;
