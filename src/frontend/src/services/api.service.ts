import axios from 'axios';
import router from '@/router';
import { useAuthStore } from '@/stores/auth.store';

const api = axios.create({
  baseURL: '/api/v1',
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request Interceptor: Attach JWT Token if present
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('gopshub_token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Response Interceptor: Global Error handling
api.interceptors.response.use(
  (response) => response.data,
  (error) => {
    if (error.response?.status === 401 || error.response?.status === 403) {
      // Clear token and memory state on auth error
      try {
        const authStore = useAuthStore();
        authStore.logout();
      } catch {
        localStorage.removeItem('gopshub_token');
      }

      if (router.currentRoute.value.path !== '/login') {
        router.push('/login');
      }
    }
    const errorData = error.response?.data;
    if (errorData && typeof errorData === 'object') {
      if (errorData.status === undefined && error.response?.status !== undefined) {
        errorData.status = error.response.status;
      }
      return Promise.reject(errorData);
    }
    return Promise.reject(error.response?.data || error);
  }
);

export default api;
