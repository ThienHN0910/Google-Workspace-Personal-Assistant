<template>
  <div class="tasks-page">
    <header class="page-header">
      <div class="header-content">
        <h1>✅ Google Tasks</h1>
        <p>Quản lý công việc đa danh sách, phân cấp subtask đồng bộ hai chiều</p>
      </div>
      <div class="header-actions">
        <!-- Task List Selector -->
        <div class="list-selector-wrapper" v-if="taskLists.length > 0">
          <label><i class="pi pi-list"></i> Danh sách:</label>
          <select v-model="currentListId" @change="onListChange" class="list-select">
            <option v-for="l in taskLists" :key="l.id" :value="l.id">
              {{ l.title }}
            </option>
          </select>
          <button class="btn-manage-lists" @click="showListModal = true" title="Quản lý danh sách">
            <i class="pi pi-cog"></i>
          </button>
        </div>

        <button 
          v-if="completedTasks.length > 0" 
          class="btn-clear-completed" 
          @click="handleClearCompleted" 
          :disabled="clearingCompleted"
          title="Xóa sạch các task đã hoàn thành trên Google Tasks"
        >
          <i class="pi pi-check-square"></i> Dọn dẹp task đã xong ({{ completedTasks.length }})
        </button>

        <button class="primary-btn" @click="openCreateModal">
          <i class="pi pi-plus"></i> Thêm Task mới
        </button>
      </div>
    </header>

    <!-- Filter Tabs -->
    <div class="filter-tabs">
      <button 
        class="filter-tab" 
        :class="{ active: activeFilter === 'all' }" 
        @click="activeFilter = 'all'"
      >
        Tất cả ({{ tasks.length }})
      </button>
      <button 
        class="filter-tab" 
        :class="{ active: activeFilter === 'today' }" 
        @click="activeFilter = 'today'"
      >
        Hôm nay / Sắp tới ({{ dueTasksCount }})
      </button>
      <button 
        class="filter-tab" 
        :class="{ active: activeFilter === 'starred' }" 
        @click="activeFilter = 'starred'"
      >
        ⭐ Được gắn sao ({{ starredTasksCount }})
      </button>
      <button 
        class="filter-tab" 
        :class="{ active: activeFilter === 'completed' }" 
        @click="activeFilter = 'completed'"
      >
        Đã hoàn thành ({{ completedTasks.length }})
      </button>
    </div>

    <LoadingSpinner v-if="loading" text="Đang tải danh sách công việc..." />

    <div v-else-if="displayTasks.length === 0" class="empty-state">
      <i class="pi pi-check-circle"></i>
      <p v-if="activeFilter === 'starred'">Bạn chưa gắn sao công việc quan trọng nào.</p>
      <p v-else-if="activeFilter === 'today'">Không có công việc nào cần làm hôm nay.</p>
      <p v-else>Tuyệt vời! Không có công việc nào đang chờ trong danh sách này.</p>
    </div>

    <div v-else class="task-list">
      <div 
        v-for="task in displayTasks" 
        :key="task.googleTaskId" 
        class="task-item"
        :class="{ 
          'completed': task.status === 'completed',
          'is-subtask': !!task.parentTaskId,
          'is-starred': task.isStarred
        }"
      >
        <div class="task-checkbox" @click="toggleComplete(task)">
          <i class="pi" :class="task.status === 'completed' ? 'pi-check-circle text-green' : 'pi-circle text-gray'"></i>
        </div>

        <div class="task-content">
          <div class="task-title-row">
            <span v-if="task.parentTaskId" class="subtask-badge">↳ Subtask</span>
            <span class="task-title">{{ task.title }}</span>
          </div>
          <div class="task-notes" v-if="task.notes">{{ cleanNotes(task.notes) }}</div>
          <div class="task-due" v-if="task.due">
            <i class="pi pi-calendar"></i> {{ formatDate(task.due) }}
          </div>
        </div>

        <div class="task-actions">
          <button 
            class="star-btn" 
            :class="{ active: task.isStarred }" 
            @click="toggleStar(task)" 
            title="Đánh dấu quan trọng"
          >
            <i class="pi" :class="task.isStarred ? 'pi-star-fill text-yellow' : 'pi-star'"></i>
          </button>
          <button class="edit-btn" @click="openEditModal(task)" title="Chỉnh sửa">
            <i class="pi pi-pencil"></i>
          </button>
          <button class="delete-btn" @click="handleDelete(task.googleTaskId)" title="Xóa">
            <i class="pi pi-trash"></i>
          </button>
        </div>
      </div>
    </div>

    <!-- Manage Task Lists Modal -->
    <div v-if="showListModal" class="modal-overlay" @click.self="showListModal = false">
      <div class="modal-content list-modal">
        <h3>📂 Quản lý Danh Sách Google Tasks</h3>
        <div class="create-list-bar">
          <input v-model="newListName" placeholder="Tên danh sách mới (VD: Dự án A, Cá nhân...)" />
          <button class="btn-submit" @click="handleCreateList" :disabled="!newListName.trim() || creatingList">
            {{ creatingList ? 'Đang tạo...' : 'Tạo' }}
          </button>
        </div>

        <div class="task-lists-container">
          <div v-for="l in taskLists" :key="l.id" class="list-row">
            <div class="list-info">
              <span class="list-name">{{ l.title }}</span>
              <span v-if="l.id === currentListId" class="current-badge">Đang chọn</span>
            </div>
            <div class="list-actions">
              <button class="btn-icon" @click="promptRenameList(l)" title="Đổi tên">
                <i class="pi pi-pencil"></i>
              </button>
              <button class="btn-icon text-red" @click="handleDeleteList(l.id)" title="Xóa danh sách">
                <i class="pi pi-trash"></i>
              </button>
            </div>
          </div>
        </div>

        <div class="modal-actions">
          <button class="btn-cancel" @click="showListModal = false">Đóng</button>
        </div>
      </div>
    </div>

    <!-- Edit Modal -->
    <div v-if="showEditModal" class="modal-overlay" @click.self="showEditModal = false">
      <div class="modal-content">
        <h3>✏️ Chỉnh sửa Task</h3>
        <form @submit.prevent="handleUpdateTask">
          <div class="form-group">
            <label>Tiêu đề <span class="required">*</span></label>
            <input v-model="editTaskForm.title" required placeholder="Nhập tiêu đề công việc..." autofocus />
          </div>
          <div class="form-group">
            <label>Ghi chú</label>
            <textarea v-model="editTaskForm.notes" rows="3" placeholder="Chi tiết..."></textarea>
          </div>
          <div class="form-group">
            <label>Hạn chót</label>
            <input type="datetime-local" v-model="editTaskForm.due" />
          </div>

          <div class="form-group-checkbox">
            <label>
              <input type="checkbox" v-model="editTaskForm.isStarred" />
              ⭐ Đánh dấu là công việc quan trọng (Starred)
            </label>
          </div>

          <div class="modal-actions">
            <button type="button" class="btn-cancel" @click="showEditModal = false">Hủy</button>
            <button type="submit" class="btn-submit" :disabled="savingEdit">
              {{ savingEdit ? 'Đang lưu...' : 'Lưu Thay Đổi' }}
            </button>
          </div>
        </form>
      </div>
    </div>

    <!-- Create Modal -->
    <div v-if="showModal" class="modal-overlay" @click.self="closeModal">
      <div class="modal-content">
        <h3>Tạo Task mới</h3>
        <form @submit.prevent="handleCreate">
          <div class="form-group">
            <label>Danh sách công việc</label>
            <select v-model="newTask.taskListId" class="form-select">
              <option v-for="l in taskLists" :key="l.id" :value="l.id">{{ l.title }}</option>
            </select>
          </div>

          <div class="form-group">
            <label>Tiêu đề <span class="required">*</span></label>
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

          <div class="form-group" v-if="activeTasks.length > 0">
            <label>Thuộc task cha (Subtask của)</label>
            <select v-model="newTask.parentTaskId" class="form-select">
              <option value="">-- Không (Task độc lập) --</option>
              <option v-for="t in activeTasks" :key="t.googleTaskId" :value="t.googleTaskId">
                {{ t.title }}
              </option>
            </select>
          </div>

          <div class="form-group-checkbox">
            <label>
              <input type="checkbox" v-model="newTask.isStarred" />
              ⭐ Gắn sao công việc quan trọng
            </label>
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
            <div class="form-group-checkbox mt-1">
              <label>
                <input type="checkbox" v-model="newTask.isPublic" />
                Công khai sự kiện trên Public Calendar
              </label>
            </div>
          </div>

          <div class="modal-actions">
            <button type="button" class="btn-cancel" @click="closeModal">Hủy</button>
            <button type="submit" class="btn-submit" :disabled="creating">
              {{ creating ? 'Đang tạo...' : 'Tạo Task' }}
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
import { showToast } from '@/services/notification.service';

