import axios from 'axios';
import { toast } from 'sonner';
import { clearAuth } from '@/lib/auth';

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
      window.location.href = '/login';
      return Promise.reject(error);
    }

    if (
      error.response?.status === 403 &&
      (error.response.data as { code?: string })?.code === 'subscription_required' &&
      typeof window !== 'undefined' &&
      !window.location.pathname.startsWith('/billing')
    ) {
      window.location.assign('/billing?locked=1');
      return Promise.reject(error);
    }

    // Show toast for all non-401/403 errors
    if (typeof window !== 'undefined' && error.response) {
      const status = error.response.status;
      const data = error.response.data;

      let message = 'Er ging iets mis.';
      if (Array.isArray(data)) {
        message = data.join('\n');
      } else if (typeof data === 'string') {
        message = data;
      } else if (data?.message) {
        message = data.message;
      } else if (data?.title) {
        message = data.title;
      }

      if (status === 403) {
        // Skip toast: access-control is being refined, don't alarm the user.
      } else if (status >= 400 && status < 500) {
        toast.error(message);
      } else if (status >= 500) {
        toast.error('Serverfout — probeer het later opnieuw.');
      }
    }

    return Promise.reject(error);
  }
);

export default apiClient;
