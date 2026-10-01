<template>
  <div class="jobs-view">
    <!-- Header -->
    <header class="page-header">
      <div class="title-group">
        <div class="title-icon-badge">
          <i class="pi pi-server"></i>
        </div>
        <div class="title-text">
          <div class="title-row">
            <h1>Trung tâm Tác vụ Chạy ngầm (Background Automation Hub)</h1>
            <span class="version-tag">Hangfire • 2h Engine</span>
          </div>
          <p class="subtitle">Giám sát 4 chu kỳ tự động hóa, theo dõi lịch sử thực thi và kích hoạt tác vụ theo yêu cầu</p>
        </div>
      </div>
      <div class="header-actions">
        <button class="btn-action btn-refresh" @click="refreshAll" :disabled="loadingJobs || loadingHistory" title="Làm mới trạng thái">
          <i class="pi" :class="(loadingJobs || loadingHistory) ? 'pi-spin pi-spinner' : 'pi-refresh'"></i>
          <span>Làm mới</span>
        </button>
        <router-link to="/settings?tab=jobs" class="btn-action btn-settings" title="Cấu hình chu kỳ thời gian">
          <i class="pi pi-cog"></i>
          <span>Cấu hình chu kỳ</span>
        </router-link>
      </div>
    </header>

    <!-- Stat Summary Cards -->
    <div class="stats-overview">
      <div class="stat-card">
        <div class="stat-icon icon-emerald">
          <i class="pi pi-check-circle"></i>
        </div>
        <div class="stat-details">
          <span class="stat-label">Chu kỳ hoạt động</span>
          <span class="stat-val">{{ jobs.length || 4 }} tác vụ</span>
          <span class="stat-sub">Mặc định 2 giờ / lần</span>
        </div>
      </div>

      <div class="stat-card">
        <div class="stat-icon icon-indigo">
          <i class="pi pi-history"></i>
        </div>
        <div class="stat-details">
          <span class="stat-label">Lịch sử ghi nhận</span>
          <span class="stat-val">{{ history.length }} lần chạy</span>
          <span class="stat-sub">Gần nhất: {{ lastExecutedTimeText }}</span>
        </div>
      </div>

      <div class="stat-card" :class="{ 'stat-danger': failedCount > 0 }">
        <div class="stat-icon" :class="failedCount > 0 ? 'icon-red' : 'icon-slate'">
          <i class="pi" :class="failedCount > 0 ? 'pi-exclamation-triangle' : 'pi-shield'"></i>
        </div>
        <div class="stat-details">
          <span class="stat-label">Lỗi ghi nhận</span>
          <span class="stat-val">{{ failedCount }} lỗi</span>
          <span class="stat-sub" v-if="failedCount > 0">Cần kiểm tra log lỗi</span>
          <span class="stat-sub" v-else>Toàn bộ tác vụ chạy trơn tru</span>
        </div>
      </div>

      <div class="stat-card">
        <div class="stat-icon icon-amber">
          <i class="pi pi-bolt"></i>
        </div>
        <div class="stat-details">
          <span class="stat-label">Cơ chế duy trì</span>
          <span class="stat-val">Keep-Alive Active</span>
          <span class="stat-sub">Bảo vệ IIS không ngủ</span>
        </div>
      </div>
    </div>

    <!-- Navigation Tabs -->
    <div class="tabs-nav-bar">
      <button
        class="tab-btn"
        :class="{ active: activeTab === 'upcoming' }"
        @click="activeTab = 'upcoming'"
      >
        <i class="pi pi-clock"></i>
        <span>Lịch trình sắp tới ({{ jobs.length }})</span>
      </button>

      <button
        class="tab-btn"
        :class="{ active: activeTab === 'history' }"
        @click="activeTab = 'history'"
      >
        <i class="pi pi-list"></i>
        <span>Lịch sử thực thi ({{ history.length }})</span>
        <span v-if="failedCount > 0" class="badge-alert">{{ failedCount }}</span>
      </button>
    </div>

    <!-- TAB 1: Upcoming Tasks -->
    <div v-if="activeTab === 'upcoming'" class="tab-content">
      <div v-if="loadingJobs && jobs.length === 0" class="panel-loading">
        <LoadingSpinner text="Đang tải danh sách lịch trình chạy ngầm..." />
      </div>

      <div v-else class="upcoming-grid">
        <div v-for="job in jobs" :key="job.id" class="job-card">
          <div class="card-head">
            <div class="job-badge-title">
              <div class="job-icon" :class="getJobColor(job.id)">
                <i :class="getJobIcon(job.id)"></i>
              </div>
              <div class="job-title-box">
                <h4>{{ job.name }}</h4>
                <span class="job-id-code">{{ job.id }}</span>
              </div>
            </div>
            <span class="status-chip active">
              <span class="pulse-dot"></span>
              {{ job.lastJobState || 'Scheduled' }}
            </span>
          </div>

          <p class="job-description">{{ job.description }}</p>

          <div class="job-schedule-box">
            <div class="cron-row">
              <span class="meta-label"><i class="pi pi-calendar"></i> Biểu thức Cron:</span>
              <code class="cron-badge">{{ job.cron }}</code>
              <span class="cron-human">({{ getHumanCron(job.cron) }})</span>
            </div>
            <div class="meta-grid">
              <div class="meta-item">
                <span class="label">Lần chạy trước:</span>
                <span class="val">{{ job.lastExecution ? formatDate(job.lastExecution) : 'Chưa chạy' }}</span>
              </div>
              <div class="meta-item">
                <span class="label">Lần chạy tiếp:</span>
                <span class="val highlight">{{ job.nextExecution ? formatDate(job.nextExecution) : 'Theo chu kỳ' }}</span>
                <span class="countdown" v-if="job.nextExecution">({{ getTimeRemaining(job.nextExecution) }})</span>
              </div>
            </div>
          </div>

          <div class="card-actions">
            <button
              class="btn-trigger"
              @click="triggerJob(job.id)"
              :disabled="triggeringId === job.id"
            >
              <i class="pi" :class="triggeringId === job.id ? 'pi-spin pi-spinner' : 'pi-play'"></i>
              <span>{{ triggeringId === job.id ? 'Đang gửi lệnh...' : 'Kích hoạt chạy ngay' }}</span>
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- TAB 2: Execution History -->
    <div v-if="activeTab === 'history'" class="tab-content">
      <div class="history-controls">
        <div class="filter-pills">
          <button
            class="pill-btn"
            :class="{ active: historyFilter === 'all' }"
            @click="historyFilter = 'all'"
          >
            Tất cả ({{ history.length }})
          </button>
          <button
            class="pill-btn success-pill"
            :class="{ active: historyFilter === 'succeeded' }"
            @click="historyFilter = 'succeeded'"
          >
            Thành công ({{ succeededCount }})
          </button>
          <button
            class="pill-btn danger-pill"
            :class="{ active: historyFilter === 'failed' }"
            @click="historyFilter = 'failed'"
          >
            Thất bại ({{ failedCount }})
          </button>
        </div>

        <button class="btn-refresh-sm" @click="fetchHistory" :disabled="loadingHistory">
          <i class="pi" :class="loadingHistory ? 'pi-spin pi-spinner' : 'pi-sync'"></i>
          Cập nhật lịch sử
        </button>
      </div>

      <div v-if="loadingHistory && history.length === 0" class="panel-loading">
        <LoadingSpinner text="Đang nạp lịch sử thực thi từ Hangfire..." />
      </div>

      <div v-else-if="filteredHistory.length === 0" class="empty-history">
        <i class="pi pi-inbox"></i>
        <p>Không có dữ liệu thực thi nào phù hợp với bộ lọc.</p>
      </div>

      <div v-else class="history-table-wrapper">
        <table class="history-table">
          <thead>
            <tr>
              <th>Tác vụ</th>
              <th>Trạng thái</th>
              <th>Thời điểm chạy</th>
              <th>Thời lượng</th>
              <th>Chi tiết / Thao tác</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in filteredHistory" :key="item.jobId" class="history-row">
              <td class="col-job">
                <div class="job-col-wrapper">
                  <div class="job-mini-icon" :class="getJobColor(item.jobKey)">
                    <i :class="getJobIcon(item.jobKey)"></i>
                  </div>
                  <div>
                    <div class="col-job-name">{{ item.jobName }}</div>
                    <div class="col-job-key">ID: #{{ item.jobId }}</div>
                  </div>
                </div>
              </td>
              <td class="col-status">
                <span
                  class="status-pill"
                  :class="{
                    'status-success': item.state === 'Succeeded',
                    'status-failed': item.state === 'Failed',
                    'status-processing': item.state === 'Processing'
                  }"
                >
                  <i
                    class="pi"
                    :class="{
                      'pi-check': item.state === 'Succeeded',
                      'pi-times': item.state === 'Failed',
                      'pi-spin pi-spinner': item.state === 'Processing'
                    }"
                  ></i>
                  {{ item.state }}
                </span>
              </td>
              <td class="col-time">
                <span class="time-main">{{ formatDate(item.executedAt) }}</span>
              </td>
              <td class="col-duration">
                <span v-if="item.durationMs !== null && item.durationMs !== undefined" class="duration-badge">
                  {{ formatDuration(item.durationMs) }}
                </span>
                <span v-else class="text-muted">—</span>
              </td>
              <td class="col-actions">
                <button
                  v-if="item.state === 'Failed'"
                  class="btn-view-error"
                  @click="openErrorModal(item)"
                  title="Xem thông điệp lỗi và chi tiết Exception"
                >
                  <i class="pi pi-search"></i> Xem chi tiết lỗi
                </button>
                <span v-else class="success-check">
                  <i class="pi pi-check-circle"></i> Hoàn thành tốt
                </span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Error Modal Dialog -->
    <div v-if="showErrorModal" class="modal-backdrop" @click="showErrorModal = false">
      <div class="modal-box" @click.stop>
        <div class="modal-header">
          <div class="modal-title">
            <i class="pi pi-exclamation-triangle text-danger"></i>
            <h3>Chi tiết Lỗi Thực thi (#{{ selectedError?.jobId }})</h3>
          </div>
          <button class="modal-close-btn" @click="showErrorModal = false">
            <i class="pi pi-times"></i>
          </button>
        </div>

        <div class="modal-body" v-if="selectedError">
          <div class="error-meta">
            <div><strong>Tác vụ:</strong> {{ selectedError.jobName }} ({{ selectedError.jobKey }})</div>
            <div><strong>Thời điểm gặp lỗi:</strong> {{ formatDate(selectedError.executedAt) }}</div>
          </div>

          <div class="error-message-block">
            <span class="block-label">Thông điệp lỗi:</span>
            <div class="error-message-text">{{ selectedError.errorMessage || 'Không có thông điệp cụ thể.' }}</div>
          </div>

          <div class="error-details-block" v-if="selectedError.exceptionDetails">
            <span class="block-label">Stack Trace & Chi tiết:</span>
            <pre class="error-trace">{{ selectedError.exceptionDetails }}</pre>
          </div>
        </div>

        <div class="modal-footer">
          <button class="btn-modal-close" @click="showErrorModal = false">Đóng</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import api from '@/services/api.service';
import { showToast } from '@/services/notification.service';
import LoadingSpinner from '@/components/common/LoadingSpinner.vue';

interface JobInfo {
  id: string;
  name: string;
  description: string;
  cron: string;
  nextExecution?: string;
  lastExecution?: string;
  lastJobState?: string;
}

interface JobExecutionHistory {
  jobId: string;
  jobKey: string;
  jobName: string;
  state: 'Succeeded' | 'Failed' | 'Processing' | string;
  executedAt?: string;
  durationMs?: number;
  errorMessage?: string;
  exceptionDetails?: string;
}

const activeTab = ref<'upcoming' | 'history'>('upcoming');
const jobs = ref<JobInfo[]>([]);
const history = ref<JobExecutionHistory[]>([]);
const loadingJobs = ref(false);
const loadingHistory = ref(false);
const triggeringId = ref<string | null>(null);

const historyFilter = ref<'all' | 'succeeded' | 'failed'>('all');
const showErrorModal = ref(false);
const selectedError = ref<JobExecutionHistory | null>(null);

const succeededCount = computed(() => history.value.filter(h => h.state === 'Succeeded').length);
const failedCount = computed(() => history.value.filter(h => h.state === 'Failed').length);

const filteredHistory = computed(() => {
  if (historyFilter.value === 'succeeded') {
    return history.value.filter(h => h.state === 'Succeeded');
  }
  if (historyFilter.value === 'failed') {
    return history.value.filter(h => h.state === 'Failed');
  }
  return history.value;
});

const lastExecutedTimeText = computed(() => {
  if (history.value.length === 0) return 'Chưa có';
  const first = history.value[0];
  return formatDate(first.executedAt);
});

const fetchJobs = async () => {
  loadingJobs.value = true;
  try {
    const res: any = await api.get('/jobs');
    if (res.success && res.data) {
      jobs.value = res.data;
    }
  } catch (e) {
    console.error('Failed to load background recurring jobs:', e);
  } finally {
    loadingJobs.value = false;
  }
};

const fetchHistory = async () => {
  loadingHistory.value = true;
  try {
    const res: any = await api.get('/jobs/history?limit=50');
    if (res.success && res.data) {
      history.value = res.data;
    }
  } catch (e) {
    console.error('Failed to load background job history:', e);
  } finally {
    loadingHistory.value = false;
  }
};

const refreshAll = async () => {
  await Promise.all([fetchJobs(), fetchHistory()]);
  showToast({
    severity: 'info',
    summary: 'Đã cập nhật',
    detail: 'Đã làm mới dữ liệu tác vụ và lịch sử thực thi từ Hangfire.',
  });
};

const triggerJob = async (jobId: string) => {
  triggeringId.value = jobId;
  try {
    const res: any = await api.post(`/jobs/${jobId}/trigger`, {});
    if (res.success) {
      showToast({
        severity: 'success',
        summary: 'Đã gửi lệnh chạy ngầm',
        detail: res.message || `Tác vụ '${jobId}' đã được xếp vào hàng đợi thực thi.`,
      });
      setTimeout(() => {
        fetchJobs();
        fetchHistory();
      }, 1500);
    } else {
      showToast({
        severity: 'error',
        summary: 'Lỗi kích hoạt',
        detail: res.message || 'Không thể kích hoạt tác vụ.',
      });
    }
  } catch (e: any) {
    showToast({
      severity: 'error',
      summary: 'Lỗi',
      detail: e.message || 'Lỗi khi gửi lệnh kích hoạt tác vụ.',
    });
  } finally {
    triggeringId.value = null;
  }
};

const openErrorModal = (item: JobExecutionHistory) => {
  selectedError.value = item;
  showErrorModal.value = true;
};

const getJobIcon = (id: string) => {
  switch (id) {
    case 'drive-guard-audit': return 'pi pi-shield';
    case 'email-cleanup': return 'pi pi-inbox';
    case 'bank-telemetry': return 'pi pi-wallet';
    case 'calendar-extractor': return 'pi pi-calendar';
    default: return 'pi pi-cog';
  }
};

const getJobColor = (id: string) => {
  switch (id) {
    case 'drive-guard-audit': return 'color-red';
    case 'email-cleanup': return 'color-indigo';
    case 'bank-telemetry': return 'color-emerald';
    case 'calendar-extractor': return 'color-amber';
    default: return 'color-blue';
  }
};

const getHumanCron = (cron: string) => {
  if (!cron) return 'Mặc định';
  if (cron === '0 */2 * * *' || cron === '*/120 * * * *') return 'Mỗi 2 giờ';
  if (cron === '*/30 * * * *') return 'Mỗi 30 phút';
  if (cron === '*/50 * * * *') return 'Mỗi 50 phút';
  if (cron === '0 */12 * * *') return 'Mỗi 12 giờ';
  return 'Theo chu kỳ tùy chỉnh';
};

const formatDate = (dateStr?: string) => {
  if (!dateStr) return 'Chưa chạy';
  const d = new Date(dateStr);
  return d.toLocaleString('vi-VN', {
    month: '2-digit',
    day: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
  });
};

const formatDuration = (ms?: number) => {
  if (ms === null || ms === undefined) return '';
  if (ms < 1000) return `${ms} ms`;
  return `${(ms / 1000).toFixed(1)} s`;
};

const getTimeRemaining = (nextExecution?: string) => {
  if (!nextExecution) return '';
  const diff = new Date(nextExecution).getTime() - Date.now();
  if (diff <= 0) return 'Đang đến hạn';
  const minutes = Math.floor(diff / 60000);
  if (minutes < 60) return `sau ${minutes} phút`;
  const hours = Math.floor(minutes / 60);
  const remMinutes = minutes % 60;
  return `sau ${hours}h ${remMinutes}m`;
};

onMounted(() => {
  fetchJobs();
  fetchHistory();
});
</script>

<style scoped lang="scss">
.jobs-view {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
  padding: 1.5rem;
  max-width: 1380px;
  margin: 0 auto;
}

/* Page Header */
.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 1rem;
}