const tasks = ref<any[]>([]);
const taskLists = ref<any[]>([]);
const currentListId = ref('');
const loading = ref(true);
const activeFilter = ref<'all' | 'today' | 'starred' | 'completed'>('all');

const showModal = ref(false);
const creating = ref(false);
const showListModal = ref(false);
const newListName = ref('');
const creatingList = ref(false);
const clearingCompleted = ref(false);

const showEditModal = ref(false);
const savingEdit = ref(false);
const editTaskForm = ref({
  id: '',
  title: '',
  notes: '',
  due: '',
  status: 'needsAction',
  isStarred: false
});

const newTask = ref({
  taskListId: '',
  title: '',
  notes: '',
  due: '',
  parentTaskId: '',
  isStarred: false,
  syncToCalendar: false,
  calendarStartTime: '',
  calendarEndTime: '',
  isPublic: true
});

const activeTasks = computed(() => tasks.value.filter(t => t.status !== 'completed'));
const completedTasks = computed(() => tasks.value.filter(t => t.status === 'completed'));
const starredTasksCount = computed(() => tasks.value.filter(t => t.isStarred).length);

const dueTasksCount = computed(() => {
  const now = new Date();
  const endOfDay = new Date(now.getFullYear(), now.getMonth(), now.getDate(), 23, 59, 59);
  return tasks.value.filter(t => t.due && new Date(t.due) <= endOfDay && t.status !== 'completed').length;
});

