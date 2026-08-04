<template>
  <div class="login-container">
    <div class="login-card">
      <div class="header">
        <span class="icon">⚡</span>
        <h1>G-Ops Hub</h1>
        <p class="subtitle">Hệ thống tự động hóa vận hành Google Workspace</p>
      </div>

      <div class="admin-notice">
        <i class="pi pi-shield"></i>
        <span>Hệ thống dành riêng cho Admin (hnt.vn.vn@gmail.com)</span>
      </div>

      <div v-if="authStore.error" class="error-banner">
        {{ authStore.error }}
      </div>

      <div class="actions">
        <button class="google-btn" @click="handleGoogleSignIn" :disabled="authStore.loading">
          <svg class="google-icon" viewBox="0 0 24 24">
            <path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"/>
            <path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"/>
            <path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.06H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.94l2.85-2.22.81-.63z"/>
            <path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.06l3.66 2.84c.87-2.6 3.3-4.52 6.16-4.52z"/>
          </svg>
          <span>{{ authStore.loading ? 'Đang xác thực...' : 'Đăng nhập bằng Google' }}</span>
        </button>

        <router-link to="/public/calendar" class="public-link">
          Xem lịch công khai (Guest View) &rarr;
        </router-link>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { useAuthStore } from '@/stores/auth.store';
import { useRouter } from 'vue-router';
import { onMounted } from 'vue';

const authStore = useAuthStore();
const router = useRouter();

const handleGoogleSignIn = () => {
  // Demo / Dev OAuth token trigger (in production, GIS popup calls callback)
  if (typeof google !== 'undefined' && google.accounts) {
    google.accounts.id.prompt();
  } else {
    // Fallback for dev testing
    alert("Vui lòng cấu hình VITE_GOOGLE_CLIENT_ID trong file src/frontend/.env để sử dụng Google OAuth thật.");
  }
};

onMounted(() => {
  if (typeof google !== 'undefined' && google.accounts) {
    const clientId = import.meta.env.VITE_GOOGLE_CLIENT_ID || 'your-google-client-id.apps.googleusercontent.com';
    google.accounts.id.initialize({
      client_id: clientId,
      callback: async (response: any) => {
        const success = await authStore.googleLogin(response.credential);
        if (success) {
          router.push('/dashboard');
        }
      },
    });
  }
});
</script>

<style scoped lang="scss">
.login-container {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: radial-gradient(circle at top right, #1e1b4b 0%, #0f172a 100%);
  padding: 1.5rem;
}

.login-card {
  background: #1e293b;
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 1.5rem;
  padding: 2.5rem;
  max-width: 440px;
  width: 100%;
  text-align: center;
  box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.5);
}

.header .icon {
  font-size: 2.5rem;
  display: inline-block;
  margin-bottom: 0.5rem;
}

.header h1 {
  font-size: 1.75rem;
  font-weight: 800;
  margin-bottom: 0.5rem;
  color: #f8fafc;
}

.subtitle {
  color: #94a3b8;
  font-size: 0.875rem;
  margin-bottom: 1.5rem;
}

.admin-notice {
  background: rgba(99, 102, 241, 0.1);
  border: 1px solid rgba(99, 102, 241, 0.3);
  border-radius: 0.75rem;
  padding: 0.75rem 1rem;
  font-size: 0.8rem;
  color: #818cf8;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  margin-bottom: 1.5rem;
}

.error-banner {
  background: rgba(239, 68, 68, 0.15);
  border: 1px solid rgba(239, 68, 68, 0.4);
  color: #fca5a5;
  padding: 0.75rem;
  border-radius: 0.5rem;
  font-size: 0.85rem;
  margin-bottom: 1.5rem;
}

.actions {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.google-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.75rem;
  background: #ffffff;
  color: #1e293b;
  border: none;
  border-radius: 0.75rem;
  padding: 0.875rem 1.5rem;
  font-weight: 600;
  font-size: 0.95rem;
  cursor: pointer;
  transition: all 0.2s ease;

  &:hover:not(:disabled) {
    background: #f1f5f9;
    transform: translateY(-1px);
  }

  &:disabled {
    opacity: 0.7;
    cursor: not-allowed;
  }
}

.google-icon {
  width: 20px;
  height: 20px;
}

.public-link {
  color: #94a3b8;
  font-size: 0.85rem;
  text-decoration: none;
  transition: color 0.2s ease;

  &:hover {
    color: #6366f1;
  }
}
</style>
