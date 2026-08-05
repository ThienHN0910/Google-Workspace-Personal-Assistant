<template>
  <div class="tasks-page">
    <header class="page-header">
      <div class="header-content">
        <h1>✅ Google Tasks</h1>
        <p>Quản lý công việc đồng bộ trực tiếp với Google Tasks</p>
      </div>
      <button class="primary-btn" @click="openCreateModal">
        <i class="pi pi-plus"></i> Thêm Task mới
      </button>
    </header>

    <LoadingSpinner v-if="loading" text="Đang tải danh sách công việc..." />

    <div v-else-if="tasks.length === 0" class="empty-state">
      <i class="pi pi-check-circle"></i>
      <p>Tuyệt vời! Bạn không có task nào đang chờ.</p>
    </div>

    <div v-else class="task-list">
      <div 
        v-for="task in activeTasks" 
        :key="task.googleTaskId" 
        class="task-item"
        :class="{ 'completed': task.status === 'completed' }"
      >
        <div class="task-checkbox" @click="handleComplete(task.googleTaskId)">
          <i class="pi" :class="task.status === 'completed' ? 'pi-check-circle text-green' : 'pi-circle text-gray'"></i>
        </div>
        <div class="task-content">
          <div class="task-title">{{ task.title }}</div>
          <div class="task-notes" v-if="task.notes">{{ task.notes }}</div>
          <div class="task-due" v-if="task.due">
            <i class="pi pi-calendar"></i> {{ formatDate(task.due) }}
          </div>
        </div>
        <div class="task-actions">
          <button class="delete-btn" @click="handleDelete(task.googleTaskId)" title="Xóa">
            <i class="pi pi-trash"></i>
          </button>
        </div>
      </div>
      
      <div v-if="completedTasks.length > 0" class="completed-section">
        <h3>Đã hoàn thành ({{ completedTasks.length }})</h3>
        <div 
          v-for="task in completedTasks" 
          :key="task.googleTaskId" 
          class="task-item completed"
        >
          <div class="task-checkbox" @click="handleUncomplete(task.googleTaskId)">
            <i class="pi pi-check-circle text-green"></i>
          </div>
          <div class="task-content">
            <div class="task-title">{{ task.title }}</div>
          </div>
          <div class="task-actions">
            <button class="delete-btn" @click="handleDelete(task.googleTaskId)" title="Xóa">
              <i class="pi pi-trash"></i>
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Create Modal -->
    <div v-if="showModal" class="modal-overlay" @click.self="closeModal">
      <div class="modal-content">
        <h3>Tạo Task mới</h3>
        <form @submit.prevent="handleCreate">
          <div class="form-group">
            <label>Tiêu đề</label>
            <input v-model="newTask.title" required placeholder="Nhập tiêu đề công việc..." autofocus />
          </div>
          <div class="form-group">
            <label>Ghi chú</label>
            <textarea v-model="newTask.notes" rows="3" placeholder="Chi tiết..."></textarea>
          </div>
          <div class="form-group">
            <label>Hạn chót</label>
            <input type="datetime-local" v-model="newTask.due" />
          </div>
          
          <div class="form-group-checkbox">
            <label>
              <input type="checkbox" v-model="newTask.syncToCalendar" />
              Đồng bộ tạo sự kiện trên Google Calendar
            </label>
          </div>

          <div v-if="newTask.syncToCalendar" class="calendar-times">
            <div class="form-group">
              <label>Thời gian bắt đầu</label>
              <input type="datetime-local" v-model="newTask.calendarStartTime" @change="onStartTimeChange" required />
            </div>
            <div class="form-group">
              <label>Thời gian kết thúc</label>
              <input type="datetime-local" v-model="newTask.calendarEndTime" required />
            </div>
          </div>

          <div class="modal-actions">
            <button type="button" class="btn-cancel" @click="closeModal">Hủy</button>
            <button type="submit" class="btn-submit" :disabled="creating">
              {{ creating ? 'Đang tạo...' : 'Lưu' }}
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import api from '@/services/api.service';
import LoadingSpinner from '@/components/common/LoadingSpinner.vue';

const tasks = ref<any[]>([]);
const loading = ref(true);
const showModal = ref(false);
const creating = ref(false);

const newTask = ref({
  title: '',
  notes: '',
  due: '',
  syncToCalendar: false,
  calendarStartTime: '',
  calendarEndTime: ''
});

const activeTasks = computed(() => tasks.value.filter(t => t.status !== 'completed'));
const completedTasks = computed(() => tasks.value.filter(t => t.status === 'completed'));

const fetchTasks = async () => {
  loading.value = true;
  try {
    const res: any = await api.get('/tasks');
    if (res.success) {
      tasks.value = res.data;
    }
  } catch (e) {
    console.error('Failed to fetch tasks:', e);
  } finally {
    loading.value = false;
  }
};

const openCreateModal = () => {
  newTask.value = { 
    title: '', notes: '', due: '', 
    syncToCalendar: false, calendarStartTime: '', calendarEndTime: '' 
  };
  showModal.value = true;
};

const closeModal = () => {
  showModal.value = false;
};

const onStartTimeChange = () => {
  if (newTask.value.calendarStartTime) {
    const start = new Date(newTask.value.calendarStartTime);
    const end = new Date(start.getTime() + 60 * 60 * 1000); // Default 60 mins
    // Format to yyyy-MM-ddThh:mm for datetime-local
    const offset = end.getTimezoneOffset() * 60000;
    const localISOTime = (new Date(end.getTime() - offset)).toISOString().slice(0, 16);
    newTask.value.calendarEndTime = localISOTime;
  }
};

