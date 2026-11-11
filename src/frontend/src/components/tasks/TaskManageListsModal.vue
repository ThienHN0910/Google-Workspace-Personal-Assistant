<template>
  <div v-if="visible" class="modal-overlay" @click.self="$emit('close')">
    <div class="modal-content list-modal">
      <h3>📂 Quản lý Danh Sách Google Tasks</h3>
      <div class="create-list-bar">
        <input v-model="newListName" placeholder="Tên danh sách mới (VD: Dự án A, Cá nhân...)" />
        <button class="btn-submit" @click="handleCreate" :disabled="!newListName.trim() || creatingList">
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
            <button class="btn-icon" @click="$emit('rename-list', l)" title="Đổi tên">
              <i class="pi pi-pencil"></i>
            </button>
            <button class="btn-icon text-red" @click="$emit('delete-list', l.id)" title="Xóa danh sách">
              <i class="pi pi-trash"></i>
            </button>
          </div>
        </div>
      </div>

      <div class="modal-actions">
        <button class="btn-cancel" @click="$emit('close')">Đóng</button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';

const props = defineProps<{
  visible: boolean;
  taskLists: any[];
  currentListId: string;
  creatingList: boolean;
}>();

const emit = defineEmits<{
  (e: 'close'): void;
  (e: 'create-list', title: string, done: () => void): void;
  (e: 'rename-list', list: any): void;
  (e: 'delete-list', listId: string): void;
}>();

const newListName = ref('');

const handleCreate = () => {
  if (!newListName.value.trim() || props.creatingList) return;
  emit('create-list', newListName.value.trim(), () => {
    newListName.value = '';
  });
};
</script>

<style scoped lang="scss">
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

  .create-list-bar {
    display: flex;
    gap: 0.5rem;
    margin-bottom: 1rem;

    input {
      flex: 1;
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

    .btn-submit {
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
      background: linear-gradient(135deg, #10b981 0%, #059669 100%);
      border: 1px solid rgba(52, 211, 153, 0.3);
      color: #fff;

      &:hover:not(:disabled) {
        filter: brightness(1.1);
        transform: translate3d(0, -1px, 0);
      }
      &:disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }
    }
  }

  .task-lists-container {
    max-height: 280px;
    overflow-y: auto;
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    margin-bottom: 1rem;
    padding-right: 0.2rem;

    &::-webkit-scrollbar {
      width: 4px;
    }
    &::-webkit-scrollbar-thumb {
      background: rgba(148, 163, 184, 0.2);
      border-radius: 2px;
    }
  }

  .list-row, .list-item {
    display: flex;
    justify-content: space-between;
    align-items: center;
    background: rgba(11, 17, 32, 0.5);
    padding: 0.65rem 0.85rem;
    border-radius: 0.5rem;
    border: 1px solid rgba(148, 163, 184, 0.1);

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
  }
}
</style>
