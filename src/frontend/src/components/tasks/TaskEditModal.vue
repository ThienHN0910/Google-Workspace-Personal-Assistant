<template>
  <div v-if="visible" class="modal-overlay" @click.self="$emit('close')">
    <div class="modal-content">
      <h3>✏️ Chỉnh sửa Task</h3>
      <form @submit.prevent="handleSubmit">
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
          <button type="button" class="btn-cancel" @click="$emit('close')">Hủy</button>
          <button type="submit" class="btn-submit" :disabled="saving">
            {{ saving ? 'Đang lưu...' : 'Lưu Thay Đổi' }}
          </button>
        </div>
      </form>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';

const props = defineProps<{
  visible: boolean;
  task: any | null;
  saving: boolean;
}>();

const emit = defineEmits<{
  (e: 'close'): void;
  (e: 'save', payload: {
    id: string;
    title: string;
    notes: string;
    due: string | null;
    status: string;
    isStarred: boolean;
  }): void;
}>();

const cleanNotes = (notes: string) => {
  return notes.replace('⭐ [Starred]\n', '').replace('⭐ [Starred]', '').trim();
};

const editTaskForm = ref({
  id: '',
  title: '',
  notes: '',
  due: '',
  status: 'needsAction',
  isStarred: false
});

const initForm = () => {
  if (!props.task) return;
  let formattedDue = '';
  if (props.task.due) {
    const d = new Date(props.task.due);
    const pad = (n: number) => (n < 10 ? '0' + n : n);
    formattedDue = `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
  }

  editTaskForm.value = {
    id: props.task.googleTaskId,
    title: props.task.title,
    notes: cleanNotes(props.task.notes || ''),
    due: formattedDue,
    status: props.task.status || 'needsAction',
    isStarred: props.task.isStarred || false
  };
};

watch(
  () => [props.visible, props.task],
  ([vis]) => {
    if (vis) {
      initForm();
    }
  },
  { immediate: true }
);

const handleSubmit = () => {
  if (!editTaskForm.value.title.trim()) return;
  emit('save', {
    id: editTaskForm.value.id,
    title: editTaskForm.value.title,
    notes: editTaskForm.value.notes,
    due: editTaskForm.value.due ? new Date(editTaskForm.value.due).toISOString() : null,
    status: editTaskForm.value.status,
    isStarred: editTaskForm.value.isStarred
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

  .form-group {
    margin-bottom: 1rem;

    label {
      display: block;
      font-size: 0.8rem;
      font-weight: 600;
      color: #94a3b8;
      margin-bottom: 0.35rem;

      .required {
        color: #fb7185;
      }
    }

    input, textarea {
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

  .form-group-checkbox {
    margin-bottom: 1rem;

    label {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      font-size: 0.8rem;
      color: #cbd5e1;
      cursor: pointer;

      input[type="checkbox"] {
        accent-color: #10b981;
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
</style>
