<template>
  <div class="email-ops-page">
    <div class="page-top-bar">
      <div class="title-group">
        <div class="title-icon-badge">
          <i class="pi pi-inbox"></i>
        </div>
        <div class="title-text">
          <div class="title-row">
            <h1>Email Operations & AI Hub</h1>
            <span class="version-tag">UC01 • UC02</span>
          </div>
          <p class="subtitle">Quản lý hộp thư, tự động dọn dẹp và phê duyệt phản hồi thông minh</p>
        </div>
      </div>
      <button class="btn-compose-main" @click="showComposeModal = true">
        <i class="pi pi-pencil"></i> Soạn thư mới
      </button>
    </div>

    <!-- Navigation Tabs (Cyber Glass & Scrollable on mobile) -->
    <div class="tabs-nav-wrapper">
      <div class="tabs-nav">
        <button :class="{ active: activeTab === 'inbox' }" @click="activeTab = 'inbox'">
          <i class="pi pi-inbox"></i>
          <span>Hộp thư đến</span>
        </button>
        <button :class="{ active: activeTab === 'drafts' }" @click="activeTab = 'drafts'">
          <i class="pi pi-sparkles"></i>
          <span>Bản nháp AI chờ duyệt</span>
          <span v-if="pendingDrafts.length > 0" class="tab-badge-violet">{{ pendingDrafts.length }}</span>
        </button>
        <button :class="{ active: activeTab === 'rules' }" @click="activeTab = 'rules'">
          <i class="pi pi-sliders-h"></i>
          <span>Quy tắc dọn dẹp</span>
        </button>
        <button :class="{ active: activeTab === 'logs' }" @click="activeTab = 'logs'">
          <i class="pi pi-history"></i>
          <span>Nhật ký & Kiểm toán</span>
        </button>
      </div>
    </div>

    <!-- Tab 1: Inbox -->
    <div v-if="activeTab === 'inbox'" class="tab-content">
      <div class="inbox-filters">
        <div class="search-box">
          <i class="pi pi-search"></i>
          <input 
            v-model="searchQuery" 
            placeholder="Tìm kiếm Gmail (từ khóa, người gửi, subject...)" 
            @keyup.enter="resetAndFetch"
          />
          <button v-if="searchQuery" class="btn-clear-search" @click="clearSearch"><i class="pi pi-times"></i></button>
        </div>
        <label class="switch-label">
          <input type="checkbox" v-model="showUnreadOnly" @change="resetAndFetch" />
          Chỉ hiển thị thư chưa đọc
        </label>
      </div>
      
      <LoadingSpinner v-if="loading && emails.length === 0" text="Đang tải email..." />
      
      <!-- Email Detail View -->
      <div v-else-if="selectedEmail" class="email-detail">
        <button class="btn-cancel" @click="selectedEmail = null"><i class="pi pi-arrow-left"></i> Quay lại</button>
        <div class="detail-header">
          <h3>{{ selectedEmail.subject }}</h3>
          <p>Từ: {{ selectedEmail.from }} | Lúc: {{ formatDate(selectedEmail.receivedAt) }}</p>
        </div>
        <div class="detail-body email-html-body">
          <div v-html="selectedEmail.body || selectedEmail.snippet"></div>
        </div>
        <div class="detail-actions">
          <button class="btn-cancel" :class="{ 'text-yellow': selectedEmail.isStarred }" @click="toggleStar(selectedEmail)">
            <i class="pi" :class="selectedEmail.isStarred ? 'pi-star-fill' : 'pi-star'"></i> {{ selectedEmail.isStarred ? 'Bỏ gắn sao' : 'Gắn sao' }}
          </button>
          <button class="btn-cancel" @click="markAsRead(selectedEmail.id)" v-if="!selectedEmail.isRead"><i class="pi pi-check"></i> Đánh dấu đã đọc</button>
          <button class="btn-danger" @click="trashEmail(selectedEmail.id)"><i class="pi pi-trash"></i> Xóa</button>
          <button class="btn-submit" @click="draftAi(selectedEmail.id)" :disabled="draftingAi">
            <i class="pi pi-sparkles"></i> {{ draftingAi ? 'Đang tạo...' : 'Tạo nháp AI' }}
          </button>
          <button class="btn-submit btn-extract" @click="extractSchedule(selectedEmail.id)" :disabled="extractingSchedule">
            <i class="pi pi-calendar-plus"></i> {{ extractingSchedule ? 'Đang trích xuất...' : 'Trích xuất lịch AI' }}
          </button>
        </div>

        <div class="reply-box">
          <h4>Trả lời</h4>
          <Editor :key="editorKey" v-model="replyText" editorStyle="height: 250px" placeholder="Nhập nội dung phản hồi..." />
          <button class="btn-submit mt-2" @click="sendReply(selectedEmail.id)" :disabled="sendingReply || !replyText">
            <i class="pi pi-send"></i> {{ sendingReply ? 'Đang gửi...' : 'Gửi phản hồi' }}
          </button>
        </div>
      </div>

      <!-- Email List -->
      <div v-else-if="emails.length === 0" class="empty-state">
        <i class="pi pi-check-circle"></i>
        <p>Hộp thư đến trống!</p>
      </div>
      <div v-else class="email-list">
        <div 
          v-for="email in emails" 
          :key="email.id" 
          class="email-card"
          :class="{ 'unread': !email.isRead }"
        >
          <div class="email-card-content" @click="selectEmail(email)">
            <div class="email-header">
              <span class="email-from">{{ email.from }}</span>
              <span class="email-date">{{ formatDate(email.receivedAt) }}</span>
            </div>
            <div class="email-subject">{{ email.subject }}</div>
            <div class="email-snippet">{{ email.snippet }}</div>
          </div>
          <div class="quick-actions">
            <button class="action-btn" :class="{ 'text-yellow': email.isStarred }" @click.stop="toggleStar(email)" title="Đánh dấu sao">
              <i class="pi" :class="email.isStarred ? 'pi-star-fill' : 'pi-star'"></i>
            </button>
            <button v-if="email.isRead" class="action-btn text-blue" @click.stop="markAsUnread(email.id)" title="Đánh dấu chưa đọc"><i class="pi pi-envelope"></i></button>
            <button v-if="!email.isRead" class="action-btn text-green" @click.stop="markAsRead(email.id)" title="Đánh dấu đã đọc"><i class="pi pi-check"></i></button>
            <button class="action-btn text-red" @click.stop="trashEmail(email.id)" title="Chuyển vào thùng rác"><i class="pi pi-trash"></i></button>
          </div>
        </div>
      </div>
      
      <InfiniteScrollObserver v-if="!selectedEmail" :loading="loading" :has-more="!!nextPageToken" @load-more="loadMore" />
    </div>

    <!-- Tab 2: Cleanup Rules -->
    <div v-else-if="activeTab === 'rules'" class="tab-content">
      <CleanupRuleList />
    </div>

    <!-- Tab 3: Logs -->
    <div v-else-if="activeTab === 'logs'" class="tab-content">
      <div class="logs-subnav">
        <button 
          class="subnav-btn" 
          :class="{ active: logsSubTab === 'actions' }" 
          @click="logsSubTab = 'actions'"
        >
          <i class="pi pi-list"></i> Chi tiết Email đã xử lý & Kiểm toán
        </button>
        <button 
          class="subnav-btn" 
          :class="{ active: logsSubTab === 'summaries' }" 
          @click="logsSubTab = 'summaries'"
        >
          <i class="pi pi-chart-bar"></i> Thống kê theo đợt dọn dẹp
        </button>
      </div>

      <!-- Sub-tab 1: Detailed Email Action Logs -->
      <div v-if="logsSubTab === 'actions'">
        <EmailActionLogList />
      </div>

      <!-- Sub-tab 2: Execution Summaries (CleanupLog) -->
      <div v-else>
        <div v-if="cleanupLogs.length === 0" class="empty-state">
          <i class="pi pi-history"></i>
          <p>Chưa có nhật ký dọn dẹp nào.</p>
        </div>
        <div v-else class="logs-list">
          <div v-for="log in cleanupLogs" :key="log.id" class="log-card">
            <div class="log-header">
              <span class="log-rule">Quy tắc: {{ log.ruleName }}</span>
              <span class="log-time">{{ formatDate(log.executedAt) }}</span>
            </div>
            <div class="log-body">
              <div class="log-stat">Quét: <strong>{{ log.totalProcessed }}</strong></div>
              <div class="log-stat text-red">Đã Xóa: <strong>{{ log.totalTrashed }}</strong></div>
              <div class="log-stat text-orange">Lưu trữ: <strong>{{ log.totalArchived }}</strong></div>
              <div class="log-stat text-gray">Bỏ qua: <strong>{{ log.totalSkipped }}</strong></div>
            </div>
            <div class="log-footer">
              <span>Thời gian xử lý: {{ log.durationMs }}ms</span>
            </div>
          </div>
        </div>
        <InfiniteScrollObserver :loading="loadingLogs" :has-more="hasMoreLogs" @load-more="loadMoreLogs" />
      </div>
    </div>

    <!-- Tab 4: AI Drafts Pending Approval (UC02) -->
    <div v-else-if="activeTab === 'drafts'" class="tab-content">
      <LoadingSpinner v-if="loadingDrafts && pendingDrafts.length === 0" text="Đang tải bản nháp AI chờ duyệt..." />
      <div v-else-if="pendingDrafts.length === 0" class="empty-state">
        <i class="pi pi-check-circle" style="color: #10b981; font-size: 2.5rem;"></i>
        <p>Tuyệt vời! Không có bản nháp AI nào đang chờ bạn phê duyệt.</p>
      </div>
      <div v-else class="drafts-list">
        <DraftReviewCard
          v-for="draft in pendingDrafts"
          :key="draft.id"
          :draft="draft"
          @approve="handleApproveDraft"
          @reject="handleRejectDraft"
        />
      </div>
    </div>

    <!-- Compose Email Modal -->
    <ComposeEmailModal
      v-if="showComposeModal"
      @close="showComposeModal = false"
      @sent="resetAndFetch"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch, defineAsyncComponent } from 'vue';
