<template>
  <div class="dashboard">
    <header class="dashboard-header">
      <div>
        <h1>Dashboard Tổng Quan</h1>
        <p class="subtitle">Chào mừng trở lại, {{ authStore.user?.displayName || 'Admin' }} 👋</p>
      </div>
      <div class="status-chip">
        <span class="pulse-dot"></span>
        <span>Hệ thống đang chạy bình thường</span>
      </div>
    </header>

    <!-- Bento Grid Summary Cards -->
    <div class="bento-grid">
      <!-- UC01 Email Cleaned -->
      <div class="bento-card">
        <div class="card-icon email-icon">
          <i class="pi pi-inbox"></i>
        </div>
        <div class="card-title">Email đã dọn hôm nay</div>
        <div class="stat-val">{{ summary.cleanedToday }}</div>
        <div class="card-footer">UC01 Auto-Clean Inbox</div>
      </div>

      <!-- UC02 AI Drafts Pending -->
      <div class="bento-card">
        <div class="card-icon draft-icon">
          <i class="pi pi-sparkles"></i>
        </div>
        <div class="card-title">AI Drafts chờ duyệt</div>
        <div class="stat-val">{{ summary.pendingDrafts }}</div>
        <div class="card-footer">UC02 Human-in-the-Loop</div>
      </div>

      <!-- UC04 Monthly Finance Net -->
      <div class="bento-card">
        <div class="card-icon finance-icon">
          <i class="pi pi-wallet"></i>
        </div>
        <div class="card-title">Thu/Chi tháng này (Net)</div>
        <div class="stat-val" :class="{ positive: summary.monthlyNetBalance >= 0, negative: summary.monthlyNetBalance < 0 }">
          {{ formatCurrency(summary.monthlyNetBalance) }}
        </div>
        <div class="card-footer">UC04 Finance Telemetry</div>
      </div>

      <!-- UC06 Security Alerts -->
      <div class="bento-card">
        <div class="card-icon alert-icon">
          <i class="pi pi-shield"></i>
        </div>
        <div class="card-title">Cảnh báo an ninh Drive</div>
        <div class="stat-val alert-val">{{ summary.activeAlerts }}</div>
        <div class="card-footer">UC06 File Guard</div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useAuthStore } from '@/stores/auth.store';
import api from '@/services/api.service';

const authStore = useAuthStore();
const summary = ref({
  cleanedToday: 0,
  pendingDrafts: 0,
  monthlyIncome: 0,
  monthlyExpense: 0,
  monthlyNetBalance: 0,
  activeAlerts: 0,
});

const formatCurrency = (val: number) => {
  return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(val);
};

onMounted(async () => {
  try {
    const res: any = await api.get('/dashboard/summary');
    if (res.success && res.data) {
      summary.value = res.data;
    }
  } catch (e) {
    console.error('Failed to load dashboard summary:', e);
  }
});
</script>

<style scoped lang="scss">
.dashboard-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 2rem;
}

h1 {
  font-size: 1.875rem;
  font-weight: 800;
}

.subtitle {
  color: #94a3b8;
  font-size: 0.95rem;
  margin-top: 0.25rem;
}

.status-chip {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  background: rgba(16, 185, 129, 0.1);
  border: 1px solid rgba(16, 185, 129, 0.3);
  color: #34d399;
  padding: 0.5rem 1rem;
  border-radius: 2rem;
  font-size: 0.85rem;
  font-weight: 600;
}

.pulse-dot {
  width: 8px;
  height: 8px;
  background: #34d399;
  border-radius: 50%;
  box-shadow: 0 0 8px #34d399;
}

.card-icon {
  width: 44px;
  height: 44px;
  border-radius: 0.75rem;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.25rem;
  margin-bottom: 1rem;
}

.email-icon { background: rgba(99, 102, 241, 0.15); color: #818cf8; }
.draft-icon { background: rgba(168, 85, 247, 0.15); color: #c084fc; }
.finance-icon { background: rgba(16, 185, 129, 0.15); color: #34d399; }
.alert-icon { background: rgba(245, 158, 11, 0.15); color: #fbbf24; }

.card-title {
  color: #94a3b8;
  font-size: 0.875rem;
  font-weight: 600;
  margin-bottom: 0.5rem;
}

.card-footer {
  margin-top: 1rem;
  font-size: 0.75rem;
  color: #64748b;
  font-weight: 500;
}

.positive { color: #34d399; }
.negative { color: #f87171; }
.alert-val { color: #fbbf24; }
</style>
