<template>
  <div class="calendar-page">
    <header class="page-header">
      <div class="header-content">
        <h1>📅 Quản lý Lịch hẹn & Scheduling (UC03)</h1>
        <p>Đồng bộ 2 chiều với Google Calendar — Lưới tương tác & Trích xuất AI</p>
      </div>
      <div class="header-actions">
        <button class="primary-btn" @click="openCreateModal">
          <i class="pi pi-plus"></i> Tạo sự kiện mới
        </button>
      </div>
    </header>

    <!-- Navigation Tabs -->
    <div class="tabs">
      <button :class="{ active: activeTab === 'calendar' }" @click="activeTab = 'calendar'">
        🗓️ Lưới Lịch Google Calendar
      </button>
      <button :class="{ active: activeTab === 'extracted' }" @click="activeTab = 'extracted'">
        ✨ Trích xuất từ Email ({{ extractedSchedules.length }})
      </button>
    </div>

    <!-- TAB 1: CALENDAR VIEW (WEEK / MONTH / AGENDA) -->
    <div v-if="activeTab === 'calendar'" class="tab-content">
      <!-- Calendar Controls Bar -->
      <div class="calendar-toolbar">
        <div class="toolbar-left">
          <button class="btn-today" @click="goToday">Hôm nay</button>
          <div class="nav-buttons">
            <button class="nav-btn" @click="goPrev" title="Thời gian trước">
              <i class="pi pi-chevron-left"></i>
            </button>
            <button class="nav-btn" @click="goNext" title="Thời gian sau">
              <i class="pi pi-chevron-right"></i>
            </button>
          </div>
          <h2 class="current-period-title">{{ currentPeriodTitle }}</h2>
        </div>

        <div class="toolbar-right">
          <div class="calendar-select-box" v-if="calendars.length > 0">
            <i class="pi pi-calendar"></i>
            <select v-model="selectedCalendarId" @change="onCalendarChange" class="cal-select">
              <option v-for="c in calendars" :key="c.id" :value="c.id">
                {{ c.summary }} {{ c.primary ? '(Chính)' : '' }}
              </option>
            </select>
          </div>

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

      <LoadingSpinner v-if="loading.upcoming" text="Đang đồng bộ Google Calendar..." />

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
                :class="{ 'private-pill': event.visibility === 'private' }"
                @click="openEventDetail(event)"
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
                'not-current-month': !isSameMonth(day),
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
                  @click="openEventDetail(event)"
                >
                  <span class="pill-dot"></span>
                  <span class="pill-text">{{ event.title }}</span>
                </div>
                <div v-if="getEventsForDay(day).length > 3" class="more-badge" @click="openDayEvents(day)">
                  +{{ getEventsForDay(day).length - 3 }} sự kiện khác
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- 3. AGENDA VIEW -->
        <div v-else-if="viewMode === 'agenda'" class="agenda-view">
          <div v-if="sortedEvents.length === 0" class="empty-state">
            <i class="pi pi-calendar"></i>
            <p>Không có sự kiện nào trong khoảng thời gian này.</p>
          </div>
          <div v-else class="agenda-list">
            <div
              v-for="event in sortedEvents"
              :key="event.id"
              class="agenda-card"
              @click="openEventDetail(event)"
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
                <button class="btn-icon" @click.stop="openEditModal(event)" title="Chỉnh sửa"><i class="pi pi-pencil"></i></button>
                <button class="btn-icon text-red" @click.stop="handleDeleteEvent(event.id)" title="Xóa"><i class="pi pi-trash"></i></button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- TAB 2: EXTRACTED SCHEDULES (UC03) -->
    <div v-if="activeTab === 'extracted'" class="tab-content">
      <LoadingSpinner v-if="loading.extracted && extractedSchedules.length === 0" text="Đang tải lịch trích xuất..." />
      <div v-else-if="extractedSchedules.length === 0" class="empty-state">
        <i class="pi pi-calendar-plus"></i>
        <p>Không có lịch hẹn AI nào cần xác nhận!</p>
      </div>
      <div v-else class="schedule-list">
        <div v-for="item in extractedSchedules" :key="item.id" class="schedule-card">
          <div class="card-header">
            <span class="event-title">{{ item.title }}</span>
            <span class="status-badge" :class="item.status === 2 ? 'confirmed' : 'pending'">
              {{ item.status === 2 ? 'Đã tạo Calendar' : 'Chờ xác nhận' }}
            </span>
          </div>
          <div class="event-details">
            <div><i class="pi pi-clock"></i> Bắt đầu: {{ formatDateTime(item.startTime) }}</div>
            <div v-if="item.location"><i class="pi pi-map-marker"></i> {{ item.location }}</div>
            <div class="source"><i class="pi pi-envelope"></i> Nguồn: {{ item.sourceEmailSubject }}</div>
          </div>
          <div class="actions" v-if="item.status !== 2">
            <button class="confirm-btn" @click="handleConfirmExtracted(item.id)">
              <i class="pi pi-check"></i> Xác nhận & Đồng bộ Calendar
            </button>
          </div>
        </div>
      </div>
      <InfiniteScrollObserver :loading="loading.extracted" :has-more="hasMoreExtracted" @load-more="loadMoreExtracted" />
    </div>

    <!-- Event Detail Dialog -->
    <div v-if="selectedEvent && !showEditModal" class="modal-overlay" @click.self="selectedEvent = null">
      <div class="modal-content event-detail-modal">
        <div class="modal-header">
          <div class="title-with-color">
            <span class="color-badge" :style="{ background: getEventColor(selectedEvent.colorId) }"></span>
            <h3>{{ selectedEvent.title }}</h3>
          </div>
          <button class="close-btn" @click="selectedEvent = null"><i class="pi pi-times"></i></button>
        </div>

        <div class="detail-body">
          <!-- Google Meet Banner -->
          <div v-if="selectedEvent.meetUrl" class="meet-banner">
            <div class="meet-info">
              <i class="pi pi-video text-green"></i>
              <div>
                <strong>Google Meet:</strong>
                <p class="meet-url-text">{{ selectedEvent.meetUrl }}</p>
              </div>
            </div>
            <a :href="selectedEvent.meetUrl" target="_blank" class="btn-join-meet">
              <i class="pi pi-video"></i> Tham gia Meet
            </a>
          </div>

          <div class="info-row">
            <i class="pi pi-clock"></i>
            <div>
              <strong>Thời gian:</strong>
              <p v-if="selectedEvent.isAllDay">Cả ngày ({{ formatDateTime(selectedEvent.start).split(' ')[0] }})</p>
              <p v-else>{{ formatDateTime(selectedEvent.start) }} - {{ selectedEvent.end ? formatDateTime(selectedEvent.end) : 'Không xác định' }}</p>
            </div>
          </div>

          <div v-if="selectedEvent.attendees && selectedEvent.attendees.length > 0" class="info-row">
            <i class="pi pi-users"></i>
            <div>
              <strong>Người tham gia ({{ selectedEvent.attendees.length }}):</strong>
              <div class="attendees-list">
                <span v-for="att in selectedEvent.attendees" :key="att" class="attendee-chip">{{ att }}</span>
              </div>
            </div>
          </div>

          <div v-if="selectedEvent.location" class="info-row">
            <i class="pi pi-map-marker"></i>
            <div>
              <strong>Địa điểm:</strong>
              <p>{{ selectedEvent.location }}</p>
            </div>
          </div>

          <div v-if="selectedEvent.description" class="info-row">
            <i class="pi pi-align-left"></i>
            <div>
              <strong>Mô tả:</strong>
              <p>{{ selectedEvent.description }}</p>
            </div>
          </div>

          <div class="info-row">
            <i class="pi pi-shield"></i>
            <div>
              <strong>Chế độ hiển thị:</strong>
              <p>{{ selectedEvent.visibility === 'public' ? '🌐 Công khai (Hiển thị chi tiết trên Public Calendar)' : '🔒 Riêng tư (Chỉ hiển thị Bận/Rảnh cho khách)' }}</p>
            </div>
          </div>
        </div>

        <div class="modal-actions">
          <a v-if="selectedEvent.htmlLink" :href="selectedEvent.htmlLink" target="_blank" class="btn-ext-link">
            <i class="pi pi-external-link"></i> Mở Google Calendar
          </a>
          <button class="btn-edit" @click="openEditModal(selectedEvent)">
            <i class="pi pi-pencil"></i> Chỉnh sửa
          </button>
          <button class="btn-danger" @click="handleDeleteEvent(selectedEvent.id)">
            <i class="pi pi-trash"></i> Xóa sự kiện
          </button>
        </div>
      </div>
    </div>

    <!-- Edit Event Modal -->
    <div v-if="showEditModal" class="modal-overlay" @click.self="showEditModal = false">
      <div class="modal-content event-edit-modal">
        <h3>✏️ Chỉnh sửa sự kiện Google Calendar</h3>
        <form @submit.prevent="handleUpdateEvent">
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
                v-for="c in calendarColors" 
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
            <button type="button" class="btn-cancel" @click="showEditModal = false">Hủy</button>
            <button type="submit" class="btn-submit" :disabled="savingEdit">
              {{ savingEdit ? 'Đang lưu...' : 'Lưu Thay Đổi' }}
            </button>
          </div>
        </form>
      </div>
    </div>

    <!-- Create Manual Event Modal -->
    <div v-if="showModal" class="modal-overlay" @click.self="closeModal">
      <div class="modal-content event-edit-modal">
        <h3>Tạo sự kiện mới</h3>
        <form @submit.prevent="handleCreateManual">
          <div class="form-group" v-if="calendars.length > 0">
            <label>Chọn Lịch Google</label>
            <select v-model="newEvent.calendarId" class="form-select">
              <option v-for="c in calendars" :key="c.id" :value="c.id">
                {{ c.summary }} {{ c.primary ? '(Chính)' : '' }}
              </option>
            </select>
          </div>

          <div class="form-group">
            <label>Tiêu đề <span class="required">*</span></label>
            <input v-model="newEvent.title" required placeholder="Họp team..." autofocus />
          </div>
          <div class="form-row">
            <div class="form-group half">
              <label>Bắt đầu <span class="required">*</span></label>
              <input type="datetime-local" v-model="newEvent.start" required />
            </div>
            <div class="form-group half">
              <label>Kết thúc</label>
              <input type="datetime-local" v-model="newEvent.end" />
            </div>
          </div>

          <div class="form-group-checkbox">
            <label>
              <input type="checkbox" v-model="newEvent.isAllDay" />
              Sự kiện cả ngày (All-day event)
            </label>
          </div>

          <div class="form-group-checkbox">
            <label>
              <input type="checkbox" v-model="newEvent.createMeetLink" />
              📹 1-Click Tạo link họp Google Meet tự động
            </label>
          </div>

          <div class="form-group">
            <label>👥 Khách mời / Người tham gia (Emails cách nhau bởi dấu phẩy)</label>
            <input v-model="newEvent.attendees" placeholder="nguyenvana@gmail.com, colleague@company.com..." />
          </div>

          <div class="form-group">
            <label>🎨 Màu sự kiện</label>
            <div class="color-palette">
              <span 
                v-for="c in calendarColors" 
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
            <label>⏰ Nhắc nhở trước</label>
            <select v-model="newEvent.reminderMinutes" class="form-select">
              <option :value="null">Mặc định Google Calendar</option>
              <option :value="10">10 phút trước</option>
              <option :value="30">30 phút trước</option>
              <option :value="60">1 tiếng trước</option>
              <option :value="1440">1 ngày trước</option>
            </select>
          </div>

          <div class="form-group">
            <label>Địa điểm</label>
            <input v-model="newEvent.location" placeholder="Phòng họp A / Google Meet..." />
          </div>
          <div class="form-group">
            <label>Mô tả</label>
            <textarea v-model="newEvent.description" rows="3" placeholder="Ghi chú thêm..."></textarea>
          </div>
          <div class="form-group-checkbox">
            <label>
              <input type="checkbox" v-model="newEvent.createTask" />
              Đồng thời tạo nhắc nhở trong Google Tasks
            </label>
          </div>
          <div class="form-group-checkbox">
            <label>
              <input type="checkbox" v-model="newEvent.isPublic" />
              Công khai sự kiện (Hiển thị chi tiết trên Public Calendar)
            </label>
          </div>
          <div class="modal-actions">
            <button type="button" class="btn-cancel" @click="closeModal">Hủy</button>
            <button type="submit" class="btn-submit" :disabled="creating">
              {{ creating ? 'Đang tạo...' : 'Tạo sự kiện' }}
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed, watch, defineAsyncComponent } from 'vue';
import api from '@/services/api.service';
import LoadingSpinner from '@/components/common/LoadingSpinner.vue';
import { showToast } from '@/services/notification.service';

