<template>
  <div class="dashboard">
    <header class="dashboard-header">
      <div class="header-content">
        <h1>Trung tâm điều khiển (Command Center)</h1>
        <p class="subtitle">Chào mừng trở lại, {{ authStore.user?.displayName || 'Admin' }} 👋</p>
      </div>
      <div class="header-actions">
        <router-link to="/email" class="quick-btn btn-primary"><i class="pi pi-envelope"></i> Soạn Email</router-link>
        <router-link to="/tasks" class="quick-btn btn-secondary"><i class="pi pi-check-square"></i> Thêm Task</router-link>
        <div class="status-chip">
          <span class="pulse-dot"></span>
          <span>Hệ thống bình thường</span>
        </div>
      </div>
    </header>

    <!-- Bento Grid Summary Cards (5 Cards) -->
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
      <div class="bento-card" :class="{ 'has-action': summary.pendingDrafts > 0 }">
        <div class="card-icon draft-icon">
          <i class="pi pi-sparkles"></i>
        </div>
        <div class="card-title">AI Drafts chờ duyệt</div>
        <div class="stat-val" :class="{ warning: summary.pendingDrafts > 0 }">{{ summary.pendingDrafts }}</div>
        <router-link v-if="summary.pendingDrafts > 0" to="/email" class="card-footer link">Duyệt ngay ➔</router-link>
        <div v-else class="card-footer">UC02 Human-in-the-Loop</div>
      </div>

      <!-- UC03 Extracted Schedules -->
      <div class="bento-card" :class="{ 'has-action': summary.pendingSchedules > 0 }">
        <div class="card-icon schedule-icon">
          <i class="pi pi-calendar-plus"></i>
        </div>
        <div class="card-title">Lịch hẹn chờ xác nhận</div>
        <div class="stat-val" :class="{ warning: summary.pendingSchedules > 0 }">{{ summary.pendingSchedules }}</div>
        <router-link v-if="summary.pendingSchedules > 0" to="/calendar" class="card-footer link">Xác nhận ngay ➔</router-link>
        <div v-else class="card-footer">UC03 Smart Calendar</div>
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
      <div class="bento-card" :class="{ 'has-action': summary.activeAlerts > 0, 'alert-critical': summary.activeAlerts > 0 }">
        <div class="card-icon alert-icon">
          <i class="pi pi-shield"></i>
        </div>
        <div class="card-title">Cảnh báo an ninh Drive</div>
        <div class="stat-val alert-val">{{ summary.activeAlerts }}</div>
        <router-link v-if="summary.activeAlerts > 0" to="/drive-guard" class="card-footer link">Xử lý ngay ➔</router-link>
        <div v-else class="card-footer">UC06 File Guard</div>
      </div>
    </div>

    <!-- Feeds Section (2 Columns) -->
    <div class="feeds-grid">
      <!-- Left Column: Activity Feed -->
      <div class="feed-section">
        <h2>🔥 Hoạt động cần chú ý</h2>
        <div class="feed-list">
          <div v-if="summary.activeAlerts === 0 && summary.pendingDrafts === 0 && summary.pendingSchedules === 0" class="empty-feed">
            <i class="pi pi-check-circle"></i> Mọi thứ đều ổn! Không có việc gì khẩn cấp.
          </div>
          <div v-if="summary.activeAlerts > 0" class="feed-item alert">
            <i class="pi pi-exclamation-triangle"></i>
            <div>
              <strong>Cảnh báo bảo mật!</strong>
              <p>Phát hiện {{ summary.activeAlerts }} file nguy hiểm trên Google Drive.</p>
            </div>
            <router-link to="/drive-guard" class="action-link">Xem</router-link>
          </div>
          <div v-if="summary.pendingDrafts > 0" class="feed-item warning">
            <i class="pi pi-pencil"></i>
            <div>
              <strong>Email nháp cần duyệt</strong>
              <p>Có {{ summary.pendingDrafts }} bản nháp AI soạn đang chờ bạn xác nhận.</p>
            </div>
            <router-link to="/email" class="action-link">Duyệt</router-link>
          </div>
          <div v-if="summary.pendingSchedules > 0" class="feed-item info">
            <i class="pi pi-calendar-plus"></i>
            <div>
              <strong>Lịch hẹn mới</strong>
              <p>Có {{ summary.pendingSchedules }} lịch hẹn được AI trích xuất chờ lưu vào Google Calendar.</p>
            </div>
            <router-link to="/calendar" class="action-link">Lưu</router-link>
          </div>
        </div>
      </div>

      <!-- Right Column: Today's Tasks -->
      <div class="feed-section">
        <h2>✅ Việc cần làm hôm nay</h2>
        <div v-if="loadingTasks" class="empty-feed">
          <i class="pi pi-spin pi-spinner"></i> Đang tải công việc...
        </div>
        <div v-else-if="tasks.length === 0" class="empty-feed">
          <i class="pi pi-sparkles"></i> Tuyệt vời! Bạn không có task nào đang chờ.
        </div>
        <div v-else class="task-list">
          <div v-for="task in tasks.slice(0, 5)" :key="task.id" class="task-item">
            <div class="task-check"><i class="pi pi-circle"></i></div>
            <div class="task-info">
              <span class="task-title">{{ task.title }}</span>
              <span v-if="task.due" class="task-due" :class="{ overdue: isOverdue(task.due) }">
                Hạn: {{ formatDate(task.due) }}
              </span>
            </div>
          </div>
          <router-link v-if="tasks.length > 5" to="/tasks" class="view-all-link">Xem tất cả ({{ tasks.length }})...</router-link>
        </div>
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
  pendingSchedules: 0,
  monthlyIncome: 0,
  monthlyExpense: 0,
  monthlyNetBalance: 0,
  activeAlerts: 0,
});

