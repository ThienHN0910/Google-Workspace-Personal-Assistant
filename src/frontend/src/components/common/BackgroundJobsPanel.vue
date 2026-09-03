<template>
  <div class="bg-jobs-panel">
    <div class="panel-header">
      <div class="header-title">
        <i class="pi pi-bolt"></i>
        <h3>Trung tâm Tác vụ Chạy ngầm (Background Automation Hub)</h3>
      </div>
      <button class="btn-refresh" @click="fetchJobs" :disabled="loading" title="Làm mới trạng thái">
        <i class="pi" :class="loading ? 'pi-spin pi-spinner' : 'pi-refresh'"></i>
      </button>
    </div>

    <div v-if="loading && jobs.length === 0" class="panel-loading">
      <i class="pi pi-spin pi-spinner"></i> Đang tải thông tin tác vụ chạy ngầm...
    </div>

    <div v-else class="jobs-grid">
      <div v-for="job in jobs" :key="job.id" class="job-card">
        <div class="job-top">
          <div class="job-icon-title">
            <div class="job-icon" :class="getJobColor(job.id)">
              <i :class="getJobIcon(job.id)"></i>
            </div>
            <div>
              <div class="job-name">{{ job.name }}</div>
              <div class="job-cron">Lịch chạy: <code>{{ job.cron }}</code></div>
            </div>
          </div>
          <span class="status-pill active">{{ job.lastJobState || 'Scheduled' }}</span>
        </div>

        <p class="job-desc">{{ job.description }}</p>

        <div class="job-meta">
          <div class="meta-row">
            <span class="label">Lần chạy trước:</span>
            <span class="value">{{ job.lastExecution ? formatDate(job.lastExecution) : 'Chưa chạy' }}</span>
          </div>
          <div class="meta-row">
            <span class="label">Lần chạy tiếp:</span>
            <span class="value highlight">{{ job.nextExecution ? formatDate(job.nextExecution) : 'Theo chu kỳ' }}</span>
          </div>
        </div>

        <div class="job-action">
          <button
            class="btn-trigger"
            @click="triggerJob(job.id)"
            :disabled="triggeringId === job.id"
          >
            <i class="pi" :class="triggeringId === job.id ? 'pi-spin pi-spinner' : 'pi-play'"></i>
            {{ triggeringId === job.id ? 'Đang gửi lệnh...' : 'Kích hoạt chạy ngay' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import api from '@/services/api.service';
import { showToast } from '@/services/notification.service';

interface JobInfo {
  id: string;
  name: string;
  description: string;
  cron: string;
  nextExecution?: string;
  lastExecution?: string;
  lastJobState?: string;
}

const jobs = ref<JobInfo[]>([]);
const loading = ref(false);
const triggeringId = ref<string | null>(null);

const fetchJobs = async () => {
  loading.value = true;
  try {
    const res: any = await api.get('/jobs');
    if (res.success && res.data) {
      jobs.value = res.data;
    }
  } catch (e) {
    console.error('Failed to load background jobs:', e);
  } finally {
    loading.value = false;
  }
};

const triggerJob = async (jobId: string) => {
  triggeringId.value = jobId;
  try {
    const res: any = await api.post(`/jobs/${jobId}/trigger`, {});
    if (res.success) {
      showToast({
        severity: 'success',
        summary: 'Đã gửi lệnh chạy ngầm',
        detail: res.message || `Tác vụ '${jobId}' đã được thêm vào hàng đợi thực thi.`,
      });
      // Delay briefly to allow Hangfire state update then refresh
      setTimeout(fetchJobs, 1500);
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
      detail: e.message || 'Lỗi khi kích hoạt tác vụ chạy ngầm.',
    });
  } finally {
    triggeringId.value = null;
  }
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

const formatDate = (dateStr?: string) => {
  if (!dateStr) return '';
  const d = new Date(dateStr);
  return d.toLocaleString('vi-VN', {
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit'
  });
};

onMounted(() => {
  fetchJobs();
});
</script>

<style scoped lang="scss">
.bg-jobs-panel {
  background: #1e293b;
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 1rem;
  padding: 1.5rem;
  margin-top: 1.75rem;

  .panel-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 1.25rem;
    padding-bottom: 0.75rem;
    border-bottom: 1px solid rgba(255, 255, 255, 0.08);

    .header-title {
      display: flex;
      align-items: center;
      gap: 0.5rem;

      i {
        color: #eab308;
        font-size: 1.25rem;
      }

      h3 {
        font-size: 1.15rem;
        font-weight: 700;
        color: #f8fafc;
        margin: 0;
      }
    }

    .btn-refresh {
      background: rgba(255, 255, 255, 0.05);
      border: 1px solid rgba(255, 255, 255, 0.1);
      color: #94a3b8;
      width: 32px;
      height: 32px;
      border-radius: 0.35rem;
      display: flex;
      align-items: center;
      justify-content: center;
      cursor: pointer;
      transition: all 0.2s;

      &:hover:not(:disabled) {
        color: #f8fafc;
        background: rgba(255, 255, 255, 0.1);
      }
      &:disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }
    }
  }

  .panel-loading {
    padding: 2rem;
    text-align: center;
    color: #94a3b8;
    font-size: 0.9rem;
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 0.5rem;
  }

  .jobs-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
    gap: 1.25rem;
  }

  .job-card {
    background: rgba(15, 23, 42, 0.6);
    border: 1px solid rgba(255, 255, 255, 0.08);
    border-radius: 0.75rem;
    padding: 1.25rem;
    display: flex;
    flex-direction: column;
    justify-content: space-between;
    transition: all 0.2s;

    &:hover {
      border-color: rgba(99, 102, 241, 0.3);
      transform: translateY(-2px);
    }

    .job-top {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      margin-bottom: 0.75rem;

      .job-icon-title {
        display: flex;
        gap: 0.75rem;
        align-items: center;

        .job-icon {
          width: 36px;
          height: 36px;
          border-radius: 0.5rem;
          display: flex;
          align-items: center;
          justify-content: center;
          font-size: 1.1rem;

          &.color-red { background: rgba(239, 68, 68, 0.15); color: #f87171; }
          &.color-indigo { background: rgba(99, 102, 241, 0.15); color: #818cf8; }
          &.color-emerald { background: rgba(16, 185, 129, 0.15); color: #34d399; }
          &.color-amber { background: rgba(245, 158, 11, 0.15); color: #fbbf24; }
          &.color-blue { background: rgba(59, 130, 246, 0.15); color: #60a5fa; }
        }

        .job-name {
          font-weight: 700;
          font-size: 0.95rem;
          color: #f1f5f9;
        }

        .job-cron {
          font-size: 0.75rem;
          color: #94a3b8;
          margin-top: 0.15rem;

          code {
            background: rgba(255, 255, 255, 0.08);
            padding: 0.1rem 0.3rem;
            border-radius: 0.25rem;
            font-family: monospace;
            color: #cbd5e1;
          }
        }
      }

      .status-pill {
        font-size: 0.7rem;
        font-weight: 700;
        padding: 0.15rem 0.5rem;
        border-radius: 1rem;
        text-transform: uppercase;

        &.active {
          background: rgba(16, 185, 129, 0.15);
          color: #34d399;
          border: 1px solid rgba(16, 185, 129, 0.25);
        }
      }
    }

    .job-desc {
      font-size: 0.8rem;
      color: #94a3b8;
      line-height: 1.4;
      margin: 0 0 1rem 0;
      flex-grow: 1;
    }

    .job-meta {
      background: rgba(255, 255, 255, 0.03);
      border-radius: 0.5rem;
      padding: 0.65rem 0.85rem;
      margin-bottom: 1rem;
      display: flex;
      flex-direction: column;
      gap: 0.35rem;

      .meta-row {
        display: flex;
        justify-content: space-between;
        font-size: 0.75rem;

        .label {
          color: #64748b;
        }

        .value {
          color: #cbd5e1;
          font-weight: 500;

          &.highlight {
            color: #60a5fa;
            font-weight: 600;
          }
        }
      }
    }

    .job-action {
      .btn-trigger {
        width: 100%;
        background: rgba(99, 102, 241, 0.15);
        border: 1px solid rgba(99, 102, 241, 0.3);
        color: #a5b4fc;
        padding: 0.55rem;
        border-radius: 0.5rem;
        font-weight: 600;
        font-size: 0.825rem;
        cursor: pointer;
        display: flex;
        align-items: center;
        justify-content: center;
        gap: 0.4rem;
        transition: all 0.2s;

        &:hover:not(:disabled) {
          background: #6366f1;
          color: #fff;
        }

        &:disabled {
          opacity: 0.5;
          cursor: not-allowed;
        }
      }
    }
  }
}
</style>
