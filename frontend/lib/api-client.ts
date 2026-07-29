import axios from 'axios';
import { clearAuth } from '@/lib/auth';
import { showApiErrorToast } from '@/lib/api-error-toast';

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

    showApiErrorToast(error, { suppressForbidden: true });

    return Promise.reject(error);
  }
);

export default apiClient;