const tasks = ref<any[]>([]);
const loadingTasks = ref(true);

const formatCurrency = (val: number) => {
  return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(val);
};

const formatDate = (dateStr: string) => {
  return new Date(dateStr).toLocaleDateString('vi-VN');
};

const isOverdue = (dateStr: string) => {
  return new Date(dateStr) < new Date();
};

const fetchSummary = async () => {
  try {
    const res: any = await api.get('/dashboard/summary');
    if (res.success && res.data) {
      summary.value = res.data;
    }
  } catch (e) {
    console.error('Failed to load dashboard summary:', e);
  }
};

const fetchTasks = async () => {
  loadingTasks.value = true;
  try {
    const res: any = await api.get('/tasks');
    if (res.success && res.data) {
      // Chỉ lấy task chưa hoàn thành
      tasks.value = res.data.filter((t: any) => t.status !== 'completed');
    }
  } catch (e) {
    console.error('Failed to load tasks:', e);
  } finally {
    loadingTasks.value = false;
  }
};

onMounted(() => {
  fetchSummary();
  fetchTasks();
});
</script>

<style scoped lang="scss">
.dashboard {
  display: flex;
  flex-direction: column;
  gap: 2rem;
}

.dashboard-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  flex-wrap: wrap;
  gap: 1rem;
}

