<template>
  <div class="calendar-toolbar">
    <div class="toolbar-left">
      <button class="btn-today" @click="$emit('today')">Hôm nay</button>
      <div class="nav-buttons">
        <button class="nav-btn" @click="$emit('prev')" title="Thời gian trước">
          <i class="pi pi-chevron-left"></i>
        </button>
        <button class="nav-btn" @click="$emit('next')" title="Thời gian sau">
          <i class="pi pi-chevron-right"></i>
        </button>
      </div>
      <h2 class="current-period-title">{{ currentPeriodTitle }}</h2>
    </div>

    <div class="toolbar-right">
      <div class="calendar-select-box" v-if="calendars.length > 0">
        <i class="pi pi-calendar"></i>
        <select :value="selectedCalendarId" @change="$emit('update:selectedCalendarId', ($event.target as HTMLSelectElement).value)" class="cal-select">
          <option v-for="c in calendars" :key="c.id" :value="c.id">
            {{ c.summary }} {{ c.primary ? '(Chính)' : '' }}
          </option>
        </select>
      </div>

      <div class="view-switcher">
        <button :class="{ active: viewMode === 'week' }" @click="$emit('update:viewMode', 'week')">
          <i class="pi pi-th-large"></i> Tuần
        </button>
        <button :class="{ active: viewMode === 'month' }" @click="$emit('update:viewMode', 'month')">
          <i class="pi pi-calendar"></i> Tháng
        </button>
        <button :class="{ active: viewMode === 'agenda' }" @click="$emit('update:viewMode', 'agenda')">
          <i class="pi pi-list"></i> Danh sách
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
defineProps<{
  currentPeriodTitle: string;
  calendars: any[];
  selectedCalendarId: string;
  viewMode: 'week' | 'month' | 'agenda';
}>();

defineEmits<{
  (e: 'today'): void;
  (e: 'prev'): void;
  (e: 'next'): void;
  (e: 'update:selectedCalendarId', id: string): void;
  (e: 'update:viewMode', mode: 'week' | 'month' | 'agenda'): void;
}>();
</script>

<style scoped lang="scss">
.calendar-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 1rem;
  margin-bottom: 1.5rem;
}

.toolbar-left {
  display: flex;
  align-items: center;
  gap: 0.75rem;

  .btn-today {
    background: rgba(255, 255, 255, 0.08);
    border: 1px solid rgba(255, 255, 255, 0.15);
    color: #f8fafc;
    padding: 0.45rem 0.9rem;
    border-radius: 0.4rem;
    font-weight: 600;
    cursor: pointer;
    &:hover { background: rgba(255, 255, 255, 0.15); }
  }

  .nav-buttons {
    display: flex;
    gap: 0.25rem;

    .nav-btn {
      background: none;
      border: 1px solid rgba(255, 255, 255, 0.12);
      color: #94a3b8;
      border-radius: 0.4rem;
      width: 32px;
      height: 32px;
      display: flex;
      align-items: center;
      justify-content: center;
      cursor: pointer;
      &:hover { color: #fff; background: rgba(255, 255, 255, 0.08); }
    }
  }

  .current-period-title {
    font-size: 1.2rem;
    font-weight: 700;
    color: #f8fafc;
    margin: 0 0 0 0.5rem;
  }
}

.view-switcher {
  display: flex;
  background: #0f172a;
  border: 1px solid rgba(255, 255, 255, 0.12);
  border-radius: 0.5rem;
  padding: 0.2rem;

  button {
    background: none;
    border: none;
    color: #94a3b8;
    padding: 0.4rem 0.85rem;
    border-radius: 0.35rem;
    font-size: 0.85rem;
    font-weight: 600;
    cursor: pointer;
    display: flex;
    align-items: center;
    gap: 0.4rem;

    &.active {
      background: #6366f1;
      color: #fff;
    }
  }
}

.calendar-select-box {
  display: flex;
  align-items: center;
  gap: 0.4rem;
  background: #0f172a;
  border: 1px solid rgba(255, 255, 255, 0.12);
  padding: 0.35rem 0.65rem;
  border-radius: 0.4rem;
  font-size: 0.85rem;
  color: #818cf8;

  .cal-select {
    background: transparent;
    border: none;
    color: #f8fafc;
    font-size: 0.85rem;
    font-weight: 600;
    cursor: pointer;
    &:focus { outline: none; }
  }
}
</style>