const InfiniteScrollObserver = defineAsyncComponent(() => import('@/components/common/InfiniteScrollObserver.vue'));

const activeTab = ref<'calendar' | 'extracted'>('calendar');
const viewMode = ref<'week' | 'month' | 'agenda'>('week');
const currentDate = ref(new Date());

const calendars = ref<any[]>([]);
const selectedCalendarId = ref('primary');

const calendarColors = [
  { id: '', name: 'Mặc định', color: '#6366f1' },
  { id: '1', name: 'Lavender', color: '#7986cb' },
  { id: '2', name: 'Sage', color: '#33b679' },
  { id: '3', name: 'Grape', color: '#8e24aa' },
  { id: '4', name: 'Flamingo', color: '#e67c73' },
  { id: '5', name: 'Banana', color: '#f6bf26' },
  { id: '6', name: 'Tangerine', color: '#f4511e' },
  { id: '7', name: 'Peacock', color: '#039be5' },
  { id: '8', name: 'Graphite', color: '#616161' },
  { id: '9', name: 'Blueberry', color: '#3f51b5' },
  { id: '10', name: 'Basil', color: '#0b8043' },
  { id: '11', name: 'Tomato', color: '#d50000' },
];

const getEventColor = (colorId?: string) => {
  if (!colorId) return '#6366f1';
  const found = calendarColors.find(c => c.id === colorId);
  return found ? found.color : '#6366f1';
};

