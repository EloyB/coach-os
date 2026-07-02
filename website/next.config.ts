import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  output: "standalone",
  async redirects() {
    // Persona routes renamed from "clubs" to "scholen" (pre-launch rebrand).
    return [
      {
        source: "/voor-tennisclubs",
        destination: "/voor-tennisscholen",
        permanent: true,
      },
      {
        source: "/voor-padelclubs",
        destination: "/voor-padelscholen",
        permanent: true,
      },
    ];
  },
};

export default nextConfig;
