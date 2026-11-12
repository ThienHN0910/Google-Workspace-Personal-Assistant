<template>
  <!-- 1. WEEK VIEW -->
  <div v-if="viewMode === 'week'" class="week-grid">
    <div class="week-header-row">
      <div
        v-for="day in weekDays"
        :key="day.toISOString()"
        class="day-header-cell"
        :class="{ 'is-today': isToday(day) }"
      >
        <span class="day-name">{{ formatDayName(day) }}</span>
        <span class="day-number">{{ day.getDate() }}</span>
      </div>
    </div>

    <div class="week-body-row">
      <div
        v-for="day in weekDays"
        :key="'col-' + day.toISOString()"
        class="day-body-column"
        :class="{ 'is-today-col': isToday(day) }"
      >
        <div
          v-for="(event, idx) in getEventsForDay(day)"
          :key="idx"
          class="event-pill"
          :class="{ 'private-pill': event.visibility === 'private' }"
          @click="$emit('select-event', event)"
        >
          <div class="event-pill-time">
            <i v-if="event.visibility === 'private'" class="pi pi-lock"></i>
            {{ formatTime(event.start) }} - {{ event.end ? formatTime(event.end) : '?' }}
          </div>
          <div class="event-pill-title">{{ event.title }}</div>
          <div v-if="event.location" class="event-pill-loc">
            <i class="pi pi-map-marker"></i> {{ event.location }}
          </div>
        </div>

        <div v-if="getEventsForDay(day).length === 0" class="no-event-slot">
          Không có sự kiện
        </div>
      </div>
    </div>
  </div>

  <!-- 2. MONTH VIEW -->
  <div v-else-if="viewMode === 'month'" class="month-grid">
    <div class="month-header-row">
      <div v-for="name in dayNamesShort" :key="name" class="month-header-cell">
        {{ name }}
      </div>
    </div>

    <div class="month-body-grid">
      <div
        v-for="day in monthGridDays"
        :key="'month-cell-' + day.toISOString()"
        class="month-day-cell"
        :class="{
          'not-current-month': !isSameMonth(day, currentDate),
          'is-today-cell': isToday(day)
        }"
      >
        <div class="cell-top">
          <span class="day-num">{{ day.getDate() }}</span>
        </div>

        <div class="cell-events">
          <div
            v-for="(event, idx) in getEventsForDay(day).slice(0, 3)"
            :key="idx"
            class="month-event-pill"
            :class="{ 'private-pill': event.visibility === 'private' }"
            @click="$emit('select-event', event)"
          >
            <span class="pill-dot"></span>
            <span class="pill-text">{{ event.title }}</span>
          </div>
          <div v-if="getEventsForDay(day).length > 3" class="more-badge" @click="$emit('select-day', day)">
            +{{ getEventsForDay(day).length - 3 }} sự kiện khác
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import {
  isToday,
  isSameMonth,
  getWeekDays,
  getMonthGridDays
} from './calendarConstants';

const props = defineProps<{
  viewMode: 'week' | 'month';
  currentDate: Date;
  events: any[];
}>();

defineEmits<{
  (e: 'select-event', event: any): void;
  (e: 'select-day', day: Date): void;
}>();