const fetchCalendars = async () => {
  try {
    const res: any = await api.get('/scheduling/calendars');
    if (res.success && res.data) {
      calendars.value = res.data;
      const primary = res.data.find((c: any) => c.primary);
      if (primary) selectedCalendarId.value = primary.id;
    }
  } catch (e) {
    console.error('Failed to fetch calendars', e);
  }
};

const onCalendarChange = () => {
  fetchCalendarEvents();
};

const upcomingEvents = ref<any[]>([]);
const extractedSchedules = ref<any[]>([]);
const selectedEvent = ref<any | null>(null);

const loading = ref({
  upcoming: false,
  extracted: false,
});

const dayNamesShort = ['T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'CN'];

// Date Navigation
const goToday = () => { currentDate.value = new Date(); };

const goPrev = () => {
  const d = new Date(currentDate.value);
  if (viewMode.value === 'month') {
    d.setMonth(d.getMonth() - 1);
  } else if (viewMode.value === 'week') {
    d.setDate(d.getDate() - 7);
  } else {
    d.setDate(d.getDate() - 14);
  }
  currentDate.value = d;
};

const goNext = () => {
  const d = new Date(currentDate.value);
  if (viewMode.value === 'month') {
    d.setMonth(d.getMonth() + 1);
  } else if (viewMode.value === 'week') {
    d.setDate(d.getDate() + 7);
  } else {
    d.setDate(d.getDate() + 14);
  }
  currentDate.value = d;
};

// Math & Grid Helpers
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
  return [...upcomingEvents.value].sort((a, b) => new Date(a.start).getTime() - new Date(b.start).getTime());
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
  return upcomingEvents.value.filter(e => isSameDay(new Date(e.start), day));
};

