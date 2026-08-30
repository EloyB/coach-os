import type { AxiosError } from "axios";
import { toast } from "sonner";

interface ApiErrorToastOptions {
  suppressForbidden?: boolean;
}

function extractApiErrorMessage(data: unknown): string {
  if (Array.isArray(data)) {
    return data.join("\n");
  }
  if (typeof data === "string") {
    return data;
  }
  if (data && typeof data === "object") {
    if ("message" in data && typeof data.message === "string") {
      return data.message;
    }
    if ("title" in data && typeof data.title === "string") {
      return data.title;
    }
  }

  return "Er ging iets mis.";
}

export function showApiErrorToast(
  error: AxiosError,
  options: ApiErrorToastOptions = {}
): void {
  if (typeof window === "undefined" || !error.response) return;

  const status = error.response.status;
  if (status === 403 && options.suppressForbidden) return;

  if (status >= 400 && status < 500) {
    toast.error(extractApiErrorMessage(error.response.data));
  } else if (status >= 500) {
    toast.error("Serverfout — probeer het later opnieuw.");
  }
}
