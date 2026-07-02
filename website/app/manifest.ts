import type { MetadataRoute } from "next";

export default function manifest(): MetadataRoute.Manifest {
  return {
    name: "CoachOS, Lessenplanning voor tennis- en padelscholen",
    short_name: "CoachOS",
    description:
      "Lessenplanning, inschrijvingen en betalingen voor tennis- en padelscholen.",
    start_url: "/",
    display: "standalone",
    background_color: "#fdfcf9",
    theme_color: "#2D5016",
    lang: "nl",
    icons: [
      {
        src: "/icon",
        sizes: "32x32",
        type: "image/png",
      },
      {
        src: "/apple-icon",
        sizes: "180x180",
        type: "image/png",
      },
    ],
  };
}
