<template>
  <div v-if="visible" class="modal-overlay" @click.self="$emit('close')">
    <div class="modal-content event-edit-modal">
      <h3>✏️ Chỉnh sửa sự kiện Google Calendar</h3>
      <form @submit.prevent="handleSubmit">
        <div class="form-group">
          <label>Tiêu đề <span class="required">*</span></label>
          <input v-model="editForm.title" required placeholder="Họp team..." />
        </div>
        <div class="form-row">
          <div class="form-group half">
            <label>Bắt đầu <span class="required">*</span></label>
            <input type="datetime-local" v-model="editForm.start" required />
          </div>
          <div class="form-group half">
            <label>Kết thúc</label>
            <input type="datetime-local" v-model="editForm.end" />
          </div>
        </div>

        <div class="form-group-checkbox">
          <label>
            <input type="checkbox" v-model="editForm.isAllDay" />
            Sự kiện cả ngày (All-day event)
          </label>
        </div>

        <div class="form-group-checkbox">
          <label>
            <input type="checkbox" v-model="editForm.createMeetLink" />
            📹 Tạo/Giữ link Google Meet tự động
          </label>
        </div>

        <div class="form-group">
          <label>👥 Khách mời / Người tham gia (Emails cách nhau bởi dấu phẩy)</label>
          <input v-model="editForm.attendees" placeholder="nguyenvana@gmail.com, colleague@company.com..." />
        </div>

        <div class="form-group">
          <label>🎨 Màu sự kiện</label>
          <div class="color-palette">
            <span 
              v-for="c in CALENDAR_COLORS" 
              :key="c.id" 
              class="color-dot" 
              :style="{ background: c.color }" 
              :class="{ active: editForm.colorId === c.id }"
              @click="editForm.colorId = c.id"
              :title="c.name"
            ></span>
          </div>
        </div>

        <div class="form-group">
          <label>⏰ Nhắc nhở trước</label>
          <select v-model="editForm.reminderMinutes" class="form-select">
            <option :value="null">Mặc định Google Calendar</option>
            <option :value="10">10 phút trước</option>
            <option :value="30">30 phút trước</option>
            <option :value="60">1 tiếng trước</option>
            <option :value="1440">1 ngày trước</option>
          </select>
        </div>

        <div class="form-group">
          <label>Địa điểm</label>
          <input v-model="editForm.location" placeholder="Phòng họp A / Google Meet..." />
        </div>
        <div class="form-group">
          <label>Mô tả chi tiết</label>
          <textarea v-model="editForm.description" rows="3" placeholder="Ghi chú nội dung họp..."></textarea>
        </div>
        <div class="form-group-checkbox">
          <label>
            <input type="checkbox" v-model="editForm.isPublic" />
            Công khai sự kiện trên Public Calendar
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
import { CALENDAR_COLORS, toLocalIsoString } from './calendarConstants';

const props = defineProps<{
  visible: boolean;
  event: any | null;
  selectedCalendarId: string;
  saving: boolean;
}>();

const emit = defineEmits<{
  (e: 'close'): void;
  (e: 'save', payload: {
    id: string;
    calendarId: string;
    title: string;
    start: string;
    end: string | null;
    location: string;
    description: string;
    createMeetLink: boolean;
    attendees: string;
    colorId: string;
    isAllDay: boolean;
    reminderMinutes: number | null;
    isPublic: boolean;
  }): void;
}>();

const editForm = ref({
  id: '',
  calendarId: 'primary',
  title: '',
  start: '',
  end: '',
  location: '',
  description: '',
  createMeetLink: false,
  attendees: '',
  colorId: '',
  isAllDay: false,
  reminderMinutes: null as number | null,
  isPublic: true,
});

const initForm = () => {
  if (!props.event) return;
  const startDate = new Date(props.event.start);
  const endDate = props.event.end ? new Date(props.event.end) : new Date(startDate.getTime() + 3600000);

  editForm.value = {
    id: props.event.id,
    calendarId: props.selectedCalendarId,
    title: props.event.title,
    start: toLocalIsoString(startDate),
    end: toLocalIsoString(endDate),
    location: props.event.location || '',
    description: props.event.description || '',
    createMeetLink: !!props.event.meetUrl,
    attendees: props.event.attendees ? props.event.attendees.join(', ') : '',
    colorId: props.event.colorId || '',
    isAllDay: !!props.event.isAllDay,
    reminderMinutes: props.event.reminderMinutes || null,
    isPublic: props.event.visibility === 'public',
  };
};

watch(
  () => [props.visible, props.event],
  ([vis]) => {
    if (vis) {
      initForm();
    }
  },
  { immediate: true }
);

const handleSubmit = () => {
  if (!editForm.value.title || !editForm.value.start) return;
  emit('save', {
    ...editForm.value
  });
};
</script>

<style scoped lang="scss">
.modal-overlay {
  position: fixed;
  inset: 0;
  background: rgba(15, 23, 42, 0.75);
  backdrop-filter: blur(4px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  padding: 1rem;
}

.modal-content {
  background: #0b1120;
  border: 1px solid rgba(255, 255, 255, 0.12);
  border-radius: 1rem;
  width: 100%;
  max-width: 580px;
  max-height: 90vh;
  overflow-y: auto;
  box-shadow: 0 25px 60px -15px rgba(0, 0, 0, 0.8);
  padding: 1.75rem;

  h3 {
    font-size: 1.25rem;
    font-weight: 700;
    color: #f8fafc;
    margin: 0 0 1.25rem 0;
  }
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
  margin-bottom: 1rem;

  label {
    font-size: 0.85rem;
    font-weight: 600;
    color: #cbd5e1;
    .required { color: #f87171; }
  }

  input, textarea, select {
    background: #0f172a;
    border: 1px solid rgba(255, 255, 255, 0.1);
    border-radius: 0.5rem;
    padding: 0.65rem 0.875rem;
    color: #f8fafc;
    font-size: 0.9rem;
    &:focus { outline: none; border-color: #6366f1; }
  }
}

.form-row {
  display: flex;
  gap: 1rem;
  .half { flex: 1; }
}

.form-group-checkbox {
  margin-bottom: 1rem;
  label {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    font-size: 0.85rem;
    color: #cbd5e1;
    cursor: pointer;
  }
}

.color-palette {
  display: flex;
  gap: 0.5rem;
  flex-wrap: wrap;
  align-items: center;
  padding: 0.5rem 0;

  .color-dot {
    width: 1.5rem;
    height: 1.5rem;
    border-radius: 50%;
    cursor: pointer;
    transition: transform 0.15s ease;
    border: 2px solid transparent;

    &:hover { transform: scale(1.2); }
    &.active { border-color: #fff; transform: scale(1.15); box-shadow: 0 0 8px rgba(255, 255, 255, 0.5); }
  }
}

.modal-actions {
  display: flex;
  justify-content: flex-end;
  gap: 0.75rem;
  margin-top: 1.5rem;
  padding-top: 1rem;
  border-top: 1px solid rgba(255, 255, 255, 0.08);

  button {
    padding: 0.6rem 1.15rem;
    border-radius: 0.5rem;
    font-size: 0.875rem;
    font-weight: 600;
    cursor: pointer;
    display: flex;
    align-items: center;
    gap: 0.4rem;
    border: none;
    text-decoration: none;
  }

  .btn-cancel { background: rgba(255, 255, 255, 0.08); color: #cbd5e1; &:hover { background: rgba(255, 255, 255, 0.14); } }
  .btn-submit { background: #10b981; color: #fff; &:hover { background: #059669; } }
}
</style>