const handleCreate = async () => {
  creating.value = true;
  try {
    const payload = {
      title: newTask.value.title,
      notes: newTask.value.notes,
      due: newTask.value.due ? new Date(newTask.value.due).toISOString() : null,
      syncToCalendar: newTask.value.syncToCalendar,
      calendarStartTime: newTask.value.syncToCalendar && newTask.value.calendarStartTime 
                         ? new Date(newTask.value.calendarStartTime).toISOString() : null,
      calendarEndTime: newTask.value.syncToCalendar && newTask.value.calendarEndTime 
                         ? new Date(newTask.value.calendarEndTime).toISOString() : null,
    };
    await api.post('/tasks', payload);
    closeModal();
    fetchTasks();
  } catch (e) {
    alert('Lỗi tạo task');
  } finally {
    creating.value = false;
  }
};

const handleComplete = async (id: string) => {
  try {
    // Optimistic update
    const task = tasks.value.find(t => t.googleTaskId === id);
    if (task) task.status = 'completed';
    
    await api.patch(`/tasks/${id}/complete`, {});
  } catch (e) {
    alert('Lỗi hoàn thành task');
    fetchTasks(); // Revert on error
  }
};

const handleUncomplete = async (id: string) => {
  try {
    // Optimistic update
    const task = tasks.value.find(t => t.googleTaskId === id);
    if (task) task.status = 'needsAction';
    
    await api.patch(`/tasks/${id}/uncomplete`, {});
  } catch (e) {
    alert('Lỗi phục hồi task');
    fetchTasks(); // Revert on error
  }
};

const handleDelete = async (id: string) => {
  if (confirm('Xóa task này vĩnh viễn?')) {
    try {
      await api.delete(`/tasks/${id}`);
      tasks.value = tasks.value.filter(t => t.googleTaskId !== id);
    } catch (e) {
      alert('Lỗi xóa task');
    }
  }
};

const formatDate = (dateStr: string) => {
  return new Date(dateStr).toLocaleString('vi-VN', {
    day: '2-digit', month: '2-digit', year: 'numeric',
    hour: '2-digit', minute: '2-digit'
  });
};

onMounted(fetchTasks);
</script>

<style scoped lang="scss">
.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 2rem;
  
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

.task-list {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.task-item {
  display: flex;
  align-items: flex-start;
  gap: 1rem;
  background: #1e293b;
  border: 1px solid rgba(255, 255, 255, 0.1);
  padding: 1rem;
  border-radius: 0.75rem;
  transition: all 0.2s;
  
  &:hover {
    border-color: rgba(99, 102, 241, 0.5);
  }
  
  &.completed {
    opacity: 0.6;
    background: #0f172a;
    .task-title { text-decoration: line-through; }
  }
}

.task-checkbox {
  cursor: pointer;
  font-size: 1.5rem;
  margin-top: 0.1rem;
  
  .text-green { color: #10b981; }
  .text-gray { color: #64748b; }
  
  &:hover .text-gray { color: #94a3b8; }
}

.task-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.task-title {
  font-weight: 600;
  font-size: 1.05rem;
  color: #f8fafc;
}

.task-notes {
  font-size: 0.9rem;
  color: #94a3b8;
  white-space: pre-wrap;
}

.task-due {
  font-size: 0.8rem;
  color: #fbbf24;
  display: flex;
  align-items: center;
  gap: 0.25rem;
  margin-top: 0.25rem;
}

.task-actions {
  opacity: 0;
  transition: opacity 0.2s;
}

.task-item:hover .task-actions {
  opacity: 1;
}

.delete-btn {
  background: transparent;
  border: none;
  color: #f87171;
  cursor: pointer;
  padding: 0.5rem;
  border-radius: 0.25rem;
  font-size: 1.1rem;
  &:hover { background: rgba(239, 68, 68, 0.1); }
}

.completed-section {
  margin-top: 2rem;
  
  h3 {
    font-size: 1rem;
    color: #94a3b8;
    margin-bottom: 1rem;
    border-bottom: 1px solid rgba(255, 255, 255, 0.1);
    padding-bottom: 0.5rem;
  }
}

.empty-state {
  text-align: center;
  padding: 4rem 0;
  color: #94a3b8;
  
  i { font-size: 3rem; color: #10b981; margin-bottom: 1rem; }
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
  
  input[type="datetime-local"], input[type="text"], textarea {
    background: #0f172a;
    border: 1px solid rgba(255, 255, 255, 0.15);
    color: #f8fafc;
    padding: 0.75rem;
    border-radius: 0.5rem;
    font-family: inherit;
    &:focus { border-color: #6366f1; outline: none; }
  }
}

.form-group-checkbox {
  margin-bottom: 1.25rem;
  
  label {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    font-size: 0.95rem;
    color: #cbd5e1;
    cursor: pointer;
    
    input {
      width: 1.25rem;
      height: 1.25rem;
      cursor: pointer;
    }
  }
}

.calendar-times {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1rem;
  background: rgba(99, 102, 241, 0.1);
  padding: 1rem;
  border-radius: 0.5rem;
  border: 1px solid rgba(99, 102, 241, 0.2);
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
