<template>
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
    <div class="header-actions" v-if="!googleError">
      <!-- View Mode Switcher -->
      <div class="view-mode-toggle">
        <button 
          type="button"
          class="btn-view-toggle" 
          :class="{ active: viewMode === 'single' }" 
          @click="$emit('update:viewMode', 'single')" 
          title="Xem một danh sách"
        >
          <i class="pi pi-bars"></i> Đơn danh sách
        </button>
        <button 
          type="button"
          class="btn-view-toggle" 
          :class="{ active: viewMode === 'board' }" 
          @click="$emit('update:viewMode', 'board')" 
          title="Xem nhiều danh sách cùng lúc dạng bảng song song"
        >
          <i class="pi pi-table"></i> Bảng đa danh sách
        </button>
      </div>

      <!-- Task List Selector (Single View) -->
      <div class="list-selector-wrapper" v-if="viewMode === 'single' && taskLists.length > 0">
        <label><i class="pi pi-list"></i> Danh sách:</label>
        <select :value="currentListId" @change="$emit('update:currentListId', ($event.target as HTMLSelectElement).value)" class="list-select">
          <option v-for="l in taskLists" :key="l.id" :value="l.id">
            {{ l.title }}
          </option>
        </select>
        <button class="btn-manage-lists" @click="$emit('manage-lists')" title="Quản lý danh sách">
          <i class="pi pi-cog"></i>
        </button>
      </div>

      <button 
        v-if="viewMode === 'board'"
        class="btn-manage-lists-board" 
        @click="$emit('manage-lists')" 
        title="Quản lý danh sách"
      >
        <i class="pi pi-cog"></i> Quản lý danh sách
      </button>

      <button 
        v-if="viewMode === 'single' && completedTasksCount > 0" 
        class="btn-clear-completed" 
        @click="$emit('clear-completed')" 
        :disabled="clearingCompleted"
        title="Xóa sạch các task đã hoàn thành trên Google Tasks"
      >
        <i class="pi pi-check-square"></i> Dọn dẹp task đã xong ({{ completedTasksCount }})
      </button>

      <button class="primary-btn" @click="$emit('create-task')">
        <i class="pi pi-plus"></i> Thêm Task mới
      </button>
    </div>
  </header>
</template>

<script setup lang="ts">
defineProps<{
  viewMode: 'single' | 'board';
  taskLists: any[];
  currentListId: string;
  completedTasksCount: number;
  clearingCompleted: boolean;
  googleError: string | null;
}>();

defineEmits<{
  (e: 'update:viewMode', mode: 'single' | 'board'): void;
  (e: 'update:currentListId', id: string): void;
  (e: 'manage-lists'): void;
  (e: 'clear-completed'): void;
  (e: 'create-task'): void;
}>();
</script>

<style scoped lang="scss">
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
}
</style>
