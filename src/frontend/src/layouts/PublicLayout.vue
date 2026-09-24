<template>
  <div class="public-layout">
    <!-- Ambient Cyber Glow Backdrop -->
    <div class="ambient-glow ambient-glow-top"></div>
    <div class="ambient-glow ambient-glow-bottom"></div>

    <header class="public-header">
      <div class="header-container">
        <!-- Logo & Brand Badge -->
        <router-link to="/login" class="brand-link" title="Về trang chủ G-Ops Hub">
          <div class="logo-badge">
            <i class="pi pi-bolt"></i>
          </div>
          <div class="brand-info">
            <div class="brand-title-row">
              <span class="brand-title">G-Ops Hub</span>
              <span class="system-tag">v2.5</span>
            </div>
            <span class="brand-sub">Google Workspace Operations Assistant</span>
          </div>
        </router-link>

        <!-- Navigation & Actions -->
        <div class="header-nav">
          <router-link to="/public/calendar" class="nav-item" active-class="nav-item-active">
            <i class="pi pi-calendar"></i>
            <span>Lịch Hẹn Khách</span>
          </router-link>

          <a 
            href="https://github.com/ThienHN0910/Google-Workspace-Personal-Assistant" 
            target="_blank" 
            rel="noopener noreferrer" 
            class="nav-item icon-only" 
            title="GitHub Repository"
          >
            <i class="pi pi-github"></i>
          </a>

          <router-link to="/login" class="admin-sign-btn">
            <i class="pi pi-shield"></i>
            <span>Admin Portal</span>
          </router-link>
        </div>
      </div>
    </header>

    <main class="public-main">
      <router-view />
    </main>

    <footer class="public-footer">
      <div class="footer-container">
        <div class="footer-left">
          <div class="status-indicator">
            <span class="pulse-dot"></span>
            <span>Gateway Daemon Online • Zero-Idle Keep-Alive</span>
          </div>
          <p class="copyright">
            © 2026 <strong>G-Ops Hub</strong>. Engineered by 
            <a href="https://thienhn.io.vn" target="_blank" rel="noopener noreferrer">ThienHN</a>. 
            All Google Workspace integrations secured via AES-256 GCM.
          </p>
        </div>

        <div class="footer-links">
          <router-link to="/login">Trang Chủ</router-link>
          <span class="separator">•</span>
          <router-link to="/public/calendar">Khung Giờ Bận / Rảnh</router-link>
          <span class="separator">•</span>
          <a href="https://github.com/ThienHN0910/Google-Workspace-Personal-Assistant" target="_blank" rel="noopener noreferrer">Mã Nguồn GitHub</a>
        </div>
      </div>
    </footer>
  </div>
</template>

<script setup lang="ts">
// Public Layout shell
</script>

<style scoped lang="scss">
.public-layout {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  background-color: #070b14;
  color: #f1f5f9;
  position: relative;
  overflow-x: hidden;
}

// Ambient glows
.ambient-glow {
  position: absolute;
  pointer-events: none;
  border-radius: 50%;
  filter: blur(80px);
  z-index: 0;

  &-top {
    top: -120px;
    left: 50%;
    transform: translateX(-50%);
    width: 600px;
    height: 350px;
    background: radial-gradient(circle, rgba(99, 102, 241, 0.16) 0%, transparent 70%);
  }

  &-bottom {
    bottom: -100px;
    right: 5%;
    width: 450px;
    height: 300px;
    background: radial-gradient(circle, rgba(6, 182, 212, 0.1) 0%, transparent 70%);
  }
}

// Header
.public-header {
  position: sticky;
  top: 0;
  z-index: 100;
  background: rgba(11, 17, 32, 0.82);
  backdrop-filter: blur(20px);
  -webkit-backdrop-filter: blur(20px);
  border-bottom: 1px solid rgba(148, 163, 184, 0.12);
  box-shadow: 0 4px 20px -2px rgba(0, 0, 0, 0.4);
}

.header-container {
  max-width: 1240px;
  margin: 0 auto;
  padding: 0.75rem 1.25rem;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
}

.brand-link {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  text-decoration: none;
  transition: transform 0.2s cubic-bezier(0.16, 1, 0.3, 1);

  &:hover {
    transform: translate3d(0, -1px, 0);
  }

  &:active {
    transform: scale(0.98);
  }
}

.logo-badge {
  width: 38px;
  height: 38px;
  border-radius: 0.65rem;
  background: linear-gradient(135deg, rgba(99, 102, 241, 0.28) 0%, rgba(6, 182, 212, 0.18) 100%);
  border: 1px solid rgba(99, 102, 241, 0.45);
  display: flex;
  align-items: center;
  justify-content: center;
  color: #38bdf8;
  font-size: 1.15rem;
  box-shadow: 0 0 16px rgba(56, 189, 248, 0.25);
}

