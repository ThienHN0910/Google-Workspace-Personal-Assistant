<template>
  <div class="public-calendar">
    <!-- Header & Live Status -->
    <div class="top-header">
      <div class="title-group">
        <div class="title-icon-badge">
          <i class="pi pi-calendar-plus"></i>
        </div>
        <div class="title-text">
          <div class="title-row">
            <h1>Lịch Làm Việc & Thời Gian Biểu</h1>
            <span class="version-tag">Public Portal</span>
          </div>
          <p class="subtitle">Đồng bộ trực tiếp từ Google Calendar — Khung giờ Bận / Rảnh khả dụng</p>
        </div>
      </div>

      <div class="live-status-pill" :class="{ busy: isBusy }">
        <span class="status-dot"></span>
        <span>{{ isBusy ? 'Hiện tại đang BẬN' : 'Hiện tại đang RẢNH' }}</span>
      </div>
    </div>

    <!-- Calendar Controls Bar -->
    <div class="calendar-toolbar">
      <div class="toolbar-left">
        <button class="btn-today" @click="goToday">Hôm nay</button>
        <div class="nav-buttons">
          <button class="nav-btn" @click="goPrev" title="Trang trước">
            <i class="pi pi-chevron-left"></i>
          </button>
          <button class="nav-btn" @click="goNext" title="Trang sau">
            <i class="pi pi-chevron-right"></i>
          </button>
        </div>
        <h2 class="current-period-title">{{ currentPeriodTitle }}</h2>
      </div>

      <div class="toolbar-right">
        <div class="view-switcher">
          <button :class="{ active: viewMode === 'week' }" @click="viewMode = 'week'">
            <i class="pi pi-th-large"></i> Tuần
          </button>
          <button :class="{ active: viewMode === 'month' }" @click="viewMode = 'month'">
            <i class="pi pi-calendar"></i> Tháng
          </button>
          <button :class="{ active: viewMode === 'agenda' }" @click="viewMode = 'agenda'">
            <i class="pi pi-list"></i> Danh sách
          </button>
        </div>
      </div>
    </div>

    <!-- Privacy Notice Banner -->
    <div class="privacy-notice">
      <div class="notice-icon">
        <i class="pi pi-shield"></i>
      </div>
      <span>Lịch hiển thị dưới dạng khung giờ Bận/Rảnh công khai để bảo vệ thông tin riêng tư cá nhân.</span>
    </div>

    <!-- Loading State -->
    <div v-if="loading" class="loading-container">
      <i class="pi pi-spin pi-spinner"></i>
      <span>Đang đồng bộ dữ liệu Google Calendar...</span>
    </div>

    <!-- Calendar Content Views -->
    <div v-else class="calendar-viewport">
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
              :class="{ 'private-pill': !event.isPublic }"
              @click="openEventDetail(event)"
            >
              <div class="event-pill-time">
                <i v-if="!event.isPublic" class="pi pi-lock"></i>
                {{ formatTime(event.start) }} - {{ event.end ? formatTime(event.end) : '?' }}
              </div>
              <div class="event-pill-title">{{ event.title }}</div>
            </div>
            
            <div v-if="getEventsForDay(day).length === 0" class="no-event-slot">
              Rảnh
            </div>
          </div>
        </div>
      </div>

      <!-- 2. MONTH VIEW -->
      <div v-else-if="viewMode === 'month'" class="month-grid">
        <div class="month-header-row">
          <div v-for="name in dayNamesShort" :key="name" class="month-day-name">
            {{ name }}
          </div>
        </div>
        <div class="month-body-grid">
          <div 
            v-for="day in monthGridDays" 
            :key="day.toISOString()" 
            class="month-cell"
            :class="{ 
              'other-month': !isSameMonth(day), 
              'is-today': isToday(day) 
            }"
          >
            <div class="month-cell-header">
              <span class="day-num">{{ day.getDate() }}</span>
            </div>
            <div class="month-events-container">
              <div 
                v-for="(event, idx) in getEventsForDay(day)" 
                :key="idx" 
                class="month-event-chip"
                :class="{ 'private-chip': !event.isPublic }"
                @click="openEventDetail(event)"
              >
                <span class="chip-dot"></span>
                <span class="chip-time">{{ formatTime(event.start) }}</span>
                <span class="chip-title">{{ event.title }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- 3. AGENDA VIEW -->
      <div v-else-if="viewMode === 'agenda'" class="agenda-view">
        <div v-if="events.length === 0" class="empty-state">
          <i class="pi pi-calendar-plus"></i>
          <p>Không có sự kiện nào trong khoảng thời gian này.</p>
        </div>
        <div v-else class="agenda-list">
          <div 
            v-for="(event, idx) in sortedEvents" 
            :key="idx" 
            class="agenda-card"
            :class="{ 'private-card': !event.isPublic }"
            @click="openEventDetail(event)"
          >
            <div class="agenda-date-box">
              <span class="month">{{ formatMonthShort(event.start) }}</span>
              <span class="day">{{ getDayNum(event.start) }}</span>
              <span class="weekday">{{ formatDayName(new Date(event.start)) }}</span>
            </div>

            <div class="agenda-content">
              <div class="agenda-time">
                <i class="pi pi-clock"></i>
                {{ formatTime(event.start) }} — {{ event.end ? formatTime(event.end) : '?' }}
              </div>
              <h4 class="agenda-title">
                <i v-if="!event.isPublic" class="pi pi-lock"></i>
                {{ event.title }}
              </h4>
              <div v-if="event.location" class="agenda-location">
                <i class="pi pi-map-marker"></i> {{ event.location }}
              </div>
            </div>

            <div class="agenda-badge" :class="event.isPublic ? 'public' : 'busy'">
              {{ event.isPublic ? 'Công khai' : 'Khung giờ bận' }}
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Event Detail Popup Modal -->
    <div v-if="selectedEvent" class="modal-overlay" @click.self="selectedEvent = null">
      <div class="modal-card">
        <div class="modal-header">
          <div class="event-type-badge" :class="selectedEvent.isPublic ? 'public' : 'busy'">
            <i :class="selectedEvent.isPublic ? 'pi pi-calendar' : 'pi pi-lock'"></i>
            <span>{{ selectedEvent.isPublic ? 'Sự kiện công khai' : 'Khung giờ bận (Riêng tư)' }}</span>
          </div>
          <button class="close-btn" @click="selectedEvent = null">
            <i class="pi pi-times"></i>
          </button>
        </div>
        <h3 class="modal-title">{{ selectedEvent.title }}</h3>
        
        <div class="modal-info-list">
          <div class="info-item">
            <i class="pi pi-clock"></i>
            <div>
              <strong>Thời gian:</strong>
              <p>{{ formatFullDateTime(selectedEvent.start) }} — {{ selectedEvent.end ? formatFullDateTime(selectedEvent.end) : '?' }}</p>
            </div>
          </div>
          <div v-if="selectedEvent.location" class="info-item">
            <i class="pi pi-map-marker"></i>
            <div>
              <strong>Địa điểm:</strong>
              <p>{{ selectedEvent.location }}</p>
            </div>
          </div>
          <div class="info-item">
            <i class="pi pi-shield"></i>
            <div>
              <strong>Chế độ bảo mật:</strong>
              <p v-if="selectedEvent.isPublic">Nội dung sự kiện được hiển thị công khai.</p>
              <p v-else>Tiêu đề chi tiết đã ẩn để giữ riêng tư cho chủ sở hữu lịch.</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue';
import api from '@/services/api.service';

const isBusy = ref(false);
const events = ref<any[]>([]);
const loading = ref(true);
const viewMode = ref<'week' | 'month' | 'agenda'>('week');
const currentDate = ref(new Date());
const selectedEvent = ref<any | null>(null);

const dayNamesShort = ['T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'CN'];

const fetchCalendarData = async () => {
  loading.value = true;
  try {
    let startDate: string;
    let endDate: string;

    if (viewMode.value === 'month') {
      const days = monthGridDays.value;
      startDate = days[0].toISOString();
      endDate = new Date(days[days.length - 1].getTime() + 86399000).toISOString();
    } else if (viewMode.value === 'week') {
      const days = weekDays.value;
      startDate = days[0].toISOString();
      endDate = new Date(days[days.length - 1].getTime() + 86399000).toISOString();
    } else {
      const d = new Date(currentDate.value);
      const start = new Date(d);
      start.setDate(d.getDate() - 7);
      const end = new Date(d);
      end.setDate(d.getDate() + 30);
      startDate = start.toISOString();
      endDate = end.toISOString();
    }

    const res: any = await api.get(`/public/calendar-status?startDate=${encodeURIComponent(startDate)}&endDate=${encodeURIComponent(endDate)}`);
    if (res.success && res.data) {
      isBusy.value = res.data.isBusyNow;
      events.value = res.data.events || [];
    }
  } catch (e) {
    console.error('Failed to load public calendar status:', e);
  } finally {
    loading.value = false;
  }
};

watch([currentDate, viewMode], () => {
  fetchCalendarData();
});

// Date math helpers
const isSameDay = (d1: Date, d2: Date) => {
  return d1.getFullYear() === d2.getFullYear() &&
         d1.getMonth() === d2.getMonth() &&
         d1.getDate() === d2.getDate();
};

const isToday = (d: Date) => isSameDay(d, new Date());

const isSameMonth = (d: Date) => {
  return d.getFullYear() === currentDate.value.getFullYear() &&
         d.getMonth() === currentDate.value.getMonth();
};

// Get array of 7 dates (Mon -> Sun) for the selected week
const weekDays = computed(() => {
  const curr = new Date(currentDate.value);
  const dayOfWeek = curr.getDay();
  const diffToMon = dayOfWeek === 0 ? 6 : dayOfWeek - 1;
  
  const monday = new Date(curr);
  monday.setDate(monday.getDate() - diffToMon);

  const days: Date[] = [];
  for (let i = 0; i < 7; i++) {
    const d = new Date(monday);
    d.setDate(monday.getDate() + i);
    days.push(d);
  }
  return days;
});

// Get 35 grid days (5 weeks) for Month View
const monthGridDays = computed(() => {
  const year = currentDate.value.getFullYear();
  const month = currentDate.value.getMonth();
  const firstDay = new Date(year, month, 1);
  
  const dayOfWeek = firstDay.getDay();
  const diffToMon = dayOfWeek === 0 ? 6 : dayOfWeek - 1;
  
  const startDate = new Date(firstDay);
  startDate.setDate(startDate.getDate() - diffToMon);

  const days: Date[] = [];
  const iter = new Date(startDate);
  for (let i = 0; i < 35; i++) {
    days.push(new Date(iter));
    iter.setDate(iter.getDate() + 1);
  }
  return days;
});

const sortedEvents = computed(() => {
  return [...events.value].sort((a, b) => new Date(a.start).getTime() - new Date(b.start).getTime());
});

const currentPeriodTitle = computed(() => {
  const year = currentDate.value.getFullYear();
  const month = currentDate.value.getMonth() + 1;
  if (viewMode.value === 'month') {
    return `Tháng ${month}, ${year}`;
  }
  if (viewMode.value === 'week' && weekDays.value.length === 7) {
    const start = weekDays.value[0];
    const end = weekDays.value[6];
    return `${start.getDate()} Thg ${start.getMonth() + 1} - ${end.getDate()} Thg ${end.getMonth() + 1}, ${year}`;
  }
  return `Tháng ${month}, ${year}`;
});

const getEventsForDay = (day: Date) => {
  return events.value.filter(e => isSameDay(new Date(e.start), day));
};

const goPrev = () => {
  const d = new Date(currentDate.value);
  if (viewMode.value === 'week') {
    d.setDate(d.getDate() - 7);
  } else if (viewMode.value === 'month') {
    d.setMonth(d.getMonth() - 1);
  } else {
    d.setDate(d.getDate() - 7);
  }
  currentDate.value = d;
};

const goNext = () => {
  const d = new Date(currentDate.value);
  if (viewMode.value === 'week') {
    d.setDate(d.getDate() + 7);
  } else if (viewMode.value === 'month') {
    d.setMonth(d.getMonth() + 1);
  } else {
    d.setDate(d.getDate() + 7);
  }
  currentDate.value = d;
};

const goToday = () => {
  currentDate.value = new Date();
};

const openEventDetail = (event: any) => {
  selectedEvent.value = event;
};

// Formatting helpers
const formatDayName = (d: Date) => {
  const days = ['CN', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7'];
  return days[d.getDay()];
};

const formatTime = (dateStr: string) => {
  if (!dateStr) return '';
  return new Date(dateStr).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });
};

const formatMonthShort = (dateStr: string) => {
  if (!dateStr) return '';
  return `Thg ${new Date(dateStr).getMonth() + 1}`;
};

const getDayNum = (dateStr: string) => {
  if (!dateStr) return '';
  return new Date(dateStr).getDate();
};

const formatFullDateTime = (dateStr: string) => {
  if (!dateStr) return '';
  return new Date(dateStr).toLocaleString('vi-VN', {
    weekday: 'long', day: '2-digit', month: '2-digit', year: 'numeric',
    hour: '2-digit', minute: '2-digit'
  });
};

onMounted(fetchCalendarData);
</script>

<style scoped lang="scss">
.public-calendar {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

/* ============================================================
   TOP HEADER & LIVE STATUS
   ============================================================ */
.top-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 1.25rem;

  .title-group {
    display: flex;
    align-items: center;
    gap: 0.85rem;

    .title-icon-badge {
      width: 44px;
      height: 44px;
      border-radius: 0.75rem;
      background: linear-gradient(135deg, rgba(99, 102, 241, 0.25) 0%, rgba(6, 182, 212, 0.2) 100%);
      border: 1px solid rgba(99, 102, 241, 0.4);
      display: flex;
      align-items: center;
      justify-content: center;
      color: #818cf8;
      font-size: 1.35rem;
      box-shadow: 0 0 20px rgba(99, 102, 241, 0.2);
      flex-shrink: 0;
    }

    .title-text {
      .title-row {
        display: flex;
        align-items: center;
        gap: 0.6rem;
        flex-wrap: wrap;

        h1 {
          font-size: 1.45rem;
          font-weight: 800;
          color: #f8fafc;
          letter-spacing: -0.02em;
          margin: 0;
        }

        .version-tag {
          font-size: 0.7rem;
          font-weight: 700;
          padding: 0.15rem 0.55rem;
          border-radius: 9999px;
          background: rgba(99, 102, 241, 0.15);
          border: 1px solid rgba(99, 102, 241, 0.35);
          color: #a5b4fc;
        }
      }

      .subtitle {
        color: #94a3b8;
        font-size: 0.85rem;
        margin: 0.2rem 0 0;
      }
    }
  }
}

.live-status-pill {
  display: inline-flex;
  align-items: center;
  gap: 0.55rem;
  background: rgba(16, 185, 129, 0.12);
  border: 1px solid rgba(16, 185, 129, 0.3);
  color: #34d399;
  padding: 0.45rem 1.15rem;
  border-radius: 9999px;
  font-weight: 700;
  font-size: 0.825rem;
  box-shadow: 0 0 16px rgba(16, 185, 129, 0.15);

  .status-dot {
    width: 8px;
    height: 8px;
    border-radius: 50%;
    background: #34d399;
    box-shadow: 0 0 8px #34d399;
    animation: neonPulse 1.5s infinite;
  }

  &.busy {
    background: rgba(245, 158, 11, 0.12);
    border-color: rgba(245, 158, 11, 0.3);
    color: #fbbf24;
    box-shadow: 0 0 16px rgba(245, 158, 11, 0.15);

    .status-dot {
      background: #fbbf24;
      box-shadow: 0 0 8px #fbbf24;
    }
  }
}

/* ============================================================
   CALENDAR TOOLBAR
   ============================================================ */
.calendar-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 1rem;
}