import api from '@/services/api.service';
import LoadingSpinner from '@/components/common/LoadingSpinner.vue';
import { showToast } from '@/services/notification.service';

// Lazy loading heavy components
const CleanupRuleList = defineAsyncComponent(() => import('@/components/email/CleanupRuleList.vue'));
const Editor = defineAsyncComponent(() => import('primevue/editor'));
const InfiniteScrollObserver = defineAsyncComponent(() => import('@/components/common/InfiniteScrollObserver.vue'));
const DraftReviewCard = defineAsyncComponent(() => import('@/components/email/DraftReviewCard.vue'));
const ComposeEmailModal = defineAsyncComponent(() => import('@/components/email/ComposeEmailModal.vue'));
const EmailActionLogList = defineAsyncComponent(() => import('@/components/email/EmailActionLogList.vue'));

const showComposeModal = ref(false);
const pendingDrafts = ref<any[]>([]);
const loadingDrafts = ref(false);

const activeTab = ref('inbox');
const logsSubTab = ref<'actions' | 'summaries'>('actions');
const emails = ref<any[]>([]);
const cleanupLogs = ref<any[]>([]);
const loading = ref(true);
const selectedEmail = ref<any>(null);
const replyText = ref('');
const editorKey = ref(0);
const draftingAi = ref(false);
const extractingSchedule = ref(false);
const sendingReply = ref(false);

