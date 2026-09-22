<template>
  <div class="login-container">
    <div class="ambient-glow"></div>
    <div class="ambient-glow ambient-glow-cyan"></div>

    <div class="login-card">
      <div class="login-header">
        <div class="brand-badge">
          <span class="brand-icon">⚡</span>
        </div>
        <h1 class="brand-title">G-Ops Hub</h1>
        <p class="brand-subtitle">Trung tâm điều hành & Tự động hóa Google Workspace</p>
      </div>

      <div class="security-clearance">
        <div class="clearance-icon">
          <i class="pi pi-shield"></i>
        </div>
        <div class="clearance-text">
          <span class="clearance-title">Hệ thống phân quyền Admin</span>
          <span class="clearance-email">hnt.vn.vn@gmail.com</span>
        </div>
      </div>

      <div v-if="authStore.error" class="error-banner">
        <i class="pi pi-exclamation-triangle"></i>
        <span>{{ authStore.error }}</span>
      </div>

      <div class="login-actions">
        <button 
          class="google-btn" 
          @click="handleGoogleSignIn" 
          :disabled="authStore.loading"
        >
          <svg class="google-icon" viewBox="0 0 24 24">
            <path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"/>
            <path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"/>
            <path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.06H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.94l2.85-2.22.81-.63z"/>
            <path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.06l3.66 2.84c.87-2.6 3.3-4.52 6.16-4.52z"/>
          </svg>
          <span class="btn-text">
            {{ authStore.loading ? 'Đang khởi tạo phiên xác thực...' : 'Đăng nhập tài khoản Google' }}
          </span>
          <i v-if="authStore.loading" class="pi pi-spin pi-spinner"></i>
        </button>

        <router-link to="/public/calendar" class="public-guest-link">
          <i class="pi pi-calendar"></i>
          <span>Xem lịch công khai (Guest Scheduler)</span>
          <i class="pi pi-arrow-right arrow-icon"></i>
        </router-link>
      </div>

      <div class="system-meta">
        <span class="meta-tag">
          <span class="pulse-dot"></span>
          Gateway v2.4 Active
        </span>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { useAuthStore } from '@/stores/auth.store';
import { useRouter, useRoute } from 'vue-router';
import { onMounted } from 'vue';

const authStore = useAuthStore();
const router = useRouter();
const route = useRoute();

const handleGoogleSignIn = () => {
  authStore.loading = true;
  // Redirect to backend OAuth consent endpoint
  const baseUrl = import.meta.env.VITE_API_BASE_URL || '/api/v1';
  window.location.href = `${baseUrl}/auth/google-redirect`;
};

onMounted(async () => {
  // Check if we just returned from Google OAuth callback
  const token = route.query.token as string;
  const error = route.query.error as string;

  if (token) {
    authStore.token = token;
    localStorage.setItem('gopshub_token', token);
    
    // Clear URL query params
    router.replace({ path: '/login', query: {} });
    
    // Fetch user and redirect to dashboard
    await authStore.fetchCurrentUser();
    if (authStore.user) {
      router.push('/dashboard');
    } else {
      authStore.error = "Không thể tải thông tin người dùng.";
      authStore.logout();
    }
  } else if (error) {
    authStore.error = decodeURIComponent(error);
    router.replace({ path: '/login', query: {} });
  } else if (authStore.isAuthenticated) {
    // Already logged in
    router.push('/dashboard');
  }
});
</script>

<style scoped lang="scss">
.login-container {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background-color: #070b14;
  padding: 1.5rem;
  position: relative;
  overflow: hidden;
}

.ambient-glow {
  position: absolute;
  top: -150px;
  left: 50%;
  transform: translateX(-50%);
  width: 700px;
  height: 450px;
  background: radial-gradient(circle, rgba(99, 102, 241, 0.16) 0%, transparent 70%);
  pointer-events: none;

  &.ambient-glow-cyan {
    top: 50%;
    left: 20%;
    width: 500px;
    height: 400px;
    background: radial-gradient(circle, rgba(6, 182, 212, 0.1) 0%, transparent 70%);
  }
}

.login-card {
  background: rgba(15, 23, 42, 0.75);
  border: 1px solid rgba(148, 163, 184, 0.15);
  border-top: 1px solid rgba(255, 255, 255, 0.2);
  border-radius: 1.25rem;
  padding: 2.25rem 2rem;
  max-width: 440px;
  width: 100%;
  text-align: center;
  box-shadow: 0 25px 60px -15px rgba(0, 0, 0, 0.8), 0 0 30px rgba(99, 102, 241, 0.08);
  backdrop-filter: blur(20px);
  -webkit-backdrop-filter: blur(20px);
  position: relative;
  z-index: 1;
  animation: modal-spring 0.3s cubic-bezier(0.16, 1, 0.3, 1);
}