.toolbar-left {
  display: flex;
  align-items: center;
  gap: 0.75rem;

  .btn-today {
    height: 34px;
    background: rgba(255, 255, 255, 0.08);
    border: 1px solid rgba(255, 255, 255, 0.15);
    color: #f8fafc;
    padding: 0 0.85rem;
    border-radius: 0.45rem;
    font-weight: 600;
    font-size: 0.825rem;
    cursor: pointer;
    transition: all 0.15s ease;

    &:hover {
      background: rgba(255, 255, 255, 0.15);
      transform: translate3d(0, -1px, 0);
    }
  }

  .nav-buttons {
    display: flex;
    gap: 0.25rem;

    .nav-btn {
      width: 34px;
      height: 34px;
      background: none;
      border: 1px solid rgba(255, 255, 255, 0.12);
      color: #94a3b8;
      border-radius: 0.45rem;
      display: flex;
      align-items: center;
      justify-content: center;
      cursor: pointer;
      transition: all 0.15s ease;

      &:hover {
        color: #fff;
        background: rgba(255, 255, 255, 0.08);
        transform: translate3d(0, -1px, 0);
      }
    }
  }

  .current-period-title {
    font-size: 1.15rem;
    font-weight: 700;
    color: #f8fafc;
    margin: 0 0 0 0.4rem;
  }
}

