<template>
  <div class="calendar-page">
    <header class="page-header">
      <div class="header-content">
        <h1>📅 Scheduling (UC03)</h1>
        <p>Quản lý lịch hẹn & Đồng bộ Google Calendar</p>
      </div>
      <button class="primary-btn" @click="openCreateModal">
        <i class="pi pi-plus"></i> Tạo sự kiện thủ công
      </button>
    </header>

    <div class="tabs">
      <button :class="{ active: activeTab === 'upcoming' }" @click="activeTab = 'upcoming'">
        🗓️ Sắp tới trên Calendar
      </button>
      <button :class="{ active: activeTab === 'extracted' }" @click="activeTab = 'extracted'">
        ✨ Trích xuất từ Email ({{ extractedSchedules.length }})
      </button>
    </div>

    <!-- Tab 1: Upcoming Events from Real Google Calendar -->
    <div v-if="activeTab === 'upcoming'" class="tab-content">
      <LoadingSpinner v-if="loading.upcoming" text="Đang tải sự kiện từ Google Calendar..." />
      <div v-else-if="upcomingEvents.length === 0" class="empty-state">
        <i class="pi pi-calendar"></i>
        <p>Không có sự kiện nào sắp tới trong 7 ngày.</p>
      </div>
      <div v-else class="schedule-list">
        <div v-for="event in upcomingEvents" :key="event.id" class="schedule-card">
          <div class="card-header">
            <a v-if="event.htmlLink" :href="event.htmlLink" target="_blank" class="event-title link-title">
              {{ event.title }} <i class="pi pi-external-link" style="font-size: 0.8rem; margin-left: 0.25rem;"></i>
            </a>
            <span v-else class="event-title">{{ event.title }}</span>
            <span class="badge google-badge">
              <i class="pi pi-google"></i> Google Calendar
            </span>
          </div>
          <div class="event-details">
            <div><i class="pi pi-clock"></i> Bắt đầu: {{ formatDateTime(event.start) }}</div>
            <div v-if="event.end"><i class="pi pi-clock"></i> Kết thúc: {{ formatDateTime(event.end) }}</div>
            <div v-if="event.location"><i class="pi pi-map-marker"></i> {{ event.location }}</div>
          </div>
        </div>
      </div>
    </div>

    <!-- Tab 2: Extracted Schedules -->
    <div v-if="activeTab === 'extracted'" class="tab-content">
      <LoadingSpinner v-if="loading.extracted" text="Đang tải lịch hẹn trích xuất..." />
      <div v-else-if="extractedSchedules.length === 0" class="empty-state">
        <i class="pi pi-calendar-plus"></i>
        <p>Không có lịch hẹn AI nào cần xác nhận!</p>
      </div>
      <div v-else class="schedule-list">
        <div v-for="item in extractedSchedules" :key="item.id" class="schedule-card">
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
          <div class="actions" v-if="item.status !== 2">
            <button class="confirm-btn" @click="handleConfirmExtracted(item.id)">
              <i class="pi pi-check"></i> Xác nhận & Đồng bộ
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Create Manual Event Modal -->
    <div v-if="showModal" class="modal-overlay" @click.self="closeModal">
      <div class="modal-content">
        <h3>Tạo sự kiện mới</h3>
        <form @submit.prevent="handleCreateManual">
          <div class="form-group">
            <label>Tiêu đề</label>
            <input v-model="newEvent.title" required placeholder="Họp team..." autofocus />
          </div>
          <div class="form-row">
            <div class="form-group half">
              <label>Bắt đầu</label>
              <input type="datetime-local" v-model="newEvent.start" required />
            </div>
            <div class="form-group half">
              <label>Kết thúc (Tùy chọn)</label>
              <input type="datetime-local" v-model="newEvent.end" />
            </div>
          </div>
          <div class="form-group">
            <label>Địa điểm (Tùy chọn)</label>
            <input v-model="newEvent.location" placeholder="Phòng họp A..." />
          </div>
          <div class="form-group">
            <label>Mô tả (Tùy chọn)</label>
            <textarea v-model="newEvent.description" rows="3" placeholder="Ghi chú thêm..."></textarea>
          </div>
          <div class="modal-actions">
            <button type="button" class="btn-cancel" @click="closeModal">Hủy</button>
            <button type="submit" class="btn-submit" :disabled="creating">
              {{ creating ? 'Đang tạo...' : 'Tạo sự kiện' }}
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch } from 'vue';
import api from '@/services/api.service';
import LoadingSpinner from '@/components/common/LoadingSpinner.vue';

const activeTab = ref('upcoming');
const extractedSchedules = ref<any[]>([]);
const upcomingEvents = ref<any[]>([]);

const loading = ref({
  extracted: false,
  upcoming: false
});

const showModal = ref(false);
const creating = ref(false);

const newEvent = ref({
  title: '',
  start: '',
  end: '',
  location: '',
  description: ''
});

const fetchExtractedSchedules = async () => {
  loading.value.extracted = true;
  try {
    const res: any = await api.get('/scheduling');
    if (res.success && res.data) {
      extractedSchedules.value = res.data.items;
    }
  } catch (e) {
    console.error('Failed to fetch extracted schedules:', e);
  } finally {
    loading.value.extracted = false;
  }
};

const fetchUpcomingEvents = async () => {
  loading.value.upcoming = true;
  try {
    const res: any = await api.get('/scheduling/upcoming?days=7');
    if (res.success && res.data) {
      upcomingEvents.value = res.data;
    }
  } catch (e) {
    console.error('Failed to fetch upcoming events:', e);
  } finally {
    loading.value.upcoming = false;
  }
};

