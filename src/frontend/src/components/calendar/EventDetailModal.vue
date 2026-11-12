<template>
  <div v-if="visible && event" class="modal-overlay" @click.self="$emit('close')">
    <div class="modal-content event-detail-modal">
      <div class="modal-header">
        <div class="title-with-color">
          <span class="color-badge" :style="{ background: getEventColor(event.colorId) }"></span>
          <h3>{{ event.title }}</h3>
        </div>
        <button class="close-btn" @click="$emit('close')"><i class="pi pi-times"></i></button>
      </div>

      <div class="detail-body">
        <!-- Read-only Notice -->
        <div v-if="event.canEdit === false" class="readonly-banner">
          <div class="readonly-info">
            <i class="pi pi-lock"></i>
            <div>
              <strong>Sự kiện chỉ đọc:</strong>
              <p>Sự kiện này được tạo tự động từ Gmail hoặc từ lời mời bên ngoài. Vui lòng thao tác trên Google Calendar gốc nếu cần cập nhật.</p>
            </div>
          </div>
        </div>

        <!-- Video Conference Banner -->
        <div v-if="event.meetUrl" class="meet-banner">
          <div class="meet-info">
            <i class="pi pi-video text-green"></i>
            <div>
              <strong>{{ getMeetingServiceLabel(event.meetUrl) }}:</strong>
              <p class="meet-url-text">{{ event.meetUrl }}</p>
            </div>
          </div>
          <a :href="event.meetUrl" target="_blank" class="btn-join-meet">
            <i class="pi pi-video"></i> Tham gia họp
          </a>
        </div>

        <div class="info-row">
          <i class="pi pi-clock"></i>
          <div>
            <strong>Thời gian:</strong>
            <p v-if="event.isAllDay">Cả ngày ({{ formatDateTime(event.start).split(' ')[0] }})</p>
            <p v-else>{{ formatDateTime(event.start) }} - {{ event.end ? formatDateTime(event.end) : 'Không xác định' }}</p>
          </div>
        </div>

        <div v-if="event.attendees && event.attendees.length > 0" class="info-row">
          <i class="pi pi-users"></i>
          <div>
            <strong>Người tham gia ({{ event.attendees.length }}):</strong>
            <div class="attendees-list">
              <span v-for="att in event.attendees" :key="att" class="attendee-chip">{{ att }}</span>
            </div>
          </div>
        </div>

        <div v-if="event.location" class="info-row">
          <i class="pi pi-map-marker"></i>
          <div>
            <strong>Địa điểm:</strong>
            <p>{{ event.location }}</p>
          </div>
        </div>

        <div v-if="event.description" class="info-row">
          <i class="pi pi-align-left"></i>
          <div>
            <strong>Mô tả:</strong>
            <p>{{ event.description }}</p>
          </div>
        </div>

        <div class="info-row">
          <i class="pi pi-shield"></i>
          <div>
            <strong>Chế độ hiển thị:</strong>
            <p>{{ event.visibility === 'public' ? '🌐 Công khai (Hiển thị chi tiết trên Public Calendar)' : '🔒 Riêng tư (Chỉ hiển thị Bận/Rảnh cho khách)' }}</p>
          </div>
        </div>
      </div>

      <div class="modal-actions">
        <a v-if="event.htmlLink" :href="event.htmlLink" target="_blank" class="btn-ext-link">
          <i class="pi pi-external-link"></i> Mở Google Calendar
        </a>
        <button v-if="event.canEdit !== false" class="btn-edit" @click="$emit('edit', event)">
          <i class="pi pi-pencil"></i> Chỉnh sửa
        </button>
        <button v-if="event.canEdit !== false" class="btn-danger" @click="$emit('delete', event.id)">
          <i class="pi pi-trash"></i> Xóa sự kiện
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { getEventColor } from './calendarConstants';

defineProps<{
  visible: boolean;
  event: any | null;
}>();

defineEmits<{
  (e: 'close'): void;
  (e: 'edit', event: any): void;
  (e: 'delete', eventId: string): void;
}>();

const formatDateTime = (dateStr: string) => {
  if (!dateStr) return '';
  return new Date(dateStr).toLocaleString('vi-VN', {
    day: '2-digit', month: '2-digit', year: 'numeric',
    hour: '2-digit', minute: '2-digit'
  });
};

