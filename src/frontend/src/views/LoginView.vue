<template>
  <div class="guest-portal-container">
    <!-- Ambient Background Lighting -->
    <div class="glow-orb glow-indigo"></div>
    <div class="glow-orb glow-cyan"></div>

    <div class="portal-grid">
      <!-- Left Column: System Vision, Value Proposition & Capability Bento -->
      <section class="portal-showcase">
        <div class="showcase-header">
          <div class="status-badge">
            <span class="status-indicator-dot"></span>
            <span class="badge-text">AUTONOMOUS OPERATIONS ENGINE</span>
          </div>

          <h1 class="hero-title">
            Trợ Lý AI & Điều Hành
            <span class="gradient-text">Google Workspace</span>
          </h1>

          <p class="hero-description">
            Nền tảng tự động hóa vận hành toàn diện: làm sạch Gmail bằng học máy Few-shot, trích xuất biến động ngân hàng sang Google Sheets, kiểm soát an ninh DriveGuard và điều khiển qua Telegram ChatOps.
          </p>
        </div>

        <!-- Bento Grid: Core Capabilities -->
        <div class="capabilities-bento">
          <div class="bento-card">
            <div class="bento-icon-wrapper icon-purple">
              <i class="pi pi-envelope"></i>
            </div>
            <div class="bento-content">
              <h3>Regex & Few-Shot AI Email Purge</h3>
              <p>Quét rác 2 pha tối ưu 0 token với Regex kết hợp Gemini AI tự học quy tắc từ phản hồi thực tế của người dùng.</p>
              <div class="bento-meta">
                <span class="pill-tag">0-Token Regex Sweep</span>
                <span class="pill-tag">Dynamic Few-Shot</span>
              </div>
            </div>
          </div>

          <div class="bento-card">
            <div class="bento-icon-wrapper icon-cyan">
              <i class="pi pi-chart-line"></i>
            </div>
            <div class="bento-content">
              <h3>Bank Telemetry to Sheets</h3>
              <p>Tự động bóc tách 11 trường dữ liệu biến động số dư ngân hàng (VPBank, Vietcombank, Momo...) và lưu trữ sổ kế toán định kỳ.</p>
              <div class="bento-meta">
                <span class="pill-tag">11-Field Extraction</span>
                <span class="pill-tag">Read-Only Safe</span>
              </div>
            </div>
          </div>

          <div class="bento-card">
            <div class="bento-icon-wrapper icon-emerald">
              <i class="pi pi-shield"></i>
            </div>
            <div class="bento-content">
              <h3>DriveGuard Security Monitor</h3>
              <p>Tự động cách ly file thực thi nghi vấn (.exe, .bat), phát hiện xóa hàng loạt và giám sát thu hồi quyền chia sẻ công khai.</p>
              <div class="bento-meta">
                <span class="pill-tag">Quarantine Vault</span>
                <span class="pill-tag">Permission Drift</span>
              </div>
            </div>
          </div>

          <div class="bento-card">
            <div class="bento-icon-wrapper icon-amber">
              <i class="pi pi-sync"></i>
            </div>
            <div class="bento-content">
              <h3>Hangfire 24/7 Anti-Sleep</h3>
              <p>4 tác vụ nền vận hành liên tục không gián đoạn, tự động khôi phục phiên Google OAuth khi mã truy cập hết hạn.</p>
              <div class="bento-meta">
                <span class="pill-tag">4 Recurring Daemons</span>
                <span class="pill-tag">Zero Sleep Keep-Alive</span>
              </div>
            </div>
          </div>
        </div>

        <!-- Guest Quick Action -->
        <div class="guest-cta-box">
          <div class="guest-cta-info">
            <i class="pi pi-calendar-plus cta-icon"></i>
            <div>
              <h4>Bạn muốn hẹn lịch hoặc kiểm tra thời gian rảnh?</h4>
              <p>Khách ghé thăm có thể xem khung giờ bận/rảnh công khai không cần tài khoản.</p>
            </div>
          </div>
          <router-link to="/public/calendar" class="guest-schedule-btn">
            <span>Xem Lịch Khách</span>
            <i class="pi pi-arrow-right"></i>
          </router-link>
        </div>
      </section>

      <!-- Right Column: Admin Clearance & Sign-in Portal -->
      <section class="portal-auth">
        <div class="auth-card">
          <div class="card-glow-edge"></div>

          <div class="auth-card-header">
            <div class="auth-brand-badge">
              <i class="pi pi-bolt"></i>
            </div>
            <h2 class="auth-card-title">Cổng Điều Hành</h2>
            <p class="auth-card-sub">Xác thực tài khoản quản trị viên Google Workspace</p>
          </div>

          <!-- Clearance Box -->
          <div class="security-clearance-box">
            <div class="clearance-badge-icon">
              <i class="pi pi-lock"></i>
            </div>
            <div class="clearance-details">
              <div class="clearance-role">TÀI KHOẢN ĐƯỢC ỦY QUYỀN</div>
              <div class="clearance-address">hnt.vn.vn@gmail.com</div>
            </div>
            <span class="clearance-tag">Admin</span>
          </div>

          <!-- Error Feedback Banner -->
          <div v-if="authStore.error" class="error-banner">
            <i class="pi pi-exclamation-triangle"></i>
            <span>{{ authStore.error }}</span>
          </div>

          <!-- Primary Actions -->
          <div class="auth-actions">
            <button 
              type="button"
              class="google-auth-button" 
              @click="handleGoogleSignIn" 
              :disabled="authStore.loading"
              aria-label="Đăng nhập tài khoản Google"
            >
              <svg class="google-svg-icon" viewBox="0 0 24 24" width="20" height="20">
                <path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"/>
                <path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"/>
                <path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.06H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.94l2.85-2.22.81-.63z"/>
                <path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.06l3.66 2.84c.87-2.6 3.3-4.52 6.16-4.52z"/>
              </svg>
              <span class="auth-btn-label">
                {{ authStore.loading ? 'Đang khởi tạo OAuth 2.0...' : 'Đăng nhập với Google' }}
              </span>
              <i v-if="authStore.loading" class="pi pi-spin pi-spinner"></i>
            </button>

            <router-link to="/public/calendar" class="alt-guest-link">
              <i class="pi pi-calendar"></i>
              <span>Truy cập Lịch công khai (Khách vãng lai)</span>
            </router-link>
          </div>

          <!-- Trust & Security Assurances -->
          <div class="security-guarantees">
            <div class="guarantee-item">
              <i class="pi pi-check-circle"></i>
              <span>Mã hóa AES-256 GCM</span>
            </div>
            <div class="guarantee-item">
              <i class="pi pi-check-circle"></i>
              <span>Read-only tài chính</span>
            </div>
            <div class="guarantee-item">
              <i class="pi pi-check-circle"></i>
              <span>Bảo vệ Trash 30 ngày</span>
            </div>
          </div>

          <div class="system-status-chip">
            <span class="pulse-chip-dot"></span>
            <span>Gateway v2.5 Online • 250k Token Shield</span>
          </div>
        </div>
      </section>
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
  const baseUrl = import.meta.env.VITE_API_BASE_URL || '/api/v1';
  window.location.href = `${baseUrl}/auth/google-redirect`;
};

