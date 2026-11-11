<template>
  <div v-if="visible" class="modal-overlay" @click.self="$emit('close')">
    <div class="modal-content">
      <h3>Tạo Task mới</h3>
      <form @submit.prevent="handleSubmit">
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
          <button type="button" class="btn-cancel" @click="$emit('close')">Hủy</button>
          <button type="submit" class="btn-submit" :disabled="creating">
            {{ creating ? 'Đang tạo...' : 'Tạo Task' }}
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
  taskLists: any[];
  defaultListId: string;
  activeTasks: any[];
  creating: boolean;
}>();

const emit = defineEmits<{
  (e: 'close'): void;
  (e: 'create', payload: {
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
  }): void;
}>();

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

const initForm = () => {
  newTask.value = {
    taskListId: props.defaultListId || (props.taskLists[0]?.id || ''),
    title: '',
    notes: '',
    due: '',
    parentTaskId: '',
    isStarred: false,
    syncToCalendar: false,
    calendarStartTime: '',
    calendarEndTime: '',
    isPublic: true
  };
};

watch(
  () => props.visible,
  (val) => {
    if (val) {
      initForm();
    }
  },
  { immediate: true }
);

const onStartTimeChange = () => {
  if (newTask.value.calendarStartTime) {
    const start = new Date(newTask.value.calendarStartTime);
    const end = new Date(start.getTime() + 60 * 60 * 1000);
    const offset = end.getTimezoneOffset() * 60000;
    const localISOTime = (new Date(end.getTime() - offset)).toISOString().slice(0, 16);
    newTask.value.calendarEndTime = localISOTime;
  }
};

const handleSubmit = () => {
  emit('create', {
    taskListId: newTask.value.taskListId || props.defaultListId || (props.taskLists[0]?.id || ''),
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

  .form-group-checkbox {
    margin-bottom: 0.85rem;

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

    &.mt-1 {
      margin-top: 0.5rem;
    }
  }

  .calendar-times {
    background: rgba(11, 17, 32, 0.5);
    border: 1px dashed rgba(16, 185, 129, 0.3);
    border-radius: 0.5rem;
    padding: 0.75rem;
    margin-bottom: 1rem;
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
