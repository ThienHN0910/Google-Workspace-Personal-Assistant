<template>
  <div class="tasks-page">
    <header class="page-header">
      <div class="title-group">
        <div class="title-icon-badge">
          <i class="pi pi-check-square"></i>
        </div>
        <div class="title-text">
          <div class="title-row">
            <h1>Google Tasks Management</h1>
            <span class="version-tag">UC03 Sync</span>
          </div>
          <p class="subtitle">Quản lý công việc đa danh sách, phân cấp subtask đồng bộ hai chiều</p>
        </div>
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

    <!-- Filter Tabs (Scrollable on mobile) -->
    <div class="tabs-nav-wrapper">
      <div class="filter-tabs">
        <button 
          class="filter-tab" 
          :class="{ active: activeFilter === 'all' }" 
          @click="activeFilter = 'all'"
        >
          <i class="pi pi-list"></i>
          <span>Tất cả</span>
          <span class="count-badge">{{ tasks.length }}</span>
        </button>
        <button 
          class="filter-tab" 
          :class="{ active: activeFilter === 'today' }" 
          @click="activeFilter = 'today'"
        >
          <i class="pi pi-calendar"></i>
          <span>Hôm nay / Sắp tới</span>
          <span class="count-badge amber">{{ dueTasksCount }}</span>
        </button>
        <button 
          class="filter-tab" 
          :class="{ active: activeFilter === 'starred' }" 
          @click="activeFilter = 'starred'"
        >
          <i class="pi pi-star-fill text-amber"></i>
          <span>Được gắn sao</span>
          <span class="count-badge yellow">{{ starredTasksCount }}</span>
        </button>
        <button 
          class="filter-tab" 
          :class="{ active: activeFilter === 'completed' }" 
          @click="activeFilter = 'completed'"
        >
          <i class="pi pi-check-circle text-emerald"></i>
          <span>Đã hoàn thành</span>
          <span class="count-badge emerald">{{ completedTasks.length }}</span>
        </button>
      </div>
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
.tasks-page {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

/* ============================================================
   CYBER-COCKPIT HEADER
   ============================================================ */
.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 0.25rem;
  flex-wrap: wrap;
  gap: 1rem;
  padding: 0.25rem 0.1rem;

  .title-group {
    display: flex;
    align-items: center;
    gap: 0.85rem;

    .title-icon-badge {
      width: 42px;
      height: 42px;
      border-radius: 0.75rem;
      background: linear-gradient(135deg, rgba(16, 185, 129, 0.25), rgba(6, 182, 212, 0.2));
      border: 1px solid rgba(16, 185, 129, 0.35);
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 1.25rem;
      color: #34d399;
      box-shadow: 0 0 16px rgba(16, 185, 129, 0.2);
    }

    .title-text {
      display: flex;
      flex-direction: column;
      gap: 0.2rem;

      .title-row {
        display: flex;
        align-items: center;
        gap: 0.65rem;
        flex-wrap: wrap;

        h1 {
          font-size: 1.45rem;
          font-weight: 800;
          letter-spacing: -0.02em;
          margin: 0;
          background: linear-gradient(135deg, #ffffff 0%, #cbd5e1 100%);
          -webkit-background-clip: text;
          -webkit-text-fill-color: transparent;
        }

        .version-tag {
          font-size: 0.675rem;
          font-family: var(--font-mono, monospace);
          font-weight: 700;
          padding: 0.15rem 0.5rem;
          border-radius: 9999px;
          background: rgba(16, 185, 129, 0.12);
          border: 1px solid rgba(16, 185, 129, 0.3);
          color: #34d399;
          letter-spacing: 0.04em;
        }
      }

      .subtitle {
        color: #94a3b8;
        font-size: 0.825rem;
        margin: 0;
      }
    }
  }

  .header-actions {
    display: flex;
    align-items: center;
    gap: 0.65rem;
    flex-wrap: wrap;
  }
}

.list-selector-wrapper {
  display: flex;
  align-items: center;
  gap: 0.45rem;
  background: rgba(15, 23, 42, 0.7);
  border: 1px solid rgba(148, 163, 184, 0.16);
  padding: 0 0.65rem;
  height: 38px;
  border-radius: 0.5rem;

  label {
    font-size: 0.8rem;
    color: #94a3b8;
    display: flex;
    align-items: center;
    gap: 0.35rem;
    white-space: nowrap;
  }

  .list-select {
    background: transparent;
    border: none;
    color: #f8fafc;
    font-size: 0.825rem;
    font-weight: 600;
    outline: none;
    cursor: pointer;
    option { background: #0f172a; color: #f8fafc; }
  }

  .btn-manage-lists {
    background: transparent;
    border: none;
    color: #94a3b8;
    cursor: pointer;
    padding: 0.25rem;
    font-size: 0.95rem;
    display: flex;
    align-items: center;
    justify-content: center;
    transition: color 0.15s ease;
    &:hover { color: #38bdf8; }
  }
}

.btn-clear-completed {
  height: 38px;
  background: rgba(16, 185, 129, 0.12);
  border: 1px solid rgba(16, 185, 129, 0.3);
  color: #34d399;
  padding: 0 0.85rem;
  border-radius: 0.5rem;
  font-size: 0.8rem;
  font-weight: 600;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  transition: all 0.15s ease;

  &:hover:not(:disabled) {
    background: #10b981;
    color: #fff;
    transform: translate3d(0, -1px, 0);
  }
  &:disabled { opacity: 0.45; cursor: not-allowed; }
}

.primary-btn {
  height: 38px;
  background: linear-gradient(135deg, #6366f1 0%, #4f46e5 100%);
  border: 1px solid rgba(99, 102, 241, 0.4);
  color: #fff;
  padding: 0 1.15rem;
  border-radius: 0.5rem;
  font-weight: 600;
  font-size: 0.825rem;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 0.45rem;
  transition: all 0.15s ease;
  box-shadow: 0 2px 10px rgba(99, 102, 241, 0.25);

  &:hover {
    filter: brightness(1.1);
    transform: translate3d(0, -1.5px, 0);
    box-shadow: 0 4px 16px rgba(99, 102, 241, 0.4);
  }
}

/* ============================================================
   TABS NAVIGATION
   ============================================================ */
.tabs-nav-wrapper {
  overflow-x: auto;
  border-bottom: 1px solid rgba(148, 163, 184, 0.12);
  margin-bottom: 0.5rem;

  &::-webkit-scrollbar { display: none; }
  -ms-overflow-style: none;
  scrollbar-width: none;

  .filter-tabs {
    display: flex;
    gap: 0.45rem;
    min-width: max-content;

    .filter-tab {
      background: none;
      border: none;
      color: #94a3b8;
      font-weight: 600;
      font-size: 0.825rem;
      padding: 0.65rem 0.95rem;
      cursor: pointer;
      display: inline-flex;
      align-items: center;
      gap: 0.45rem;
      border-bottom: 2px solid transparent;
      transition: all 0.15s ease;

      i { font-size: 0.9rem; }

      &:hover { color: #f1f5f9; }

      &.active {
        color: #34d399;
        border-bottom-color: #10b981;
      }

      .count-badge {
        font-size: 0.7rem;
        font-weight: 700;
        padding: 0.1rem 0.45rem;
        border-radius: 9999px;
        background: rgba(148, 163, 184, 0.15);
        color: #cbd5e1;

        &.amber { background: rgba(245, 158, 11, 0.15); color: #fbbf24; border: 1px solid rgba(245, 158, 11, 0.3); }
        &.yellow { background: rgba(234, 179, 8, 0.15); color: #facc15; border: 1px solid rgba(234, 179, 8, 0.3); }
        &.emerald { background: rgba(16, 185, 129, 0.15); color: #34d399; border: 1px solid rgba(16, 185, 129, 0.3); }
      }
    }
  }
}

/* ============================================================
   TASK LIST & ITEMS
   ============================================================ */
.task-list {
  display: flex;
  flex-direction: column;
  gap: 0.55rem;
}

.task-item {
  display: flex;
  align-items: flex-start;
  gap: 0.75rem;
  background: rgba(15, 23, 42, 0.7);
  border: 1px solid rgba(148, 163, 184, 0.12);
  border-top: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 0.75rem;
  padding: 0.75rem 0.95rem;
  backdrop-filter: blur(14px);
  -webkit-backdrop-filter: blur(14px);
  transition: all 0.15s ease;

  &:hover {
    border-color: rgba(16, 185, 129, 0.35);
    transform: translate3d(0, -1px, 0);
    box-shadow: 0 4px 16px rgba(0, 0, 0, 0.3);
  }

  &.completed {
    opacity: 0.65;
    background: rgba(11, 17, 32, 0.5);

    .task-title {
      text-decoration: line-through;
      color: #64748b;
    }
  }

  &.is-starred {
    border-color: rgba(245, 158, 11, 0.3);
  }

  .task-checkbox {
    cursor: pointer;
    font-size: 1.15rem;
    margin-top: 2px;
    flex-shrink: 0;

    .text-green { color: #34d399; }
    .text-gray { color: #64748b; }
  }

  .task-content {
    flex: 1;
    overflow: hidden;

    .task-title-row {
      display: flex;
      align-items: center;
      gap: 0.45rem;
      flex-wrap: wrap;

      .subtask-badge {
        font-size: 0.7rem;
        font-weight: 700;
        color: #22d3ee;
        background: rgba(6, 182, 212, 0.12);
        padding: 0.1rem 0.4rem;
        border-radius: 0.35rem;
      }

      .task-title {
        font-size: 0.875rem;
        font-weight: 600;
        color: #f8fafc;
      }
    }

    .task-notes {
      font-size: 0.775rem;
      color: #94a3b8;
      margin-top: 0.25rem;
      line-height: 1.4;
    }

    .task-due {
      font-size: 0.725rem;
      color: #64748b;
      margin-top: 0.3rem;
      display: flex;
      align-items: center;
      gap: 0.3rem;

      &.overdue {
        color: #fb7185;
        font-weight: 600;
      }
    }
  }

  .task-actions {
    display: flex;
    align-items: center;
    gap: 0.35rem;

    .btn-icon {
      width: 30px;
      height: 30px;
      border-radius: 0.4rem;
      background: rgba(30, 41, 59, 0.5);
      border: 1px solid rgba(148, 163, 184, 0.15);
      color: #94a3b8;
      display: flex;
      align-items: center;
      justify-content: center;
      cursor: pointer;
      font-size: 0.85rem;
      transition: all 0.15s ease;

      &:hover {
        background: rgba(51, 65, 85, 0.8);
        color: #fff;
        transform: translate3d(0, -1px, 0);
      }

      &.text-yellow { color: #fbbf24; }
      &.text-red:hover { background: #f43f5e; color: #fff; }
    }
  }
}

.empty-state {
  text-align: center;
  padding: 3rem 1rem;
  color: #64748b;

  i { font-size: 2.25rem; margin-bottom: 0.75rem; color: #34d399; opacity: 0.8; }
  p { font-size: 0.85rem; }
}

/* ============================================================
   MODALS (CYBER-COCKPIT SPRING)
   ============================================================ */
.modal-overlay {
  position: fixed;
  inset: 0;
  z-index: 9999;
  background: rgba(3, 7, 18, 0.75);
  backdrop-filter: blur(6px);
  -webkit-backdrop-filter: blur(6px);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 1rem;
}

.modal-content {
  background: linear-gradient(145deg, #1e293b 0%, #0f172a 100%);
  border: 1px solid rgba(148, 163, 184, 0.2);
  border-top: 1px solid rgba(255, 255, 255, 0.2);
  border-radius: 1rem;
  width: 100%;
  max-width: 500px;
  box-shadow: 0 24px 48px -12px rgba(0, 0, 0, 0.7), 0 0 24px rgba(16, 185, 129, 0.1);
  padding: 1.5rem;
  animation: modal-spring 0.22s cubic-bezier(0.16, 1, 0.3, 1);

  h3 {
    font-size: 1.15rem;
    font-weight: 700;
    color: #f8fafc;
    margin-bottom: 1.25rem;
  }

  .form-group {
    margin-bottom: 1rem;

    label {
      display: block;
      font-size: 0.8rem;
      font-weight: 600;
      color: #94a3b8;
      margin-bottom: 0.35rem;
    }

    input, select, textarea {
      width: 100%;
      background: rgba(11, 17, 32, 0.7);
      border: 1px solid rgba(148, 163, 184, 0.18);
      border-radius: 0.5rem;
      padding: 0.55rem 0.75rem;
      color: #f8fafc;
      font-size: 0.825rem;
      outline: none;
      transition: all 0.15s ease;

      &:focus {
        border-color: rgba(16, 185, 129, 0.45);
        box-shadow: 0 0 10px rgba(16, 185, 129, 0.15);
      }
    }
  }

  .modal-actions {
    display: flex;
    justify-content: flex-end;
    gap: 0.65rem;
    margin-top: 1.5rem;

    button {
      height: 38px;
      padding: 0 1.15rem;
      border-radius: 0.5rem;
      font-weight: 600;
      font-size: 0.825rem;
      cursor: pointer;
      display: inline-flex;
      align-items: center;
      gap: 0.4rem;
      transition: all 0.15s ease;
      border: 1px solid transparent;
    }

    .btn-cancel {
      background: rgba(30, 41, 59, 0.6);
      border-color: rgba(148, 163, 184, 0.2);
      color: #cbd5e1;
      &:hover { background: rgba(51, 65, 85, 0.8); color: #fff; }
    }

    .btn-submit {
      background: linear-gradient(135deg, #10b981 0%, #059669 100%);
      border-color: rgba(52, 211, 153, 0.3);
      color: #fff;
      &:hover:not(:disabled) { filter: brightness(1.1); transform: translate3d(0, -1px, 0); }
      &:disabled { opacity: 0.5; cursor: not-allowed; }
    }
  }
}

/* Manage List Item */
.list-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  background: rgba(11, 17, 32, 0.5);
  padding: 0.65rem 0.85rem;
  border-radius: 0.5rem;
  border: 1px solid rgba(148, 163, 184, 0.1);
  margin-bottom: 0.5rem;

  .list-info {
    display: flex;
    align-items: center;
    gap: 0.55rem;
    .list-name { font-weight: 600; color: #f8fafc; font-size: 0.825rem; }
    .current-badge {
      font-size: 0.7rem;
      background: rgba(16, 185, 129, 0.15);
      border: 1px solid rgba(16, 185, 129, 0.3);
      color: #34d399;
      padding: 0.1rem 0.45rem;
      border-radius: 9999px;
      font-weight: 700;
    }
  }

  .list-actions {
    display: flex;
    gap: 0.35rem;
    .btn-icon {
      width: 28px;
      height: 28px;
      background: none;
      border: none;
      color: #94a3b8;
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      border-radius: 0.35rem;
      &:hover { color: #f8fafc; background: rgba(255, 255, 255, 0.08); }
      &.text-red:hover { color: #fb7185; }
    }
  }
}

/* ============================================================
   RESPONSIVE MEDIA QUERIES
   ============================================================ */
@media (max-width: 768px) {
  .page-header {
    flex-direction: column;
    align-items: stretch;

    .header-actions {
      flex-direction: column;
      align-items: stretch;

      .list-selector-wrapper, .btn-clear-completed, .primary-btn {
        width: 100%;
        justify-content: center;
      }
    }
  }
}
</style>