onMounted(async () => {
  const token = route.query.token as string;
  const error = route.query.error as string;

  if (token) {
    authStore.token = token;
    localStorage.setItem('gopshub_token', token);
    router.replace({ path: '/login', query: {} });
    
    await authStore.fetchCurrentUser();
    if (authStore.user) {
      router.push('/dashboard');
    } else {
      authStore.error = 'Không thể tải thông tin quản trị viên.';
      authStore.logout();
    }
  } else if (error) {
    authStore.error = decodeURIComponent(error);
    router.replace({ path: '/login', query: {} });
  } else if (authStore.isAuthenticated) {
    router.push('/dashboard');
  }
});
</script>

<style scoped lang="scss">
.guest-portal-container {
  min-height: calc(100vh - 150px);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 1.5rem 0;
  position: relative;
}

// Background glow orbs
.glow-orb {
  position: absolute;
  pointer-events: none;
  border-radius: 50%;
  filter: blur(100px);
  z-index: 0;

  &.glow-indigo {
    top: -50px;
    left: 20%;
    width: 500px;
    height: 400px;
    background: radial-gradient(circle, rgba(99, 102, 241, 0.15) 0%, transparent 70%);
  }

  &.glow-cyan {
    bottom: 0;
    right: 15%;
    width: 450px;
    height: 350px;
    background: radial-gradient(circle, rgba(6, 182, 212, 0.12) 0%, transparent 70%);
  }
}