.brand-info {
  display: flex;
  flex-direction: column;
}

.brand-title-row {
  display: flex;
  align-items: center;
  gap: 0.45rem;
}

.brand-title {
  font-size: 1.15rem;
  font-weight: 800;
  letter-spacing: -0.02em;
  background: linear-gradient(135deg, #f8fafc 0%, #cbd5e1 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
}

.system-tag {
  font-size: 0.625rem;
  font-weight: 700;
  text-transform: uppercase;
  padding: 0.1rem 0.4rem;
  border-radius: 9999px;
  background: rgba(99, 102, 241, 0.2);
  border: 1px solid rgba(99, 102, 241, 0.35);
  color: #a5b4fc;
  font-family: var(--font-mono, monospace);
}

.brand-sub {
  font-size: 0.7rem;
  color: #64748b;
  font-weight: 500;
  white-space: nowrap;

  @media (max-width: 640px) {
    display: none;
  }
}

// Nav items
.header-nav {
  display: flex;
  align-items: center;
  gap: 0.65rem;
}

.nav-item {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  color: #94a3b8;
  text-decoration: none;
  font-size: 0.825rem;
  font-weight: 600;
  padding: 0.45rem 0.85rem;
  border-radius: 0.5rem;
  border: 1px solid transparent;
  transition: color 0.15s ease, background 0.15s ease, transform 0.15s ease;

  &:hover {
    color: #e2e8f0;
    background: rgba(255, 255, 255, 0.05);
    border-color: rgba(255, 255, 255, 0.08);
    transform: translate3d(0, -1px, 0);
  }

  &:active {
    transform: scale(0.98);
  }

  &.icon-only {
    padding: 0.45rem;
    font-size: 1rem;
  }

  &-active {
    color: #38bdf8;
    background: rgba(6, 182, 212, 0.1);
    border-color: rgba(6, 182, 212, 0.25);
  }
}

.admin-sign-btn {
  display: inline-flex;
  align-items: center;
  gap: 0.45rem;
  color: #ffffff;
  text-decoration: none;
  font-size: 0.825rem;
  font-weight: 700;
  padding: 0.45rem 1rem;
  border-radius: 0.55rem;
  background: linear-gradient(135deg, #6366f1 0%, #4f46e5 100%);
  border: 1px solid rgba(165, 180, 252, 0.35);
  box-shadow: 0 2px 10px rgba(99, 102, 241, 0.3);
  transition: transform 0.15s ease, box-shadow 0.15s ease, filter 0.15s ease;

  &:hover {
    filter: brightness(1.1);
    transform: translate3d(0, -1.5px, 0);
    box-shadow: 0 4px 16px rgba(99, 102, 241, 0.45);
  }

  &:active {
    transform: scale(0.98);
  }
}

// Main container
.public-main {
  flex: 1;
  width: 100%;
  max-width: 1240px;
  margin: 0 auto;
  padding: 1.5rem 1.25rem 3rem;
  position: relative;
  z-index: 1;

  @media (max-width: 640px) {
    padding: 1rem 0.85rem 2rem;
  }
}

// Footer
.public-footer {
  background: rgba(11, 17, 32, 0.9);
  border-top: 1px solid rgba(148, 163, 184, 0.1);
  padding: 1.5rem 1.25rem;
  position: relative;
  z-index: 1;
}

.footer-container {
  max-width: 1240px;
  margin: 0 auto;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1.5rem;
  flex-wrap: wrap;

  @media (max-width: 768px) {
    flex-direction: column;
    align-items: flex-start;
  }
}

.footer-left {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
}

.status-indicator {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.725rem;
  color: #38bdf8;
  font-family: var(--font-mono, monospace);
  font-weight: 600;

  .pulse-dot {
    width: 6px;
    height: 6px;
    border-radius: 50%;
    background: #10b981;
    box-shadow: 0 0 8px #10b981;
    animation: pulse-dot 2s infinite ease-in-out;
  }
}

.copyright {
  font-size: 0.75rem;
  color: #64748b;
  margin: 0;

  a {
    color: #94a3b8;
    text-decoration: underline;
    text-underline-offset: 2px;

    &:hover {
      color: #38bdf8;
    }
  }
}

.footer-links {
  display: flex;
  align-items: center;
  gap: 0.65rem;
  font-size: 0.775rem;

  a {
    color: #94a3b8;
    text-decoration: none;
    transition: color 0.15s ease;

    &:hover {
      color: #38bdf8;
    }
  }

  .separator {
    color: #334155;
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
  .brand-link,
  .nav-item,
  .admin-sign-btn,
  .pulse-dot {
    transition: none !important;
    animation: none !important;
  }
}
</style>
