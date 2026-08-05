<template>
  <div class="email-ops-page">
    <div class="tabs">
      <button :class="{ active: activeTab === 'inbox' }" @click="activeTab = 'inbox'">
        📥 Hộp thư đến (Inbox)
      </button>
      <button :class="{ active: activeTab === 'rules' }" @click="activeTab = 'rules'">
        🧹 Quy tắc dọn Inbox
      </button>
      <button :class="{ active: activeTab === 'logs' }" @click="activeTab = 'logs'">
        📋 Nhật ký dọn dẹp
      </button>
    </div>

    <!-- Tab 1: Inbox -->
    <div v-if="activeTab === 'inbox'" class="tab-content">
      <div class="inbox-filters">
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
            <button v-if="email.isRead" class="action-btn text-blue" @click.stop="markAsUnread(email.id)" title="Đánh dấu chưa đọc"><i class="pi pi-envelope"></i></button>
            <button v-if="!email.isRead" class="action-btn text-green" @click.stop="markAsRead(email.id)" title="Đánh dấu đã đọc"><i class="pi pi-check"></i></button>
            <button class="action-btn text-red" @click.stop="trashEmail(email.id)" title="Xóa tạm"><i class="pi pi-trash"></i></button>
          </div>
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
</template>

<script setup lang="ts">
import { ref, onMounted, watch, defineAsyncComponent } from 'vue';
import api from '@/services/api.service';
import LoadingSpinner from '@/components/common/LoadingSpinner.vue';

// Lazy loading heavy components
const CleanupRuleList = defineAsyncComponent(() => import('@/components/email/CleanupRuleList.vue'));
const Editor = defineAsyncComponent(() => import('primevue/editor'));
const InfiniteScrollObserver = defineAsyncComponent(() => import('@/components/common/InfiniteScrollObserver.vue'));

const activeTab = ref('inbox');
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

const resetAndFetch = () => {
  emails.value = [];
  nextPageToken.value = null;
  fetchInbox();
};

const fetchInbox = async (token: string | null = null) => {
  loading.value = true;
  try {
    const isReadParam = showUnreadOnly.value ? 'false' : 'true';
    let url = `/emailops/inbox?isRead=${isReadParam}&maxResults=10`;
    if (token) url += `&pageToken=${token}`;
    
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
  } catch (e) {
    alert('Lỗi xóa email');
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
    }
  } catch (e) {
    alert('Lỗi tạo nháp AI');
  } finally {
    draftingAi.value = false;
  }
};

const extractSchedule = async (id: string) => {
  extractingSchedule.value = true;
  try {
    const res: any = await api.post('/scheduling/extract', { gmailMessageId: id });
    if (res.success) {
      alert('Đã trích xuất lịch thành công! Bạn hãy mở tab Lịch (Scheduling) để xem và xác nhận.');
    }
  } catch (e) {
    alert('Lỗi trích xuất lịch AI. Email này có thể không chứa thông tin sự kiện.');
  } finally {
    extractingSchedule.value = false;
  }
};

const sendReply = async (id: string) => {
  sendingReply.value = true;
  try {
    await api.post(`/emailops/${id}/reply`, { body: replyText.value });
    alert('Đã gửi phản hồi thành công');
    selectedEmail.value = null;
  } catch (e) {
    alert('Lỗi gửi phản hồi');
  } finally {
    sendingReply.value = false;
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
  if (newTab === 'logs' && cleanupLogs.value.length === 0) fetchLogs(1);
});

onMounted(() => {
  fetchInbox();
});
</script>

<style scoped lang="scss">
.tabs {
  display: flex;
  gap: 1rem;
  margin-bottom: 1.5rem;
  border-bottom: 1px solid rgba(255, 255, 255, 0.1);
  padding-bottom: 0.5rem;
}

button {
  background: none;
  border: none;
  color: #94a3b8;
  font-weight: 600;
  font-size: 1rem;
  padding: 0.5rem 1rem;
  cursor: pointer;
  border-bottom: 2px solid transparent;

  &.active {
    color: #818cf8;
    border-bottom-color: #818cf8;
  }
}