const showUnreadOnly = ref(true);
const nextPageToken = ref<string | null>(null);
const searchQuery = ref('');

const resetAndFetch = () => {
  emails.value = [];
  nextPageToken.value = null;
  fetchInbox();
};

const clearSearch = () => {
  searchQuery.value = '';
  resetAndFetch();
};

const fetchInbox = async (token: string | null = null) => {
  loading.value = true;
  try {
    const isReadParam = showUnreadOnly.value ? 'false' : 'true';
    let url = `/emailops/inbox?isRead=${isReadParam}&maxResults=10`;
    if (token) url += `&pageToken=${encodeURIComponent(token)}`;
    if (searchQuery.value.trim()) url += `&search=${encodeURIComponent(searchQuery.value.trim())}`;
    
    const res: any = await api.get(url);
    if (res.success && res.data) {
      if (token) {
        emails.value.push(...res.data.items);
      } else {
        emails.value = res.data.items;
      }
      nextPageToken.value = res.data.nextPageToken;
    }
  } catch (e) {
    console.error('Failed to fetch inbox:', e);
  } finally {
    loading.value = false;
  }
};

const loadMore = () => {
  if (nextPageToken.value) fetchInbox(nextPageToken.value);
};

const toggleStar = async (email: any) => {
  const isNowStarred = !email.isStarred;
  email.isStarred = isNowStarred;
  try {
    if (isNowStarred) {
      await api.post(`/emailops/${email.id}/star`, {});
      showToast({ severity: 'info', summary: 'Đã gắn sao ⭐', detail: email.subject });
    } else {
      await api.post(`/emailops/${email.id}/unstar`, {});
      showToast({ severity: 'info', summary: 'Đã bỏ gắn sao', detail: email.subject });
    }
  } catch (e) {
    email.isStarred = !isNowStarred;
  }
};

