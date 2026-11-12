<template>
  <div class="calendar-page">
    <header class="page-header">
      <div class="title-group">
        <div class="title-icon-badge">
          <i class="pi pi-calendar"></i>
        </div>
        <div class="title-text">
          <div class="title-row">
            <h1>Lịch Hẹn & Google Calendar Cockpit</h1>
            <span class="version-tag">UC03 Scheduling</span>
          </div>
          <p class="subtitle">Đồng bộ 2 chiều với Google Calendar — Lưới tương tác & Trích xuất AI</p>
        </div>
      </div>
      <div class="header-actions">
        <button class="primary-btn" @click="openCreateModal">
          <i class="pi pi-plus"></i> Tạo sự kiện mới
        </button>
      </div>
    </header>

    <!-- Navigation Tabs (Cyber Glass & Scrollable on mobile) -->
    <div class="tabs-nav-wrapper">
      <div class="tabs-nav">
        <button :class="{ active: activeTab === 'calendar' }" @click="activeTab = 'calendar'">
          <i class="pi pi-calendar"></i>
          <span>Lưới Lịch Google Calendar</span>
        </button>
        <button :class="{ active: activeTab === 'extracted' }" @click="activeTab = 'extracted'">
          <i class="pi pi-sparkles"></i>
          <span>Trích xuất từ Email</span>
          <span v-if="extractedSchedules.length > 0" class="tab-badge-violet">{{ extractedSchedules.length }}</span>
        </button>
      </div>
    </div>

    <!-- TAB 1: CALENDAR VIEW (WEEK / MONTH / AGENDA) -->
    <div v-if="activeTab === 'calendar'" class="tab-content">
      <CalendarToolbar
        :current-period-title="currentPeriodTitle"
        :calendars="calendars"
        :selected-calendar-id="selectedCalendarId"
        :view-mode="viewMode"
        @today="goToday"
        @prev="goPrev"
        @next="goNext"
        @update:selected-calendar-id="onCalendarSelect"
        @update:view-mode="viewMode = $event"
      />

      <LoadingSpinner v-if="loading.upcoming" text="Đang đồng bộ Google Calendar..." />

      <GoogleReconnectNotice v-else-if="googleError" :message="googleError" />

      <div v-else class="calendar-viewport">
        <!-- Week and Month Grid View -->
        <CalendarGrid
          v-if="viewMode === 'week' || viewMode === 'month'"
          :view-mode="viewMode"
          :current-date="currentDate"
          :events="upcomingEvents"
          @select-event="openEventDetail"
          @select-day="openDayEvents"
        />

        <!-- Agenda View -->
        <CalendarAgendaView
          v-else-if="viewMode === 'agenda'"
          :events="upcomingEvents"
          @select-event="openEventDetail"
          @edit-event="openEditModal"
          @delete-event="handleDeleteEvent"
        />
      </div>
    </div>

    <!-- TAB 2: EXTRACTED SCHEDULES (UC03) -->
    <CalendarExtractedTab
      v-else-if="activeTab === 'extracted'"
      :schedules="extractedSchedules"
      :loading="loading.extracted"
      :has-more="hasMoreExtracted"
      :action-in-progress-id="actionInProgressId"
      @confirm="handleConfirmExtracted"
      @delete="handleDeleteExtracted"
      @load-more="loadMoreExtracted"
    />

    <!-- Event Detail Dialog -->
    <EventDetailModal
      :visible="!!selectedEvent && !showEditModal"
      :event="selectedEvent"
      @close="selectedEvent = null"
      @edit="openEditModal"
      @delete="handleDeleteEvent"
    />

    <!-- Edit Event Modal -->
    <EventEditModal
      :visible="showEditModal"
      :event="selectedEvent"
      :selected-calendar-id="selectedCalendarId"
      :saving="savingEdit"
      @close="showEditModal = false"
      @save="handleUpdateEvent"
    />

    <!-- Create Manual Event Modal -->
    <EventCreateModal
      :visible="showModal"
      :calendars="calendars"
      :selected-calendar-id="selectedCalendarId"
      :creating="creating"
      @close="closeModal"
      @create="handleCreateManual"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed, watch } from 'vue';
import api from '@/services/api.service';
import LoadingSpinner from '@/components/common/LoadingSpinner.vue';
import GoogleReconnectNotice from '@/components/common/GoogleReconnectNotice.vue';
import { isGoogleDisconnectedError, googleDisconnectedMessage } from '@/utils/googleConnection';
import { showToast } from '@/services/notification.service';
import CalendarToolbar from '@/components/calendar/CalendarToolbar.vue';
import CalendarGrid from '@/components/calendar/CalendarGrid.vue';
import CalendarAgendaView from '@/components/calendar/CalendarAgendaView.vue';
import CalendarExtractedTab from '@/components/calendar/CalendarExtractedTab.vue';
import EventDetailModal from '@/components/calendar/EventDetailModal.vue';
import EventEditModal from '@/components/calendar/EventEditModal.vue';
import EventCreateModal from '@/components/calendar/EventCreateModal.vue';
import { getWeekDays, getMonthGridDays } from '@/components/calendar/calendarConstants';

