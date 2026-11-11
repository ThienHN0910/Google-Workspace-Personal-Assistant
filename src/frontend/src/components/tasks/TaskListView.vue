<template>
  <div v-if="tasks.length === 0" class="empty-state">
    <i class="pi pi-check-circle"></i>
    <p v-if="activeFilter === 'starred'">Bạn chưa gắn sao công việc quan trọng nào.</p>
    <p v-else-if="activeFilter === 'today'">Không có công việc nào cần làm hôm nay.</p>
    <p v-else>Tuyệt vời! Không có công việc nào đang chờ trong danh sách này.</p>
  </div>

  <div v-else class="task-list">
    <div 
      v-for="task in tasks" 
      :key="task.googleTaskId" 
      class="task-item"
      :class="{ 
        'completed': task.status === 'completed',
        'is-subtask': !!task.parentTaskId,
        'is-starred': task.isStarred
      }"
    >
      <div class="task-checkbox" @click="$emit('toggle-complete', task)">
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
          @click="$emit('toggle-star', task)" 
          title="Đánh dấu quan trọng"
        >
          <i class="pi" :class="task.isStarred ? 'pi-star-fill text-yellow' : 'pi-star'"></i>
        </button>
        <button class="edit-btn" @click="$emit('edit-task', task)" title="Chỉnh sửa">
          <i class="pi pi-pencil"></i>
        </button>
        <button class="delete-btn" @click="$emit('delete-task', task.googleTaskId)" title="Xóa">
          <i class="pi pi-trash"></i>
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
defineProps<{
  tasks: any[];
  activeFilter: 'all' | 'today' | 'starred' | 'completed';
}>();

defineEmits<{
  (e: 'toggle-complete', task: any): void;
  (e: 'toggle-star', task: any): void;
  (e: 'edit-task', task: any): void;
  (e: 'delete-task', taskId: string): void;
}>();

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
</style>