const selectEmail = (email: any) => {
  selectedEmail.value = email;
  replyText.value = '';
  editorKey.value++;
};

const markAsRead = async (id: string) => {
  try {
    await api.post(`/emailops/${id}/read`, {});
    if (selectedEmail.value && selectedEmail.value.id === id) selectedEmail.value.isRead = true;
    const item = emails.value.find(e => e.id === id);
    if (item) item.isRead = true;
  } catch (e) {
    console.error(e);
  }
};

const markAsUnread = async (id: string) => {
  try {
    await api.post(`/emailops/${id}/unread`, {});
    if (selectedEmail.value && selectedEmail.value.id === id) selectedEmail.value.isRead = false;
    const item = emails.value.find(e => e.id === id);
    if (item) item.isRead = false;
  } catch (e) {
    console.error(e);
  }
};

const trashEmail = async (id: string) => {
  try {
    await api.delete(`/emailops/${id}`);
    emails.value = emails.value.filter(e => e.id !== id);
    selectedEmail.value = null;
    showToast({
      severity: 'info',
      summary: 'Đã xóa',
      detail: 'Đã chuyển email vào thùng rác.',
    });
  } catch (e) {
    showToast({
      severity: 'error',
      summary: 'Lỗi',
      detail: 'Không thể xóa email.',
    });
  }
};

const draftAi = async (id: string) => {
  draftingAi.value = true;
  try {
    const res: any = await api.post(`/emailops/${id}/draft-ai`, {});
    if (res.success) {
      let content = '';
      if (typeof res.data === 'string') {
        content = res.data;
      } else if (res.data && res.data.draftContent) {
        content = res.data.draftContent;
      }
      
      replyText.value = content;
      editorKey.value++; // Ép PrimeVue Editor render lại khi có dữ liệu
      showToast({
        severity: 'success',
        summary: 'Tạo nháp AI thành công',
        detail: 'Đã điền nội dung đề xuất vào khung soạn thảo.',
      });
    }
  } catch (e) {
    showToast({
      severity: 'error',
      summary: 'Lỗi AI',
      detail: 'Không thể tạo bản nháp AI từ email này.',
    });
  } finally {
    draftingAi.value = false;
  }
};

const extractSchedule = async (id: string) => {
  extractingSchedule.value = true;
  try {
    const res: any = await api.post('/scheduling/extract', { gmailMessageId: id });
    if (res.success) {
      showToast({
        severity: 'success',
        summary: 'Trích xuất lịch thành công',
        detail: 'Hãy mở tab Scheduling (Lịch) để xem và xác nhận sự kiện.',
      });
    }
  } catch (e) {
    showToast({
      severity: 'warn',
      summary: 'Không trích xuất được',
      detail: 'Email này có thể không chứa thông tin ngày giờ sự kiện.',
    });
  } finally {
    extractingSchedule.value = false;
  }
};

