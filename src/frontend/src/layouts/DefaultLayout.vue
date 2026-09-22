<template>
  <div class="app-layout">
    <div class="ambient-glow"></div>

    <!-- Mobile Topbar (Sticky on mobile & tablet) -->
    <header class="mobile-topbar">
      <div class="mobile-logo">
        <span class="logo-icon-badge">⚡</span>
        <span class="logo-text">G-Ops Hub</span>
      </div>
      <button 
        class="mobile-menu-btn" 
        @click="toggleDrawer" 
        :aria-label="isDrawerOpen ? 'Đóng menu' : 'Mở menu'"
      >
        <i :class="isDrawerOpen ? 'pi pi-times' : 'pi pi-bars'"></i>
      </button>
    </header>

    <!-- Backdrop for mobile drawer -->
    <div 
      class="drawer-backdrop" 
      v-if="isDrawerOpen" 
      @click="closeDrawer"
    ></div>

    <!-- Sidebar (Desktop standard / Mobile sliding drawer) -->
    <aside class="sidebar" :class="{ 'drawer-open': isDrawerOpen }">
      <div class="sidebar-header">
        <div class="logo">
          <div class="logo-icon-badge">⚡</div>
          <div class="logo-info">
            <span class="logo-text">G-Ops Hub</span>
            <span class="system-status">
              <span class="pulse-dot"></span>
              SYS ONLINE
            </span>
          </div>
        </div>
      </div>

      <nav class="nav-menu">
        <router-link to="/dashboard" class="nav-item" @click="closeDrawer">
          <i class="pi pi-home"></i>
          <span>Tổng quan (Dashboard)</span>
        </router-link>
        <router-link to="/email" class="nav-item" @click="closeDrawer">
          <i class="pi pi-inbox"></i>
          <span>Email Operations</span>
        </router-link>
        <router-link to="/calendar" class="nav-item" @click="closeDrawer">
          <i class="pi pi-calendar"></i>
          <span>Lịch làm việc (Calendar)</span>
        </router-link>
        <router-link to="/tasks" class="nav-item" @click="closeDrawer">
          <i class="pi pi-check-square"></i>
          <span>Nhiệm vụ (Tasks)</span>
        </router-link>
        <router-link to="/finance" class="nav-item" @click="closeDrawer">
          <i class="pi pi-wallet"></i>
          <span>Tài chính (Finance)</span>
        </router-link>
        <router-link to="/drive-guard" class="nav-item" @click="closeDrawer">
          <i class="pi pi-shield"></i>
          <span>Drive Guard</span>
        </router-link>
        <router-link to="/settings" class="nav-item" @click="closeDrawer">
          <i class="pi pi-cog"></i>
          <span>Cài đặt hệ thống</span>
        </router-link>

        <div class="nav-divider"></div>

        <router-link to="/public/calendar" class="nav-item public-item" @click="closeDrawer">
          <i class="pi pi-eye"></i>
          <span>Đặt lịch khách (Guest)</span>
        </router-link>
      </nav>

      <div class="user-profile" v-if="authStore.user">
        <div class="avatar-wrapper">
          <img :src="authStore.user.avatarUrl || 'https://via.placeholder.com/40'" class="avatar" alt="Avatar" />
          <span class="user-status-dot"></span>
        </div>
        <div class="user-info">
          <div class="user-name" :title="authStore.user.displayName">{{ authStore.user.displayName }}</div>
          <div class="user-email" :title="authStore.user.email">{{ authStore.user.email }}</div>
        </div>
        <button class="logout-btn" @click="handleLogout" title="Đăng xuất">
          <i class="pi pi-sign-out"></i>
        </button>
      </div>
    </aside>

    <!-- Main Content Area -->
    <main class="main-content">
      <router-view />
    </main>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { useAuthStore } from '@/stores/auth.store';
import { useRouter } from 'vue-router';

const authStore = useAuthStore();
const router = useRouter();
const isDrawerOpen = ref(false);

const toggleDrawer = () => {
  isDrawerOpen.value = !isDrawerOpen.value;
};

const closeDrawer = () => {
  isDrawerOpen.value = false;
};

const handleLogout = () => {
  closeDrawer();
  authStore.logout();
  router.push('/login');
};
</script>

<style scoped lang="scss">
.app-layout {
  display: flex;
  min-height: 100vh;
  position: relative;
  background-color: #070b14;
}

/* ============================================================
   MOBILE TOPBAR
   ============================================================ */