.empty-state {
  text-align: center;
  padding: 3rem;
  color: #94a3b8;

  i { font-size: 2.5rem; margin-bottom: 1rem; color: #34d399; }
}

.email-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.email-card {
  background: #1e293b;
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 0.75rem;
  transition: all 0.2s;
  display: flex;
  justify-content: space-between;
  align-items: center;
  overflow: hidden;
  
  &:hover { border-color: rgba(99, 102, 241, 0.5); }
  
  &.unread {
    border-left: 4px solid #6366f1;
    .email-subject { font-weight: 800; color: #fff; }
  }
}

.email-card-content {
  padding: 1.25rem;
  flex: 1;
  cursor: pointer;
}

.quick-actions {
  display: flex;
  gap: 0.5rem;
  padding: 0 1.25rem;
  
  .action-btn {
    background: transparent;
    border: none;
    cursor: pointer;
    font-size: 1.2rem;
    padding: 0.5rem;
    border-radius: 0.25rem;
    transition: background 0.2s;
    &:hover { background: rgba(255,255,255,0.1); }
    &.text-blue { color: #60a5fa; }
    &.text-green { color: #34d399; }
    &.text-red { color: #f87171; }
  }
}

.inbox-filters {
  margin-bottom: 1rem;
  display: flex;
  justify-content: flex-end;
}

.switch-label {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  color: #cbd5e1;
  font-size: 0.9rem;
  cursor: pointer;
  input { width: 1.2rem; height: 1.2rem; cursor: pointer; }
}

.load-more {
  display: flex;
  justify-content: center;
  margin-top: 1.5rem;
}

.btn-secondary {
  background: rgba(255, 255, 255, 0.1);
  border: 1px solid rgba(255,255,255,0.2);
  color: #e2e8f0;
  padding: 0.5rem 1.5rem;
  border-radius: 0.5rem;
  cursor: pointer;
  font-weight: 600;
  &:hover:not(:disabled) { background: rgba(255, 255, 255, 0.15); }
  &:disabled { opacity: 0.5; cursor: not-allowed; }
}

.email-header {
  display: flex;
  justify-content: space-between;
  margin-bottom: 0.5rem;
}
.email-from { font-weight: 600; color: #94a3b8; }
.email-date { font-size: 0.8rem; color: #64748b; }
.email-subject { font-weight: 600; font-size: 1.1rem; color: #f8fafc; margin-bottom: 0.25rem; }
.email-snippet { font-size: 0.9rem; color: #94a3b8; }

.email-detail {
  background: #1e293b;
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 1rem;
  padding: 2rem;
}

.detail-header {
  margin-top: 1.5rem;
  margin-bottom: 1rem;
  padding-bottom: 1rem;
  border-bottom: 1px solid rgba(255,255,255,0.1);
  h3 { margin: 0; font-size: 1.5rem; color: #fff; }
  p { margin: 0.5rem 0 0; color: #94a3b8; font-size: 0.9rem; }
}

.detail-body {
  color: #e2e8f0;
  line-height: 1.6;
  margin-bottom: 2rem;
  white-space: pre-wrap;
}

.detail-actions {
  display: flex;
  gap: 1rem;
  margin-bottom: 2rem;
}

.btn-cancel {
  background: rgba(255,255,255,0.1);
  border: none;
  color: #e2e8f0;
  padding: 0.5rem 1rem;
  border-radius: 0.5rem;
  cursor: pointer;
  &:hover { background: rgba(255,255,255,0.2); }
}

.btn-danger {
  background: rgba(239, 68, 68, 0.2);
  border: none;
  color: #f87171;
  padding: 0.5rem 1rem;
  border-radius: 0.5rem;
  cursor: pointer;
  &:hover { background: rgba(239, 68, 68, 0.4); }
}

.btn-submit {
  background: #6366f1;
  border: none;
  color: white;
  padding: 0.5rem 1rem;
  border-radius: 0.5rem;
  cursor: pointer;
  &:hover:not(:disabled) { background: #4f46e5; }
  &:disabled { opacity: 0.5; cursor: not-allowed; }
}

.mt-2 { margin-top: 1rem; }

.reply-box {
  background: #0f172a;
  padding: 1.5rem;
  border-radius: 0.75rem;
  h4 { margin: 0 0 1rem; color: #cbd5e1; }
  textarea {
    width: 100%;
    background: #1e293b;
    border: 1px solid rgba(255,255,255,0.1);
    color: #fff;
    padding: 1rem;
    border-radius: 0.5rem;
    font-family: inherit;
    resize: vertical;
    &:focus { outline: none; border-color: #6366f1; }
  }
}
.email-html-body {
  overflow-x: auto;
  max-width: 100%;
  padding: 1rem;
  background-color: #fff;
  color: #333;
  border-radius: 8px;
  border: 1px solid #e2e8f0;
}

.logs-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.log-card {
  background: #1e293b;
  border: 1px solid rgba(255,255,255,0.1);
  padding: 1.25rem;
  border-radius: 0.75rem;

  .log-header {
    display: flex;
    justify-content: space-between;
    margin-bottom: 0.5rem;
    .log-rule { font-weight: 700; color: #f8fafc; }
    .log-time { font-size: 0.85rem; color: #94a3b8; }
  }

  .log-body {
    display: flex;
    align-items: center;
    flex-wrap: wrap;
    gap: 1.5rem;
    color: #cbd5e1;
    font-size: 0.95rem;
    padding: 0.75rem 0;
    border-bottom: 1px solid rgba(255,255,255,0.05);

    .log-stat {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      strong { font-size: 1.1rem; }
    }
    .text-red { color: #fca5a5; }
    .text-orange { color: #fcd34d; }
    .text-gray { color: #94a3b8; }
  }

  .log-footer {
    margin-top: 0.75rem;
    font-size: 0.8rem;
    color: #64748b;
  }
}
</style>