const fetchCalendarEvents = async () => {
  loading.value.upcoming = true;
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
      start.setDate(d.getDate() - 14);
      const end = new Date(d);
      end.setDate(d.getDate() + 45);
      startDate = start.toISOString();
      endDate = end.toISOString();
    }

    const res: any = await api.get(`/scheduling/upcoming?startDate=${encodeURIComponent(startDate)}&endDate=${encodeURIComponent(endDate)}&calendarId=${encodeURIComponent(selectedCalendarId.value)}`);
    if (res.success && res.data) {
      upcomingEvents.value = res.data;
    }
  } catch (e) {
    console.error('Failed to load Google Calendar events:', e);
  } finally {
    loading.value.upcoming = false;
  }
};

watch([currentDate, viewMode], () => {
  if (activeTab.value === 'calendar') {
    fetchCalendarEvents();
  }
});

// Event detail & Edit
const openEventDetail = (event: any) => {
  selectedEvent.value = event;
};

const openDayEvents = (day: Date) => {
  currentDate.value = new Date(day);
  viewMode.value = 'week';
};

const showEditModal = ref(false);
const savingEdit = ref(false);
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

const toLocalIsoString = (d: Date) => {
  const pad = (n: number) => (n < 10 ? '0' + n : n);
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
};

const openEditModal = (event: any) => {
  const startDate = new Date(event.start);
  const endDate = event.end ? new Date(event.end) : new Date(startDate.getTime() + 3600000);

  editForm.value = {
    id: event.id,
    calendarId: selectedCalendarId.value,
    title: event.title,
    start: toLocalIsoString(startDate),
    end: toLocalIsoString(endDate),
    location: event.location || '',
    description: event.description || '',
    createMeetLink: !!event.meetUrl,
    attendees: event.attendees ? event.attendees.join(', ') : '',
    colorId: event.colorId || '',
    isAllDay: !!event.isAllDay,
    reminderMinutes: event.reminderMinutes || null,
    isPublic: event.visibility === 'public',
  };
  showEditModal.value = true;
};

