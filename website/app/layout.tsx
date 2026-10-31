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
    default: "CoachOS: lessenplanning voor tennis- en padelclubs",
    template: "%s · CoachOS",
  },
  description:
    "Lessenplanning voor tennis- en padelclubs: lessenreeksen, inschrijvingen, kampen, betalingen en automatische planning in één systeem. 30 dagen gratis proberen.",
  applicationName: "CoachOS",
  authors: [{ name: "CoachOS" }],
  category: "business",
  openGraph: {
    type: "website",
    locale: "nl_BE",
    alternateLocale: ["nl_NL"],
    url: "https://coach-os.be",
    title: "CoachOS: lessenplanning voor tennis- en padelclubs",
    description:
      "Lessenplanning, eindelijk eenvoudig. Lessenreeksen, inschrijvingen zonder account en automatische planning voor tennis- en padelclubs.",
    siteName: "CoachOS",
  },
  twitter: {
    card: "summary_large_image",
    title: "CoachOS: lessenplanning voor tennis- en padelclubs",
    description:
      "Lessenplanning voor tennis- en padelclubs in Nederland en België.",
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
  // you create the GSC + Bing Webmaster Tools properties — no code change
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
    <html lang="nl-BE">
      <body
        className={`${inter.variable} ${jetbrainsMono.variable} antialiased bg-paper text-ink`}
      >
        {children}
      </body>
    </html>
  );
}
