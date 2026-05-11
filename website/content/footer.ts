export const FOOTER = {
  tagline: "Lessenplanning, eindelijk eenvoudig.",
  columns: [
    {
      heading: "Product",
      links: [
        { href: "/#hoe-het-werkt", label: "Hoe het werkt" },
        { href: "/#features", label: "Features" },
        // { href: "/blog", label: "Blog" },
        { href: "/#faq", label: "FAQ" },
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