.title-group {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.title-icon-badge {
  width: 48px;
  height: 48px;
  border-radius: 14px;
  background: linear-gradient(135deg, rgba(99, 102, 241, 0.2), rgba(6, 182, 212, 0.15));
  border: 1px solid rgba(99, 102, 241, 0.3);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.4rem;
  color: #818cf8;
}

.title-row {
  display: flex;
  align-items: center;
  gap: 0.75rem;

  h1 {
    font-size: 1.4rem;
    font-weight: 700;
    color: #f1f5f9;
    margin: 0;
  }
}

.version-tag {
  font-size: 0.75rem;
  padding: 0.15rem 0.6rem;
  border-radius: 999px;
  background: rgba(99, 102, 241, 0.15);
  color: #a5b4fc;
  border: 1px solid rgba(99, 102, 241, 0.3);
  font-weight: 600;
}

.subtitle {
  color: #94a3b8;
  font-size: 0.88rem;
  margin: 0.25rem 0 0;
}

.header-actions {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.btn-action {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.55rem 1rem;
  border-radius: 10px;
  font-size: 0.85rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;
  text-decoration: none;
  border: 1px solid rgba(148, 163, 184, 0.15);

  &.btn-refresh {
    background: rgba(30, 41, 59, 0.7);
    color: #cbd5e1;

    &:hover:not(:disabled) {
      background: rgba(51, 65, 85, 0.9);
      color: #fff;
    }
  }

  &.btn-settings {
    background: linear-gradient(135deg, rgba(99, 102, 241, 0.2), rgba(168, 85, 247, 0.15));
    border-color: rgba(99, 102, 241, 0.35);
    color: #c7d2fe;

    &:hover {
      background: linear-gradient(135deg, rgba(99, 102, 241, 0.3), rgba(168, 85, 247, 0.25));
      color: #fff;
    }
  }
}

/* Stats Overview */
.stats-overview {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
  gap: 1rem;
}

.stat-card {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 1rem 1.25rem;
  border-radius: 14px;
  background: rgba(15, 23, 42, 0.65);
  border: 1px solid rgba(148, 163, 184, 0.12);
  backdrop-filter: blur(10px);
  transition: transform 0.2s ease, border-color 0.2s ease;

  &:hover {
    transform: translateY(-2px);
    border-color: rgba(148, 163, 184, 0.25);
  }

  &.stat-danger {
    border-color: rgba(239, 68, 68, 0.35);
    background: rgba(239, 68, 68, 0.05);
  }
}

.stat-icon {
  width: 44px;
  height: 44px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.25rem;

  &.icon-emerald { background: rgba(16, 185, 129, 0.15); color: #34d399; }
  &.icon-indigo { background: rgba(99, 102, 241, 0.15); color: #818cf8; }
  &.icon-red { background: rgba(239, 68, 68, 0.15); color: #f87171; }
  &.icon-slate { background: rgba(148, 163, 184, 0.1); color: #94a3b8; }
  &.icon-amber { background: rgba(245, 158, 11, 0.15); color: #fbbf24; }
}

.stat-details {
  display: flex;
  flex-direction: column;
}

.stat-label {
  font-size: 0.78rem;
  color: #94a3b8;
  font-weight: 500;
}

.stat-val {
  font-size: 1.2rem;
  font-weight: 700;
  color: #f8fafc;
}

.stat-sub {
  font-size: 0.72rem;
  color: #64748b;
}

/* Tabs Navigation Bar */
.tabs-nav-bar {
  display: flex;
  gap: 0.5rem;
  border-bottom: 1px solid rgba(148, 163, 184, 0.15);
  padding-bottom: 0.25rem;
}

.tab-btn {
  display: inline-flex;
  align-items: center;
  gap: 0.6rem;
  padding: 0.65rem 1.2rem;
  border-radius: 10px 10px 0 0;
  font-size: 0.9rem;
  font-weight: 600;
  color: #94a3b8;
  background: transparent;
  border: none;
  cursor: pointer;
  transition: all 0.2s ease;
  position: relative;

  &:hover {
    color: #e2e8f0;
    background: rgba(30, 41, 59, 0.4);
  }

  &.active {
    color: #818cf8;
    background: rgba(99, 102, 241, 0.1);

    &::after {
      content: '';
      position: absolute;
      bottom: -1px;
      left: 0;
      right: 0;
      height: 2px;
      background: #818cf8;
    }
  }
}

.badge-alert {
  padding: 0.1rem 0.45rem;
  border-radius: 999px;
  background: rgba(239, 68, 68, 0.25);
  color: #f87171;
  font-size: 0.72rem;
  font-weight: 700;
}

/* Upcoming Tasks Grid */
.upcoming-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
  gap: 1.25rem;
  margin-top: 1rem;
}

.job-card {
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  padding: 1.25rem;
  border-radius: 16px;
  background: rgba(15, 23, 42, 0.6);
  border: 1px solid rgba(148, 163, 184, 0.12);
  backdrop-filter: blur(12px);
  transition: all 0.2s ease;

  &:hover {
    border-color: rgba(99, 102, 241, 0.35);
    box-shadow: 0 8px 24px -4px rgba(0, 0, 0, 0.35);
  }
}

.card-head {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 0.75rem;
}

.job-badge-title {
  display: flex;
  align-items: center;
  gap: 0.85rem;
}

.job-icon {
  width: 42px;
  height: 42px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.25rem;

  &.color-red { background: rgba(239, 68, 68, 0.15); color: #f87171; border: 1px solid rgba(239, 68, 68, 0.3); }
  &.color-indigo { background: rgba(99, 102, 241, 0.15); color: #818cf8; border: 1px solid rgba(99, 102, 241, 0.3); }
  &.color-emerald { background: rgba(16, 185, 129, 0.15); color: #34d399; border: 1px solid rgba(16, 185, 129, 0.3); }
  &.color-amber { background: rgba(245, 158, 11, 0.15); color: #fbbf24; border: 1px solid rgba(245, 158, 11, 0.3); }
  &.color-blue { background: rgba(59, 130, 246, 0.15); color: #60a5fa; border: 1px solid rgba(59, 130, 246, 0.3); }
}

.job-title-box {
  h4 {
    margin: 0;
    font-size: 1rem;
    font-weight: 600;
    color: #f1f5f9;
  }
}

.job-id-code {
  font-size: 0.72rem;
  color: #64748b;
  font-family: monospace;
}

.status-chip {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  padding: 0.2rem 0.6rem;
  border-radius: 999px;
  font-size: 0.72rem;
  font-weight: 600;

  &.active {
    background: rgba(16, 185, 129, 0.15);
    color: #34d399;
    border: 1px solid rgba(16, 185, 129, 0.3);
  }
}

.pulse-dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: #34d399;
  box-shadow: 0 0 6px #34d399;
}

.job-description {
  color: #94a3b8;
  font-size: 0.85rem;
  line-height: 1.45;
  margin: 0.9rem 0;
  min-height: 40px;
}

.job-schedule-box {
  background: rgba(2, 6, 23, 0.45);
  border-radius: 12px;
  padding: 0.85rem;
  border: 1px solid rgba(148, 163, 184, 0.08);
  margin-bottom: 1rem;
}

.cron-row {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin-bottom: 0.65rem;
  font-size: 0.8rem;
  color: #cbd5e1;
}

.cron-badge {
  background: rgba(99, 102, 241, 0.15);
  color: #c7d2fe;
  padding: 0.15rem 0.45rem;
  border-radius: 6px;
  border: 1px solid rgba(99, 102, 241, 0.3);
  font-family: monospace;
  font-size: 0.82rem;
}

.cron-human {
  color: #94a3b8;
  font-size: 0.78rem;
}

.meta-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0.5rem;
}

.meta-item {
  display: flex;
  flex-direction: column;

  .label {
    font-size: 0.72rem;
    color: #64748b;
  }

  .val {
    font-size: 0.82rem;
    color: #e2e8f0;
    font-weight: 500;

    &.highlight {
      color: #38bdf8;
    }
  }

  .countdown {
    font-size: 0.7rem;
    color: #fbbf24;
  }
}

.card-actions {
  display: flex;
  justify-content: flex-end;
}

.btn-trigger {
  width: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 0.6rem 1rem;
  border-radius: 10px;
  background: linear-gradient(135deg, rgba(99, 102, 241, 0.25), rgba(6, 182, 212, 0.2));
  border: 1px solid rgba(99, 102, 241, 0.35);
  color: #e0e7ff;
  font-size: 0.85rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;

  &:hover:not(:disabled) {
    background: linear-gradient(135deg, rgba(99, 102, 241, 0.4), rgba(6, 182, 212, 0.3));
    color: #fff;
    box-shadow: 0 4px 12px rgba(99, 102, 241, 0.25);
  }

  &:disabled {
    opacity: 0.6;
    cursor: not-allowed;
  }
}

/* History Controls & Table */
.history-controls {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 0.75rem;
  margin-top: 1rem;
}

.filter-pills {
  display: flex;
  gap: 0.5rem;
}

.pill-btn {
  padding: 0.4rem 0.85rem;
  border-radius: 999px;
  background: rgba(30, 41, 59, 0.5);
  border: 1px solid rgba(148, 163, 184, 0.15);
  color: #94a3b8;
  font-size: 0.8rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s ease;

  &:hover {
    color: #e2e8f0;
    background: rgba(51, 65, 85, 0.6);
  }

  &.active {
    background: rgba(99, 102, 241, 0.2);
    border-color: rgba(99, 102, 241, 0.4);
    color: #a5b4fc;
    font-weight: 600;
  }

  &.success-pill.active {
    background: rgba(16, 185, 129, 0.2);
    border-color: rgba(16, 185, 129, 0.4);
    color: #6ee7b7;
  }

  &.danger-pill.active {
    background: rgba(239, 68, 68, 0.2);
    border-color: rgba(239, 68, 68, 0.4);
    color: #fca5a5;
  }
}

.btn-refresh-sm {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  padding: 0.4rem 0.8rem;
  border-radius: 8px;
  background: rgba(30, 41, 59, 0.6);
  border: 1px solid rgba(148, 163, 184, 0.15);
  color: #cbd5e1;
  font-size: 0.8rem;
  cursor: pointer;

  &:hover:not(:disabled) {
    background: rgba(51, 65, 85, 0.8);
    color: #fff;
  }
}

.history-table-wrapper {
  overflow-x: auto;
  border-radius: 14px;
  border: 1px solid rgba(148, 163, 184, 0.12);
  background: rgba(15, 23, 42, 0.55);
  backdrop-filter: blur(10px);
  margin-top: 0.75rem;
}

.history-table {
  width: 100%;
  border-collapse: collapse;
  text-align: left;
  font-size: 0.86rem;

  th {
    padding: 0.85rem 1rem;
    color: #94a3b8;
    font-weight: 600;
    font-size: 0.78rem;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    background: rgba(30, 41, 59, 0.4);
    border-bottom: 1px solid rgba(148, 163, 184, 0.12);
  }

  td {
    padding: 0.85rem 1rem;
    border-bottom: 1px solid rgba(148, 163, 184, 0.08);
    vertical-align: middle;
  }

  tr:hover td {
    background: rgba(30, 41, 59, 0.25);
  }
}

.job-col-wrapper {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.job-mini-icon {
  width: 32px;
  height: 32px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1rem;

  &.color-red { background: rgba(239, 68, 68, 0.15); color: #f87171; }
  &.color-indigo { background: rgba(99, 102, 241, 0.15); color: #818cf8; }
  &.color-emerald { background: rgba(16, 185, 129, 0.15); color: #34d399; }
  &.color-amber { background: rgba(245, 158, 11, 0.15); color: #fbbf24; }
  &.color-blue { background: rgba(59, 130, 246, 0.15); color: #60a5fa; }
}

.col-job-name {
  font-weight: 600;
  color: #f1f5f9;
}

.col-job-key {
  font-size: 0.72rem;
  color: #64748b;
  font-family: monospace;
}

.status-pill {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  padding: 0.2rem 0.6rem;
  border-radius: 999px;
  font-size: 0.75rem;
  font-weight: 600;

  &.status-success {
    background: rgba(16, 185, 129, 0.15);
    color: #34d399;
    border: 1px solid rgba(16, 185, 129, 0.3);
  }

  &.status-failed {
    background: rgba(239, 68, 68, 0.15);
    color: #f87171;
    border: 1px solid rgba(239, 68, 68, 0.3);
  }

  &.status-processing {
    background: rgba(59, 130, 246, 0.15);
    color: #60a5fa;
    border: 1px solid rgba(59, 130, 246, 0.3);
  }
}

.duration-badge {
  background: rgba(30, 41, 59, 0.5);
  color: #cbd5e1;
  padding: 0.15rem 0.5rem;
  border-radius: 6px;
  font-size: 0.78rem;
  font-family: monospace;
}

.btn-view-error {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  padding: 0.3rem 0.65rem;
  border-radius: 6px;
  background: rgba(239, 68, 68, 0.15);
  border: 1px solid rgba(239, 68, 68, 0.3);
  color: #fca5a5;
  font-size: 0.78rem;
  font-weight: 600;
  cursor: pointer;

  &:hover {
    background: rgba(239, 68, 68, 0.25);
    color: #fff;
  }
}

.success-check {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  color: #34d399;
  font-size: 0.78rem;
}

.empty-history {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 3rem 1rem;
  color: #64748b;
  font-size: 0.9rem;
  gap: 0.5rem;

  i {
    font-size: 2rem;
  }
}

.panel-loading {
  padding: 3rem 1rem;
  display: flex;
  justify-content: center;
}

/* Modal */
.modal-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.75);
  backdrop-filter: blur(6px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 9999;
  padding: 1rem;
}

.modal-box {
  width: 100%;
  max-width: 680px;
  border-radius: 16px;
  background: #0f172a;
  border: 1px solid rgba(239, 68, 68, 0.35);
  box-shadow: 0 20px 40px -10px rgba(0, 0, 0, 0.8), 0 0 20px rgba(239, 68, 68, 0.15);
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1rem 1.25rem;
  border-bottom: 1px solid rgba(148, 163, 184, 0.12);

  .modal-title {
    display: flex;
    align-items: center;
    gap: 0.6rem;

    h3 {
      font-size: 1.05rem;
      font-weight: 600;
      color: #f1f5f9;
      margin: 0;
    }
  }
}

.text-danger {
  color: #f87171;
}

.modal-close-btn {
  background: transparent;
  border: none;
  color: #94a3b8;
  font-size: 1.1rem;
  cursor: pointer;

  &:hover {
    color: #fff;
  }
}

.modal-body {
  padding: 1.25rem;
  display: flex;
  flex-direction: column;
  gap: 1rem;
  max-height: 70vh;
  overflow-y: auto;
}

.error-meta {
  display: flex;
  flex-direction: column;
  gap: 0.3rem;
  font-size: 0.82rem;
  color: #94a3b8;
}

.block-label {
  font-size: 0.78rem;
  font-weight: 600;
  color: #cbd5e1;
  display: block;
  margin-bottom: 0.35rem;
}

.error-message-text {
  padding: 0.75rem 1rem;
  border-radius: 8px;
  background: rgba(239, 68, 68, 0.1);
  border: 1px solid rgba(239, 68, 68, 0.25);
  color: #fca5a5;
  font-size: 0.85rem;
  font-family: monospace;
}

.error-trace {
  padding: 0.75rem 1rem;
  border-radius: 8px;
  background: #020617;
  border: 1px solid rgba(148, 163, 184, 0.1);
  color: #94a3b8;
  font-size: 0.76rem;
  font-family: monospace;
  white-space: pre-wrap;
  word-break: break-all;
  max-height: 240px;
  overflow-y: auto;
}

.modal-footer {
  display: flex;
  justify-content: flex-end;
  padding: 0.85rem 1.25rem;
  border-top: 1px solid rgba(148, 163, 184, 0.12);
}

.btn-modal-close {
  padding: 0.5rem 1.2rem;
  border-radius: 8px;
  background: rgba(51, 65, 85, 0.7);
  border: 1px solid rgba(148, 163, 184, 0.2);
  color: #fff;
  font-size: 0.85rem;
  cursor: pointer;

  &:hover {
    background: rgba(71, 85, 105, 0.9);
  }
}
</style>
