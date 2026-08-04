<template>
  <div class="email-ops-page">
    <div class="tabs">
      <button :class="{ active: activeTab === 'inbox' }" @click="activeTab = 'inbox'">
        📥 Hộp thư đến (Inbox)
      </button>
      <button :class="{ active: activeTab === 'rules' }" @click="activeTab = 'rules'">
        🧹 Quy tắc dọn Inbox
      </button>
    </div>

    <!-- Tab 1: Inbox -->
    <div v-if="activeTab === 'inbox'" class="tab-content">
      <div v-if="loading" class="loading">Đang tải email...</div>
      
      <!-- Email Detail View -->
      <div v-else-if="selectedEmail" class="email-detail">
        <button class="btn-cancel" @click="selectedEmail = null"><i class="pi pi-arrow-left"></i> Quay lại</button>
        <div class="detail-header">
          <h3>{{ selectedEmail.subject }}</h3>
          <p>Từ: {{ selectedEmail.from }} | Lúc: {{ formatDate(selectedEmail.receivedAt) }}</p>
        </div>
        <div class="detail-body">
          <p>{{ selectedEmail.snippet }}</p>
        </div>
        <div class="detail-actions">
          <button class="btn-cancel" @click="markAsRead(selectedEmail.id)" v-if="!selectedEmail.isRead"><i class="pi pi-check"></i> Đánh dấu đã đọc</button>
          <button class="btn-danger" @click="trashEmail(selectedEmail.id)"><i class="pi pi-trash"></i> Xóa</button>
          <button class="btn-submit" @click="draftAi(selectedEmail.id)" :disabled="draftingAi">
            <i class="pi pi-sparkles"></i> {{ draftingAi ? 'Đang tạo...' : 'Tạo nháp AI' }}
          </button>
        </div>

        <div class="reply-box">
          <h4>Trả lời</h4>
          <textarea v-model="replyText" rows="5" placeholder="Nhập nội dung phản hồi..."></textarea>
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
          @click="selectEmail(email)"
        >
          <div class="email-header">
            <span class="email-from">{{ email.from }}</span>
            <span class="email-date">{{ formatDate(email.receivedAt) }}</span>
          </div>
          <div class="email-subject">{{ email.subject }}</div>
          <div class="email-snippet">{{ email.snippet }}</div>
        </div>
      </div>
    </div>

    <!-- Tab 2: Cleanup Rules -->
    <div v-else class="tab-content">
      <CleanupRuleList />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch } from 'vue';
import api from '@/services/api.service';
import CleanupRuleList from '@/components/email/CleanupRuleList.vue';

const activeTab = ref('inbox');
const emails = ref<any[]>([]);
const loading = ref(true);
const selectedEmail = ref<any>(null);
const replyText = ref('');
const draftingAi = ref(false);
const sendingReply = ref(false);

const fetchInbox = async () => {
  loading.value = true;
  try {
    const res: any = await api.get('/emailops/inbox');
    if (res.success && res.data) {
      emails.value = res.data;
    }
  } catch (e) {
    console.error('Failed to fetch inbox:', e);
  } finally {
    loading.value = false;
  }
};

const selectEmail = (email: any) => {
  selectedEmail.value = email;
  replyText.value = '';
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
      replyText.value = res.data;
    }
  } catch (e) {
    alert('Lỗi tạo nháp AI');
  } finally {
    draftingAi.value = false;
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
  return new Date(dateStr).toLocaleString('vi-VN', {
    day: '2-digit', month: '2-digit', year: 'numeric',
    hour: '2-digit', minute: '2-digit'
  });
};

watch(activeTab, (newTab) => {
  if (newTab === 'inbox' && emails.value.length === 0) fetchInbox();
});

onMounted(fetchInbox);
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
  padding: 1.25rem;
  border-radius: 0.75rem;
  cursor: pointer;
  transition: all 0.2s;
  
  &:hover { border-color: rgba(99, 102, 241, 0.5); }
  
  &.unread {
    border-left: 4px solid #6366f1;
    .email-subject { font-weight: 800; color: #fff; }
  }
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
</style>