const getMeetingServiceLabel = (url?: string | null): string => {
  if (!url) return 'Cuộc họp trực tuyến';
  const lower = url.toLowerCase();
  if (lower.includes('teams.microsoft.com') || lower.includes('teams.live.com')) return 'Microsoft Teams';
  if (lower.includes('zoom.us')) return 'Zoom Meeting';
  if (lower.includes('meet.google.com')) return 'Google Meet';
  if (lower.includes('webex.com')) return 'Cisco Webex';
  return 'Phòng họp trực tuyến';
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

.event-detail-modal {
  .modal-header {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    border-bottom: 1px solid rgba(255, 255, 255, 0.08);
    padding-bottom: 0.75rem;
    margin-bottom: 1.25rem;
    h3 { margin: 0; font-size: 1.3rem; }
  }

  .close-btn {
    background: none;
    border: none;
    color: #94a3b8;
    font-size: 1.1rem;
    cursor: pointer;
    &:hover { color: #fff; }
  }

  .detail-body {
    display: flex;
    flex-direction: column;
    gap: 1rem;
  }

  .info-row {
    display: flex;
    gap: 0.75rem;
    align-items: flex-start;
    i { color: #818cf8; font-size: 1.1rem; margin-top: 0.15rem; }
    strong { font-size: 0.85rem; color: #cbd5e1; }
    p { margin: 0.15rem 0 0; font-size: 0.9rem; color: #f8fafc; }
  }
}

.title-with-color {
  display: flex;
  align-items: center;
  gap: 0.6rem;

  .color-badge {
    width: 1rem;
    height: 1rem;
    border-radius: 50%;
    flex-shrink: 0;
  }
}

.readonly-banner {
  background: rgba(245, 158, 11, 0.1);
  border: 1px solid rgba(245, 158, 11, 0.3);
  padding: 0.85rem 1rem;
  border-radius: 0.5rem;
  margin-bottom: 0.5rem;

  .readonly-info {
    display: flex;
    align-items: flex-start;
    gap: 0.6rem;
    i { font-size: 1.25rem; color: #fbbf24; margin-top: 0.15rem; }
    strong { color: #fbbf24; font-size: 0.85rem; }
    p { margin: 0.2rem 0 0; font-size: 0.85rem; color: #e2e8f0; line-height: 1.4; }
  }
}

.meet-banner {
  background: rgba(16, 185, 129, 0.1);
  border: 1px solid rgba(16, 185, 129, 0.3);
  padding: 0.85rem 1rem;
  border-radius: 0.5rem;
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 1rem;
  margin-bottom: 1rem;

  .meet-info {
    display: flex;
    align-items: center;
    gap: 0.6rem;
    i { font-size: 1.5rem; }
    .meet-url-text { font-size: 0.85rem; color: #34d399; word-break: break-all; margin: 0; }
  }

  .btn-join-meet {
    background: #10b981;
    color: #fff;
    padding: 0.45rem 0.85rem;
    border-radius: 0.4rem;
    font-size: 0.85rem;
    font-weight: 700;
    display: flex;
    align-items: center;
    gap: 0.35rem;
    text-decoration: none;
    white-space: nowrap;
    &:hover { background: #059669; }
  }
}

.attendees-list {
  display: flex;
  flex-wrap: wrap;
  gap: 0.35rem;
  margin-top: 0.35rem;

  .attendee-chip {
    background: rgba(99, 102, 241, 0.15);
    color: #c7d2fe;
    border: 1px solid rgba(99, 102, 241, 0.3);
    padding: 0.2rem 0.5rem;
    border-radius: 1rem;
    font-size: 0.8rem;
  }
}

.modal-actions {
  display: flex;
  justify-content: flex-end;
  gap: 0.75rem;
  margin-top: 1.5rem;
  padding-top: 1rem;
  border-top: 1px solid rgba(255, 255, 255, 0.08);

  button, a {
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

  .btn-ext-link { background: rgba(255, 255, 255, 0.08); color: #818cf8; &:hover { background: rgba(255, 255, 255, 0.14); } }
  .btn-edit { background: #6366f1; color: #fff; &:hover { background: #4f46e5; } }
  .btn-danger { background: rgba(239, 68, 68, 0.15); color: #f87171; &:hover { background: rgba(239, 68, 68, 0.3); } }
}
</style>