.view-switcher {
  display: flex;
  background: #0b1120;
  border: 1px solid rgba(255, 255, 255, 0.12);
  border-radius: 0.5rem;
  padding: 0.2rem;

  button {
    height: 32px;
    background: none;
    border: none;
    color: #94a3b8;
    padding: 0 0.85rem;
    border-radius: 0.35rem;
    font-size: 0.825rem;
    font-weight: 600;
    cursor: pointer;
    display: inline-flex;
    align-items: center;
    gap: 0.4rem;
    transition: all 0.15s ease;

    &.active {
      background: #6366f1;
      color: #fff;
      box-shadow: 0 2px 8px rgba(99, 102, 241, 0.35);
    }

    &:hover:not(.active) {
      color: #f8fafc;
    }
  }
}

/* ============================================================
   PRIVACY NOTICE BANNER
   ============================================================ */
.privacy-notice {
  background: rgba(11, 17, 32, 0.75);
  backdrop-filter: blur(12px);
  border: 1px solid rgba(6, 182, 212, 0.3);
  padding: 0.75rem 1.15rem;
  border-radius: 0.6rem;
  font-size: 0.825rem;
  color: #94a3b8;
  display: flex;
  align-items: center;
  gap: 0.75rem;

  .notice-icon {
    width: 28px;
    height: 28px;
    border-radius: 0.4rem;
    background: rgba(6, 182, 212, 0.15);
    color: #22d3ee;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 0.85rem;
    flex-shrink: 0;
  }
}

