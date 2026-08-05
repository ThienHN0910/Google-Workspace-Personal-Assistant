<template>
  <div class="public-calendar">
    <div class="header">
      <h1>📅 Lịch làm việc cá nhân</h1>
      <p>Chế độ xem công khai (Busy/Free Slots)</p>
    </div>

    <div class="status-card" v-if="!loading">
      <div class="status-badge" :class="{ busy: isBusy }">
        <i :class="isBusy ? 'pi pi-clock' : 'pi pi-check-circle'"></i>
        <span>{{ isBusy ? 'Hiện tại đang có lịch bận' : 'Hiện tại đang Rảnh' }}</span>
      </div>
      <p class="notice">Lịch dưới đây chỉ hiển thị khoảng thời gian bận/rảnh để bảo vệ quyền riêng tư.</p>
    </div>

    <div v-if="loading" class="loading-state">
      <i class="pi pi-spin pi-spinner"></i> Đang tải dữ liệu lịch...
    </div>
    
    <div v-else class="events-container">
      <h2>Sự kiện sắp tới (7 ngày)</h2>
      <div v-if="events.length === 0" class="empty-state">
        <i class="pi pi-check-circle"></i>
        <p>Không có sự kiện nào sắp tới. Thời gian hoàn toàn trống!</p>
      </div>
      <div v-else class="events-list">
        <div 
          v-for="(event, index) in events" 
          :key="index" 
          class="event-card"
          :class="{ 'private-event': !event.isPublic }"
        >
          <div class="event-time">
            <div class="date">{{ formatDateShort(event.start) }}</div>
            <div class="time">{{ formatTime(event.start) }} - {{ event.end ? formatTime(event.end) : '?' }}</div>
          </div>
          <div class="event-details">
            <h4 class="event-title">
              <i v-if="!event.isPublic" class="pi pi-lock lock-icon"></i>
              {{ event.title }}
            </h4>
            <div v-if="event.location" class="event-meta">
              <i class="pi pi-map-marker"></i> {{ event.location }}
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import api from '@/services/api.service';

const isBusy = ref(false);
const events = ref<any[]>([]);
const loading = ref(true);

const formatDateShort = (dateStr: string) => {
  return new Date(dateStr).toLocaleDateString('vi-VN', { weekday: 'short', day: '2-digit', month: '2-digit' });
};

const formatTime = (dateStr: string) => {
  return new Date(dateStr).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });
};

onMounted(async () => {
  try {
    const res: any = await api.get('/public/calendar-status');
    if (res.success && res.data) {
      isBusy.value = res.data.isBusyNow;
      events.value = res.data.events || [];
    }
  } catch (e) {
    console.error('Failed to load public calendar status:', e);
  } finally {
    loading.value = false;
  }
});
</script>

<style scoped lang="scss">
.public-calendar {
  max-width: 800px;
  margin: 0 auto;
  padding-top: 2rem;
  padding-bottom: 4rem;

  .header {
    text-align: center;
    margin-bottom: 2rem;

    h1 {
      font-size: 2rem;
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
  padding: 1.5rem;
  text-align: center;
  margin-bottom: 2.5rem;
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

.loading-state {
  text-align: center;
  padding: 3rem;
  color: #94a3b8;
  display: flex;
  justify-content: center;
  align-items: center;
  gap: 0.75rem;
}

.events-container h2 {
  font-size: 1.25rem;
  margin-bottom: 1rem;
  border-bottom: 1px solid rgba(255, 255, 255, 0.1);
  padding-bottom: 0.75rem;
}

.empty-state {
  text-align: center;
  padding: 3rem;
  color: #94a3b8;
  background: rgba(255,255,255,0.02);
  border-radius: 1rem;
  border: 1px dashed rgba(255,255,255,0.1);
  
  i {
    font-size: 2.5rem;
    margin-bottom: 1rem;
    color: #34d399;
    opacity: 0.5;
  }
}

.events-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.event-card {
  display: flex;
  background: #1e293b;
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 0.75rem;
  overflow: hidden;
  transition: transform 0.2s;
  
  &:hover {
    transform: translateY(-2px);
    border-color: rgba(255, 255, 255, 0.2);
  }

  &.private-event {
    opacity: 0.75;
    background: repeating-linear-gradient(
      45deg,
      #1e293b,
      #1e293b 10px,
      rgba(255, 255, 255, 0.02) 10px,
      rgba(255, 255, 255, 0.02) 20px
    );
  }
}

.event-time {
  background: rgba(0, 0, 0, 0.2);
  padding: 1.25rem;
  min-width: 130px;
  display: flex;
  flex-direction: column;
  justify-content: center;
  border-right: 1px solid rgba(255, 255, 255, 0.05);
  
  .date {
    font-weight: 700;
    color: #e2e8f0;
    margin-bottom: 0.25rem;
    text-transform: capitalize;
  }
  .time {
    font-size: 0.85rem;
    color: #94a3b8;
  }
}

.event-details {
  padding: 1.25rem;
  flex: 1;
  display: flex;
  flex-direction: column;
  justify-content: center;
}

.event-title {
  margin: 0 0 0.5rem 0;
  font-size: 1.1rem;
  color: #f8fafc;
  display: flex;
  align-items: center;
  gap: 0.5rem;
  
  .lock-icon {
    font-size: 0.9rem;
    color: #94a3b8;
  }
}

.event-meta {
  font-size: 0.85rem;
  color: #94a3b8;
  display: flex;
  align-items: center;
  gap: 0.35rem;
}
</style>
