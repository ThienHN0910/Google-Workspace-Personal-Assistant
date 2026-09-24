import { createRouter, createWebHistory } from 'vue-router';
import { useAuthStore } from '@/stores/auth.store';

import DefaultLayout from '@/layouts/DefaultLayout.vue';
import PublicLayout from '@/layouts/PublicLayout.vue';

const routes = [
  // Public routes for Anonymous viewers / Guests
  {
    path: '/public',
    component: PublicLayout,
    children: [
      {
        path: '',
        redirect: '/public/calendar',
      },
      {
        path: 'calendar',
        name: 'PublicCalendar',
        component: () => import('@/views/PublicCalendarView.vue'),
        meta: {
          title: 'Lịch Làm Việc & Hẹn Lịch Công Khai',
          description: 'Khung giờ làm việc bận/rảnh công khai được đồng bộ tự động từ Google Calendar. Đăng ký và kết nối lịch hẹn nhanh chóng.',
          isPublic: true,
        },
      },
    ],
  },

  // Auth & Guest Landing
  {
    path: '/login',
    name: 'Login',
    component: () => import('@/views/LoginView.vue'),
    meta: {
      title: 'Cổng Điều Hành & Đăng Nhập',
      description: 'G-Ops Hub — Nền tảng điều hành và tự động hóa tác vụ Google Workspace toàn diện tích hợp Gemini AI và Hangfire.',
      isPublic: true,
    },
  },

  // Protected Admin routes (SEO: Strictly noindex, nofollow)
  {
    path: '/',
    component: DefaultLayout,
    meta: { requiresAuth: true, isPublic: false },
    children: [
      {
        path: '',
        redirect: '/dashboard',
      },
      {
        path: 'dashboard',
        name: 'Dashboard',
        component: () => import('@/views/DashboardView.vue'),
        meta: { title: 'Bảng Điều Khiển' },
      },
      {
        path: 'email',
        name: 'EmailOps',
        component: () => import('@/views/EmailOpsView.vue'),
        meta: { title: 'Email Ops & Dọn Dẹp AI' },
      },
      {
        path: 'calendar',
        name: 'Calendar',
        component: () => import('@/views/CalendarView.vue'),
        meta: { title: 'Lịch Làm Việc Admin' },
      },
      {
        path: 'finance',
        name: 'Finance',
        component: () => import('@/views/FinanceView.vue'),
        meta: { title: 'Biến Động Số Dư & Tài Chính' },
      },
      {
        path: 'tasks',
        name: 'Tasks',
        component: () => import('@/views/TasksView.vue'),
        meta: { title: 'Quản Lý Công Việc' },
      },
      {
        path: 'drive-guard',
        name: 'DriveGuard',
        component: () => import('@/views/DriveGuardView.vue'),
        meta: { title: 'DriveGuard Bảo Mật' },
      },
      {
        path: 'settings',
        name: 'Settings',
        component: () => import('@/views/SettingsView.vue'),
        meta: { title: 'Cấu Hình Hệ Thống' },
      },
    ],
  },

  // Fallback
  {
    path: '/:pathMatch(.*)*',
    redirect: '/login',
  },
];

const router = createRouter({
  history: createWebHistory(),
  routes,
  scrollBehavior(_to, _from, savedPosition) {
    if (savedPosition) {
      return savedPosition;
    } else {
      return { top: 0 };
    }
  },
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

router.afterEach((to) => {
  // 1. Dynamic Document Title
  const baseTitle = 'G-Ops Hub';
  if (to.meta.title) {
    document.title = `${to.meta.title} — ${baseTitle}`;
  } else {
    document.title = `${baseTitle} — Google Workspace AI Assistant`;
  }

  // 2. Strict SEO Robots Meta Handling
  // Guest surfaces get indexed; protected authenticated views are strictly noindexed
  let robotsMeta = document.querySelector('meta[name="robots"]') as HTMLMetaElement | null;
  if (!robotsMeta) {
    robotsMeta = document.createElement('meta');
    robotsMeta.name = 'robots';
    document.head.appendChild(robotsMeta);
  }

  const isGuestRoute = to.meta.isPublic === true;
  if (isGuestRoute) {
    robotsMeta.content = 'index, follow';
  } else {
    robotsMeta.content = 'noindex, nofollow, noarchive';
  }

  // 3. Dynamic Meta Description & OG Description for guest pages
  if (to.meta.description) {
    const descContent = to.meta.description as string;
    
    let descMeta = document.querySelector('meta[name="description"]') as HTMLMetaElement | null;
    if (descMeta) {
      descMeta.content = descContent;
    }

    let ogDescMeta = document.querySelector('meta[property="og:description"]') as HTMLMetaElement | null;
    if (ogDescMeta) {
      ogDescMeta.content = descContent;
    }

    let twDescMeta = document.querySelector('meta[property="twitter:description"]') as HTMLMetaElement | null;
    if (twDescMeta) {
      twDescMeta.content = descContent;
    }
  }
});

export default router;
