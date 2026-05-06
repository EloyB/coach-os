export const FOOTER = {
  tagline: "Lesplanning voor tennis- en padelclubs in de Benelux.",
  columns: [
    {
      heading: "Product",
      links: [
        { href: "#hoe-het-werkt", label: "Hoe het werkt" },
        { href: "#features", label: "Features" },
        { href: "#faq", label: "FAQ" },
      ],
    },
    {
      heading: "Bedrijf",
      links: [
        { href: "#contact", label: "Boek een demo" },
        { href: "#contact", label: "Neem contact op" },
      ],
    },
    {
      heading: "Juridisch",
      links: [
        { href: "/privacy", label: "Privacy" },
        { href: "/voorwaarden", label: "Voorwaarden" },
      ],
    },
  ],
  languages: "Nederlands · Français binnenkort",
  copyright: `© ${new Date().getFullYear()} CoachOS`,
} as const;
