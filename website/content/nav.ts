export interface NavLink {
  href: string;
  label: string;
}

export const NAV_LINKS: NavLink[] = [
  { href: "#voor-wie", label: "Voor wie" },
  { href: "#hoe-het-werkt", label: "Hoe het werkt" },
  { href: "#features", label: "Features" },
  { href: "#faq", label: "FAQ" },
];

export const NAV_CTA = {
  primary: { label: "Op de wachtlijst", href: "#contact" },
  login: { label: "Inloggen", href: "/login" },
} as const;
