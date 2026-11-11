<template>
  <div class="google-reconnect-notice">
    <div class="notice-card">
      <div class="notice-icon-wrapper">
        <svg class="google-svg-icon" viewBox="0 0 24 24" width="28" height="28" aria-hidden="true">
          <path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"/>
          <path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"/>
          <path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.06H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.94l2.85-2.22.81-.63z"/>
          <path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.06l3.66 2.84c.87-2.6 3.3-4.52 6.16-4.52z"/>
        </svg>
      </div>
      <div class="notice-body">
        <h3 class="notice-title">Yêu cầu kết nối tài khoản Google</h3>
        <p class="notice-message">{{ message || defaultMessage }}</p>
      </div>
      <button type="button" class="btn-reconnect" @click="handleReconnect">
        <i class="pi pi-sync"></i>
        <span>Kết nối lại Google</span>
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { DEFAULT_GOOGLE_DISCONNECTED_MESSAGE } from '@/utils/googleConnection';

withDefaults(
  defineProps<{
    message?: string;
  }>(),
  {
    message: ''
  }
);

const defaultMessage = DEFAULT_GOOGLE_DISCONNECTED_MESSAGE;

const emit = defineEmits<{
  (e: 'reconnect'): void;
}>();

const handleReconnect = () => {
  emit('reconnect');
  const baseUrl = import.meta.env.VITE_API_BASE_URL || '/api/v1';
  window.location.href = `${baseUrl}/auth/google-redirect`;
};
</script>

<style scoped lang="scss">
.google-reconnect-notice {
  display: flex;
  justify-content: center;
  align-items: center;
  padding: 3rem 1.5rem;
  width: 100%;
}

.notice-card {
  max-width: 520px;
  width: 100%;
  background: rgba(15, 23, 42, 0.75);
  border: 1px solid rgba(245, 158, 11, 0.35);
  border-top: 1px solid rgba(251, 191, 36, 0.5);
  border-radius: 1rem;
  padding: 2rem 1.75rem;
  text-align: center;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1.15rem;
  backdrop-filter: blur(14px);
  -webkit-backdrop-filter: blur(14px);
  box-shadow: 0 16px 36px -8px rgba(0, 0, 0, 0.6), 0 0 24px rgba(245, 158, 11, 0.12);

  .notice-icon-wrapper {
    width: 56px;
    height: 56px;
    border-radius: 1rem;
    background: linear-gradient(135deg, rgba(245, 158, 11, 0.2), rgba(239, 68, 68, 0.15));
    border: 1px solid rgba(245, 158, 11, 0.4);
    display: flex;
    align-items: center;
    justify-content: center;
    box-shadow: 0 0 20px rgba(245, 158, 11, 0.2);
  }

  .notice-body {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;

    .notice-title {
      font-size: 1.15rem;
      font-weight: 700;
      color: #f8fafc;
      margin: 0;
    }

    .notice-message {
      font-size: 0.875rem;
      color: #94a3b8;
      line-height: 1.55;
      margin: 0;
      word-break: break-word;
    }
  }

  .btn-reconnect {
    margin-top: 0.35rem;
    height: 42px;
    padding: 0 1.5rem;
    background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%);
    border: 1px solid rgba(251, 191, 36, 0.4);
    border-radius: 0.6rem;
    color: #ffffff;
    font-size: 0.875rem;
    font-weight: 600;
    cursor: pointer;
    display: inline-flex;
    align-items: center;
    gap: 0.5rem;
    transition: all 0.2s cubic-bezier(0.16, 1, 0.3, 1);
    box-shadow: 0 4px 14px rgba(245, 158, 11, 0.35);

    &:hover {
      filter: brightness(1.1);
      transform: translateY(-2px);
      box-shadow: 0 6px 20px rgba(245, 158, 11, 0.5);
    }

    &:active {
      transform: translateY(0);
    }
  }
}
</style>