/* ============================================================
   LOADING
   ============================================================ */
.loading-container {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.75rem;
  padding: 4rem 1rem;
  color: #818cf8;
  font-weight: 600;
  font-size: 0.95rem;

  i { font-size: 1.5rem; }
}

/* ============================================================
   1. WEEK VIEW
   ============================================================ */
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
  padding: 0.85rem 0.5rem;
  text-align: center;
  border-right: 1px solid rgba(255, 255, 255, 0.06);

  &:last-child { border-right: none; }

  .day-name { display: block; font-size: 0.725rem; color: #94a3b8; text-transform: uppercase; font-weight: 700; letter-spacing: 0.04em; }
  .day-number { display: inline-block; font-size: 1.2rem; font-weight: 800; color: #f8fafc; margin-top: 0.2rem; }

  &.is-today {
    .day-number {
      background: #6366f1;
      color: #fff;
      width: 26px;
      height: 26px;
      border-radius: 50%;
      line-height: 26px;
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
    background: rgba(245, 158, 11, 0.15);
    border-left-color: #f59e0b;
    .event-pill-time { color: #fbbf24; }
  }

  .event-pill-time {
    font-size: 0.725rem;
    color: #818cf8;
    font-weight: 700;
  }

  .event-pill-title {
    font-size: 0.8rem;
    font-weight: 700;
    color: #f8fafc;
    margin-top: 0.2rem;
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

/* ============================================================
   2. MONTH VIEW
   ============================================================ */
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

  .month-day-name {
    padding: 0.75rem;
    text-align: center;
    font-size: 0.75rem;
    font-weight: 700;
    color: #94a3b8;
    text-transform: uppercase;
    letter-spacing: 0.04em;
  }
}

.month-body-grid {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
}

.month-cell {
  min-height: 100px;
  border-right: 1px solid rgba(255, 255, 255, 0.06);
  border-bottom: 1px solid rgba(255, 255, 255, 0.06);
  padding: 0.4rem;
  display: flex;
  flex-direction: column;
  gap: 0.25rem;

  &:nth-child(7n) { border-right: none; }

  &.other-month {
    background: rgba(0, 0, 0, 0.2);
    opacity: 0.35;
  }

  &.is-today {
    background: rgba(99, 102, 241, 0.08);

    .day-num {
      background: #6366f1;
      color: #fff;
      border-radius: 50%;
      width: 22px;
      height: 22px;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      line-height: 22px;
    }
  }

  .month-cell-header {
    margin-bottom: 0.25rem;
    .day-num { font-size: 0.8rem; font-weight: 700; color: #cbd5e1; }
  }
}

.month-events-container {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.month-event-chip {
  background: rgba(99, 102, 241, 0.2);
  border: 1px solid rgba(99, 102, 241, 0.35);
  color: #c7d2fe;
  padding: 0.2rem 0.4rem;
  border-radius: 0.3rem;
  font-size: 0.7rem;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 0.3rem;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  transition: background 0.15s ease;

  &:hover { background: rgba(99, 102, 241, 0.35); }

  .chip-dot { width: 6px; height: 6px; border-radius: 50%; background: #818cf8; flex-shrink: 0; }
  .chip-time { font-weight: 700; flex-shrink: 0; }
  .chip-title { overflow: hidden; text-overflow: ellipsis; }

  &.private-chip {
    background: rgba(245, 158, 11, 0.15);
    border-color: rgba(245, 158, 11, 0.35);
    color: #fbbf24;
    .chip-dot { background: #fbbf24; }
  }
}

/* ============================================================
   3. AGENDA VIEW
   ============================================================ */
.agenda-list {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.agenda-card {
  display: flex;
  align-items: center;
  background: rgba(11, 17, 32, 0.75);
  backdrop-filter: blur(12px);
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 0.75rem;
  padding: 1rem 1.25rem;
  cursor: pointer;
  transition: transform 0.15s ease, border-color 0.15s ease;

  &:hover {
    border-color: rgba(99, 102, 241, 0.5);
    transform: translate3d(4px, 0, 0);
  }

  &.private-card {
    border-left: 3px solid #f59e0b;
  }
}

.agenda-date-box {
  display: flex;
  flex-direction: column;
  align-items: center;
  min-width: 70px;
  padding-right: 1.25rem;
  border-right: 1px solid rgba(255, 255, 255, 0.08);

  .month { font-size: 0.725rem; color: #94a3b8; font-weight: 700; text-transform: uppercase; }
  .day { font-size: 1.45rem; font-weight: 800; color: #f8fafc; line-height: 1; margin: 0.2rem 0; }
  .weekday { font-size: 0.725rem; color: #818cf8; font-weight: 600; }
}

.agenda-content {
  flex: 1;
  padding-left: 1.25rem;

  .agenda-time { font-size: 0.775rem; color: #94a3b8; margin-bottom: 0.2rem; display: flex; align-items: center; gap: 0.35rem; }
  .agenda-title { margin: 0; font-size: 0.975rem; font-weight: 700; color: #f8fafc; display: flex; align-items: center; gap: 0.35rem; }
  .agenda-location { font-size: 0.775rem; color: #64748b; margin-top: 0.2rem; display: flex; align-items: center; gap: 0.35rem; }
}

.agenda-badge {
  font-size: 0.725rem;
  padding: 0.25rem 0.65rem;
  border-radius: 9999px;
  font-weight: 700;

  &.public { background: rgba(16, 185, 129, 0.15); border: 1px solid rgba(16, 185, 129, 0.3); color: #34d399; }
  &.busy { background: rgba(245, 158, 11, 0.15); border: 1px solid rgba(245, 158, 11, 0.3); color: #fbbf24; }
}

.empty-state {
  text-align: center;
  padding: 4rem 1.5rem;
  color: #94a3b8;
  background: rgba(11, 17, 32, 0.75);
  backdrop-filter: blur(12px);
  border-radius: 1rem;
  border: 1px dashed rgba(255, 255, 255, 0.12);

  i { font-size: 2.75rem; color: #6366f1; margin-bottom: 1rem; }
}

/* ============================================================
   EVENT DETAIL MODAL
   ============================================================ */
.modal-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.75);
  backdrop-filter: blur(4px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  padding: 1rem;
}

.modal-card {
  background: #0b1120;
  border: 1px solid rgba(255, 255, 255, 0.12);
  border-radius: 1rem;
  padding: 1.75rem;
  width: 100%;
  max-width: 480px;
  box-shadow: 0 25px 60px -15px rgba(0, 0, 0, 0.8);
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
}

.event-type-badge {
  display: flex;
  align-items: center;
  gap: 0.35rem;
  font-size: 0.725rem;
  font-weight: 700;
  padding: 0.25rem 0.65rem;
  border-radius: 0.35rem;

  &.public { background: rgba(99, 102, 241, 0.15); border: 1px solid rgba(99, 102, 241, 0.3); color: #818cf8; }
  &.busy { background: rgba(245, 158, 11, 0.15); border: 1px solid rgba(245, 158, 11, 0.3); color: #fbbf24; }
}

.close-btn {
  background: transparent;
  border: none;
  color: #94a3b8;
  font-size: 1.1rem;
  cursor: pointer;
  padding: 0.25rem;
  &:hover { color: #fff; }
}

.modal-title {
  margin: 0 0 1.25rem 0;
  font-size: 1.25rem;
  color: #f8fafc;
  font-weight: 700;
}

.modal-info-list {
  display: flex;
  flex-direction: column;
  gap: 1.15rem;
}

.info-item {
  display: flex;
  gap: 0.85rem;
  font-size: 0.85rem;

  i { font-size: 1.1rem; color: #818cf8; margin-top: 0.15rem; }
  strong { color: #cbd5e1; display: block; margin-bottom: 0.15rem; }
  p { margin: 0; color: #94a3b8; }
}

/* ============================================================
   RESPONSIVENESS
   ============================================================ */
@media (max-width: 768px) {
  .top-header {
    flex-direction: column;
    align-items: flex-start;

    .live-status-pill {
      align-self: flex-start;
    }
  }

  .calendar-toolbar {
    flex-direction: column;
    align-items: stretch;

    .toolbar-left {
      justify-content: space-between;
    }

    .view-switcher {
      width: 100%;
      button {
        flex: 1;
        justify-content: center;
      }
    }
  }

  .agenda-card {
    flex-direction: column;
    align-items: flex-start;
    gap: 0.75rem;

    .agenda-date-box {
      flex-direction: row;
      border-right: none;
      border-bottom: 1px solid rgba(255, 255, 255, 0.08);
      width: 100%;
      padding-bottom: 0.5rem;
      gap: 0.75rem;
      align-items: center;
    }

    .agenda-content {
      padding-left: 0;
    }

    .agenda-badge {
      align-self: flex-end;
    }
  }
}
</style>
