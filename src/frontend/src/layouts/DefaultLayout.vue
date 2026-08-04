<template>
  <div class="app-layout">
    <!-- Sidebar -->
    <aside class="sidebar">
      <div class="logo">
        <span class="logo-icon">⚡</span>
        <span class="logo-text">G-Ops Hub</span>
      </div>

      <nav class="nav-menu">
        <router-link to="/dashboard" class="nav-item">
          <i class="pi pi-home"></i>
          <span>Dashboard</span>
        </router-link>
        <router-link to="/email" class="nav-item">
          <i class="pi pi-inbox"></i>
          <span>Email Operations</span>
        </router-link>
        <router-link to="/calendar" class="nav-item">
          <i class="pi pi-calendar"></i>
          <span>Scheduling</span>
        </router-link>
        <router-link to="/tasks" class="nav-item">
          <i class="pi pi-check-square"></i>
          <span>Google Tasks</span>
        </router-link>
        <router-link to="/finance" class="nav-item">
          <i class="pi pi-wallet"></i>
          <span>Finance Telemetry</span>
        </router-link>
        <router-link to="/drive-guard" class="nav-item">
          <i class="pi pi-shield"></i>
          <span>Drive Guard</span>
        </router-link>

        <div class="nav-divider"></div>

        <router-link to="/public/calendar" class="nav-item public-item">
          <i class="pi pi-eye"></i>
          <span>Public Guest View</span>
        </router-link>
      </nav>

      <div class="user-profile" v-if="authStore.user">
        <img :src="authStore.user.avatarUrl || 'https://via.placeholder.com/40'" class="avatar" />
        <div class="user-info">
          <div class="user-name">{{ authStore.user.displayName }}</div>
          <div class="user-email">{{ authStore.user.email }}</div>
        </div>
        <button class="logout-btn" @click="handleLogout" title="Đăng xuất">
          <i class="pi pi-sign-out"></i>
        </button>
      </div>
    </aside>

    <!-- Main Content -->
    <main class="main-content">
      <router-view />
    </main>
  </div>
</template>

<script setup lang="ts">
import { useAuthStore } from '@/stores/auth.store';
import { useRouter } from 'vue-router';

const authStore = useAuthStore();
const router = useRouter();

const handleLogout = () => {
  authStore.logout();
  router.push('/login');
};
</script>

<style scoped lang="scss">
.app-layout {
  display: flex;
  min-height: 100vh;
}

.sidebar {
  width: 260px;
  background: #1e293b;
  border-right: 1px solid rgba(255, 255, 255, 0.1);
  display: flex;
  flex-direction: column;
  padding: 1.5rem 1rem;
}

.logo {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  font-size: 1.25rem;
  font-weight: 800;
  margin-bottom: 2rem;
  padding-left: 0.5rem;
  color: #818cf8;
}

.nav-menu {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  flex: 1;
}

.nav-divider {
  height: 1px;
  background: rgba(255, 255, 255, 0.1);
  margin: 0.75rem 0;
}

.nav-item {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.75rem 1rem;
  border-radius: 0.5rem;
  color: #94a3b8;
  text-decoration: none;
  font-weight: 500;
  transition: all 0.2s ease;

  &:hover, &.router-link-exact-active {
    background: rgba(99, 102, 241, 0.15);
    color: #6366f1;
  }
}

.public-item {
  color: #64748b;
  font-size: 0.85rem;
}

.user-profile {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding-top: 1rem;
  border-top: 1px solid rgba(255, 255, 255, 0.1);
}

.avatar {
  width: 36px;
  height: 36px;
  border-radius: 50%;
}

.user-info {
  flex: 1;
  overflow: hidden;
}

.user-name {
  font-size: 0.875rem;
  font-weight: 600;
  white-space: nowrap;
  text-overflow: ellipsis;
  overflow: hidden;
}

.user-email {
  font-size: 0.75rem;
  color: #94a3b8;
  white-space: nowrap;
  text-overflow: ellipsis;
  overflow: hidden;
}

.logout-btn {
  background: none;
  border: none;
  color: #ef4444;
  cursor: pointer;
  padding: 0.5rem;
  font-size: 1.1rem;
}

.main-content {
  flex: 1;
  padding: 2rem;
  overflow-y: auto;
}
</style>
