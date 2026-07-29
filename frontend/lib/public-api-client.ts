import axios from "axios";
import { showApiErrorToast } from "@/lib/api-error-toast";

/**
 * Axios client for anonymous/public endpoints.
 *
 * Do not attach the logged-in user's bearer token here: public links must behave
 * the same for visitors who happen to have an active CoachOS session in another
 * organization as they do in an incognito window.
 */
const publicApiClient = axios.create({
  baseURL: process.env.NEXT_PUBLIC_API_URL,
  headers: {
    "Content-Type": "application/json",
  },
});

publicApiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    showApiErrorToast(error);
    return Promise.reject(error);
  }
);

export default publicApiClient;
