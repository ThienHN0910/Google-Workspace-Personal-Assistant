<template>
  <div class="tasks-page">
    <TaskHeader
      :view-mode="viewMode"
      :task-lists="taskLists"
      :current-list-id="currentListId"
      :completed-tasks-count="completedTasks.length"
      :clearing-completed="clearingCompleted"
      :google-error="googleError"
      @update:view-mode="viewMode = $event"
      @update:current-list-id="onCurrentListIdChange"
      @manage-lists="showListModal = true"
      @clear-completed="handleClearCompleted"
      @create-task="openCreateModal"
    />

    <LoadingSpinner v-if="loading" text="Đang tải danh sách công việc..." />

    <!-- Google Reconnect Notice (UC03 Auth/Token expired) -->
    <GoogleReconnectNotice v-else-if="googleError" :message="googleError" />

    <template v-else>
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
            <span class="count-badge">{{ totalTasksCount }}</span>
          </button>
          <button 
            class="filter-tab" 
            :class="{ active: activeFilter === 'today' }" 
            @click="activeFilter = 'today'"
          >
            <i class="pi pi-calendar"></i>
            <span>Hôm nay / Sắp tới</span>
            <span class="count-badge amber">{{ totalDueTasksCount }}</span>
          </button>
          <button 
            class="filter-tab" 
            :class="{ active: activeFilter === 'starred' }" 
            @click="activeFilter = 'starred'"
          >
            <i class="pi pi-star-fill text-amber"></i>
            <span>Được gắn sao</span>
            <span class="count-badge yellow">{{ totalStarredTasksCount }}</span>
          </button>
          <button 
            class="filter-tab" 
            :class="{ active: activeFilter === 'completed' }" 
            @click="activeFilter = 'completed'"
          >
            <i class="pi pi-check-circle text-emerald"></i>
            <span>Đã hoàn thành</span>
            <span class="count-badge emerald">{{ totalCompletedTasksCount }}</span>
          </button>
        </div>
      </div>

      <!-- Single List View -->
      <TaskListView
        v-if="viewMode === 'single'"
        :tasks="displayTasks"
        :active-filter="activeFilter"
        @toggle-complete="toggleComplete"
        @toggle-star="toggleStar"
        @edit-task="openEditModal"
        @delete-task="handleDelete"
      />

      <!-- Multi-List Board View -->
      <TaskBoardView
        v-else-if="viewMode === 'board'"
        :task-lists="taskLists"
        :board-tasks-map="boardTasksMap"
        :loading-board="loadingBoard"
        :active-filter="activeFilter"
        @refresh-board="fetchBoardTasks"
        @create-task="openCreateModal"
        @clear-completed="handleClearCompleted"
        @toggle-complete="toggleComplete"
        @toggle-star="toggleStar"
        @edit-task="openEditModal"
        @delete-task="handleDelete"
      />
    </template>

    <!-- Manage Task Lists Modal -->
    <TaskManageListsModal
      :visible="showListModal"
      :task-lists="taskLists"
      :current-list-id="currentListId"
      :creating-list="creatingList"
      @close="showListModal = false"
      @create-list="handleCreateList"
      @rename-list="promptRenameList"
      @delete-list="handleDeleteList"
    />

    <!-- Edit Modal -->
    <TaskEditModal
      :visible="showEditModal"
      :task="selectedTaskForEdit"
      :saving="savingEdit"
      @close="showEditModal = false"
      @save="handleUpdateTask"
    />

    <!-- Create Modal -->
    <TaskCreateModal
      :visible="showModal"
      :task-lists="taskLists"
      :default-list-id="createTaskListId"
      :active-tasks="activeTasks"
      :creating="creating"
      @close="closeModal"
      @create="handleCreate"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import api from '@/services/api.service';
import LoadingSpinner from '@/components/common/LoadingSpinner.vue';
import GoogleReconnectNotice from '@/components/common/GoogleReconnectNotice.vue';
import { isGoogleDisconnectedError, googleDisconnectedMessage } from '@/utils/googleConnection';
import { showToast } from '@/services/notification.service';
import TaskHeader from '@/components/tasks/TaskHeader.vue';
import TaskListView from '@/components/tasks/TaskListView.vue';
import TaskBoardView from '@/components/tasks/TaskBoardView.vue';
import TaskManageListsModal from '@/components/tasks/TaskManageListsModal.vue';
import TaskCreateModal from '@/components/tasks/TaskCreateModal.vue';
import TaskEditModal from '@/components/tasks/TaskEditModal.vue';