const dayNamesShort = ['T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'CN'];

const weekDays = computed(() => getWeekDays(props.currentDate));
const monthGridDays = computed(() => getMonthGridDays(props.currentDate));

const getEventsForDay = (day: Date) => {
  const dayStart = new Date(day.getFullYear(), day.getMonth(), day.getDate(), 0, 0, 0, 0);
  const dayEnd = new Date(day.getFullYear(), day.getMonth(), day.getDate(), 23, 59, 59, 999);
  return props.events.filter(e => {
    const start = new Date(e.start);
    const end = e.end ? new Date(e.end) : start;
    return start <= dayEnd && end >= dayStart;
  });
};

const formatDayName = (date: Date) => {
  const days = ['Chủ Nhật', 'Thứ 2', 'Thứ 3', 'Thứ 4', 'Thứ 5', 'Thứ 6', 'Thứ 7'];
  return days[date.getDay()];
};

const formatTime = (dateStr: string) => {
  if (!dateStr) return '';
  return new Date(dateStr).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit', hour12: false });
};
</script>

<style scoped lang="scss">
/* 1. Week Grid */
.week-grid {
  background: rgba(11, 17, 32, 0.85);
  backdrop-filter: blur(16px);
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 1rem;
  overflow: hidden;
  box-shadow: 0 16px 36px rgba(0, 0, 0, 0.35);
}

.week-header-row {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  border-bottom: 1px solid rgba(255, 255, 255, 0.08);
  background: rgba(15, 23, 42, 0.6);
}

.day-header-cell {
  padding: 1rem 0.5rem;
  text-align: center;
  border-right: 1px solid rgba(255, 255, 255, 0.06);

  &:last-child { border-right: none; }

  .day-name { display: block; font-size: 0.75rem; color: #94a3b8; text-transform: uppercase; font-weight: 600; }
  .day-number { display: inline-block; font-size: 1.25rem; font-weight: 800; color: #f8fafc; margin-top: 0.25rem; }

  &.is-today {
    .day-number {
      background: #6366f1;
      color: #fff;
      width: 28px;
      height: 28px;
      border-radius: 50%;
      line-height: 28px;
    }
  }
}

.week-body-row {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  min-height: 380px;
}

.day-body-column {
  padding: 0.5rem;
  border-right: 1px solid rgba(255, 255, 255, 0.06);
  display: flex;
  flex-direction: column;
  gap: 0.5rem;

  &:last-child { border-right: none; }
  &.is-today-col { background: rgba(99, 102, 241, 0.04); }
}

.event-pill {
  background: rgba(99, 102, 241, 0.18);
  border-left: 3px solid #6366f1;
  border-radius: 0.4rem;
  padding: 0.5rem;
  cursor: pointer;
  transition: transform 0.15s ease, background 0.15s ease;

  &:hover {
    transform: translate3d(0, -2px, 0);
    background: rgba(99, 102, 241, 0.28);
  }

  &.private-pill {
    background: rgba(148, 163, 184, 0.12);
    border-left-color: #94a3b8;
  }

  .event-pill-time {
    font-size: 0.725rem;
    color: #818cf8;
    font-weight: 700;
  }

  .event-pill-title {
    font-size: 0.825rem;
    font-weight: 700;
    color: #f8fafc;
    margin: 0.2rem 0;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }

  .event-pill-loc {
    font-size: 0.7rem;
    color: #94a3b8;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }
}

.no-event-slot {
  font-size: 0.75rem;
  color: #64748b;
  text-align: center;
  margin-top: 1.5rem;
}

/* 2. Month Grid */
.month-grid {
  background: rgba(11, 17, 32, 0.85);
  backdrop-filter: blur(16px);
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 1rem;
  overflow: hidden;
  box-shadow: 0 16px 36px rgba(0, 0, 0, 0.35);
}

.month-header-row {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  background: rgba(15, 23, 42, 0.6);
  border-bottom: 1px solid rgba(255, 255, 255, 0.08);

  .month-header-cell {
    padding: 0.75rem;
    text-align: center;
    font-size: 0.8rem;
    font-weight: 700;
    color: #94a3b8;
  }
}

.month-body-grid {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
}

.month-day-cell {
  min-height: 100px;
  padding: 0.4rem;
  border-right: 1px solid rgba(255, 255, 255, 0.06);
  border-bottom: 1px solid rgba(255, 255, 255, 0.06);
  display: flex;
  flex-direction: column;
  gap: 0.25rem;

  &:nth-child(7n) { border-right: none; }

  &.not-current-month {
    opacity: 0.35;
    background: rgba(0, 0, 0, 0.2);
  }

  &.is-today-cell {
    background: rgba(99, 102, 241, 0.08);
    .day-num {
      background: #6366f1;
      color: #fff;
      width: 22px;
      height: 22px;
      border-radius: 50%;
      text-align: center;
      line-height: 22px;
    }
  }

  .day-num {
    font-size: 0.8rem;
    font-weight: 700;
    color: #cbd5e1;
    display: inline-block;
  }
}

.month-event-pill {
  background: rgba(99, 102, 241, 0.2);
  border-radius: 0.25rem;
  padding: 0.2rem 0.35rem;
  font-size: 0.725rem;
  color: #f8fafc;
  display: flex;
  align-items: center;
  gap: 0.3rem;
  cursor: pointer;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  transition: background 0.15s ease;

  &:hover { background: rgba(99, 102, 241, 0.35); }

  .pill-dot {
    width: 6px;
    height: 6px;
    border-radius: 50%;
    background: #6366f1;
  }
}

.more-badge {
  font-size: 0.7rem;
  color: #818cf8;
  cursor: pointer;
  font-weight: 600;
  &:hover { text-decoration: underline; }
}
</style>
