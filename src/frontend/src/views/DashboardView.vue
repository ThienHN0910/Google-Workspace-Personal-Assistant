<template>
  <div class="dashboard">
    <header class="dashboard-header">
      <div class="header-content">
        <h1>Trung tâm điều khiển (Command Center)</h1>
        <p class="subtitle">Chào mừng trở lại, {{ authStore.user?.displayName || 'Admin' }} 👋</p>
      </div>
      <div class="header-actions">
        <button @click="showComposeModal = true" class="quick-btn btn-primary"><i class="pi pi-envelope"></i> Soạn Email</button>
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

      <!-- UC01 Unread Emails -->
      <div class="bento-card" :class="{ 'has-action': summary.unreadEmails > 0 }">
        <div class="card-icon draft-icon">
          <i class="pi pi-envelope"></i>
        </div>
        <div class="card-title">Email chưa đọc</div>
        <div class="stat-val" :class="{ warning: summary.unreadEmails > 0 }">{{ summary.unreadEmails }}</div>
        <router-link v-if="summary.unreadEmails > 0" to="/email" class="card-footer link">Đọc ngay ➔</router-link>
        <div v-else class="card-footer">Inbox Zero!</div>
      </div>

      <!-- UC02 AI Drafts Pending -->
      <div class="bento-card" :class="{ 'has-action': (summary.pendingDraftsCount || 0) > 0 }">
        <div class="card-icon" style="background: rgba(168, 85, 247, 0.15); color: #c084fc;">
          <i class="pi pi-sparkles"></i>
        </div>
        <div class="card-title">Bản nháp AI chờ duyệt</div>
        <div class="stat-val" :class="{ warning: (summary.pendingDraftsCount || 0) > 0 }">{{ summary.pendingDraftsCount || 0 }}</div>
        <router-link v-if="(summary.pendingDraftsCount || 0) > 0" to="/email" class="card-footer link">Duyệt ngay ➔</router-link>
        <div v-else class="card-footer">UC02 AI Drafts</div>
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
          <div v-if="summary.activeAlerts === 0 && summary.unreadEmails === 0" class="empty-feed">
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
          <div v-if="summary.unreadEmails > 0" class="feed-item info">
            <i class="pi pi-envelope"></i>
            <div>
              <strong>Có Email mới!</strong>
              <p>Bạn có {{ summary.unreadEmails }} email chưa đọc trong hộp thư.</p>
            </div>
            <router-link to="/email" class="action-link">Đọc</router-link>
          </div>
        </div>
      </div>

      <!-- Middle Column: AI Drafts Quick Review (UC02) -->
      <div class="feed-section">
        <div class="feed-header-row">
          <h2>✨ Bản nháp AI chờ duyệt</h2>
          <router-link to="/email" class="sub-link">Tất cả</router-link>
        </div>
        <div v-if="loadingQuickDrafts" class="empty-feed">
          <LoadingSpinner text="Đang tải bản nháp..." />
        </div>
        <div v-else-if="quickDrafts.length === 0" class="empty-feed">
          <i class="pi pi-sparkles"></i> Tuyệt vời! Không có bản nháp AI nào cần duyệt.
        </div>
        <div v-else class="quick-drafts-list">
          <div v-for="d in quickDrafts" :key="d.id" class="quick-draft-card">
            <div class="qd-sender">{{ d.originalEmail?.from || 'Người gửi' }}</div>
            <div class="qd-subject">{{ d.originalEmail?.subject || '(Không có tiêu đề)' }}</div>
            <div class="qd-snippet">{{ d.draftContent?.substring(0, 90) }}...</div>
            <div class="qd-actions">
              <button class="btn-qd-approve" @click="quickApprove(d.id, d.draftContent)">
                <i class="pi pi-check"></i> Duyệt nhanh
              </button>
              <button class="btn-qd-reject" @click="quickReject(d.id)" title="Từ chối">
                <i class="pi pi-times"></i>
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Right Column: Today's Tasks -->
      <div class="feed-section">
        <h2>✅ Việc cần làm hôm nay</h2>
        <div v-if="loadingTasks" class="empty-feed">
          <LoadingSpinner text="Đang tải danh sách công việc..." />
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

    <!-- Compose Email Modal -->
    <ComposeEmailModal
      v-if="showComposeModal"
      @close="showComposeModal = false"
      @sent="fetchSummary"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, defineAsyncComponent } from 'vue';
import { useAuthStore } from '@/stores/auth.store';
import api from '@/services/api.service';
import { showToast } from '@/services/notification.service';

const LoadingSpinner = defineAsyncComponent(() => import('@/components/common/LoadingSpinner.vue'));
const ComposeEmailModal = defineAsyncComponent(() => import('@/components/email/ComposeEmailModal.vue'));

