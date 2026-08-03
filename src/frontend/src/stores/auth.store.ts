import { defineStore } from 'pinia';
import api from '@/services/api.service';

export interface User {
  id: string;
  email: string;
  displayName: string;
  avatarUrl?: string;
  role: string;
}

export const useAuthStore = defineStore('auth', {
  state: () => ({
    token: localStorage.getItem('gopshub_token') || (null as string | null),
    user: null as User | null,
    loading: false,
    error: null as string | null,
  }),

  getters: {
    isAuthenticated: (state) => !!state.token,
    isAdmin: (state) => state.user?.email === 'hnt.vn.vn@gmail.com',
  },

  actions: {
    async googleLogin(idToken: string) {
      this.loading = true;
      this.error = null;
      try {
        const res: any = await api.post('/auth/google-login', { idToken });
        if (res.success && res.data) {
          this.token = res.data.accessToken;
          this.user = res.data.user;
          localStorage.setItem('gopshub_token', res.data.accessToken);
          return true;
        }
        return false;
      } catch (err: any) {
        this.error = err.message || 'Đăng nhập thất bại. Chỉ admin mới có quyền truy cập.';
        return false;
      } finally {
        this.loading = false;
      }
    },

    async fetchCurrentUser() {
      if (!this.token) return;
      try {
        const res: any = await api.get('/auth/me');
        if (res.success) {
          this.user = res.data;
        }
      } catch {
        this.logout();
      }
    },

    logout() {
      this.token = null;
      this.user = null;
      localStorage.removeItem('gopshub_token');
    },
  },
});