const displayTasks = computed(() => {
  if (activeFilter.value === 'completed') {
    return completedTasks.value;
  }
  if (activeFilter.value === 'starred') {
    return tasks.value.filter(t => t.isStarred);
  }
  if (activeFilter.value === 'today') {
    const now = new Date();
    const endOfDay = new Date(now.getFullYear(), now.getMonth(), now.getDate(), 23, 59, 59);
    return tasks.value.filter(t => t.due && new Date(t.due) <= endOfDay && t.status !== 'completed');
  }
  return activeTasks.value;
});

const fetchTaskLists = async () => {
  try {
    const res: any = await api.get('/tasks/lists');
    if (res.success && res.data && res.data.length > 0) {
      taskLists.value = res.data;
      if (!currentListId.value) {
        currentListId.value = res.data[0].id;
      }
    }
  } catch (e) {
    console.error('Failed to fetch task lists', e);
  }
};

const fetchTasks = async (listId?: string) => {
  loading.value = true;
  try {
    const target = listId || currentListId.value;
    const url = target ? `/tasks?listId=${encodeURIComponent(target)}` : '/tasks';
    const res: any = await api.get(url);
    if (res.success) {
      tasks.value = res.data;
    }
  } catch (e) {
    console.error('Failed to fetch tasks:', e);
  } finally {
    loading.value = false;
  }
};

const onListChange = () => {
  fetchTasks(currentListId.value);
};