const handleUpdateEvent = async () => {
  if (!editForm.value.title || !editForm.value.start) return;

  savingEdit.value = true;
  try {
    const attendeesList = editForm.value.attendees
      ? editForm.value.attendees.split(',').map((e: string) => e.trim()).filter((e: string) => e.length > 0)
      : [];

    const res: any = await api.put(`/scheduling/events/${editForm.value.id}`, {
      CalendarId: selectedCalendarId.value,
      Title: editForm.value.title,
      Start: new Date(editForm.value.start).toISOString(),
      End: editForm.value.end ? new Date(editForm.value.end).toISOString() : null,
      Location: editForm.value.location,
      Description: editForm.value.description,
      CreateMeetLink: editForm.value.createMeetLink,
      Attendees: attendeesList,
      ColorId: editForm.value.colorId || null,
      IsAllDay: editForm.value.isAllDay,
      ReminderMinutes: editForm.value.reminderMinutes,
      IsPublic: editForm.value.isPublic,
    });

    if (res.success) {
      showToast({
        severity: 'success',
        summary: 'Cập nhật thành công',
        detail: 'Sự kiện Google Calendar đã được chỉnh sửa.',
      });
      showEditModal.value = false;
      selectedEvent.value = null;
      await fetchCalendarEvents();
    } else {
      showToast({
        severity: 'error',
        summary: 'Lỗi',
        detail: res.message || 'Không thể cập nhật sự kiện.',
      });
    }
  } catch (err: any) {
    showToast({
      severity: 'error',
      summary: 'Lỗi hệ thống',
      detail: err.message || 'Không thể lưu thay đổi sự kiện.',
    });
  } finally {
    savingEdit.value = false;
  }
};

const handleDeleteEvent = async (id: string) => {
  if (!confirm('Bạn có chắc chắn muốn xóa sự kiện này khỏi Google Calendar?')) return;

  try {
    const res: any = await api.delete(`/scheduling/events/${id}?calendarId=${encodeURIComponent(selectedCalendarId.value)}`);
    if (res.success) {
      showToast({
        severity: 'info',
        summary: 'Đã xóa',
        detail: 'Sự kiện đã được xóa khỏi Google Calendar.',
      });
      selectedEvent.value = null;
      upcomingEvents.value = upcomingEvents.value.filter(e => e.id !== id);
    }
  } catch (err: any) {
    showToast({
      severity: 'error',
      summary: 'Lỗi xóa',
      detail: err.message || 'Không thể xóa sự kiện.',
    });
  }
};

// Create Event
const showModal = ref(false);
const creating = ref(false);
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

