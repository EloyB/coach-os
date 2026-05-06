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
    default: "CoachOS — Lesplanning voor tennis- en padelclubs",
    template: "%s · CoachOS",
  },
  description:
    "Lesplanning voor tennis- en padelclubs in Nederland en België. Lesreeksen, anonieme inschrijvingen, automatische scheduling en magic-link bevestiging.",
  applicationName: "CoachOS",
  authors: [{ name: "CoachOS" }],
  keywords: [
    "lesplanning tennisclub",
    "lesplanning padelclub",
    "tennisles inschrijven",
    "padel lesreeksen",
    "ledenadministratie tennisclub",
    "trainersplanning",
    "sportclub software Benelux",
  ],
  category: "business",
  openGraph: {
    type: "website",
    locale: "nl_BE",
    alternateLocale: ["nl_NL"],
    url: "https://coach-os.be",
    title: "CoachOS — Lesplanning voor tennis- en padelclubs",
    description:
      "Een planning die zichzelf bevestigt. Lesreeksen, anonieme inschrijvingen en automatische scheduling voor tennis- en padelclubs in de Benelux.",
    siteName: "CoachOS",
  },
  twitter: {
    card: "summary_large_image",
    title: "CoachOS — Lesplanning voor tennis- en padelclubs",
    description:
      "Lesplanning voor tennis- en padelclubs in Nederland en België.",
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
