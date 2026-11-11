<template>
  <div class="tab-content">
    <LoadingSpinner v-if="loading && schedules.length === 0" text="Đang tải lịch trích xuất..." />
    <div v-else-if="schedules.length === 0" class="empty-state">
      <i class="pi pi-calendar-plus"></i>
      <p>Không có lịch hẹn AI nào cần xác nhận!</p>
    </div>
    <div v-else class="schedule-list">
      <div v-for="item in schedules" :key="item.id" class="schedule-card">
        <div class="card-header">
          <span class="event-title">{{ item.title }}</span>
          <span class="status-badge" :class="item.status === 2 ? 'confirmed' : 'pending'">
            {{ item.status === 2 ? 'Đã tạo Calendar' : 'Chờ xác nhận' }}
          </span>
        </div>
        <div class="event-details">
          <div><i class="pi pi-clock"></i> Bắt đầu: {{ formatDateTime(item.startTime) }}</div>
          <div v-if="item.location"><i class="pi pi-map-marker"></i> {{ item.location }}</div>
          <div class="source"><i class="pi pi-envelope"></i> Nguồn: {{ item.sourceEmailSubject }}</div>
        </div>
        <div class="actions">
          <button 
            v-if="item.status !== 2" 
            class="confirm-btn" 
            @click="$emit('confirm', item.id)"
            :disabled="actionInProgressId === item.id"
          >
            <i class="pi pi-check"></i> Xác nhận & Đồng bộ Calendar
          </button>
          <button 
            class="delete-btn" 
            :class="{ 'delete-btn-ghost': item.status === 2 }"
            @click="$emit('delete', item.id, item.status === 2)"
            :disabled="actionInProgressId === item.id"
            :title="item.status === 2 ? 'Xóa bản ghi trích xuất khỏi database (không xóa trên Google Calendar)' : 'Từ chối lịch hẹn và xóa khỏi database'"
          >
            <i class="pi pi-trash"></i> {{ item.status === 2 ? 'Xóa bản ghi (Dọn nhẹ DB)' : 'Xóa / Từ chối' }}
          </button>
        </div>
      </div>
    </div>
    <InfiniteScrollObserver :loading="loading" :has-more="hasMore" @load-more="$emit('load-more')" />
  </div>
</template>

<script setup lang="ts">
import { defineAsyncComponent } from 'vue';
import LoadingSpinner from '@/components/common/LoadingSpinner.vue';

const InfiniteScrollObserver = defineAsyncComponent(() => import('@/components/common/InfiniteScrollObserver.vue'));

defineProps<{
  schedules: any[];
  loading: boolean;
  hasMore: boolean;
  actionInProgressId: string | null;
}>();

defineEmits<{
  (e: 'confirm', id: string): void;
  (e: 'delete', id: string, isConfirmed: boolean): void;
  (e: 'load-more'): void;
}>();

const formatDateTime = (dateStr: string) => {
  if (!dateStr) return '';
  return new Date(dateStr).toLocaleString('vi-VN', {
    day: '2-digit', month: '2-digit', year: 'numeric',
    hour: '2-digit', minute: '2-digit'
  });
};
</script>

<style scoped lang="scss">
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 3rem;
  color: #94a3b8;
  gap: 0.75rem;
  i { font-size: 3rem; color: #cbd5e1; opacity: 0.4; }
}

.schedule-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.schedule-card {
  background: rgba(11, 17, 32, 0.75);
  backdrop-filter: blur(12px);
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 0.75rem;
  padding: 1.25rem;
  transition: transform 0.15s ease, border-color 0.15s ease;

  &:hover {
    border-color: rgba(99, 102, 241, 0.4);
    transform: translate3d(0, -2px, 0);
  }

  .card-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 0.75rem;
    .event-title { font-size: 1.1rem; font-weight: 700; color: #f8fafc; }
  }

  .status-badge {
    font-size: 0.75rem;
    font-weight: 600;
    padding: 0.25rem 0.5rem;
    border-radius: 0.25rem;
    &.confirmed { background: rgba(16, 185, 129, 0.15); color: #34d399; }
    &.pending { background: rgba(245, 158, 11, 0.15); color: #fbbf24; }
  }

  .event-details {
    display: flex;
    flex-direction: column;
    gap: 0.35rem;
    font-size: 0.85rem;
    color: #94a3b8;
    i { color: #818cf8; margin-right: 0.35rem; }
  }

  .actions {
    margin-top: 1rem;
    display: flex;
    align-items: center;
    gap: 0.6rem;
    flex-wrap: wrap;

    .confirm-btn {
      background: #10b981;
      color: #fff;
      border: none;
      padding: 0.5rem 1rem;
      border-radius: 0.4rem;
      font-size: 0.85rem;
      font-weight: 600;
      cursor: pointer;
      display: flex;
      align-items: center;
      gap: 0.4rem;
      transition: background-color 0.2s ease, transform 0.15s ease;

      &:hover:not(:disabled) {
        background: #059669;
        transform: translate3d(0, -1px, 0);
      }

      &:disabled {
        opacity: 0.6;
        cursor: not-allowed;
      }
    }

    .delete-btn {
      background: rgba(239, 68, 68, 0.15);
      color: #f87171;
      border: 1px solid rgba(239, 68, 68, 0.35);
      padding: 0.5rem 0.9rem;
      border-radius: 0.4rem;
      font-size: 0.85rem;
      font-weight: 600;
      cursor: pointer;
      display: flex;
      align-items: center;
      gap: 0.4rem;
      transition: background-color 0.2s ease, color 0.2s ease, border-color 0.2s ease, transform 0.15s ease;

      &:hover:not(:disabled) {
        background: rgba(239, 68, 68, 0.85);
        color: #ffffff;
        border-color: #ef4444;
        transform: translate3d(0, -1px, 0);
      }

      &.delete-btn-ghost {
        background: rgba(148, 163, 184, 0.08);
        color: #94a3b8;
        border: 1px solid rgba(148, 163, 184, 0.2);

        &:hover:not(:disabled) {
          background: rgba(239, 68, 68, 0.2);
          color: #fca5a5;
          border-color: rgba(239, 68, 68, 0.4);
        }
      }

      &:disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }
    }
  }
}
</style>
