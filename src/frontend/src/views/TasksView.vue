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
        <!-- View Mode Switcher -->
        <div class="view-mode-toggle">
          <button 
            type="button"
            class="btn-view-toggle" 
            :class="{ active: viewMode === 'single' }" 
            @click="setSingleView" 
            title="Xem một danh sách"
          >
            <i class="pi pi-bars"></i> Đơn danh sách
          </button>
          <button 
            type="button"
            class="btn-view-toggle" 
            :class="{ active: viewMode === 'board' }" 
            @click="setBoardView" 
            title="Xem nhiều danh sách cùng lúc dạng bảng song song"
          >
            <i class="pi pi-table"></i> Bảng đa danh sách
          </button>
        </div>

        <!-- Task List Selector (Single View) -->
        <div class="list-selector-wrapper" v-if="viewMode === 'single' && taskLists.length > 0">
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
          v-if="viewMode === 'board'"
          class="btn-manage-lists-board" 
          @click="showListModal = true" 
          title="Quản lý danh sách"
        >
          <i class="pi pi-cog"></i> Quản lý danh sách
        </button>

        <button 
          v-if="viewMode === 'single' && completedTasks.length > 0" 
          class="btn-clear-completed" 
          @click="() => handleClearCompleted()" 
          :disabled="clearingCompleted"
          title="Xóa sạch các task đã hoàn thành trên Google Tasks"
        >
          <i class="pi pi-check-square"></i> Dọn dẹp task đã xong ({{ completedTasks.length }})
        </button>

        <button class="primary-btn" @click="() => openCreateModal()">
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
    <template v-if="viewMode === 'single'">
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
    </template>

    <!-- Multi-List Board View -->
    <template v-else-if="viewMode === 'board'">
      <div class="board-view-container">
        <!-- Board List Filter Toolbar -->
        <div class="board-toolbar" v-if="taskLists.length > 1">
          <div class="toolbar-left">
            <span class="toolbar-label"><i class="pi pi-filter"></i> Lọc danh sách cột:</span>
            <div class="list-pills">
              <button 
                class="list-pill" 
                :class="{ active: selectedBoardListIds.length === 0 || selectedBoardListIds.length === taskLists.length }" 
                @click="selectAllBoardLists"
              >
                Tất cả ({{ taskLists.length }})
              </button>
              <button 
                v-for="l in taskLists" 
                :key="l.id" 
                class="list-pill"
                :class="{ active: isListSelected(l.id) }"
                @click="toggleBoardList(l.id)"
              >
                {{ l.title }}
              </button>
            </div>
          </div>
          <button class="btn-refresh-board" @click="fetchBoardTasks" :disabled="loadingBoard" title="Tải lại tất cả danh sách">
            <i class="pi pi-sync" :class="{ 'pi-spin': loadingBoard }"></i> Tải lại bảng
          </button>
        </div>

        <LoadingSpinner v-if="loadingBoard" text="Đang tải dữ liệu tất cả danh sách..." />

        <div v-else-if="visibleBoardLists.length === 0" class="empty-state">
          <i class="pi pi-exclamation-circle"></i>
          <p>Không có danh sách nào được chọn để hiển thị.</p>
        </div>

        <div v-else class="tasks-board">
          <div 
            v-for="list in visibleBoardLists" 
            :key="list.id" 
            class="board-column"
          >
            <!-- Column Header -->
            <div class="column-header">
              <div class="column-title-group">
                <span class="column-dot"></span>
                <h3 class="column-title" :title="list.title">{{ list.title }}</h3>
                <span class="column-count">{{ getFilteredTasksForList(list.id).length }}</span>
              </div>
              <div class="column-actions">
                <button 
                  class="btn-column-add" 
                  @click="openCreateModal(list.id)" 
                  :title="`Thêm task vào ${list.title}`"
                >
                  <i class="pi pi-plus"></i>
                </button>
                <button 
                  v-if="hasCompletedTasksInList(list.id)" 
                  class="btn-column-clear" 
                  @click="handleClearCompleted(list.id)" 
                  :title="`Dọn dẹp task hoàn thành trong ${list.title}`"
                >
                  <i class="pi pi-check"></i>
                </button>
              </div>
            </div>

            <!-- Column Tasks List -->
            <div class="column-body">
              <div 
                v-if="getFilteredTasksForList(list.id).length === 0" 
                class="column-empty"
              >
                <i class="pi pi-inbox"></i>
                <span>Không có task</span>
              </div>

              <div 
                v-else 
                v-for="task in getFilteredTasksForList(list.id)" 
                :key="task.googleTaskId" 
                class="board-task-card"
                :class="{ 
                  'completed': task.status === 'completed',
                  'is-subtask': !!task.parentTaskId,
                  'is-starred': task.isStarred
                }"
              >
                <div class="task-card-main">
                  <div class="task-checkbox" @click="toggleComplete(task, list.id)">
                    <i class="pi" :class="task.status === 'completed' ? 'pi-check-circle text-green' : 'pi-circle text-gray'"></i>
                  </div>

                  <div class="task-card-content">
                    <div class="task-title-row">
                      <span v-if="task.parentTaskId" class="subtask-badge">↳ Subtask</span>
                      <span class="task-title">{{ task.title }}</span>
                    </div>
                    <div class="task-notes" v-if="task.notes">{{ cleanNotes(task.notes) }}</div>
                    <div class="task-due" v-if="task.due">
                      <i class="pi pi-calendar"></i> {{ formatDate(task.due) }}
                    </div>
                  </div>

                  <div class="task-card-actions">
                    <button 
                      class="star-btn" 
                      :class="{ active: task.isStarred }" 
                      @click="toggleStar(task, list.id)" 
                      title="Đánh dấu quan trọng"
                    >
                      <i class="pi" :class="task.isStarred ? 'pi-star-fill text-yellow' : 'pi-star'"></i>
                    </button>
                    <button class="edit-btn" @click="openEditModal(task, list.id)" title="Chỉnh sửa">
                      <i class="pi pi-pencil"></i>
                    </button>
                    <button class="delete-btn" @click="handleDelete(task.googleTaskId, list.id)" title="Xóa">
                      <i class="pi pi-trash"></i>
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </template>

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

