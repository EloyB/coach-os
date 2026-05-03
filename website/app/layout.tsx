import type { Metadata } from "next";
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
  metadataBase: new URL("https://coachos.nl"),
  title: {
    default: "CoachOS — Lesplanning voor tennis- en padelclubs",
    template: "%s · CoachOS",
  },
  description:
    "Lesreeksen, inschrijvingen en betalingen voor tennis- en padelclubs in de Benelux. Eén systeem, zonder accounts voor leerlingen.",
  openGraph: {
    type: "website",
    locale: "nl_BE",
    url: "https://coachos.nl",
    title: "CoachOS — Lesplanning voor tennis- en padelclubs",
    description:
      "Een planning die zichzelf bevestigt. Lesreeksen, anonieme inschrijvingen en automatische scheduling.",
    siteName: "CoachOS",
  },
  twitter: {
    card: "summary_large_image",
    title: "CoachOS",
    description:
      "Lesplanning voor tennis- en padelclubs in de Benelux.",
  },
  alternates: {
    canonical: "https://coachos.nl",
  },
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
