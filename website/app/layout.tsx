import type { Metadata, Viewport } from "next";
import { Inter, JetBrains_Mono } from "next/font/google";
import "./globals.css";

const inter = Inter({
  variable: "--font-inter",
  subsets: ["latin"],
});

const jetbrainsMono = JetBrains_Mono({
  variable: "--font-jetbrains-mono",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  metadataBase: new URL("https://coach-os.be"),
  title: {
    default: "CoachOS, Lessenplanning voor tennis- en padelscholen",
    template: "%s · CoachOS",
  },
  description:
    "Lessenplanning voor tennis- en padelscholen in Nederland en België. Lessenreeksen, anonieme inschrijvingen, automatische scheduling en magic-link bevestiging.",
  applicationName: "CoachOS",
  authors: [{ name: "CoachOS" }],
  keywords: [
    "lessenplanning tennisschool",
    "lessenplanning padelschool",
    "tennisles inschrijven",
    "padel lessenreeksen",
    "leerlingadministratie tennisschool",
    "trainersplanning",
    "lessenplanning software Benelux",
  ],
  category: "business",
  openGraph: {
    type: "website",
    locale: "nl_BE",
    alternateLocale: ["nl_NL"],
    url: "https://coach-os.be",
    title: "CoachOS, Lessenplanning voor tennis- en padelscholen",
    description:
      "Een planning die zichzelf bevestigt. Lessenreeksen, anonieme inschrijvingen en automatische scheduling voor tennis- en padelscholen in de Benelux.",
    siteName: "CoachOS",
  },
  twitter: {
    card: "summary_large_image",
    title: "CoachOS, Lessenplanning voor tennis- en padelscholen",
    description:
      "Lessenplanning voor tennis- en padelscholen in Nederland en België.",
  },
  alternates: {
    canonical: "https://coach-os.be",
    languages: {
      "nl-BE": "https://coach-os.be",
      "nl-NL": "https://coach-os.be",
      "x-default": "https://coach-os.be",
    },
  },
  robots: {
    index: true,
    follow: true,
    googleBot: {
      index: true,
      follow: true,
      "max-image-preview": "large",
      "max-snippet": -1,
    },
  },
  formatDetection: {
    email: false,
    telephone: false,
  },
  // Search engine ownership verification. Set the env vars in your hosting
  // dashboard (Vercel → Project → Settings → Environment Variables) when
  // you create the GSC + Bing Webmaster Tools properties, no code change
  // needed afterward. Undefined values are silently omitted by Next.js.
  verification: {
    google: process.env.GOOGLE_SITE_VERIFICATION,
    other: process.env.BING_SITE_VERIFICATION
      ? { "msvalidate.01": process.env.BING_SITE_VERIFICATION }
      : undefined,
  },
};

export const viewport: Viewport = {
  themeColor: "#2D5016",
  colorScheme: "light",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="nl">
      <body
        className={`${inter.variable} ${jetbrainsMono.variable} antialiased bg-paper text-ink`}
      >
        {children}
      </body>
    </html>
  );
}