.login-header {
  margin-bottom: 1.75rem;

  .brand-badge {
    width: 56px;
    height: 56px;
    border-radius: 1rem;
    background: linear-gradient(135deg, rgba(99, 102, 241, 0.25), rgba(6, 182, 212, 0.2));
    border: 1px solid rgba(99, 102, 241, 0.4);
    display: inline-flex;
    align-items: center;
    justify-content: center;
    margin-bottom: 1rem;
    box-shadow: 0 0 24px rgba(99, 102, 241, 0.3);

    .brand-icon {
      font-size: 1.75rem;
    }
  }

  .brand-title {
    font-size: 1.65rem;
    font-weight: 800;
    letter-spacing: -0.02em;
    background: linear-gradient(135deg, #ffffff 0%, #cbd5e1 100%);
    -webkit-background-clip: text;
    -webkit-text-fill-color: transparent;
    margin-bottom: 0.35rem;
  }

  .brand-subtitle {
    color: #94a3b8;
    font-size: 0.825rem;
    line-height: 1.45;
  }
}

.security-clearance {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  background: rgba(11, 17, 32, 0.7);
  border: 1px solid rgba(6, 182, 212, 0.25);
  border-radius: 0.75rem;
  padding: 0.75rem 1rem;
  margin-bottom: 1.25rem;
  text-align: left;

  .clearance-icon {
    width: 32px;
    height: 32px;
    border-radius: 0.5rem;
    background: rgba(6, 182, 212, 0.15);
    display: flex;
    align-items: center;
    justify-content: center;
    color: #22d3ee;
    font-size: 1rem;
    flex-shrink: 0;
  }

  .clearance-text {
    display: flex;
    flex-direction: column;
    overflow: hidden;

    .clearance-title {
      font-size: 0.725rem;
      font-weight: 700;
      color: #38bdf8;
      letter-spacing: 0.02em;
    }

    .clearance-email {
      font-size: 0.775rem;
      color: #cbd5e1;
      font-family: var(--font-mono, monospace);
      white-space: nowrap;
      text-overflow: ellipsis;
      overflow: hidden;
    }
  }
}

.error-banner {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  background: rgba(244, 63, 94, 0.15);
  border: 1px solid rgba(244, 63, 94, 0.35);
  color: #fca5a5;
  padding: 0.75rem 1rem;
  border-radius: 0.65rem;
  font-size: 0.825rem;
  margin-bottom: 1.25rem;
  text-align: left;

  i {
    font-size: 1rem;
    flex-shrink: 0;
  }
}

.login-actions {
  display: flex;
  flex-direction: column;
  gap: 0.85rem;
}

.google-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.75rem;
  height: 44px;
  background: #ffffff;
  color: #0f172a;
  border: 1px solid rgba(255, 255, 255, 0.8);
  border-radius: 0.65rem;
  padding: 0 1.25rem;
  font-weight: 600;
  font-size: 0.875rem;
  cursor: pointer;
  transition: all 0.15s ease;
  box-shadow: 0 4px 14px rgba(0, 0, 0, 0.3);

  &:hover:not(:disabled) {
    background: #f8fafc;
    box-shadow: 0 6px 20px rgba(255, 255, 255, 0.2);
    transform: translate3d(0, -1.5px, 0);
  }

  &:active:not(:disabled) {
    transform: translate3d(0, 0, 0);
  }

  &:disabled {
    opacity: 0.65;
    cursor: not-allowed;
  }

  .google-icon {
    width: 20px;
    height: 20px;
    flex-shrink: 0;
  }
}

.public-guest-link {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  height: 38px;
  color: #94a3b8;
  font-size: 0.825rem;
  text-decoration: none;
  border-radius: 0.5rem;
  border: 1px solid rgba(148, 163, 184, 0.12);
  background: rgba(11, 17, 32, 0.5);
  transition: all 0.15s ease;

  .arrow-icon {
    font-size: 0.75rem;
    transition: transform 0.15s ease;
  }

  &:hover {
    color: #38bdf8;
    background: rgba(30, 41, 59, 0.6);
    border-color: rgba(6, 182, 212, 0.3);

    .arrow-icon {
      transform: translate3d(2px, 0, 0);
    }
  }
}

.system-meta {
  margin-top: 1.5rem;
  display: flex;
  justify-content: center;

  .meta-tag {
    display: inline-flex;
    align-items: center;
    gap: 0.4rem;
    font-size: 0.7rem;
    color: #64748b;
    font-weight: 600;
    letter-spacing: 0.04em;

    .pulse-dot {
      width: 6px;
      height: 6px;
      border-radius: 50%;
      background: #10b981;
      box-shadow: 0 0 6px #10b981;
      animation: pulse-dot 2s infinite ease-in-out;
    }
  }
}

@media (max-width: 480px) {
  .login-card {
    padding: 1.75rem 1.25rem;
  }
  
  .login-header .brand-title {
    font-size: 1.45rem;
  }
}
</style>
