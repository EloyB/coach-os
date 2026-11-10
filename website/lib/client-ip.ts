import { isIP } from "node:net";

// nginx overschrijft X-Real-IP altijd met het echte bezoekers-IP en de website is in productie
// enkel via nginx bereikbaar. We sturen alleen dat ene, gevalideerde adres door, zodat de API
// per bezoeker kan rate-limiten. Een door de client meegestuurde X-Forwarded-For telt nooit.
export function forwardedClientHeaders(req: Request): Record<string, string> {
  const ip = req.headers.get("x-real-ip")?.trim();
  return ip && isIP(ip) ? { "X-Forwarded-For": ip } : {};
}