.mobile-topbar {
  display: none;
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  height: 56px;
  background: rgba(11, 17, 32, 0.88);
  backdrop-filter: blur(16px);
  -webkit-backdrop-filter: blur(16px);
  border-bottom: 1px solid rgba(148, 163, 184, 0.12);
  z-index: 1000;
  padding: 0 1rem;
  align-items: center;
  justify-content: space-between;

  .mobile-logo {
    display: flex;
    align-items: center;
    gap: 0.6rem;

    .logo-icon-badge {
      width: 32px;
      height: 32px;
      border-radius: 0.5rem;
      background: linear-gradient(135deg, rgba(99, 102, 241, 0.25), rgba(6, 182, 212, 0.2));
      border: 1px solid rgba(99, 102, 241, 0.35);
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 1rem;
    }

    .logo-text {
      font-size: 1.05rem;
      font-weight: 800;
      letter-spacing: -0.01em;
      background: linear-gradient(135deg, #ffffff 0%, #cbd5e1 100%);
      -webkit-background-clip: text;
      -webkit-text-fill-color: transparent;
    }
  }

  .mobile-menu-btn {
    width: 38px;
    height: 38px;
    background: rgba(30, 41, 59, 0.6);
    border: 1px solid rgba(148, 163, 184, 0.2);
    border-radius: 0.5rem;
    color: #cbd5e1;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 1.15rem;
    cursor: pointer;
    transition: all 0.15s ease;

    &:active {
      transform: scale(0.95);
      background: rgba(99, 102, 241, 0.25);
    }
  }
}

.drawer-backdrop {
  display: none;
  position: fixed;
  inset: 0;
  background: rgba(3, 7, 18, 0.7);
  backdrop-filter: blur(4px);
  -webkit-backdrop-filter: blur(4px);
  z-index: 1050;
  animation: fade-in 0.2s ease;
}

@keyframes fade-in {
  from { opacity: 0; }
  to { opacity: 1; }
}

/* ============================================================
   CYBER-COCKPIT SIDEBAR
   ============================================================ */
.sidebar {
  width: 250px;
  background: rgba(11, 17, 32, 0.85);
  backdrop-filter: blur(18px);
  -webkit-backdrop-filter: blur(18px);
  border-right: 1px solid rgba(148, 163, 184, 0.12);
  display: flex;
  flex-direction: column;
  padding: 1.25rem 0.85rem;
  position: sticky;
  top: 0;
  height: 100vh;
  z-index: 1100;
  transition: transform 0.25s cubic-bezier(0.16, 1, 0.3, 1);
}

.sidebar-header {
  margin-bottom: 1.25rem;
  padding: 0 0.4rem;

  .logo {
    display: flex;
    align-items: center;
    gap: 0.75rem;

    .logo-icon-badge {
      width: 38px;
      height: 38px;
      border-radius: 0.65rem;
      background: linear-gradient(135deg, rgba(99, 102, 241, 0.3), rgba(6, 182, 212, 0.2));
      border: 1px solid rgba(99, 102, 241, 0.4);
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 1.15rem;
      box-shadow: 0 0 16px rgba(99, 102, 241, 0.25);
    }

    .logo-info {
      display: flex;
      flex-direction: column;
      gap: 0.15rem;

      .logo-text {
        font-size: 1.15rem;
        font-weight: 800;
        letter-spacing: -0.02em;
        background: linear-gradient(135deg, #ffffff 0%, #cbd5e1 100%);
        -webkit-background-clip: text;
        -webkit-text-fill-color: transparent;
      }

      .system-status {
        display: flex;
        align-items: center;
        gap: 0.35rem;
        font-size: 0.675rem;
        font-weight: 700;
        letter-spacing: 0.05em;
        color: #34d399;

        .pulse-dot {
          width: 6px;
          height: 6px;
          border-radius: 50%;
          background: #10b981;
          box-shadow: 0 0 8px #10b981;
          animation: pulse-dot 2s infinite ease-in-out;
        }
      }
    }
  }
}

.nav-menu {
  display: flex;
  flex-direction: column;
  gap: 0.3rem;
  flex: 1;
  overflow-y: auto;
  padding-right: 0.2rem;
}

.nav-divider {
  height: 1px;
  background: rgba(148, 163, 184, 0.12);
  margin: 0.65rem 0.5rem;
}

.nav-item {
  display: flex;
  align-items: center;
  gap: 0.65rem;
  height: 38px;
  padding: 0 0.75rem;
  border-radius: 0.5rem;
  color: #94a3b8;
  text-decoration: none;
  font-weight: 500;
  font-size: 0.825rem;
  border: 1px solid transparent;
  transition: all 0.15s ease;
  position: relative;

  i {
    font-size: 0.95rem;
    color: #64748b;
    transition: color 0.15s ease;
  }

  &:hover {
    background: rgba(30, 41, 59, 0.6);
    color: #f1f5f9;
    transform: translate3d(2px, 0, 0);

    i {
      color: #94a3b8;
    }
  }

  &.router-link-active {
    background: linear-gradient(90deg, rgba(99, 102, 241, 0.18) 0%, rgba(6, 182, 212, 0.06) 100%);
    border: 1px solid rgba(99, 102, 241, 0.3);
    color: #f8fafc;
    font-weight: 600;

    i {
      color: #38bdf8;
    }

    &::before {
      content: '';
      position: absolute;
      left: 0;
      top: 20%;
      bottom: 20%;
      width: 3px;
      border-radius: 0 2px 2px 0;
      background: #06b6d4;
      box-shadow: 0 0 8px #06b6d4;
    }
  }
}

.public-item {
  color: #64748b;
  font-size: 0.8rem;
  opacity: 0.9;

  &:hover {
    color: #a5b4fc;
    i { color: #a5b4fc; }
  }
}

.user-profile {
  display: flex;
  align-items: center;
  gap: 0.65rem;
  padding: 0.75rem 0.5rem 0.25rem;
  border-top: 1px solid rgba(148, 163, 184, 0.12);
  margin-top: auto;

  .avatar-wrapper {
    position: relative;
    width: 34px;
    height: 34px;

    .avatar {
      width: 100%;
      height: 100%;
      border-radius: 50%;
      border: 1px solid rgba(99, 102, 241, 0.4);
      object-fit: cover;
    }

    .user-status-dot {
      position: absolute;
      bottom: 0;
      right: 0;
      width: 8px;
      height: 8px;
      background: #10b981;
      border-radius: 50%;
      border: 1.5px solid #0b1120;
    }
  }

  .user-info {
    flex: 1;
    overflow: hidden;

    .user-name {
      font-size: 0.8rem;
      font-weight: 600;
      color: #f1f5f9;
      white-space: nowrap;
      text-overflow: ellipsis;
      overflow: hidden;
    }

    .user-email {
      font-size: 0.7rem;
      color: #64748b;
      white-space: nowrap;
      text-overflow: ellipsis;
      overflow: hidden;
    }
  }

  .logout-btn {
    width: 30px;
    height: 30px;
    background: rgba(244, 63, 94, 0.1);
    border: 1px solid rgba(244, 63, 94, 0.2);
    border-radius: 0.4rem;
    color: #fb7185;
    cursor: pointer;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 0.9rem;
    transition: all 0.15s ease;

    &:hover {
      background: #f43f5e;
      color: #fff;
      transform: translate3d(0, -1px, 0);
      box-shadow: 0 2px 8px rgba(244, 63, 94, 0.35);
    }
  }
}

.main-content {
  flex: 1;
  padding: 1.5rem 2rem;
  overflow-y: auto;
  position: relative;
  z-index: 1;
  max-width: 1600px;
  margin: 0 auto;
  width: 100%;
}

/* ============================================================
   RESPONSIVE MEDIA QUERIES
   ============================================================ */
@media (max-width: 1024px) {
  .sidebar {
    width: 230px;
  }
  .main-content {
    padding: 1.25rem 1.5rem;
  }
}

@media (max-width: 860px) {
  .mobile-topbar {
    display: flex;
  }

  .drawer-backdrop {
    display: block;
  }

  .app-layout {
    flex-direction: column;
    padding-top: 56px;
  }

  .sidebar {
    position: fixed;
    top: 0;
    bottom: 0;
    left: 0;
    width: 260px;
    height: 100vh;
    transform: translate3d(-100%, 0, 0);
    box-shadow: none;

    &.drawer-open {
      transform: translate3d(0, 0, 0);
      box-shadow: 0 0 30px rgba(0, 0, 0, 0.8);
    }
  }

  .main-content {
    padding: 1rem;
    width: 100%;
  }
}

@media (max-width: 480px) {
  .main-content {
    padding: 0.75rem 0.5rem;
  }
}
</style>
