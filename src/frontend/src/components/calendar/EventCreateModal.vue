<template>
  <div v-if="visible" class="modal-overlay" @click.self="$emit('close')">
    <div class="modal-content event-edit-modal">
      <h3>Tạo sự kiện mới</h3>
      <form @submit.prevent="handleSubmit">
        <div class="form-group" v-if="calendars.length > 0">
          <label for="create-calendar">Chọn Lịch Google</label>
          <select id="create-calendar" name="calendarId" v-model="newEvent.calendarId" class="form-select">
            <option v-for="c in calendars" :key="c.id" :value="c.id">
              {{ c.summary }} {{ c.primary ? '(Chính)' : '' }}
            </option>
          </select>
        </div>

        <div class="form-group">
          <label for="create-title">Tiêu đề <span class="required">*</span></label>
          <input id="create-title" name="title" v-model="newEvent.title" required placeholder="Họp team..." autofocus />
        </div>
        <div class="form-row">
          <div class="form-group half">
            <label for="create-start">Bắt đầu <span class="required">*</span></label>
            <input id="create-start" name="start" type="datetime-local" v-model="newEvent.start" required />
          </div>
          <div class="form-group half">
            <label for="create-end">Kết thúc</label>
            <input id="create-end" name="end" type="datetime-local" v-model="newEvent.end" />
          </div>
        </div>

        <div class="form-group-checkbox">
          <label for="create-allday">
            <input id="create-allday" name="isAllDay" type="checkbox" v-model="newEvent.isAllDay" />
            Sự kiện cả ngày (All-day event)
          </label>
        </div>

        <div class="form-group-checkbox">
          <label for="create-meet">
            <input id="create-meet" name="createMeetLink" type="checkbox" v-model="newEvent.createMeetLink" />
            📹 1-Click Tạo link họp Google Meet tự động
          </label>
        </div>

        <div class="form-group">
          <label for="create-attendees">👥 Khách mời / Người tham gia (Emails cách nhau bởi dấu phẩy)</label>
          <input id="create-attendees" name="attendees" v-model="newEvent.attendees" placeholder="nguyenvana@gmail.com, colleague@company.com..." />
        </div>

        <div class="form-group">
          <label>🎨 Màu sự kiện</label>
          <div class="color-palette">
            <span 
              v-for="c in CALENDAR_COLORS" 
              :key="c.id" 
              class="color-dot" 
              :style="{ background: c.color }" 
              :class="{ active: newEvent.colorId === c.id }"
              @click="newEvent.colorId = c.id"
              :title="c.name"
            ></span>
          </div>
        </div>

        <div class="form-group">
          <label for="create-reminder">⏰ Nhắc nhở trước</label>
          <select id="create-reminder" name="reminderMinutes" v-model="newEvent.reminderMinutes" class="form-select">
            <option :value="null">Mặc định Google Calendar</option>
            <option :value="10">10 phút trước</option>
            <option :value="30">30 phút trước</option>
            <option :value="60">1 tiếng trước</option>
            <option :value="1440">1 ngày trước</option>
          </select>
        </div>

        <div class="form-group">
          <label for="create-location">Địa điểm</label>
          <input id="create-location" name="location" v-model="newEvent.location" placeholder="Phòng họp A / Google Meet..." />
        </div>
        <div class="form-group">
          <label for="create-desc">Mô tả</label>
          <textarea id="create-desc" name="description" v-model="newEvent.description" rows="3" placeholder="Ghi chú thêm..."></textarea>
        </div>
        <div class="form-group-checkbox">
          <label for="create-task">
            <input id="create-task" name="createTask" type="checkbox" v-model="newEvent.createTask" />
            Đồng thời tạo nhắc nhở trong Google Tasks
          </label>
        </div>
        <div class="form-group-checkbox">
          <label for="create-public">
            <input id="create-public" name="isPublic" type="checkbox" v-model="newEvent.isPublic" />
            Công khai sự kiện (Hiển thị chi tiết trên Public Calendar)
          </label>
        </div>
        <div class="modal-actions">
          <button type="button" class="btn-cancel" @click="$emit('close')">Hủy</button>
          <button type="submit" class="btn-submit" :disabled="creating">
            {{ creating ? 'Đang tạo...' : 'Tạo sự kiện' }}
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
  calendars: any[];
  selectedCalendarId: string;
  creating: boolean;
}>();

const emit = defineEmits<{
  (e: 'close'): void;
  (e: 'create', payload: {
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
    createTask: boolean;
    isPublic: boolean;
  }): void;
}>();

const newEvent = ref({
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
  createTask: false,
  isPublic: true,
});

const initForm = () => {
  const now = new Date();
  now.setMinutes(0);
  now.setSeconds(0);
  now.setHours(now.getHours() + 1);

  const end = new Date(now);
  end.setHours(now.getHours() + 1);

  newEvent.value = {
    calendarId: props.selectedCalendarId || 'primary',
    title: '',
    start: toLocalIsoString(now),
    end: toLocalIsoString(end),
    location: '',
    description: '',
    createMeetLink: false,
    attendees: '',
    colorId: '',
    isAllDay: false,
    reminderMinutes: null,
    createTask: false,
    isPublic: true,
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

const handleSubmit = () => {
  emit('create', {
    ...newEvent.value,
    calendarId: newEvent.value.calendarId || props.selectedCalendarId || 'primary'
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