const sendReply = async (id: string) => {
  sendingReply.value = true;
  try {
    await api.post(`/emailops/${id}/reply`, { body: replyText.value });
    showToast({
      severity: 'success',
      summary: 'Đã gửi',
      detail: 'Phản hồi đã được gửi thành công.',
    });
    selectedEmail.value = null;
  } catch (e) {
    showToast({
      severity: 'error',
      summary: 'Lỗi',
      detail: 'Không thể gửi phản hồi.',
    });
  } finally {
    sendingReply.value = false;
  }
};

const fetchPendingDrafts = async () => {
  loadingDrafts.value = true;
  try {
    const res: any = await api.get('/emailops/drafts/pending?page=1&pageSize=50');
    if (res.success && res.data) {
      pendingDrafts.value = res.data.items || [];
    }
  } catch (err) {
    console.error('Failed to fetch pending drafts:', err);
  } finally {
    loadingDrafts.value = false;
  }
};

const handleApproveDraft = async ({ id, content }: { id: string; content: string }) => {
  try {
    const res: any = await api.post(`/emailops/drafts/${id}/approve`, { customContent: content });
    if (res.success) {
      showToast({
        severity: 'success',
        summary: 'Đã phê duyệt nháp',
        detail: 'Bản nháp phản hồi đã được lưu trên Gmail của bạn.',
      });
      pendingDrafts.value = pendingDrafts.value.filter(d => d.id !== id);
    }
  } catch (err: any) {
    showToast({
      severity: 'error',
      summary: 'Lỗi duyệt nháp',
      detail: err.message || 'Không thể phê duyệt bản nháp.',
    });
  }
};

const handleRejectDraft = async ({ id }: { id: string }) => {
  try {
    const res: any = await api.post(`/emailops/drafts/${id}/reject`, { reason: 'Từ chối bởi người dùng' });
    if (res.success) {
      showToast({
        severity: 'info',
        summary: 'Đã từ chối',
        detail: 'Bản nháp AI đã bị từ chối.',
      });
      pendingDrafts.value = pendingDrafts.value.filter(d => d.id !== id);
    }
  } catch (err: any) {
    showToast({
      severity: 'error',
      summary: 'Lỗi',
      detail: err.message || 'Không thể từ chối bản nháp.',
    });
  }
};

const formatDate = (dateStr: string) => {
  if (!dateStr) return '';
  return new Date(dateStr).toLocaleString('vi-VN', {
    day: '2-digit', month: '2-digit', year: 'numeric',
    hour: '2-digit', minute: '2-digit'
  });
};

const pageLogs = ref(1);
const hasMoreLogs = ref(true);
const loadingLogs = ref(false);

const fetchLogs = async (page = 1) => {
  loadingLogs.value = true;
  try {
    const res: any = await api.get(`/emailops/logs?page=${page}&pageSize=20`);
    if (res.success && res.data) {
      if (page === 1) {
        cleanupLogs.value = res.data.items;
      } else {
        cleanupLogs.value = [...cleanupLogs.value, ...res.data.items];
      }
      hasMoreLogs.value = page < res.data.totalPages;
      pageLogs.value = page;
    }
  } catch (e) {
    console.error('Failed to load logs:', e);
  } finally {
    loadingLogs.value = false;
  }
};

const loadMoreLogs = () => {
  if (!loadingLogs.value && hasMoreLogs.value) {
    fetchLogs(pageLogs.value + 1);
  }
};

watch(activeTab, (newTab) => {
  if (newTab === 'inbox' && emails.value.length === 0) fetchInbox();
  if (newTab === 'drafts') fetchPendingDrafts();
  if (newTab === 'logs' && cleanupLogs.value.length === 0) fetchLogs(1);
});

onMounted(() => {
  fetchInbox();
  fetchPendingDrafts();
});
</script>

