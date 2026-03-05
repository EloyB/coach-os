"use client";

import { useEffect, useState } from "react";
import { getAuthUser } from "@/lib/auth";

export function WelcomeHeader() {
  const [name, setName] = useState<string | null>(null);
  const [isTrainer, setIsTrainer] = useState(false);

  useEffect(() => {
    const user = getAuthUser();
    if (user) {
      setName(user.firstName);
      setIsTrainer(user.role === "Trainer");
    }
  }, []);

  const greeting = name ? `Goedemorgen, ${name} 👋` : "Goedemorgen, Coach 👋";

  return (
    <div className="mb-8">
      <h1 className="text-2xl font-bold text-gray-900 tracking-tight">
        {greeting}
      </h1>
      <p className="text-gray-400 text-sm mt-1">
        {isTrainer
          ? "Bekijk en beheer je eigen lesreeksen."
          : "Hier is een overzicht van vandaag."}
      </p>
    </div>
  );
}
