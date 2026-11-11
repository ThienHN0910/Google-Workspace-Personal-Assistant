<template>
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
      <button class="btn-refresh-board" @click="$emit('refresh-board')" :disabled="loadingBoard" title="Tải lại tất cả danh sách">
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
              @click="$emit('create-task', list.id)" 
              :title="`Thêm task vào ${list.title}`"
            >
              <i class="pi pi-plus"></i>
            </button>
            <button 
              v-if="hasCompletedTasksInList(list.id)" 
              class="btn-column-clear" 
              @click="$emit('clear-completed', list.id)" 
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
              <div class="task-checkbox" @click="$emit('toggle-complete', task, list.id)">
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
                  @click="$emit('toggle-star', task, list.id)" 
                  title="Đánh dấu quan trọng"
                >
                  <i class="pi" :class="task.isStarred ? 'pi-star-fill text-yellow' : 'pi-star'"></i>
                </button>
                <button class="edit-btn" @click="$emit('edit-task', task, list.id)" title="Chỉnh sửa">
                  <i class="pi pi-pencil"></i>
                </button>
                <button class="delete-btn" @click="$emit('delete-task', task.googleTaskId, list.id)" title="Xóa">
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

<script setup lang="ts">
import { ref, computed } from 'vue';
import LoadingSpinner from '@/components/common/LoadingSpinner.vue';

const props = defineProps<{
  taskLists: any[];
  boardTasksMap: Record<string, any[]>;
  loadingBoard: boolean;
  activeFilter: 'all' | 'today' | 'starred' | 'completed';
}>();

defineEmits<{
  (e: 'refresh-board'): void;
  (e: 'create-task', listId: string): void;
  (e: 'clear-completed', listId: string): void;
  (e: 'toggle-complete', task: any, listId: string): void;
  (e: 'toggle-star', task: any, listId: string): void;
  (e: 'edit-task', task: any, listId: string): void;
  (e: 'delete-task', taskId: string, listId: string): void;
}>();

const selectedBoardListIds = ref<string[]>([]);

const visibleBoardLists = computed(() => {
  if (selectedBoardListIds.value.length === 0) return props.taskLists;
  return props.taskLists.filter(l => selectedBoardListIds.value.includes(l.id));
});

const isListSelected = (listId: string) => {
  if (selectedBoardListIds.value.length === 0) return true;
  return selectedBoardListIds.value.includes(listId);
};

const toggleBoardList = (listId: string) => {
  if (selectedBoardListIds.value.length === 0) {
    selectedBoardListIds.value = props.taskLists.map(l => l.id).filter(id => id !== listId);
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
  const items = props.boardTasksMap[listId] || [];
  return items.some(t => t.status === 'completed');
};

const getFilteredTasksForList = (listId: string) => {
  const listTasks = props.boardTasksMap[listId] || [];
  if (props.activeFilter === 'completed') {
    return listTasks.filter(t => t.status === 'completed');
  }
  if (props.activeFilter === 'starred') {
    return listTasks.filter(t => t.isStarred);
  }
  if (props.activeFilter === 'today') {
    const now = new Date();
    const endOfDay = new Date(now.getFullYear(), now.getMonth(), now.getDate(), 23, 59, 59);
    return listTasks.filter(t => t.due && new Date(t.due) <= endOfDay && t.status !== 'completed');
  }
  return listTasks.filter(t => t.status !== 'completed');
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
</script>

<style scoped lang="scss">
.empty-state {
  text-align: center;
  padding: 3rem 1rem;
  color: #64748b;

  i { font-size: 2.25rem; margin-bottom: 0.75rem; color: #34d399; opacity: 0.8; }
  p { font-size: 0.85rem; }
}

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

@media (max-width: 768px) {
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