const viewMode = ref<'single' | 'board'>('single');
const tasks = ref<any[]>([]);
const taskLists = ref<any[]>([]);
const currentListId = ref('');
const loading = ref(true);
const activeFilter = ref<'all' | 'today' | 'starred' | 'completed'>('all');

// Board View State
const boardTasksMap = ref<Record<string, any[]>>({});
const loadingBoard = ref(false);
const selectedBoardListIds = ref<string[]>([]);
const editingListId = ref('');

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

const visibleBoardLists = computed(() => {
  if (selectedBoardListIds.value.length === 0) return taskLists.value;
  return taskLists.value.filter(l => selectedBoardListIds.value.includes(l.id));
});

const isListSelected = (listId: string) => {
  if (selectedBoardListIds.value.length === 0) return true;
  return selectedBoardListIds.value.includes(listId);
};

const toggleBoardList = (listId: string) => {
  if (selectedBoardListIds.value.length === 0) {
    selectedBoardListIds.value = taskLists.value.map(l => l.id).filter(id => id !== listId);
  } else if (selectedBoardListIds.value.includes(listId)) {
    if (selectedBoardListIds.value.length > 1) {
      selectedBoardListIds.value = selectedBoardListIds.value.filter(id => id !== listId);
    }
  } else {
    selectedBoardListIds.value.push(listId);
  }
};

const selectAllBoardLists = () => {
  selectedBoardListIds.value = [];
};

const hasCompletedTasksInList = (listId: string) => {
  const items = boardTasksMap.value[listId] || [];
  return items.some(t => t.status === 'completed');
};