// Main 2-column responsive layout
.portal-grid {
  display: grid;
  grid-template-columns: 1.25fr 0.95fr;
  gap: 2.5rem;
  width: 100%;
  align-items: center;
  position: relative;
  z-index: 1;

  @media (max-width: 1024px) {
    grid-template-columns: 1fr;
    gap: 2rem;
  }
}

// Left Column: Showcase
.portal-showcase {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.showcase-header {
  .status-badge {
    display: inline-flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.25rem 0.75rem;
    border-radius: 9999px;
    background: rgba(99, 102, 241, 0.12);
    border: 1px solid rgba(99, 102, 241, 0.3);
    margin-bottom: 0.75rem;

    .status-indicator-dot {
      width: 6px;
      height: 6px;
      border-radius: 50%;
      background: #38bdf8;
      box-shadow: 0 0 6px #38bdf8;
    }

    .badge-text {
      font-size: 0.675rem;
      font-weight: 700;
      letter-spacing: 0.08em;
      color: #a5b4fc;
      font-family: var(--font-mono, monospace);
    }
  }

  .hero-title {
    font-size: 2.25rem;
    font-weight: 800;
    line-height: 1.2;
    letter-spacing: -0.03em;
    color: #f8fafc;
    margin: 0 0 0.75rem 0;

    .gradient-text {
      background: linear-gradient(135deg, #38bdf8 0%, #818cf8 50%, #c084fc 100%);
      -webkit-background-clip: text;
      -webkit-text-fill-color: transparent;
      display: block;
    }

    @media (max-width: 640px) {
      font-size: 1.75rem;
    }
  }

  .hero-description {
    font-size: 0.95rem;
    line-height: 1.6;
    color: #94a3b8;
    margin: 0;
  }
}

// Bento Grid
.capabilities-bento {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 1rem;

  @media (max-width: 640px) {
    grid-template-columns: 1fr;
  }
}

.bento-card {
  background: rgba(15, 23, 42, 0.55);
  border: 1px solid rgba(148, 163, 184, 0.12);
  border-radius: 0.85rem;
  padding: 1.15rem;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  backdrop-filter: blur(12px);
  -webkit-backdrop-filter: blur(12px);
  transition: transform 0.2s cubic-bezier(0.16, 1, 0.3, 1), border-color 0.2s ease, background-color 0.2s ease;

  &:hover {
    transform: translate3d(0, -2px, 0);
    border-color: rgba(99, 102, 241, 0.35);
    background: rgba(15, 23, 42, 0.75);
  }

  .bento-icon-wrapper {
    width: 36px;
    height: 36px;
    border-radius: 0.6rem;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 1.1rem;

    &.icon-purple {
      background: rgba(168, 85, 247, 0.15);
      border: 1px solid rgba(168, 85, 247, 0.3);
      color: #c084fc;
    }

    &.icon-cyan {
      background: rgba(6, 182, 212, 0.15);
      border: 1px solid rgba(6, 182, 212, 0.3);
      color: #38bdf8;
    }

    &.icon-emerald {
      background: rgba(16, 185, 129, 0.15);
      border: 1px solid rgba(16, 185, 129, 0.3);
      color: #34d399;
    }

    &.icon-amber {
      background: rgba(245, 158, 11, 0.15);
      border: 1px solid rgba(245, 158, 11, 0.3);
      color: #fbbf24;
    }
  }

  .bento-content {
    h3 {
      font-size: 0.9rem;
      font-weight: 700;
      color: #f1f5f9;
      margin: 0 0 0.35rem 0;
      letter-spacing: -0.01em;
    }

    p {
      font-size: 0.775rem;
      line-height: 1.45;
      color: #94a3b8;
      margin: 0 0 0.65rem 0;
    }
  }

  .bento-meta {
    display: flex;
    gap: 0.4rem;
    flex-wrap: wrap;

    .pill-tag {
      font-size: 0.65rem;
      font-weight: 600;
      padding: 0.15rem 0.45rem;
      border-radius: 4px;
      background: rgba(255, 255, 255, 0.05);
      color: #cbd5e1;
      border: 1px solid rgba(255, 255, 255, 0.08);
      font-family: var(--font-mono, monospace);
    }
  }
}

// Guest CTA Box
.guest-cta-box {
  background: linear-gradient(135deg, rgba(30, 41, 59, 0.7) 0%, rgba(15, 23, 42, 0.8) 100%);
  border: 1px solid rgba(6, 182, 212, 0.3);
  border-radius: 0.85rem;
  padding: 1.15rem 1.35rem;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1.25rem;
  box-shadow: 0 8px 24px -6px rgba(0, 0, 0, 0.4);

  @media (max-width: 640px) {
    flex-direction: column;
    align-items: flex-start;
  }

  .guest-cta-info {
    display: flex;
    align-items: center;
    gap: 0.85rem;

    .cta-icon {
      font-size: 1.6rem;
      color: #38bdf8;
      flex-shrink: 0;
    }

    h4 {
      font-size: 0.875rem;
      font-weight: 700;
      color: #f8fafc;
      margin: 0 0 0.2rem 0;
    }

    p {
      font-size: 0.775rem;
      color: #94a3b8;
      margin: 0;
    }
  }

  .guest-schedule-btn {
    display: inline-flex;
    align-items: center;
    gap: 0.45rem;
    background: rgba(6, 182, 212, 0.15);
    border: 1px solid rgba(6, 182, 212, 0.4);
    color: #38bdf8;
    text-decoration: none;
    font-size: 0.825rem;
    font-weight: 700;
    padding: 0.55rem 1.15rem;
    border-radius: 0.55rem;
    white-space: nowrap;
    transition: background 0.15s ease, transform 0.15s ease, border-color 0.15s ease;

    &:hover {
      background: rgba(6, 182, 212, 0.25);
      border-color: rgba(6, 182, 212, 0.6);
      transform: translate3d(0, -1.5px, 0);
    }

    &:active {
      transform: scale(0.98);
    }
  }
}

// Right Column: Auth Card
.portal-auth {
  display: flex;
  justify-content: center;
}

.auth-card {
  width: 100%;
  max-width: 440px;
  background: rgba(15, 23, 42, 0.75);
  border: 1px solid rgba(148, 163, 184, 0.16);
  border-radius: 1.25rem;
  padding: 2.25rem 2rem;
  backdrop-filter: blur(24px);
  -webkit-backdrop-filter: blur(24px);
  box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.7), 0 0 35px rgba(99, 102, 241, 0.08);
  position: relative;
  overflow: hidden;

  .card-glow-edge {
    position: absolute;
    top: 0;
    left: 0;
    right: 0;
    height: 2px;
    background: linear-gradient(90deg, transparent, #38bdf8, #818cf8, transparent);
  }
}

.auth-card-header {
  text-align: center;
  margin-bottom: 1.75rem;

  .auth-brand-badge {
    width: 52px;
    height: 52px;
    border-radius: 1rem;
    background: linear-gradient(135deg, rgba(99, 102, 241, 0.25), rgba(6, 182, 212, 0.2));
    border: 1px solid rgba(99, 102, 241, 0.45);
    display: inline-flex;
    align-items: center;
    justify-content: center;
    color: #38bdf8;
    font-size: 1.5rem;
    margin-bottom: 0.85rem;
    box-shadow: 0 0 24px rgba(99, 102, 241, 0.3);
  }

  .auth-card-title {
    font-size: 1.5rem;
    font-weight: 800;
    letter-spacing: -0.02em;
    color: #f8fafc;
    margin: 0 0 0.35rem 0;
  }

  .auth-card-sub {
    font-size: 0.825rem;
    color: #94a3b8;
    margin: 0;
  }
}

.security-clearance-box {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  background: rgba(11, 17, 32, 0.75);
  border: 1px solid rgba(6, 182, 212, 0.28);
  border-radius: 0.75rem;
  padding: 0.75rem 1rem;
  margin-bottom: 1.35rem;

  .clearance-badge-icon {
    width: 32px;
    height: 32px;
    border-radius: 0.5rem;
    background: rgba(6, 182, 212, 0.15);
    display: flex;
    align-items: center;
    justify-content: center;
    color: #22d3ee;
    font-size: 0.95rem;
    flex-shrink: 0;
  }

  .clearance-details {
    flex: 1;
    overflow: hidden;

    .clearance-role {
      font-size: 0.675rem;
      font-weight: 700;
      color: #38bdf8;
      letter-spacing: 0.05em;
      font-family: var(--font-mono, monospace);
    }

    .clearance-address {
      font-size: 0.8rem;
      color: #e2e8f0;
      font-family: var(--font-mono, monospace);
      white-space: nowrap;
      text-overflow: ellipsis;
      overflow: hidden;
    }
  }

  .clearance-tag {
    font-size: 0.65rem;
    font-weight: 700;
    text-transform: uppercase;
    padding: 0.15rem 0.45rem;
    border-radius: 4px;
    background: rgba(16, 185, 129, 0.15);
    border: 1px solid rgba(16, 185, 129, 0.35);
    color: #34d399;
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
}

.auth-actions {
  display: flex;
  flex-direction: column;
  gap: 0.85rem;
}

.google-auth-button {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.75rem;
  height: 46px;
  background: #ffffff;
  color: #0f172a;
  border: 1px solid rgba(255, 255, 255, 0.9);
  border-radius: 0.65rem;
  padding: 0 1.25rem;
  font-weight: 700;
  font-size: 0.9rem;
  cursor: pointer;
  transition: transform 0.15s cubic-bezier(0.16, 1, 0.3, 1), box-shadow 0.15s ease, background-color 0.15s ease;
  box-shadow: 0 4px 14px rgba(0, 0, 0, 0.3);

  &:hover:not(:disabled) {
    background: #f8fafc;
    box-shadow: 0 6px 20px rgba(255, 255, 255, 0.25);
    transform: translate3d(0, -1.5px, 0);
  }

  &:active:not(:disabled) {
    transform: scale(0.98);
  }

  &:disabled {
    opacity: 0.65;
    cursor: not-allowed;
  }

  .google-svg-icon {
    flex-shrink: 0;
  }

  .auth-btn-label {
    letter-spacing: -0.01em;
  }
}

.alt-guest-link {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  height: 40px;
  color: #94a3b8;
  font-size: 0.825rem;
  font-weight: 600;
  text-decoration: none;
  border-radius: 0.55rem;
  border: 1px solid rgba(148, 163, 184, 0.14);
  background: rgba(11, 17, 32, 0.5);
  transition: color 0.15s ease, background 0.15s ease, border-color 0.15s ease, transform 0.15s ease;

  &:hover {
    color: #38bdf8;
    background: rgba(30, 41, 59, 0.65);
    border-color: rgba(6, 182, 212, 0.3);
    transform: translate3d(0, -1px, 0);
  }

  &:active {
    transform: scale(0.98);
  }
}

// Guarantees
.security-guarantees {
  display: flex;
  align-items: center;
  justify-content: space-around;
  margin-top: 1.5rem;
  padding-top: 1.25rem;
  border-top: 1px solid rgba(148, 163, 184, 0.1);
  gap: 0.5rem;
  flex-wrap: wrap;

  .guarantee-item {
    display: flex;
    align-items: center;
    gap: 0.35rem;
    font-size: 0.7rem;
    color: #64748b;

    i {
      color: #10b981;
      font-size: 0.75rem;
    }
  }
}

.system-status-chip {
  margin-top: 1.25rem;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.45rem;
  font-size: 0.675rem;
  color: #64748b;
  font-family: var(--font-mono, monospace);

  .pulse-chip-dot {
    width: 6px;
    height: 6px;
    border-radius: 50%;
    background: #10b981;
    box-shadow: 0 0 6px #10b981;
    animation: pulse-dot 2s infinite ease-in-out;
  }
}

@keyframes pulse-dot {
  0%, 100% {
    opacity: 1;
    transform: scale(1);
  }
  50% {
    opacity: 0.4;
    transform: scale(0.9);
  }
}

@media (prefers-reduced-motion: reduce) {
  .bento-card,
  .guest-schedule-btn,
  .google-auth-button,
  .alt-guest-link,
  .pulse-chip-dot {
    transition: none !important;
    animation: none !important;
  }
}
</style>
