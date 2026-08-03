<template>
  <div class="calendar-page">
    <header class="page-header">
      <h1>📅 Lịch trình Extracted (UC03)</h1>
      <p>Trích xuất tự động từ Email bằng Gemini AI & Đồng bộ Google Calendar</p>
    </header>

    <div v-if="loading" class="loading">Đang tải lịch trình...</div>

    <div v-else-if="schedules.length === 0" class="empty-state">
      <i class="pi pi-calendar-plus"></i>
      <p>Chưa có lịch hẹn mới nào cần xử lý!</p>
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
          <div><i class="pi pi-clock"></i> Bắt đầu: {{ formatDate(item.startTime) }}</div>
          <div v-if="item.location"><i class="pi pi-map-marker"></i> {{ item.location }}</div>
          <div class="source"><i class="pi pi-envelope"></i> Nguồn: {{ item.sourceEmailSubject }}</div>
        </div>

        <div class="actions" v-if="item.status !== 2">
          <button class="confirm-btn" @click="handleConfirm(item.id)">
            <i class="pi pi-check"></i> Xác nhận & Đồng bộ Calendar
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import api from '@/services/api.service';

const schedules = ref<any[]>([]);
const loading = ref(true);

const fetchSchedules = async () => {
  loading.value = true;
  try {
    const res: any = await api.get('/scheduling');
    if (res.success && res.data) {
      schedules.value = res.data.items;
    }
  } catch (e) {
    console.error('Failed to fetch schedules:', e);
  } finally {
    loading.value = false;
  }
};

const handleConfirm = async (id: string) => {
  try {
    const res: any = await api.post(`/scheduling/${id}/confirm`, {});
    if (res.success) {
      fetchSchedules();
    }
  } catch (e) {
    alert('Lỗi xác nhận lịch hẹn');
  }
};

const formatDate = (dateStr: string) => {
  return new Date(dateStr).toLocaleString('vi-VN');
};

onMounted(fetchSchedules);
</script>

<style scoped lang="scss">
.page-header { margin-bottom: 2rem; }
.schedule-list { display: flex; flex-direction: column; gap: 1rem; }

.schedule-card {
  background: #1e293b;
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 1rem;
  padding: 1.5rem;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
}

.event-title { font-weight: 800; font-size: 1.1rem; color: #818cf8; }

.status-badge {
  font-size: 0.75rem;
  padding: 0.25rem 0.625rem;
  border-radius: 0.25rem;
  font-weight: 700;
  &.confirmed { background: rgba(16, 185, 129, 0.2); color: #34d399; }
  &.pending { background: rgba(245, 158, 11, 0.2); color: #fbbf24; }
}

.event-details {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
  color: #94a3b8;
  font-size: 0.9rem;
  margin-bottom: 1rem;
}

.actions { display: flex; justify-content: flex-end; }

.confirm-btn {
  background: #10b981;
  color: #fff;
  border: none;
  padding: 0.5rem 1rem;
  border-radius: 0.5rem;
  font-weight: 600;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 0.35rem;
  &:hover { background: #059669; }
}

.empty-state { text-align: center; padding: 3rem; color: #94a3b8; i { font-size: 2.5rem; margin-bottom: 1rem; color: #6366f1; } }
</style>