const googleError = ref<string | null>(null);

const viewMode = ref<'single' | 'board'>('single');
const tasks = ref<any[]>([]);
const taskLists = ref<any[]>([]);
const currentListId = ref('');
const loading = ref(true);
const activeFilter = ref<'all' | 'today' | 'starred' | 'completed'>('all');

// Board View State
const boardTasksMap = ref<Record<string, any[]>>({});
const loadingBoard = ref(false);
const editingListId = ref('');

const showModal = ref(false);
const creating = ref(false);
const createTaskListId = ref('');

const showListModal = ref(false);
const creatingList = ref(false);
const clearingCompleted = ref(false);

const showEditModal = ref(false);
const savingEdit = ref(false);
const selectedTaskForEdit = ref<any | null>(null);

const activeTasks = computed(() => tasks.value.filter(t => t.status !== 'completed'));
const completedTasks = computed(() => tasks.value.filter(t => t.status === 'completed'));

// Aggregate Board Tasks
const allBoardTasks = computed(() => {
  return Object.values(boardTasksMap.value).flat();
});

const currentTasksSource = computed(() => {
  return viewMode.value === 'board' ? allBoardTasks.value : tasks.value;
});

const totalTasksCount = computed(() => {
  return currentTasksSource.value.length;
});

const totalDueTasksCount = computed(() => {
  const now = new Date();
  const endOfDay = new Date(now.getFullYear(), now.getMonth(), now.getDate(), 23, 59, 59);
  return currentTasksSource.value.filter(t => t.due && new Date(t.due) <= endOfDay && t.status !== 'completed').length;
});

const totalStarredTasksCount = computed(() => {
  return currentTasksSource.value.filter(t => t.isStarred).length;
});

const totalCompletedTasksCount = computed(() => {
  return currentTasksSource.value.filter(t => t.status === 'completed').length;
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
      googleError.value = null;
    }
  } catch (e: any) {
    console.error('Failed to fetch task lists', e);
    if (isGoogleDisconnectedError(e)) {
      googleError.value = googleDisconnectedMessage(e);
    }
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
      if (target) {
        boardTasksMap.value[target] = res.data;
      }
      googleError.value = null;
    }
  } catch (e: any) {
    console.error('Failed to fetch tasks:', e);
    if (isGoogleDisconnectedError(e)) {
      googleError.value = googleDisconnectedMessage(e);
    }
  } finally {
    loading.value = false;
  }
};

const fetchBoardTasks = async () => {
  if (taskLists.value.length === 0) return;
  loadingBoard.value = true;
  try {
    const promises = taskLists.value.map(async (list) => {
      try {
        const res: any = await api.get(`/tasks?listId=${encodeURIComponent(list.id)}`);
        if (res.success && res.data) {
          return { listId: list.id, tasks: res.data };
        }
      } catch (e: any) {
        console.error(`Failed to fetch tasks for list ${list.id}`, e);
        if (isGoogleDisconnectedError(e)) {
          googleError.value = googleDisconnectedMessage(e);
        }
      }
      return { listId: list.id, tasks: [] };
    });
    const results = await Promise.all(promises);
    const map: Record<string, any[]> = {};
    results.forEach(r => { map[r.listId] = r.tasks; });
    boardTasksMap.value = map;
    if (currentListId.value && map[currentListId.value]) {
      tasks.value = map[currentListId.value];
    }
  } finally {
    loadingBoard.value = false;
  }
};

const onCurrentListIdChange = (id: string) => {
  currentListId.value = id;
  fetchTasks(id);
};

