import axios from 'axios';
import { loginPathFor } from './redirect';
import { clearAuth } from '@/lib/auth';
import { showApiErrorToast } from '@/lib/api-error-toast';

declare module 'axios' {
  interface AxiosRequestConfig {
    /** Achtergrond-calls waarvan een fout de gebruiker niet mag storen: geen foutmelding tonen. */
    skipErrorToast?: boolean;
    /** Een 409 is hier een waarschuwing die de aanroeper zelf afhandelt (bv. met een bevestigingsdialoog): geen toast. */
    skipConflictToast?: boolean;
  }
}

const apiClient = axios.create({
  baseURL: process.env.NEXT_PUBLIC_API_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

apiClient.interceptors.request.use((config) => {
  if (typeof window !== 'undefined') {
    const token = localStorage.getItem('token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
  }
  return config;
});

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401 && typeof window !== 'undefined') {
      clearAuth();
      window.location.href = loginPathFor(window.location.pathname, window.location.search);
      return Promise.reject(error);
    }

    const isHandledConflict = error.config?.skipConflictToast && error.response?.status === 409;
    if (!error.config?.skipErrorToast && !isHandledConflict) {
      showApiErrorToast(error, { suppressForbidden: true });
    }

    return Promise.reject(error);
  }
);

export default apiClient;