<style scoped lang="scss">
.email-ops-page {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

/* ============================================================
   TOP BAR & CYBER HEADER
   ============================================================ */
.page-top-bar {
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
      background: linear-gradient(135deg, rgba(6, 182, 212, 0.25), rgba(99, 102, 241, 0.2));
      border: 1px solid rgba(6, 182, 212, 0.35);
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 1.25rem;
      color: #22d3ee;
      box-shadow: 0 0 16px rgba(6, 182, 212, 0.2);
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
          background: rgba(6, 182, 212, 0.12);
          border: 1px solid rgba(6, 182, 212, 0.3);
          color: #22d3ee;
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

  .btn-compose-main {
    background: linear-gradient(135deg, #6366f1 0%, #4f46e5 100%);
    border: 1px solid rgba(99, 102, 241, 0.4);
    color: #ffffff;
    height: 38px;
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
}

/* ============================================================
   TABS NAVIGATION BAR
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

      &:hover {
        color: #f1f5f9;
      }

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

.tab-content {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

/* ============================================================
   INBOX FILTERS & SEARCH
   ============================================================ */
.inbox-filters {
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 0.75rem;

  .search-box {
    display: flex;
    align-items: center;
    background: rgba(15, 23, 42, 0.7);
    border: 1px solid rgba(148, 163, 184, 0.16);
    border-radius: 0.5rem;
    padding: 0 0.75rem;
    height: 38px;
    flex: 1;
    max-width: 480px;
    gap: 0.5rem;
    transition: all 0.15s ease;

    &:focus-within {
      border-color: rgba(6, 182, 212, 0.4);
      box-shadow: 0 0 12px rgba(6, 182, 212, 0.15);
    }

    i { color: #64748b; font-size: 0.85rem; }

    input {
      background: none;
      border: none;
      color: #f8fafc;
      width: 100%;
      font-size: 0.825rem;
      outline: none;
      &::placeholder { color: #64748b; }
    }

    .btn-clear-search {
      background: none;
      border: none;
      color: #94a3b8;
      cursor: pointer;
      padding: 0.2rem;
      &:hover { color: #fff; }
    }
  }

  .switch-label {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    font-size: 0.825rem;
    color: #cbd5e1;
    cursor: pointer;
    user-select: none;

    input[type="checkbox"] {
      accent-color: #06b6d4;
      width: 16px;
      height: 16px;
      cursor: pointer;
    }
  }
}

/* ============================================================
   EMAIL LIST & CARDS
   ============================================================ */
.email-list {
  display: flex;
  flex-direction: column;
  gap: 0.65rem;
}

.email-card {
  background: rgba(15, 23, 42, 0.7);
  border: 1px solid rgba(148, 163, 184, 0.12);
  border-top: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 0.75rem;
  backdrop-filter: blur(14px);
  -webkit-backdrop-filter: blur(14px);
  display: flex;
  justify-content: space-between;
  align-items: center;
  overflow: hidden;
  transition: all 0.15s ease;
  
  &:hover {
    border-color: rgba(99, 102, 241, 0.35);
    transform: translate3d(0, -1.5px, 0);
    box-shadow: 0 4px 16px rgba(0, 0, 0, 0.3);
  }
  
  &.unread {
    border-left: 3px solid #06b6d4;
    background: rgba(15, 23, 42, 0.85);

    .email-subject {
      font-weight: 700;
      color: #ffffff;
    }
  }

  .email-card-content {
    padding: 0.85rem 1rem;
    flex: 1;
    cursor: pointer;
    overflow: hidden;

    .email-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 0.25rem;

      .email-from {
        font-size: 0.8rem;
        font-weight: 600;
        color: #38bdf8;
        white-space: nowrap;
        text-overflow: ellipsis;
        overflow: hidden;
      }

      .email-date {
        font-size: 0.725rem;
        color: #64748b;
        white-space: nowrap;
      }
    }

    .email-subject {
      font-size: 0.85rem;
      font-weight: 500;
      color: #e2e8f0;
      margin-bottom: 0.2rem;
      white-space: nowrap;
      text-overflow: ellipsis;
      overflow: hidden;
    }

    .email-snippet {
      font-size: 0.775rem;
      color: #94a3b8;
      white-space: nowrap;
      text-overflow: ellipsis;
      overflow: hidden;
      line-height: 1.35;
    }
  }

  .quick-actions {
    display: flex;
    align-items: center;
    gap: 0.35rem;
    padding: 0 0.85rem;

    .action-btn {
      width: 32px;
      height: 32px;
      border-radius: 0.4rem;
      background: rgba(30, 41, 59, 0.5);
      border: 1px solid rgba(148, 163, 184, 0.15);
      color: #94a3b8;
      display: flex;
      align-items: center;
      justify-content: center;
      cursor: pointer;
      font-size: 0.85rem;
      transition: all 0.15s ease;

      &:hover {
        background: rgba(51, 65, 85, 0.8);
        color: #fff;
        transform: translate3d(0, -1px, 0);
      }

      &.text-yellow { color: #fbbf24; }
      &.text-blue { color: #38bdf8; }
      &.text-green { color: #34d399; }
      &.text-red { color: #fb7185; }
    }
  }
}

/* ============================================================
   EMAIL DETAIL VIEW
   ============================================================ */
.email-detail {
  background: rgba(15, 23, 42, 0.75);
  border: 1px solid rgba(148, 163, 184, 0.14);
  border-radius: 0.85rem;
  padding: 1.25rem;
  backdrop-filter: blur(16px);
  -webkit-backdrop-filter: blur(16px);
  display: flex;
  flex-direction: column;
  gap: 1rem;

  .detail-header {
    border-bottom: 1px solid rgba(148, 163, 184, 0.1);
    padding-bottom: 0.75rem;

    h3 {
      font-size: 1.15rem;
      font-weight: 700;
      color: #f8fafc;
      margin: 0 0 0.35rem 0;
    }

    p {
      font-size: 0.775rem;
      color: #94a3b8;
      margin: 0;
    }
  }

  .detail-body {
    padding: 0.75rem 0;
    color: #cbd5e1;
    font-size: 0.875rem;
    line-height: 1.55;
    max-height: 400px;
    overflow-y: auto;
  }

  .email-html-body {
    overflow-x: auto;
    max-width: 100%;
    padding: 1rem;
    background-color: #0b1120;
    color: #e2e8f0;
    border-radius: 0.5rem;
    border: 1px solid rgba(148, 163, 184, 0.12);
  }

  .detail-actions {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    flex-wrap: wrap;
    padding-top: 0.5rem;
    border-top: 1px solid rgba(148, 163, 184, 0.1);

    button {
      height: 36px;
      padding: 0 0.85rem;
      border-radius: 0.45rem;
      font-size: 0.8rem;
      font-weight: 600;
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
      &:hover { background: rgba(51, 65, 85, 0.8); color: #fff; transform: translate3d(0, -1px, 0); }
      &.text-yellow { color: #fbbf24; }
    }

    .btn-danger {
      background: rgba(244, 63, 94, 0.15);
      border-color: rgba(244, 63, 94, 0.3);
      color: #fb7185;
      &:hover { background: #f43f5e; color: #fff; transform: translate3d(0, -1px, 0); }
    }

    .btn-submit {
      background: linear-gradient(135deg, #6366f1 0%, #4f46e5 100%);
      border-color: rgba(99, 102, 241, 0.4);
      color: #fff;
      &:hover:not(:disabled) { filter: brightness(1.1); transform: translate3d(0, -1px, 0); }
      &:disabled { opacity: 0.5; cursor: not-allowed; }

      &.btn-extract {
        background: linear-gradient(135deg, #06b6d4 0%, #0891b2 100%);
        border-color: rgba(6, 182, 212, 0.4);
      }
    }
  }

  .reply-box {
    margin-top: 0.5rem;
    padding-top: 1rem;
    border-top: 1px solid rgba(148, 163, 184, 0.1);

    h4 {
      font-size: 0.9rem;
      font-weight: 700;
      color: #f8fafc;
      margin-bottom: 0.75rem;
    }

    .btn-submit {
      margin-top: 0.75rem;
      height: 38px;
      padding: 0 1.15rem;
      background: linear-gradient(135deg, #10b981 0%, #059669 100%);
      border: 1px solid rgba(52, 211, 153, 0.3);
      color: #fff;
      border-radius: 0.45rem;
      font-weight: 600;
      cursor: pointer;
      display: inline-flex;
      align-items: center;
      gap: 0.4rem;

      &:hover:not(:disabled) { filter: brightness(1.1); transform: translate3d(0, -1px, 0); }
      &:disabled { opacity: 0.5; cursor: not-allowed; }
    }
  }
}

/* ============================================================
   LOGS SUBNAV & CARDS
   ============================================================ */
.logs-subnav {
  display: flex;
  gap: 0.5rem;
  margin-bottom: 1rem;

  .subnav-btn {
    background: rgba(15, 23, 42, 0.6);
    border: 1px solid rgba(148, 163, 184, 0.15);
    border-radius: 0.5rem;
    color: #94a3b8;
    padding: 0.5rem 0.85rem;
    font-size: 0.8rem;
    font-weight: 600;
    cursor: pointer;
    display: inline-flex;
    align-items: center;
    gap: 0.4rem;
    transition: all 0.15s ease;

    &:hover { color: #f1f5f9; background: rgba(30, 41, 59, 0.6); }

    &.active {
      background: rgba(6, 182, 212, 0.15);
      border-color: rgba(6, 182, 212, 0.35);
      color: #22d3ee;
    }
  }
}

.logs-list {
  display: flex;
  flex-direction: column;
  gap: 0.65rem;
}

.log-card {
  background: rgba(15, 23, 42, 0.7);
  border: 1px solid rgba(148, 163, 184, 0.12);
  border-radius: 0.75rem;
  padding: 0.85rem 1rem;
  backdrop-filter: blur(12px);

  .log-header {
    display: flex;
    justify-content: space-between;
    margin-bottom: 0.4rem;
    .log-rule { font-weight: 700; color: #f8fafc; font-size: 0.825rem; }
    .log-time { font-size: 0.725rem; color: #64748b; }
  }

  .log-body {
    display: flex;
    align-items: center;
    flex-wrap: wrap;
    gap: 1.25rem;
    color: #cbd5e1;
    font-size: 0.8rem;
    padding: 0.5rem 0;
    border-bottom: 1px solid rgba(148, 163, 184, 0.08);

    .log-stat {
      display: flex;
      align-items: center;
      gap: 0.35rem;
      strong { font-size: 0.95rem; font-variant-numeric: tabular-nums; }
    }
    .text-red { color: #fb7185; }
    .text-orange { color: #fbbf24; }
    .text-gray { color: #94a3b8; }
  }

  .log-footer {
    margin-top: 0.4rem;
    font-size: 0.7rem;
    color: #64748b;
  }
}

.drafts-list {
  display: flex;
  flex-direction: column;
  gap: 0.85rem;
}

.empty-state {
  text-align: center;
  padding: 3rem 1rem;
  color: #64748b;

  i { font-size: 2.25rem; margin-bottom: 0.75rem; color: #34d399; opacity: 0.8; }
  p { font-size: 0.85rem; }
}

/* ============================================================
   RESPONSIVE MEDIA QUERIES
   ============================================================ */
@media (max-width: 768px) {
  .page-top-bar {
    flex-direction: column;
    align-items: stretch;
    gap: 0.75rem;

    .btn-compose-main {
      width: 100%;
      justify-content: center;
    }
  }

  .email-card {
    flex-direction: column;
    align-items: stretch;

    .quick-actions {
      justify-content: flex-end;
      padding: 0.5rem 1rem;
      border-top: 1px solid rgba(148, 163, 184, 0.08);
      background: rgba(11, 17, 32, 0.4);
    }
  }
}
</style>
