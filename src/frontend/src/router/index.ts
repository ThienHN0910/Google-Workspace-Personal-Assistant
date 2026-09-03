import { createRouter, createWebHistory } from 'vue-router';
import { useAuthStore } from '@/stores/auth.store';

import DefaultLayout from '@/layouts/DefaultLayout.vue';
import PublicLayout from '@/layouts/PublicLayout.vue';

const routes = [
  // Public routes for Anonymous viewers
  {
    path: '/public',
    component: PublicLayout,
    children: [
      {
        path: 'calendar',
        name: 'PublicCalendar',
        component: () => import('@/views/PublicCalendarView.vue'),
      },
    ],
  },

  // Auth
  {
    path: '/login',
    name: 'Login',
    component: () => import('@/views/LoginView.vue'),
  },

  // Protected Admin routes
  {
    path: '/',
    component: DefaultLayout,
    meta: { requiresAuth: true },
    children: [
      {
        path: '',
        redirect: '/dashboard',
      },
      {
        path: 'dashboard',
        name: 'Dashboard',
        component: () => import('@/views/DashboardView.vue'),
      },
      {
        path: 'email',
        name: 'EmailOps',
        component: () => import('@/views/EmailOpsView.vue'),
      },
      {
        path: 'calendar',
        name: 'Calendar',
        component: () => import('@/views/CalendarView.vue'),
      },
      {
        path: 'finance',
        name: 'Finance',
        component: () => import('@/views/FinanceView.vue'),
      },
      {
        path: 'tasks',
        name: 'Tasks',
        component: () => import('@/views/TasksView.vue'),
      },
      {
        path: 'drive-guard',
        name: 'DriveGuard',
        component: () => import('@/views/DriveGuardView.vue'),
      },
      {
        path: 'settings',
        name: 'Settings',
        component: () => import('@/views/SettingsView.vue'),
      },
    ],
  },

  // Fallback
  {
    path: '/:pathMatch(.*)*',
    redirect: '/dashboard',
  },
];

const router = createRouter({
  history: createWebHistory(),
  routes,
});

router.beforeEach(async (to) => {
  const authStore = useAuthStore();
  
  if (authStore.token && !authStore.user) {
    await authStore.fetchCurrentUser();
  }

  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    return { name: 'Login' };
  }
});

export default router;
