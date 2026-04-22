import axios from "axios";

/**
 * Unwraps an axios error response body into a list of user-facing messages.
 * Backend returns either a string[] of validation errors or a single string
 * (ProblemDetails-style). Anything else falls back to `fallbackMessage`.
 */
export function getAxiosErrorMessages(
  error: unknown,
  fallbackMessage: string
): string[] {
  if (axios.isAxiosError(error) && error.response?.data) {
    const data = error.response.data;
    if (Array.isArray(data)) return data;
    if (typeof data === "string") return [data];
  }
  return [fallbackMessage];
}