const handleCreateList = async (title: string, done?: () => void) => {
  if (!title.trim()) return;
  creatingList.value = true;
  try {
    const res: any = await api.post('/tasks/lists', { title: title.trim() });
    if (res.success) {
      showToast({
        severity: 'success',
        summary: 'Thành công',
        detail: `Đã tạo danh sách "${title}"`,
      });
      if (done) done();
      await fetchTaskLists();
      if (viewMode.value === 'board') {
        fetchBoardTasks();
      }
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
      delete boardTasksMap.value[listId];
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

const handleClearCompleted = async (listId?: string) => {
  const targetListId = listId || currentListId.value;
  if (!targetListId) return;
  if (!confirm('Bạn có muốn xóa sạch toàn bộ các task đã hoàn thành trong danh sách này?')) return;

  clearingCompleted.value = true;
  try {
    const res: any = await api.post(`/tasks/lists/${targetListId}/clear-completed`, {});
    if (res.success) {
      showToast({
        severity: 'success',
        summary: 'Đã dọn dẹp',
        detail: 'Đã xóa toàn bộ công việc đã hoàn thành.',
      });
      if (targetListId === currentListId.value) {
        fetchTasks(currentListId.value);
      }
      if (viewMode.value === 'board') {
        fetchBoardTasks();
      }
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

const openCreateModal = (preselectedListId?: string) => {
  createTaskListId.value = preselectedListId || currentListId.value || (taskLists.value[0]?.id || '');
  showModal.value = true;
};

const closeModal = () => {
  showModal.value = false;
};

const openEditModal = (task: any, listId?: string) => {
  editingListId.value = listId || currentListId.value;
  selectedTaskForEdit.value = task;
  showEditModal.value = true;
};

const handleUpdateTask = async (payload: {
  id: string;
  title: string;
  notes: string;
  due: string | null;
  status: string;
  isStarred: boolean;
}) => {
  savingEdit.value = true;
  const targetListId = editingListId.value || currentListId.value;
  try {
    const res: any = await api.put(`/tasks/${payload.id}`, {
      taskListId: targetListId,
      title: payload.title,
      notes: payload.notes,
      due: payload.due,
      status: payload.status,
      isStarred: payload.isStarred
    });
    if (res.success) {
      showToast({
        severity: 'success',
        summary: 'Cập nhật thành công',
        detail: 'Task đã được lưu vào Google Tasks.',
      });
      showEditModal.value = false;
      if (targetListId === currentListId.value) {
        fetchTasks(currentListId.value);
      }
      if (viewMode.value === 'board') {
        fetchBoardTasks();
      }
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

const cleanNotes = (notes: string) => {
  return notes.replace('⭐ [Starred]\n', '').replace('⭐ [Starred]', '').trim();
};

const toggleStar = async (task: any, listId?: string) => {
  const targetListId = listId || currentListId.value;
  const newStarred = !task.isStarred;
  task.isStarred = newStarred;
  try {
    await api.put(`/tasks/${task.googleTaskId}`, {
      taskListId: targetListId,
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

const toggleComplete = async (task: any, listId?: string) => {
  const targetListId = listId || currentListId.value;
  const isNowCompleted = task.status !== 'completed';
  task.status = isNowCompleted ? 'completed' : 'needsAction';
  const endpoint = isNowCompleted 
    ? `/tasks/${task.googleTaskId}/complete?listId=${encodeURIComponent(targetListId)}`
    : `/tasks/${task.googleTaskId}/uncomplete?listId=${encodeURIComponent(targetListId)}`;

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

const handleCreate = async (payload: {
  taskListId: string;
  title: string;
  notes: string;
  due: string | null;
  parentTaskId: string | null;
  isStarred: boolean;
  syncToCalendar: boolean;
  calendarStartTime: string | null;
  calendarEndTime: string | null;
  isPublic: boolean;
}) => {
  creating.value = true;
  const targetListId = payload.taskListId || currentListId.value;
  try {
    await api.post('/tasks', payload);
    showToast({
      severity: 'success',
      summary: 'Đã tạo task',
      detail: 'Công việc mới đã được lưu vào Google Tasks.',
    });
    closeModal();
    if (targetListId === currentListId.value) {
      fetchTasks(currentListId.value);
    }
    if (viewMode.value === 'board') {
      fetchBoardTasks();
    }
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

const handleDelete = async (id: string, listId?: string) => {
  const targetListId = listId || currentListId.value;
  if (confirm('Xóa task này vĩnh viễn?')) {
    try {
      await api.delete(`/tasks/${id}?listId=${encodeURIComponent(targetListId)}`);
      tasks.value = tasks.value.filter(t => t.googleTaskId !== id);
      if (boardTasksMap.value[targetListId]) {
        boardTasksMap.value[targetListId] = boardTasksMap.value[targetListId].filter(t => t.googleTaskId !== id);
      }
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

onMounted(async () => {
  await fetchTaskLists();
  if (!googleError.value) {
    await fetchTasks(currentListId.value);
  } else {
    loading.value = false;
  }
});
</script>

<style scoped lang="scss">
.tasks-page {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
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
</style>