const openCreateModal = () => {
  const now = new Date();
  now.setMinutes(0);
  now.setSeconds(0);
  now.setHours(now.getHours() + 1);

  const end = new Date(now);
  end.setHours(now.getHours() + 1);

  newEvent.value = {
    calendarId: selectedCalendarId.value || 'primary',
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
  showModal.value = true;
};

const closeModal = () => {
  showModal.value = false;
};

const handleCreateManual = async () => {
  creating.value = true;
  try {
    const attendeesList = newEvent.value.attendees
      ? newEvent.value.attendees.split(',').map((e: string) => e.trim()).filter((e: string) => e.length > 0)
      : [];

    const res: any = await api.post('/scheduling/manual', {
      CalendarId: newEvent.value.calendarId || selectedCalendarId.value,
      Title: newEvent.value.title,
      Start: new Date(newEvent.value.start).toISOString(),
      End: newEvent.value.end ? new Date(newEvent.value.end).toISOString() : null,
      Location: newEvent.value.location,
      Description: newEvent.value.description,
      CreateMeetLink: newEvent.value.createMeetLink,
      Attendees: attendeesList,
      ColorId: newEvent.value.colorId || null,
      IsAllDay: newEvent.value.isAllDay,
      ReminderMinutes: newEvent.value.reminderMinutes,
      CreateTask: newEvent.value.createTask,
      IsPublic: newEvent.value.isPublic,
    });

    if (res.success) {
      showToast({
        severity: 'success',
        summary: 'Tạo sự kiện thành công',
        detail: 'Sự kiện đã được đồng bộ lên Google Calendar.',
      });
      closeModal();
      await fetchCalendarEvents();
    } else {
      showToast({
        severity: 'error',
        summary: 'Lỗi',
        detail: res.message || 'Không thể tạo sự kiện.',
      });
    }
  } catch (e: any) {
    showToast({
      severity: 'error',
      summary: 'Lỗi',
      detail: e.message || 'Lỗi khi gửi yêu cầu tạo sự kiện.',
    });
  } finally {
    creating.value = false;
  }
};

// Extracted schedules logic
const extractedPage = ref(1);
const hasMoreExtracted = ref(true);

const fetchExtracted = async (page = 1) => {
  loading.value.extracted = true;
  try {
    const res: any = await api.get(`/scheduling?page=${page}&pageSize=10`);
    if (res.success && res.data) {
      if (page === 1) {
        extractedSchedules.value = res.data.items;
      } else {
        extractedSchedules.value = [...extractedSchedules.value, ...res.data.items];
      }
      hasMoreExtracted.value = page < res.data.totalPages;
      extractedPage.value = page;
    }
  } catch (e) {
    console.error('Failed to load extracted schedules:', e);
  } finally {
    loading.value.extracted = false;
  }
};

const loadMoreExtracted = () => {
  if (!loading.value.extracted && hasMoreExtracted.value) {
    fetchExtracted(extractedPage.value + 1);
  }
};

const handleConfirmExtracted = async (id: string) => {
  try {
    const res: any = await api.post(`/scheduling/${id}/confirm`);
    if (res.success) {
      showToast({
        severity: 'success',
        summary: 'Đã xác nhận',
        detail: 'Đã tạo sự kiện trên Google Calendar thành công!',
      });
      const item = extractedSchedules.value.find(s => s.id === id);
      if (item) item.status = 2;
      fetchCalendarEvents();
    }
  } catch (e) {
    showToast({
      severity: 'error',
      summary: 'Lỗi',
      detail: 'Không thể xác nhận sự kiện.',
    });
  }
};

// Formatting helpers
const formatDayName = (date: Date) => {
  const days = ['Chủ Nhật', 'Thứ 2', 'Thứ 3', 'Thứ 4', 'Thứ 5', 'Thứ 6', 'Thứ 7'];
  return days[date.getDay()];
};

const formatTime = (dateStr: string) => {
  if (!dateStr) return '';
  return new Date(dateStr).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit', hour12: false });
};

const formatDateShort = (dateStr: string) => {
  if (!dateStr) return '';
  return new Date(dateStr).toLocaleDateString('vi-VN', { weekday: 'short', day: '2-digit', month: '2-digit' });
};

const formatDateTime = (dateStr: string) => {
  if (!dateStr) return '';
  return new Date(dateStr).toLocaleString('vi-VN', {
    day: '2-digit', month: '2-digit', year: 'numeric',
    hour: '2-digit', minute: '2-digit'
  });
};

onMounted(async () => {
  await fetchCalendars();
  fetchCalendarEvents();
  fetchExtracted(1);
});
</script>

<style scoped lang="scss">
.calendar-page {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 1rem;

  h1 {
    font-size: 1.75rem;
    font-weight: 800;
    color: #f8fafc;
    margin: 0;
  }

  p {
    font-size: 0.875rem;
    color: #94a3b8;
    margin: 0.25rem 0 0;
  }
}

.primary-btn {
  background: linear-gradient(135deg, #6366f1, #8b5cf6);
  color: #fff;
  border: none;
  border-radius: 0.5rem;
  padding: 0.65rem 1.25rem;
  font-weight: 600;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 0.5rem;
  box-shadow: 0 4px 12px rgba(99, 102, 241, 0.3);
  &:hover { filter: brightness(1.1); }
}

.tabs {
  display: flex;
  gap: 0.75rem;
  border-bottom: 1px solid rgba(255, 255, 255, 0.1);
  padding-bottom: 0.5rem;

  button {
    background: none;
    border: none;
    color: #94a3b8;
    font-weight: 600;
    font-size: 0.95rem;
    padding: 0.5rem 1rem;
    cursor: pointer;
    border-bottom: 2px solid transparent;

    &.active {
      color: #818cf8;
      border-bottom-color: #818cf8;
    }
  }
}

/* Toolbar */
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

/* 1. Week Grid */
.week-grid {
  background: #1e293b;
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 1rem;
  overflow: hidden;
}

.week-header-row {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  border-bottom: 1px solid rgba(255, 255, 255, 0.1);
  background: rgba(15, 23, 42, 0.4);
}

.day-header-cell {
  padding: 1rem 0.5rem;
  text-align: center;
  border-right: 1px solid rgba(255, 255, 255, 0.08);

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
  border-right: 1px solid rgba(255, 255, 255, 0.08);
  display: flex;
  flex-direction: column;
  gap: 0.5rem;

  &:last-child { border-right: none; }
  &.is-today-col { background: rgba(99, 102, 241, 0.03); }
}

.event-pill {
  background: rgba(99, 102, 241, 0.18);
  border-left: 3px solid #6366f1;
  border-radius: 0.4rem;
  padding: 0.5rem;
  cursor: pointer;
  transition: transform 0.15s;

  &:hover {
    transform: translateY(-2px);
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
  background: #1e293b;
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 1rem;
  overflow: hidden;
}

.month-header-row {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  background: rgba(15, 23, 42, 0.4);
  border-bottom: 1px solid rgba(255, 255, 255, 0.1);

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
    background: rgba(0, 0, 0, 0.15);
  }

  &.is-today-cell {
    background: rgba(99, 102, 241, 0.05);
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

/* 3. Agenda View */
.agenda-view {
  .agenda-list {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
  }

  .agenda-card {
    background: #1e293b;
    border: 1px solid rgba(255, 255, 255, 0.1);
    border-radius: 0.75rem;
    padding: 1rem 1.25rem;
    display: flex;
    align-items: center;
    gap: 1.5rem;
    cursor: pointer;
    transition: all 0.2s;

    &:hover {
      background: rgba(255, 255, 255, 0.05);
      border-color: #6366f1;
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

/* Modals & Dialogs */
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
  background: #1e293b;
  border: 1px solid rgba(255, 255, 255, 0.12);
  border-radius: 1rem;
  width: 100%;
  max-width: 580px;
  max-height: 90vh;
  overflow-y: auto;
  box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.5);
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
  .btn-cancel { background: rgba(255, 255, 255, 0.08); color: #cbd5e1; &:hover { background: rgba(255, 255, 255, 0.14); } }
  .btn-submit { background: #10b981; color: #fff; &:hover { background: #059669; } }
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
  margin-bottom: 1rem;

  label { font-size: 0.85rem; font-weight: 600; color: #cbd5e1; .required { color: #f87171; } }

  input, textarea {
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

/* Schedule List (Tab 2) */
.schedule-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.schedule-card {
  background: #1e293b;
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 0.75rem;
  padding: 1.25rem;

  .card-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 0.75rem;
    .event-title { font-size: 1.1rem; font-weight: 700; color: #f8fafc; }
  }

  .status-badge {
    font-size: 0.75rem;
    font-weight: 600;
    padding: 0.25rem 0.5rem;
    border-radius: 0.25rem;
    &.confirmed { background: rgba(16, 185, 129, 0.15); color: #34d399; }
    &.pending { background: rgba(245, 158, 11, 0.15); color: #fbbf24; }
  }

  .event-details {
    display: flex;
    flex-direction: column;
    gap: 0.35rem;
    font-size: 0.85rem;
    color: #94a3b8;
    i { color: #818cf8; margin-right: 0.35rem; }
  }

  .actions {
    margin-top: 1rem;
    .confirm-btn {
      background: #10b981;
      color: #fff;
      border: none;
      padding: 0.5rem 1rem;
      border-radius: 0.4rem;
      font-size: 0.85rem;
      font-weight: 600;
      cursor: pointer;
      display: flex;
      align-items: center;
      gap: 0.4rem;
      &:hover { background: #059669; }
    }
  }
}

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

/* New Calendar & Meet Styles */
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
</style>