const handleCreateList = async () => {
  if (!newListName.value.trim()) return;
  creatingList.value = true;
  try {
    const res: any = await api.post('/tasks/lists', { title: newListName.value.trim() });
    if (res.success) {
      showToast({
        severity: 'success',
        summary: 'Thành công',
        detail: `Đã tạo danh sách "${newListName.value}"`,
      });
      newListName.value = '';
      await fetchTaskLists();
    }
  } catch (e) {
    showToast({
      severity: 'error',
      summary: 'Lỗi',
      detail: 'Không thể tạo danh sách mới.',
    });
  } finally {
    creatingList.value = false;
  }
};

const promptRenameList = async (list: any) => {
  const newTitle = prompt('Nhập tên mới cho danh sách:', list.title);
  if (!newTitle || newTitle.trim() === list.title) return;

  try {
    const res: any = await api.put(`/tasks/lists/${list.id}`, { title: newTitle.trim() });
    if (res.success) {
      showToast({
        severity: 'success',
        summary: 'Đã đổi tên',
        detail: 'Danh sách công việc đã được cập nhật.',
      });
      list.title = newTitle.trim();
    }
  } catch (e) {
    showToast({
      severity: 'error',
      summary: 'Lỗi',
      detail: 'Không thể đổi tên danh sách.',
    });
  }
};

const handleDeleteList = async (listId: string) => {
  if (!confirm('Bạn có chắc chắn muốn xóa danh sách này cùng toàn bộ các task bên trong?')) return;

  try {
    const res: any = await api.delete(`/tasks/lists/${listId}`);
    if (res.success) {
      showToast({
        severity: 'info',
        summary: 'Đã xóa',
        detail: 'Đã xóa danh sách công việc.',
      });
      taskLists.value = taskLists.value.filter(l => l.id !== listId);
      if (currentListId.value === listId) {
        currentListId.value = taskLists.value[0]?.id || '';
        fetchTasks(currentListId.value);
      }
    }
  } catch (e) {
    showToast({
      severity: 'error',
      summary: 'Lỗi',
      detail: 'Không thể xóa danh sách này.',
    });
  }
};

const handleClearCompleted = async () => {
  if (!currentListId.value) return;
  if (!confirm('Bạn có muốn xóa sạch toàn bộ các task đã hoàn thành?')) return;

  clearingCompleted.value = true;
  try {
    const res: any = await api.post(`/tasks/lists/${currentListId.value}/clear-completed`, {});
    if (res.success) {
      showToast({
        severity: 'success',
        summary: 'Đã dọn dẹp',
        detail: 'Đã xóa toàn bộ công việc đã hoàn thành.',
      });
      fetchTasks(currentListId.value);
    }
  } catch (e) {
    showToast({
      severity: 'error',
      summary: 'Lỗi',
      detail: 'Không thể dọn dẹp task hoàn thành.',
    });
  } finally {
    clearingCompleted.value = false;
  }
};

const openCreateModal = () => {
  newTask.value = { 
    taskListId: currentListId.value,
    title: '', notes: '', due: '', 
    parentTaskId: '', isStarred: false,
    syncToCalendar: false, calendarStartTime: '', calendarEndTime: '',
    isPublic: true
  };
  showModal.value = true;
};

const closeModal = () => {
  showModal.value = false;
};