const handleConfirmExtracted = async (id: string) => {
  try {
    const res: any = await api.post(`/scheduling/${id}/confirm`, {});
    if (res.success) {
      fetchExtractedSchedules();
      if (activeTab.value === 'upcoming') {
        fetchUpcomingEvents();
      }
    }
  } catch (e) {
    alert('Lỗi xác nhận lịch hẹn. Vui lòng kiểm tra kết nối Google OAuth.');
  }
};

const openCreateModal = () => {
  newEvent.value = { title: '', start: '', end: '', location: '', description: '' };
  showModal.value = true;
};

const closeModal = () => {
  showModal.value = false;
};

const handleCreateManual = async () => {
  creating.value = true;
  try {
    const payload = {
      title: newEvent.value.title,
      start: new Date(newEvent.value.start).toISOString(),
      end: newEvent.value.end ? new Date(newEvent.value.end).toISOString() : null,
      location: newEvent.value.location,
      description: newEvent.value.description
    };
    const res: any = await api.post('/scheduling/manual', payload);
    if (res.success) {
      closeModal();
      activeTab.value = 'upcoming';
      fetchUpcomingEvents();
    }
  } catch (e) {
    alert('Lỗi tạo sự kiện. Vui lòng kiểm tra kết nối Google OAuth.');
  } finally {
    creating.value = false;
  }
};

const formatDateTime = (dateStr: string) => {
  if (!dateStr) return '';
  return new Date(dateStr).toLocaleString('vi-VN', {
    day: '2-digit', month: '2-digit', year: 'numeric',
    hour: '2-digit', minute: '2-digit'
  });
};

watch(activeTab, (newTab) => {
  if (newTab === 'extracted' && extractedSchedules.value.length === 0) fetchExtractedSchedules();
  if (newTab === 'upcoming' && upcomingEvents.value.length === 0) fetchUpcomingEvents();
});

onMounted(() => {
  fetchUpcomingEvents();
});
</script>

<style scoped lang="scss">
.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1.5rem;
  
  h1 { font-size: 1.8rem; font-weight: 800; margin-bottom: 0.25rem; }
  p { color: #94a3b8; font-size: 0.95rem; }
}

.primary-btn {
  background: #6366f1;
  color: #fff;
  border: none;
  padding: 0.75rem 1.25rem;
  border-radius: 0.5rem;
  font-weight: 600;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 0.5rem;
  &:hover { background: #4f46e5; }
}

.tabs {
  display: flex;
  gap: 1rem;
  margin-bottom: 1.5rem;
  border-bottom: 1px solid rgba(255, 255, 255, 0.1);
  padding-bottom: 0.5rem;
  
  button {
    background: none;
    border: none;
    color: #94a3b8;
    font-weight: 600;
    font-size: 1rem;
    padding: 0.5rem 1rem;
    cursor: pointer;
    border-bottom: 2px solid transparent;
    
    &.active {
      color: #818cf8;
      border-bottom-color: #818cf8;
    }
  }
}

.schedule-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.schedule-card {
  background: #1e293b;
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 1rem;
  padding: 1.5rem;
  transition: border-color 0.2s;
  
  &:hover { border-color: rgba(99, 102, 241, 0.4); }
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
}

.event-title { font-weight: 800; font-size: 1.1rem; color: #f8fafc; }
.link-title { text-decoration: none; transition: color 0.2s; }
.link-title:hover { color: #818cf8; text-decoration: underline; }

.status-badge {
  font-size: 0.75rem;
  padding: 0.25rem 0.625rem;
  border-radius: 0.25rem;
  font-weight: 700;
  &.confirmed { background: rgba(16, 185, 129, 0.2); color: #34d399; }
  &.pending { background: rgba(245, 158, 11, 0.2); color: #fbbf24; }
}

.google-badge {
  background: rgba(66, 133, 244, 0.15);
  color: #60a5fa;
  font-size: 0.75rem;
  padding: 0.25rem 0.5rem;
  border-radius: 0.25rem;
  display: flex;
  align-items: center;
  gap: 0.3rem;
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

.empty-state {
  text-align: center;
  padding: 3rem;
  color: #94a3b8;
  i { font-size: 2.5rem; margin-bottom: 1rem; color: #6366f1; }
}

/* Modal */
.modal-overlay {
  position: fixed;
  top: 0; left: 0; right: 0; bottom: 0;
  background: rgba(0, 0, 0, 0.7);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.modal-content {
  background: #1e293b;
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 1rem;
  padding: 2rem;
  width: 100%;
  max-width: 500px;
  
  h3 { margin-top: 0; margin-bottom: 1.5rem; }
}

.form-group {
  margin-bottom: 1.25rem;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  
  label { font-size: 0.9rem; color: #cbd5e1; font-weight: 500; }
  
  input, textarea {
    background: #0f172a;
    border: 1px solid rgba(255, 255, 255, 0.15);
    color: #f8fafc;
    padding: 0.75rem;
    border-radius: 0.5rem;
    font-family: inherit;
    &:focus { border-color: #6366f1; outline: none; }
  }
}

.form-row {
  display: flex;
  gap: 1rem;
  .half { flex: 1; }
}

.modal-actions {
  display: flex;
  justify-content: flex-end;
  gap: 1rem;
  margin-top: 2rem;
}

.btn-cancel {
  background: transparent;
  border: none;
  color: #94a3b8;
  cursor: pointer;
  padding: 0.5rem 1rem;
  font-weight: 600;
  &:hover { color: #f8fafc; }
}

.btn-submit {
  background: #6366f1;
  color: #fff;
  border: none;
  padding: 0.6rem 1.5rem;
  border-radius: 0.5rem;
  font-weight: 600;
  cursor: pointer;
  &:hover:not(:disabled) { background: #4f46e5; }
  &:disabled { opacity: 0.7; cursor: not-allowed; }
}
</style>
