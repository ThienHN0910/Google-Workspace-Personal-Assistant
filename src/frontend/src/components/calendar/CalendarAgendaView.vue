<template>
  <div class="agenda-view">
    <div v-if="sortedEvents.length === 0" class="empty-state">
      <i class="pi pi-calendar"></i>
      <p>Không có sự kiện nào trong khoảng thời gian này.</p>
    </div>
    <div v-else class="agenda-list">
      <div
        v-for="event in sortedEvents"
        :key="event.id"
        class="agenda-card"
        @click="$emit('select-event', event)"
      >
        <div class="agenda-time-box">
          <div class="agenda-date">{{ formatDateShort(event.start) }}</div>
          <div class="agenda-hours">{{ formatTime(event.start) }} - {{ event.end ? formatTime(event.end) : '' }}</div>
        </div>
        <div class="agenda-details">
          <div class="agenda-title">
            {{ event.title }}
            <span v-if="event.visibility === 'private'" class="badge-private"><i class="pi pi-lock"></i> Riêng tư</span>
          </div>
          <div v-if="event.location" class="agenda-location">
            <i class="pi pi-map-marker"></i> {{ event.location }}
          </div>
          <div v-if="event.description" class="agenda-desc">
            {{ event.description }}
          </div>
        </div>
        <div class="agenda-actions">
          <button class="btn-icon" @click.stop="$emit('edit-event', event)" title="Chỉnh sửa"><i class="pi pi-pencil"></i></button>
          <button class="btn-icon text-red" @click.stop="$emit('delete-event', event.id)" title="Xóa"><i class="pi pi-trash"></i></button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';

const props = defineProps<{
  events: any[];
}>();

defineEmits<{
  (e: 'select-event', event: any): void;
  (e: 'edit-event', event: any): void;
  (e: 'delete-event', eventId: string): void;
}>();

const sortedEvents = computed(() => {
  return [...props.events].sort((a, b) => new Date(a.start).getTime() - new Date(b.start).getTime());
});

const formatTime = (dateStr: string) => {
  if (!dateStr) return '';
  return new Date(dateStr).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit', hour12: false });
};

const formatDateShort = (dateStr: string) => {
  if (!dateStr) return '';
  return new Date(dateStr).toLocaleDateString('vi-VN', { weekday: 'short', day: '2-digit', month: '2-digit' });
};
</script>

<style scoped lang="scss">
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 3rem;
  color: #94a3b8;
  gap: 0.75rem;
  i { font-size: 3rem; color: #cbd5e1; opacity: 0.4; }
}

.agenda-view {
  .agenda-list {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
  }

  .agenda-card {
    background: rgba(11, 17, 32, 0.75);
    backdrop-filter: blur(12px);
    border: 1px solid rgba(255, 255, 255, 0.08);
    border-radius: 0.75rem;
    padding: 1rem 1.25rem;
    display: flex;
    align-items: center;
    gap: 1.5rem;
    cursor: pointer;
    transition: transform 0.15s ease, border-color 0.15s ease, background 0.15s ease;

    &:hover {
      background: rgba(15, 23, 42, 0.85);
      border-color: rgba(99, 102, 241, 0.5);
      transform: translate3d(0, -2px, 0);
    }
  }

  .agenda-time-box {
    min-width: 140px;
    .agenda-date { font-weight: 700; color: #818cf8; font-size: 0.9rem; }
    .agenda-hours { font-size: 0.8rem; color: #94a3b8; }
  }

  .agenda-details {
    flex: 1;
    .agenda-title {
      font-size: 1rem;
      font-weight: 700;
      color: #f8fafc;
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }
    .badge-private {
      font-size: 0.7rem;
      background: rgba(148, 163, 184, 0.15);
      color: #94a3b8;
      padding: 0.15rem 0.45rem;
      border-radius: 0.25rem;
    }
    .agenda-location {
      font-size: 0.825rem;
      color: #cbd5e1;
      margin-top: 0.2rem;
    }
    .agenda-desc {
      font-size: 0.8rem;
      color: #94a3b8;
      margin-top: 0.25rem;
    }
  }

  .agenda-actions {
    display: flex;
    gap: 0.5rem;
  }
}

.btn-icon {
  background: none;
  border: 1px solid rgba(255, 255, 255, 0.1);
  color: #cbd5e1;
  padding: 0.4rem;
  border-radius: 0.4rem;
  cursor: pointer;
  &:hover { background: rgba(255, 255, 255, 0.1); }
  &.text-red { color: #f87171; &:hover { background: rgba(239, 68, 68, 0.15); } }
}
</style>