const openEditModal = (task: any) => {
  let formattedDue = '';
  if (task.due) {
    const d = new Date(task.due);
    const pad = (n: number) => (n < 10 ? '0' + n : n);
    formattedDue = `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
  }

  editTaskForm.value = {
    id: task.googleTaskId,
    title: task.title,
    notes: cleanNotes(task.notes || ''),
    due: formattedDue,
    status: task.status || 'needsAction',
    isStarred: task.isStarred || false
  };
  showEditModal.value = true;
};

const handleUpdateTask = async () => {
  if (!editTaskForm.value.title.trim()) return;
  savingEdit.value = true;
  try {
    const payload = {
      taskListId: currentListId.value,
      title: editTaskForm.value.title,
      notes: editTaskForm.value.notes,
      due: editTaskForm.value.due ? new Date(editTaskForm.value.due).toISOString() : null,
      status: editTaskForm.value.status,
      isStarred: editTaskForm.value.isStarred
    };
    const res: any = await api.put(`/tasks/${editTaskForm.value.id}`, payload);
    if (res.success) {
      showToast({
        severity: 'success',
        summary: 'Cập nhật thành công',
        detail: 'Task đã được lưu vào Google Tasks.',
      });
      showEditModal.value = false;
      fetchTasks(currentListId.value);
    }
  } catch (err: any) {
    showToast({
      severity: 'error',
      summary: 'Lỗi',
      detail: 'Không thể cập nhật task.',
    });
  } finally {
    savingEdit.value = false;
  }
};

const toggleStar = async (task: any) => {
  const newStarred = !task.isStarred;
  task.isStarred = newStarred;
  try {
    await api.put(`/tasks/${task.googleTaskId}`, {
      taskListId: currentListId.value,
      title: task.title,
      notes: cleanNotes(task.notes || ''),
      due: task.due,
      status: task.status,
      isStarred: newStarred
    });
    showToast({
      severity: 'info',
      summary: newStarred ? 'Đã gắn sao ⭐' : 'Đã bỏ gắn sao',
      detail: task.title,
    });
  } catch (e) {
    task.isStarred = !newStarred;
  }
};

const toggleComplete = async (task: any) => {
  const isNowCompleted = task.status !== 'completed';
  task.status = isNowCompleted ? 'completed' : 'needsAction';
  const endpoint = isNowCompleted 
    ? `/tasks/${task.googleTaskId}/complete?listId=${encodeURIComponent(currentListId.value)}`
    : `/tasks/${task.googleTaskId}/uncomplete?listId=${encodeURIComponent(currentListId.value)}`;

  try {
    await api.patch(endpoint, {});
    showToast({
      severity: 'success',
      summary: isNowCompleted ? 'Đã hoàn thành' : 'Đã mở lại task',
      detail: task.title,
    });
  } catch (e) {
    task.status = isNowCompleted ? 'needsAction' : 'completed';
  }
};

const onStartTimeChange = () => {
  if (newTask.value.calendarStartTime) {
    const start = new Date(newTask.value.calendarStartTime);
    const end = new Date(start.getTime() + 60 * 60 * 1000);
    const offset = end.getTimezoneOffset() * 60000;
    const localISOTime = (new Date(end.getTime() - offset)).toISOString().slice(0, 16);
    newTask.value.calendarEndTime = localISOTime;
  }
};

const handleCreate = async () => {
  creating.value = true;
  try {
    const payload = {
      taskListId: newTask.value.taskListId || currentListId.value,
      title: newTask.value.title,
      notes: newTask.value.notes,
      due: newTask.value.due ? new Date(newTask.value.due).toISOString() : null,
      parentTaskId: newTask.value.parentTaskId || null,
      isStarred: newTask.value.isStarred,
      syncToCalendar: newTask.value.syncToCalendar,
      calendarStartTime: newTask.value.syncToCalendar && newTask.value.calendarStartTime 
                         ? new Date(newTask.value.calendarStartTime).toISOString() : null,
      calendarEndTime: newTask.value.syncToCalendar && newTask.value.calendarEndTime 
                         ? new Date(newTask.value.calendarEndTime).toISOString() : null,
      isPublic: newTask.value.isPublic
    };
    await api.post('/tasks', payload);
    showToast({
      severity: 'success',
      summary: 'Đã tạo task',
      detail: 'Công việc mới đã được lưu vào Google Tasks.',
    });
    closeModal();
    fetchTasks(currentListId.value);
  } catch (e) {
    showToast({
      severity: 'error',
      summary: 'Lỗi',
      detail: 'Không thể tạo task mới.',
    });
  } finally {
    creating.value = false;
  }
};

const handleDelete = async (id: string) => {
  if (confirm('Xóa task này vĩnh viễn?')) {
    try {
      await api.delete(`/tasks/${id}?listId=${encodeURIComponent(currentListId.value)}`);
      tasks.value = tasks.value.filter(t => t.googleTaskId !== id);
      showToast({
        severity: 'info',
        summary: 'Đã xóa',
        detail: 'Đã xóa task khỏi Google Tasks.',
      });
    } catch (e) {
      showToast({
        severity: 'error',
        summary: 'Lỗi',
        detail: 'Không thể xóa task.',
      });
    }
  }
};

const cleanNotes = (notes: string) => {
  return notes.replace('⭐ [Starred]\n', '').replace('⭐ [Starred]', '').trim();
};

const formatDate = (dateStr: string) => {
  return new Date(dateStr).toLocaleString('vi-VN', {
    day: '2-digit', month: '2-digit', year: 'numeric',
    hour: '2-digit', minute: '2-digit'
  });
};

onMounted(async () => {
  await fetchTaskLists();
  await fetchTasks(currentListId.value);
});
</script>

<style scoped lang="scss">
.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1.5rem;
  flex-wrap: wrap;
  gap: 1rem;
  
  h1 { font-size: 1.8rem; font-weight: 800; margin-bottom: 0.25rem; }
  p { color: #94a3b8; font-size: 0.95rem; }
}

.header-actions {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  flex-wrap: wrap;
}

.list-selector-wrapper {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  background: #1e293b;
  border: 1px solid rgba(255, 255, 255, 0.1);
  padding: 0.4rem 0.75rem;
  border-radius: 0.5rem;

  label {
    font-size: 0.85rem;
    color: #94a3b8;
    display: flex;
    align-items: center;
    gap: 0.35rem;
  }

  .list-select {
    background: #0f172a;
    border: 1px solid rgba(255, 255, 255, 0.15);
    color: #f8fafc;
    padding: 0.35rem 0.6rem;
    border-radius: 0.35rem;
    font-size: 0.85rem;
    font-weight: 600;
    &:focus { outline: none; border-color: #6366f1; }
  }

  .btn-manage-lists {
    background: transparent;
    border: none;
    color: #94a3b8;
    cursor: pointer;
    padding: 0.25rem;
    font-size: 1rem;
    &:hover { color: #818cf8; }
  }
}

.btn-clear-completed {
  background: rgba(16, 185, 129, 0.15);
  border: 1px solid rgba(16, 185, 129, 0.3);
  color: #34d399;
  padding: 0.5rem 0.85rem;
  border-radius: 0.5rem;
  font-size: 0.85rem;
  font-weight: 600;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 0.4rem;
  &:hover:not(:disabled) { background: rgba(16, 185, 129, 0.25); }
  &:disabled { opacity: 0.5; cursor: not-allowed; }
}

.primary-btn {
  background: #6366f1;
  color: #fff;
  border: none;
  padding: 0.6rem 1.25rem;
  border-radius: 0.5rem;
  font-weight: 600;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 0.5rem;
  &:hover { background: #4f46e5; }
}

.filter-tabs {
  display: flex;
  gap: 0.5rem;
  margin-bottom: 1.5rem;
  border-bottom: 1px solid rgba(255, 255, 255, 0.1);
  padding-bottom: 0.75rem;

  .filter-tab {
    background: transparent;
    border: none;
    color: #94a3b8;
    padding: 0.4rem 0.85rem;
    border-radius: 0.4rem;
    font-weight: 600;
    cursor: pointer;
    font-size: 0.9rem;
    &:hover { color: #f8fafc; background: rgba(255, 255, 255, 0.05); }
    &.active {
      color: #818cf8;
      background: rgba(99, 102, 241, 0.15);
      border: 1px solid rgba(99, 102, 241, 0.3);
    }
  }
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
  border: 1px solid rgba(255, 255, 255, 0.05);
  padding: 1rem 1.25rem;
  border-radius: 0.75rem;
  transition: all 0.2s ease;
  
  &:hover {
    border-color: rgba(99, 102, 241, 0.5);
    background: #243047;
  }
  
  &.completed {
    opacity: 0.6;
    background: #0f172a;
    .task-title { text-decoration: line-through; }
  }

  &.is-subtask {
    margin-left: 2rem;
    border-left: 3px solid #6366f1;
    background: rgba(30, 41, 59, 0.6);
  }

  &.is-starred {
    border-color: rgba(234, 179, 8, 0.35);
  }
}

.task-checkbox {
  cursor: pointer;
  font-size: 1.4rem;
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

.task-title-row {
  display: flex;
  align-items: center;
  gap: 0.5rem;

  .subtask-badge {
    font-size: 0.75rem;
    background: rgba(99, 102, 241, 0.2);
    color: #a5b4fc;
    padding: 0.15rem 0.4rem;
    border-radius: 0.25rem;
    font-weight: 700;
  }
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
  display: flex;
  align-items: center;
  gap: 0.35rem;
  font-size: 0.85rem;
  color: #fbbf24;
  margin-top: 0.25rem;
}

.task-actions {
  display: flex;
  gap: 0.25rem;
  align-items: center;
}

.star-btn {
  background: transparent;
  border: none;
  color: #64748b;
  cursor: pointer;
  padding: 0.4rem;
  border-radius: 0.25rem;
  font-size: 1.1rem;
  &:hover { color: #eab308; }
  &.active { color: #eab308; }
  .text-yellow { color: #eab308; }
}

.edit-btn {
  background: transparent;
  border: none;
  color: #818cf8;
  cursor: pointer;
  padding: 0.4rem;
  border-radius: 0.25rem;
  font-size: 1.1rem;
  &:hover { background: rgba(99, 102, 241, 0.15); }
}

.delete-btn {
  background: transparent;
  border: none;
  color: #f87171;
  cursor: pointer;
  padding: 0.4rem;
  border-radius: 0.25rem;
  font-size: 1.1rem;
  &:hover { background: rgba(239, 68, 68, 0.1); }
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
  max-width: 520px;
  max-height: 90vh;
  overflow-y: auto;
  h3 { margin-top: 0; margin-bottom: 1.5rem; }
}

.form-group {
  margin-bottom: 1.25rem;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  
  label { font-size: 0.9rem; color: #cbd5e1; font-weight: 500; }
  
  input[type="datetime-local"], input[type="text"], textarea, .form-select {
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
    input { width: 1.25rem; height: 1.25rem; cursor: pointer; }
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

/* List Modal */
.create-list-bar {
  display: flex;
  gap: 0.75rem;
  margin-bottom: 1.5rem;
  input {
    flex: 1;
    background: #0f172a;
    border: 1px solid rgba(255, 255, 255, 0.15);
    color: #fff;
    padding: 0.6rem 0.85rem;
    border-radius: 0.5rem;
    &:focus { outline: none; border-color: #6366f1; }
  }
}

.task-lists-container {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  max-height: 300px;
  overflow-y: auto;
  margin-bottom: 1rem;
}

.list-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  background: rgba(255, 255, 255, 0.03);
  padding: 0.75rem 1rem;
  border-radius: 0.5rem;
  border: 1px solid rgba(255, 255, 255, 0.06);

  .list-info {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    .list-name { font-weight: 600; color: #f8fafc; }
    .current-badge {
      font-size: 0.75rem;
      background: rgba(16, 185, 129, 0.2);
      color: #34d399;
      padding: 0.15rem 0.5rem;
      border-radius: 1rem;
      font-weight: 600;
    }
  }

  .list-actions {
    display: flex;
    gap: 0.5rem;
    .btn-icon {
      background: none;
      border: none;
      color: #94a3b8;
      cursor: pointer;
      padding: 0.35rem;
      border-radius: 0.25rem;
      &:hover { color: #f8fafc; background: rgba(255, 255, 255, 0.08); }
      &.text-red:hover { color: #f87171; }
    }
  }
}
</style>
