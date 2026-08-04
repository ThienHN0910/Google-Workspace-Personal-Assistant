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
        <!-- Official Google Sign-In Button Container -->
        <div id="google-signin-btn" class="google-btn-wrapper"></div>

        <button v-if="authStore.loading" class="loading-btn" disabled>
          <span>Đang xác thực với Google...</span>
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

onMounted(() => {
  const initGoogleGIS = () => {
    if (typeof google !== 'undefined' && google.accounts) {
      const clientId = import.meta.env.VITE_GOOGLE_CLIENT_ID || '454801425475-d3ta6arq9ftm0ddalbe5fqlddemc53o1.apps.googleusercontent.com';
      
      google.accounts.id.initialize({
        client_id: clientId,
        callback: async (response: any) => {
          const success = await authStore.googleLogin(response.credential);
          if (success) {
            router.push('/dashboard');
          }
        },
      });

      const parent = document.getElementById('google-signin-btn');
      if (parent) {
        parent.innerHTML = '';
        google.accounts.id.renderButton(parent, {
          theme: 'filled_blue',
          size: 'large',
          text: 'continue_with',
          shape: 'pill',
          width: 320
        });
      }
    } else {
      setTimeout(initGoogleGIS, 300);
    }
  };

  initGoogleGIS();
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
  align-items: center;
  gap: 1.25rem;
}

.google-btn-wrapper {
  display: flex;
  justify-content: center;
  width: 100%;
  min-height: 44px;
}

.loading-btn {
  background: rgba(255, 255, 255, 0.1);
  color: #94a3b8;
  border: none;
  border-radius: 0.75rem;
  padding: 0.875rem 1.5rem;
  font-size: 0.9rem;
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