const getFilteredTasksForList = (listId: string) => {
  const listTasks = boardTasksMap.value[listId] || [];
  if (activeFilter.value === 'completed') {
    return listTasks.filter(t => t.status === 'completed');
  }
  if (activeFilter.value === 'starred') {
    return listTasks.filter(t => t.isStarred);
  }
  if (activeFilter.value === 'today') {
    const now = new Date();
    const endOfDay = new Date(now.getFullYear(), now.getMonth(), now.getDate(), 23, 59, 59);
    return listTasks.filter(t => t.due && new Date(t.due) <= endOfDay && t.status !== 'completed');
  }
  return listTasks.filter(t => t.status !== 'completed');
};

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
      if (target) {
        boardTasksMap.value[target] = res.data;
      }
    }
  } catch (e) {
    console.error('Failed to fetch tasks:', e);
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
      } catch (e) {
        console.error(`Failed to fetch tasks for list ${list.id}`, e);
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

const setSingleView = () => {
  viewMode.value = 'single';
};

const setBoardView = () => {
  viewMode.value = 'board';
  if (Object.keys(boardTasksMap.value).length === 0) {
    fetchBoardTasks();
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
  newTask.value = { 
    taskListId: preselectedListId || currentListId.value || (taskLists.value[0]?.id || ''),
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

const openEditModal = (task: any, listId?: string) => {
  editingListId.value = listId || currentListId.value;
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
  const targetListId = editingListId.value || currentListId.value;
  try {
    const payload = {
      taskListId: targetListId,
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
  const targetListId = newTask.value.taskListId || currentListId.value;
  try {
    const payload = {
      taskListId: targetListId,
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

/* View Mode Toggle */
.view-mode-toggle {
  display: flex;
  background: rgba(15, 23, 42, 0.7);
  border: 1px solid rgba(148, 163, 184, 0.16);
  border-radius: 0.5rem;
  padding: 0.18rem;
  gap: 0.2rem;

  .btn-view-toggle {
    height: 32px;
    padding: 0 0.75rem;
    border-radius: 0.35rem;
    font-size: 0.75rem;
    font-weight: 600;
    border: none;
    background: transparent;
    color: #94a3b8;
    cursor: pointer;
    display: inline-flex;
    align-items: center;
    gap: 0.35rem;
    transition: all 0.15s ease;

    &:hover {
      color: #f8fafc;
    }

    &.active {
      background: linear-gradient(135deg, rgba(16, 185, 129, 0.25), rgba(6, 182, 212, 0.2));
      border: 1px solid rgba(16, 185, 129, 0.4);
      color: #34d399;
      box-shadow: 0 0 10px rgba(16, 185, 129, 0.15);
    }
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

.btn-manage-lists-board {
  height: 38px;
  background: rgba(15, 23, 42, 0.7);
  border: 1px solid rgba(148, 163, 184, 0.16);
  color: #94a3b8;
  padding: 0 0.85rem;
  border-radius: 0.5rem;
  font-size: 0.8rem;
  font-weight: 600;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  transition: all 0.15s ease;

  &:hover {
    color: #f8fafc;
    border-color: rgba(148, 163, 184, 0.3);
  }
}

/* ============================================================
   MULTI-LIST BOARD VIEW
   ============================================================ */
.board-view-container {
  display: flex;
  flex-direction: column;
  gap: 1rem;

  .board-toolbar {
    display: flex;
    justify-content: space-between;
    align-items: center;
    flex-wrap: wrap;
    gap: 0.75rem;
    background: rgba(15, 23, 42, 0.6);
    border: 1px solid rgba(148, 163, 184, 0.12);
    border-radius: 0.65rem;
    padding: 0.55rem 0.85rem;

    .toolbar-left {
      display: flex;
      align-items: center;
      gap: 0.65rem;
      flex-wrap: wrap;
    }

    .toolbar-label {
      font-size: 0.775rem;
      color: #94a3b8;
      display: inline-flex;
      align-items: center;
      gap: 0.35rem;
      font-weight: 500;
    }

    .list-pills {
      display: flex;
      flex-wrap: wrap;
      gap: 0.35rem;
    }

    .list-pill {
      height: 28px;
      padding: 0 0.65rem;
      border-radius: 9999px;
      font-size: 0.725rem;
      font-weight: 600;
      border: 1px solid rgba(148, 163, 184, 0.18);
      background: rgba(11, 17, 32, 0.6);
      color: #94a3b8;
      cursor: pointer;
      transition: all 0.15s ease;

      &:hover {
        color: #f8fafc;
        border-color: rgba(6, 182, 212, 0.3);
      }

      &.active {
        background: rgba(6, 182, 212, 0.15);
        border-color: rgba(6, 182, 212, 0.45);
        color: #22d3ee;
        box-shadow: 0 0 10px rgba(6, 182, 212, 0.15);
      }
    }

    .btn-refresh-board {
      height: 28px;
      padding: 0 0.75rem;
      border-radius: 0.35rem;
      font-size: 0.725rem;
      font-weight: 600;
      background: rgba(148, 163, 184, 0.1);
      border: 1px solid rgba(148, 163, 184, 0.2);
      color: #cbd5e1;
      cursor: pointer;
      display: inline-flex;
      align-items: center;
      gap: 0.35rem;
      transition: all 0.15s ease;

      &:hover:not(:disabled) {
        background: rgba(148, 163, 184, 0.2);
        color: #fff;
      }
    }
  }

  .tasks-board {
    display: flex;
    gap: 1.15rem;
    overflow-x: auto;
    padding-bottom: 1rem;
    align-items: flex-start;
    scroll-behavior: smooth;

    /* Custom scrollbar for horizontal board */
    &::-webkit-scrollbar {
      height: 8px;
    }
    &::-webkit-scrollbar-track {
      background: rgba(11, 17, 32, 0.5);
      border-radius: 4px;
    }
    &::-webkit-scrollbar-thumb {
      background: rgba(148, 163, 184, 0.25);
      border-radius: 4px;
      &:hover { background: rgba(148, 163, 184, 0.45); }
    }
  }

  .board-column {
    min-width: 310px;
    max-width: 360px;
    width: 330px;
    flex-shrink: 0;
    background: rgba(15, 23, 42, 0.75);
    border: 1px solid rgba(148, 163, 184, 0.12);
    border-top: 2px solid rgba(6, 182, 212, 0.4);
    border-radius: 0.85rem;
    padding: 0.95rem;
    box-shadow: 0 4px 16px rgba(0, 0, 0, 0.25);
    display: flex;
    flex-direction: column;
    gap: 0.75rem;

    .column-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding-bottom: 0.65rem;
      border-bottom: 1px solid rgba(148, 163, 184, 0.1);

      .column-title-group {
        display: flex;
        align-items: center;
        gap: 0.45rem;
        min-width: 0;

        .column-dot {
          width: 8px;
          height: 8px;
          border-radius: 50%;
          background: #22d3ee;
          box-shadow: 0 0 8px #22d3ee;
          flex-shrink: 0;
        }

        .column-title {
          font-size: 0.9rem;
          font-weight: 700;
          color: #f1f5f9;
          margin: 0;
          white-space: nowrap;
          overflow: hidden;
          text-overflow: ellipsis;
        }

        .column-count {
          font-size: 0.675rem;
          font-weight: 700;
          background: rgba(148, 163, 184, 0.15);
          color: #94a3b8;
          padding: 0.1rem 0.45rem;
          border-radius: 9999px;
          flex-shrink: 0;
        }
      }

      .column-actions {
        display: flex;
        align-items: center;
        gap: 0.3rem;
        flex-shrink: 0;

        button {
          width: 26px;
          height: 26px;
          border-radius: 0.35rem;
          background: rgba(148, 163, 184, 0.1);
          border: 1px solid rgba(148, 163, 184, 0.18);
          color: #94a3b8;
          cursor: pointer;
          display: inline-flex;
          align-items: center;
          justify-content: center;
          font-size: 0.75rem;
          transition: all 0.15s ease;

          &:hover {
            color: #f8fafc;
            background: rgba(148, 163, 184, 0.2);
          }

          &.btn-column-add:hover {
            background: rgba(16, 185, 129, 0.2);
            border-color: rgba(16, 185, 129, 0.4);
            color: #34d399;
          }

          &.btn-column-clear:hover {
            background: rgba(59, 130, 246, 0.2);
            border-color: rgba(59, 130, 246, 0.4);
            color: #60a5fa;
          }
        }
      }
    }

    .column-body {
      display: flex;
      flex-direction: column;
      gap: 0.55rem;
      max-height: calc(100vh - 280px);
      overflow-y: auto;
      padding-right: 0.2rem;

      &::-webkit-scrollbar {
        width: 4px;
      }
      &::-webkit-scrollbar-thumb {
        background: rgba(148, 163, 184, 0.2);
        border-radius: 2px;
      }

      .column-empty {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        gap: 0.35rem;
        padding: 2rem 0;
        color: #475569;
        font-size: 0.775rem;

        i { font-size: 1.25rem; }
      }
    }

    .board-task-card {
      background: rgba(11, 17, 32, 0.65);
      border: 1px solid rgba(148, 163, 184, 0.1);
      border-radius: 0.6rem;
      padding: 0.65rem 0.75rem;
      transition: all 0.15s ease;

      &:hover {
        border-color: rgba(148, 163, 184, 0.25);
        background: rgba(11, 17, 32, 0.85);
        transform: translate3d(0, -1px, 0);
      }

      &.completed {
        opacity: 0.55;
        .task-title {
          text-decoration: line-through;
          color: #64748b;
        }
      }

      &.is-starred {
        border-color: rgba(245, 158, 11, 0.3);
        box-shadow: 0 0 10px rgba(245, 158, 11, 0.05);
      }

      .task-card-main {
        display: flex;
        align-items: flex-start;
        gap: 0.65rem;
      }

      .task-checkbox {
        cursor: pointer;
        padding-top: 0.1rem;
        font-size: 1rem;
        color: #64748b;
        flex-shrink: 0;

        &:hover { color: #34d399; }
      }

      .task-card-content {
        flex: 1;
        min-width: 0;
        display: flex;
        flex-direction: column;
        gap: 0.25rem;

        .task-title-row {
          display: flex;
          align-items: center;
          gap: 0.35rem;
          flex-wrap: wrap;

          .task-title {
            font-size: 0.825rem;
            font-weight: 600;
            color: #f1f5f9;
            word-break: break-word;
          }
        }

        .task-notes {
          font-size: 0.725rem;
          color: #94a3b8;
          white-space: pre-line;
          word-break: break-word;
          display: -webkit-box;
          -webkit-line-clamp: 2;
          -webkit-box-orient: vertical;
          overflow: hidden;
        }

        .task-due {
          font-size: 0.7rem;
          color: #f59e0b;
          display: inline-flex;
          align-items: center;
          gap: 0.25rem;
          font-variant-numeric: tabular-nums;
        }
      }

      .task-card-actions {
        display: flex;
        align-items: center;
        gap: 0.2rem;
        flex-shrink: 0;

        button {
          width: 24px;
          height: 24px;
          border-radius: 0.3rem;
          border: none;
          background: transparent;
          color: #64748b;
          cursor: pointer;
          display: inline-flex;
          align-items: center;
          justify-content: center;
          font-size: 0.725rem;
          transition: all 0.15s ease;

          &:hover {
            background: rgba(255, 255, 255, 0.08);
            color: #f8fafc;
          }

          &.star-btn.active {
            color: #fbbf24;
          }

          &.delete-btn:hover {
            color: #fb7185;
            background: rgba(244, 63, 94, 0.15);
          }
        }
      }
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

      .view-mode-toggle {
        width: 100%;
        .btn-view-toggle {
          flex: 1;
          justify-content: center;
        }
      }

      .list-selector-wrapper, .btn-manage-lists-board, .btn-clear-completed, .primary-btn {
        width: 100%;
        justify-content: center;
      }
    }
  }

  .board-view-container .board-toolbar {
    flex-direction: column;
    align-items: stretch;
    .btn-refresh-board {
      width: 100%;
      justify-content: center;
    }
  }
}
</style>