.header-content h1 {
  font-size: 1.875rem;
  font-weight: 800;
  margin: 0;
  background: linear-gradient(135deg, #f8fafc, #94a3b8);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
}

.subtitle {
  color: #94a3b8;
  font-size: 0.95rem;
  margin-top: 0.25rem;
}

.header-actions {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.quick-btn {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 1rem;
  border-radius: 0.5rem;
  font-size: 0.85rem;
  font-weight: 600;
  text-decoration: none;
  transition: all 0.2s;

  &.btn-primary {
    background: #6366f1;
    color: white;
    &:hover { background: #4f46e5; }
  }
  
  &.btn-secondary {
    background: rgba(255, 255, 255, 0.1);
    color: #f8fafc;
    &:hover { background: rgba(255, 255, 255, 0.2); }
  }
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

/* Bento Grid */
.bento-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 1.5rem;
}

.bento-card {
  background: #1e293b;
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 1.25rem;
  padding: 1.5rem;
  display: flex;
  flex-direction: column;
  transition: transform 0.2s, box-shadow 0.2s;

  &.has-action:hover {
    transform: translateY(-2px);
    box-shadow: 0 8px 16px rgba(0, 0, 0, 0.2);
  }

  &.alert-critical {
    border-color: rgba(239, 68, 68, 0.5);
    background: linear-gradient(180deg, #1e293b, rgba(239, 68, 68, 0.1));
  }
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
.schedule-icon { background: rgba(56, 189, 248, 0.15); color: #38bdf8; }
.finance-icon { background: rgba(16, 185, 129, 0.15); color: #34d399; }
.alert-icon { background: rgba(245, 158, 11, 0.15); color: #fbbf24; }

.card-title {
  color: #94a3b8;
  font-size: 0.875rem;
  font-weight: 600;
  margin-bottom: 0.5rem;
}

.stat-val {
  font-size: 2rem;
  font-weight: 800;
  color: #f8fafc;
  line-height: 1.2;
}

.card-footer {
  margin-top: auto;
  padding-top: 1rem;
  font-size: 0.75rem;
  color: #64748b;
  font-weight: 500;
}

.card-footer.link {
  color: #818cf8;
  text-decoration: none;
  font-weight: 700;
  &:hover { text-decoration: underline; }
}

.positive { color: #34d399; }
.negative { color: #f87171; }
.warning { color: #fbbf24; }
.alert-val { color: #ef4444; }

/* Feeds Grid */
.feeds-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1.5rem;
  
  @media (max-width: 768px) {
    grid-template-columns: 1fr;
  }
}

.feed-section {
  background: #1e293b;
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 1.25rem;
  padding: 1.5rem;

  h2 {
    font-size: 1.125rem;
    font-weight: 700;
    margin-bottom: 1.5rem;
    color: #f8fafc;
  }
}

.empty-feed {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  color: #94a3b8;
  padding: 2rem 0;
  font-size: 0.9rem;
  text-align: center;
  
  i { font-size: 2rem; color: #cbd5e1; opacity: 0.5; }
}

.feed-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.feed-item {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 1rem;
  border-radius: 0.75rem;
  background: rgba(255, 255, 255, 0.03);

  i { font-size: 1.5rem; }

  &.alert {
    background: rgba(239, 68, 68, 0.1);
    border-left: 4px solid #ef4444;
    i { color: #ef4444; }
  }
  
  &.warning {
    background: rgba(245, 158, 11, 0.1);
    border-left: 4px solid #fbbf24;
    i { color: #fbbf24; }
  }

  &.info {
    background: rgba(56, 189, 248, 0.1);
    border-left: 4px solid #38bdf8;
    i { color: #38bdf8; }
  }

  div {
    flex: 1;
    strong { display: block; font-size: 0.9rem; color: #f8fafc; margin-bottom: 0.2rem; }
    p { margin: 0; font-size: 0.8rem; color: #94a3b8; }
  }
}

.action-link {
  padding: 0.4rem 0.75rem;
  background: rgba(255, 255, 255, 0.1);
  color: #f8fafc;
  text-decoration: none;
  border-radius: 0.5rem;
  font-size: 0.75rem;
  font-weight: 600;
  white-space: nowrap;
  
  &:hover { background: rgba(255, 255, 255, 0.2); }
}

.task-list {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.task-item {
  display: flex;
  align-items: flex-start;
  gap: 0.75rem;
  padding: 0.75rem;
  background: rgba(255, 255, 255, 0.02);
  border-radius: 0.5rem;
  transition: background 0.2s;

  &:hover { background: rgba(255, 255, 255, 0.05); }

  .task-check {
    color: #475569;
    font-size: 1.25rem;
    margin-top: 2px;
  }

  .task-info {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
  }

  .task-title {
    font-size: 0.9rem;
    color: #f8fafc;
    font-weight: 500;
  }

  .task-due {
    font-size: 0.75rem;
    color: #94a3b8;
    
    &.overdue { color: #ef4444; font-weight: 600; }
  }
}

.view-all-link {
  display: block;
  text-align: center;
  padding-top: 1rem;
  color: #818cf8;
  text-decoration: none;
  font-size: 0.85rem;
  font-weight: 600;
  
  &:hover { text-decoration: underline; }
}
</style>