const googleError = ref<string | null>(null);

const activeTab = ref<'calendar' | 'extracted'>('calendar');
const viewMode = ref<'week' | 'month' | 'agenda'>('week');
const currentDate = ref(new Date());

const calendars = ref<any[]>([]);
const selectedCalendarId = ref('primary');

const upcomingEvents = ref<any[]>([]);
const extractedSchedules = ref<any[]>([]);
const selectedEvent = ref<any | null>(null);

const loading = ref({
  upcoming: false,
  extracted: false,
});

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

const weekDays = computed(() => getWeekDays(currentDate.value));
const monthGridDays = computed(() => getMonthGridDays(currentDate.value));

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

const fetchCalendars = async () => {
  try {
    const res: any = await api.get('/scheduling/calendars');
    if (res.success && res.data) {
      calendars.value = res.data;
      const primary = res.data.find((c: any) => c.primary);
      if (primary) selectedCalendarId.value = primary.id;
      googleError.value = null;
    }
  } catch (e: any) {
    console.error('Failed to fetch calendars', e);
    if (isGoogleDisconnectedError(e)) {
      googleError.value = googleDisconnectedMessage(e);
    }
  }
};

const onCalendarSelect = (id: string) => {
  selectedCalendarId.value = id;
  fetchCalendarEvents();
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
      googleError.value = null;
    }
  } catch (e: any) {
    console.error('Failed to load Google Calendar events:', e);
    if (isGoogleDisconnectedError(e)) {
      googleError.value = googleDisconnectedMessage(e);
    }
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

const openEditModal = (event: any) => {
  selectedEvent.value = event;
  showEditModal.value = true;
};

const handleUpdateEvent = async (payload: {
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
}) => {
  savingEdit.value = true;
  try {
    const attendeesList = payload.attendees
      ? payload.attendees.split(',').map((e: string) => e.trim()).filter((e: string) => e.length > 0)
      : [];

    const res: any = await api.put(`/scheduling/events/${payload.id}`, {
      CalendarId: selectedCalendarId.value,
      Title: payload.title,
      Start: new Date(payload.start).toISOString(),
      End: payload.end ? new Date(payload.end).toISOString() : null,
      Location: payload.location,
      Description: payload.description,
      CreateMeetLink: payload.createMeetLink,
      Attendees: attendeesList,
      ColorId: payload.colorId || null,
      IsAllDay: payload.isAllDay,
      ReminderMinutes: payload.reminderMinutes,
      IsPublic: payload.isPublic,
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
      summary: 'Lỗi cập nhật',
      detail: err.response?.data?.message || err.message || 'Không thể lưu thay đổi sự kiện.',
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
      detail: err.response?.data?.message || err.message || 'Không thể xóa sự kiện.',
    });
  }
};

// Create Event
const showModal = ref(false);
const creating = ref(false);

const openCreateModal = () => {
  showModal.value = true;
};

const closeModal = () => {
  showModal.value = false;
};

const handleCreateManual = async (payload: {
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
}) => {
  creating.value = true;
  try {
    const attendeesList = payload.attendees
      ? payload.attendees.split(',').map((e: string) => e.trim()).filter((e: string) => e.length > 0)
      : [];

    const res: any = await api.post('/scheduling/manual', {
      CalendarId: payload.calendarId || selectedCalendarId.value,
      Title: payload.title,
      Start: new Date(payload.start).toISOString(),
      End: payload.end ? new Date(payload.end).toISOString() : null,
      Location: payload.location,
      Description: payload.description,
      CreateMeetLink: payload.createMeetLink,
      Attendees: attendeesList,
      ColorId: payload.colorId || null,
      IsAllDay: payload.isAllDay,
      ReminderMinutes: payload.reminderMinutes,
      CreateTask: payload.createTask,
      IsPublic: payload.isPublic,
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

const actionInProgressId = ref<string | null>(null);

const handleConfirmExtracted = async (id: string) => {
  actionInProgressId.value = id;
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
  } finally {
    actionInProgressId.value = null;
  }
};

const handleDeleteExtracted = async (id: string, isConfirmed: boolean) => {
  const confirmMsg = isConfirmed
    ? 'Bạn có chắc chắn muốn xóa bản ghi trích xuất này khỏi cơ sở dữ liệu để làm nhẹ DB? (Sự kiện đã đồng bộ lên Google Calendar vẫn được giữ nguyên).'
    : 'Bạn có chắc chắn muốn xóa lịch hẹn này khỏi hệ thống? (Lịch hẹn sẽ bị hủy bỏ và không đưa lên Google Calendar).';

  if (!confirm(confirmMsg)) return;

  actionInProgressId.value = id;
  try {
    const res: any = await api.delete(`/scheduling/${id}`);
    if (res.success) {
      showToast({
        severity: 'info',
        summary: 'Đã xóa',
        detail: 'Đã xóa lịch trích xuất thành công.',
      });
      extractedSchedules.value = extractedSchedules.value.filter(s => s.id !== id);
    } else {
      showToast({
        severity: 'error',
        summary: 'Lỗi xóa',
        detail: res.message || 'Không thể xóa lịch trích xuất.',
      });
    }
  } catch (e: any) {
    showToast({
      severity: 'error',
      summary: 'Lỗi hệ thống',
      detail: e.message || 'Không thể xóa lịch trích xuất.',
    });
  } finally {
    actionInProgressId.value = null;
  }
};

onMounted(async () => {
  await fetchCalendars();
  if (!googleError.value) {
    fetchCalendarEvents();
  } else {
    loading.value.upcoming = false;
  }
  fetchExtracted(1);
});
</script>

<style scoped lang="scss">
.calendar-page {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

/* ============================================================
   CYBER-COCKPIT HEADER
   ============================================================ */
.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 1rem;
  padding: 0.25rem 0.1rem;

  .title-group {
    display: flex;
    align-items: center;
    gap: 0.85rem;

    .title-icon-badge {
      width: 42px;
      height: 42px;
      border-radius: 0.75rem;
      background: linear-gradient(135deg, rgba(56, 189, 248, 0.25), rgba(99, 102, 241, 0.2));
      border: 1px solid rgba(56, 189, 248, 0.35);
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 1.25rem;
      color: #38bdf8;
      box-shadow: 0 0 16px rgba(56, 189, 248, 0.2);
    }

    .title-text {
      display: flex;
      flex-direction: column;
      gap: 0.2rem;

      .title-row {
        display: flex;
        align-items: center;
        gap: 0.65rem;
        flex-wrap: wrap;

        h1 {
          font-size: 1.45rem;
          font-weight: 800;
          letter-spacing: -0.02em;
          margin: 0;
          background: linear-gradient(135deg, #ffffff 0%, #cbd5e1 100%);
          -webkit-background-clip: text;
          -webkit-text-fill-color: transparent;
        }

        .version-tag {
          font-size: 0.675rem;
          font-family: var(--font-mono, monospace);
          font-weight: 700;
          padding: 0.15rem 0.5rem;
          border-radius: 9999px;
          background: rgba(56, 189, 248, 0.12);
          border: 1px solid rgba(56, 189, 248, 0.3);
          color: #38bdf8;
          letter-spacing: 0.04em;
        }
      }

      .subtitle {
        color: #94a3b8;
        font-size: 0.825rem;
        margin: 0;
      }
    }
  }

  .header-actions {
    display: flex;
    align-items: center;
    gap: 0.65rem;
    flex-wrap: wrap;
  }
}

.primary-btn {
  height: 38px;
  background: linear-gradient(135deg, #6366f1 0%, #4f46e5 100%);
  border: 1px solid rgba(99, 102, 241, 0.4);
  color: #fff;
  padding: 0 1.15rem;
  border-radius: 0.5rem;
  font-weight: 600;
  font-size: 0.825rem;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 0.45rem;
  transition: all 0.15s ease;
  box-shadow: 0 2px 10px rgba(99, 102, 241, 0.25);

  &:hover {
    filter: brightness(1.1);
    transform: translate3d(0, -1.5px, 0);
    box-shadow: 0 4px 16px rgba(99, 102, 241, 0.4);
  }
}

/* ============================================================
   TABS NAVIGATION
   ============================================================ */
.tabs-nav-wrapper {
  overflow-x: auto;
  border-bottom: 1px solid rgba(148, 163, 184, 0.12);
  margin-bottom: 0.25rem;

  &::-webkit-scrollbar { display: none; }
  -ms-overflow-style: none;
  scrollbar-width: none;

  .tabs-nav {
    display: flex;
    gap: 0.5rem;
    min-width: max-content;

    button {
      background: none;
      border: none;
      color: #94a3b8;
      font-weight: 600;
      font-size: 0.85rem;
      padding: 0.65rem 0.95rem;
      cursor: pointer;
      display: inline-flex;
      align-items: center;
      gap: 0.45rem;
      border-bottom: 2px solid transparent;
      transition: all 0.15s ease;

      i { font-size: 0.95rem; }

      &:hover { color: #f1f5f9; }

      &.active {
        color: #38bdf8;
        border-bottom-color: #06b6d4;
      }

      .tab-badge-violet {
        background: rgba(139, 92, 246, 0.2);
        border: 1px solid rgba(139, 92, 246, 0.4);
        color: #c084fc;
        font-size: 0.7rem;
        padding: 0.1rem 0.45rem;
        border-radius: 9999px;
        font-weight: 700;
      }
    }
  }
}
</style>