const authStore = useAuthStore();
const summary = ref({
  cleanedToday: 0,
  unreadEmails: 0,
  monthlyIncome: 0,
  monthlyExpense: 0,
  monthlyNetBalance: 0,
  activeAlerts: 0,
  pendingDraftsCount: 0,
  pendingSchedulesCount: 0,
});

const showComposeModal = ref(false);
const quickDrafts = ref<any[]>([]);
const loadingQuickDrafts = ref(false);

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
      summary.value = {
        ...summary.value,
        ...res.data,
      };
    }
  } catch (e) {
    console.error('Failed to load dashboard summary:', e);
  }
};

const fetchQuickDrafts = async () => {
  loadingQuickDrafts.value = true;
  try {
    const res: any = await api.get('/emailops/drafts/pending?page=1&pageSize=3');
    if (res.success && res.data) {
      quickDrafts.value = res.data.items || [];
    }
  } catch (err) {
    console.error('Failed to load quick drafts:', err);
  } finally {
    loadingQuickDrafts.value = false;
  }
};

const quickApprove = async (id: string, content: string) => {
  try {
    const res: any = await api.post(`/emailops/drafts/${id}/approve`, { customContent: content });
    if (res.success) {
      showToast({
        severity: 'success',
        summary: 'Đã phê duyệt nháp',
        detail: 'Bản nháp phản hồi đã được lưu trên Gmail.',
      });
      quickDrafts.value = quickDrafts.value.filter(d => d.id !== id);
      fetchSummary();
    }
  } catch (err: any) {
    showToast({
      severity: 'error',
      summary: 'Lỗi',
      detail: err.message || 'Không thể phê duyệt bản nháp.',
    });
  }
};

const quickReject = async (id: string) => {
  try {
    const res: any = await api.post(`/emailops/drafts/${id}/reject`, { reason: 'Từ chối từ Dashboard' });
    if (res.success) {
      showToast({
        severity: 'info',
        summary: 'Đã từ chối',
        detail: 'Bản nháp AI đã bị từ chối.',
      });
      quickDrafts.value = quickDrafts.value.filter(d => d.id !== id);
      fetchSummary();
    }
  } catch (err: any) {
    showToast({
      severity: 'error',
      summary: 'Lỗi',
      detail: err.message || 'Không thể từ chối bản nháp.',
    });
  }
};

const fetchTasks = async () => {
  loadingTasks.value = true;
  try {
    const res: any = await api.get('/tasks');
    if (res.success && res.data) {
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
  fetchQuickDrafts();
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
  grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
  gap: 1.5rem;
}

.feed-section {
  background: #1e293b;
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 1.25rem;
  padding: 1.5rem;

  .feed-header-row {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 1.25rem;

    h2 {
      font-size: 1.125rem;
      font-weight: 700;
      color: #f8fafc;
      margin: 0;
    }

    .sub-link {
      font-size: 0.8rem;
      color: #818cf8;
      text-decoration: none;
      font-weight: 600;
      &:hover { text-decoration: underline; }
    }
  }

  h2 {
    font-size: 1.125rem;
    font-weight: 700;
    margin-bottom: 1.5rem;
    color: #f8fafc;
  }
}

.quick-drafts-list {
  display: flex;
  flex-direction: column;
  gap: 0.875rem;
}

.quick-draft-card {
  background: rgba(15, 23, 42, 0.6);
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 0.75rem;
  padding: 0.875rem 1rem;

  .qd-sender {
    font-size: 0.8rem;
    color: #818cf8;
    font-weight: 600;
  }

  .qd-subject {
    font-size: 0.9rem;
    font-weight: 700;
    color: #f8fafc;
    margin: 0.2rem 0;
  }

  .qd-snippet {
    font-size: 0.8rem;
    color: #94a3b8;
    margin-bottom: 0.75rem;
  }

  .qd-actions {
    display: flex;
    gap: 0.5rem;

    .btn-qd-approve {
      background: #10b981;
      color: #fff;
      border: none;
      border-radius: 0.4rem;
      padding: 0.35rem 0.75rem;
      font-size: 0.8rem;
      font-weight: 600;
      cursor: pointer;
      display: flex;
      align-items: center;
      gap: 0.3rem;
      &:hover { background: #059669; }
    }

    .btn-qd-reject {
      background: rgba(239, 68, 68, 0.15);
      color: #f87171;
      border: none;
      border-radius: 0.4rem;
      padding: 0.35rem 0.6rem;
      font-size: 0.8rem;
      cursor: pointer;
      &:hover { background: rgba(239, 68, 68, 0.3); }
    }
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
