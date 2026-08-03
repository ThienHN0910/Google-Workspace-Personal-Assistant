<template>
  <div class="public-calendar">
    <div class="header">
      <h1>📅 Lịch làm việc cá nhân</h1>
      <p>Chế độ xem công khai (Busy/Free Slots)</p>
    </div>

    <div class="status-card">
      <div class="status-badge" :class="{ busy: isBusy }">
        <i :class="isBusy ? 'pi pi-clock' : 'pi pi-check-circle'"></i>
        <span>{{ isBusy ? 'Hiện tại đang có lịch bận' : 'Hiện tại đang Rảnh' }}</span>
      </div>
      <p class="notice">Lịch chỉ hiển thị khoảng thời gian bận/rảnh để bảo vệ quyền riêng tư.</p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import api from '@/services/api.service';

const isBusy = ref(false);

onMounted(async () => {
  try {
    const res: any = await api.get('/public/calendar-status');
    if (res.success && res.data) {
      isBusy.value = res.data.isBusyNow;
    }
  } catch (e) {
    console.error('Failed to load public calendar status:', e);
  }
});
</script>

<style scoped lang="scss">
.public-calendar {
  max-width: 600px;
  margin: 0 auto;

  .header {
    text-align: center;
    margin-bottom: 2rem;

    h1 {
      font-size: 1.75rem;
      font-weight: 800;
      margin-bottom: 0.5rem;
    }

    p {
      color: #94a3b8;
    }
  }
}

.status-card {
  background: #1e293b;
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 1rem;
  padding: 2rem;
  text-align: center;
}

.status-badge {
  display: inline-flex;
  align-items: center;
  gap: 0.75rem;
  background: rgba(16, 185, 129, 0.15);
  color: #34d399;
  border: 1px solid rgba(16, 185, 129, 0.3);
  padding: 0.875rem 1.5rem;
  border-radius: 2rem;
  font-size: 1.1rem;
  font-weight: 700;
  margin-bottom: 1rem;

  &.busy {
    background: rgba(245, 158, 11, 0.15);
    color: #fbbf24;
    border-color: rgba(245, 158, 11, 0.3);
  }
}

.notice {
  font-size: 0.85rem;
  color: #64748b;
}
</style>
