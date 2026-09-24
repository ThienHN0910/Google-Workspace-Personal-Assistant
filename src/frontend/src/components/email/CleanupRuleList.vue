<template>
  <div class="cleanup-rules-container">
    <!-- Header Section -->
    <div class="header-actions">
      <div class="header-title-group">
        <h2>
          <i class="pi pi-shield-check title-icon"></i>
          Quy tắc dọn dẹp Email (UC01 Inbox Zero)
        </h2>
        <p class="subtitle">Quản lý danh sách quy tắc dọn dẹp Regex và AI tự động học để dọn dẹp hộp thư đến</p>
      </div>
      <div class="header-btns">
        <button class="secondary-btn" @click="openCreateModal">
          <i class="pi pi-plus"></i> Tạo quy tắc mới
        </button>
        <button class="primary-btn" @click="handleRunAll" :disabled="runningCleanup">
          <i class="pi" :class="runningCleanup ? 'pi-spin pi-spinner' : 'pi-play'"></i>
          {{ runningCleanup ? 'Đang thực thi...' : 'Chạy dọn dẹp ngay' }}
        </button>
      </div>
    </div>

    <!-- Summary Statistics Bar -->
    <div class="stats-overview">
      <div class="stat-card">
        <div class="stat-icon icon-indigo"><i class="pi pi-list"></i></div>
        <div class="stat-info">
          <span class="stat-value">{{ totalRules }}</span>
          <span class="stat-label">Tổng số Quy tắc</span>
        </div>
      </div>
      <div class="stat-card">
        <div class="stat-icon icon-emerald"><i class="pi pi-check-circle"></i></div>
        <div class="stat-info">
          <span class="stat-value">{{ activeRulesCount }}</span>
          <span class="stat-label">Đang kích hoạt</span>
        </div>
      </div>
      <div class="stat-card">
        <div class="stat-icon icon-purple"><i class="pi pi-sparkles"></i></div>
        <div class="stat-info">
          <span class="stat-value">{{ autoLearnedCount }}</span>
          <span class="stat-label">AI Tự động học</span>
        </div>
      </div>
      <div class="stat-card">
        <div class="stat-icon icon-rose"><i class="pi pi-trash"></i></div>
        <div class="stat-info">
          <span class="stat-value">{{ trashRulesCount }}</span>
          <span class="stat-label">Hành động Xóa (Trash)</span>
        </div>
      </div>
    </div>

    <!-- Filter & Search Bar -->
    <div class="filter-bar">
      <div class="search-box">
        <i class="pi pi-search search-icon"></i>
        <input 
          v-model="searchQuery" 
          type="text" 
          placeholder="Tìm quy tắc theo tên, regex hoặc từ khóa..."
        />
        <button v-if="searchQuery" class="clear-search" @click="searchQuery = ''">
          <i class="pi pi-times"></i>
        </button>
      </div>

      <div class="filter-tabs">
        <button 
          v-for="tab in filterTabs" 
          :key="tab.id"
          class="filter-tab"
          :class="{ active: activeFilterTab === tab.id }"
          @click="activeFilterTab = tab.id"
        >
          <span>{{ tab.label }}</span>
          <span class="tab-count">{{ tab.count }}</span>
        </button>
      </div>
    </div>

    <!-- Loading State -->
    <LoadingSpinner v-if="loading" text="Đang tải dữ liệu..." />

    <!-- Feedback Management View -->
    <div v-else-if="activeFilterTab === 'feedback'" class="feedbacks-container">
      <div v-if="filteredFeedbacks.length === 0" class="empty-state">
        <div class="empty-icon"><i class="pi pi-sparkles" style="color: #c084fc;"></i></div>
        <h3>Chưa có mẫu nào được dạy cho AI</h3>
        <p>
          Khi duyệt Hộp thư đến, hãy bấm nút <b>"Dọn & Dạy AI"</b> trên các email không muốn giữ lại. AI sẽ lưu mẫu và sử dụng để học quy tắc dọn dẹp tốt hơn.
        </p>
      </div>
      <div v-else class="feedbacks-grid">
        <div v-for="fb in filteredFeedbacks" :key="fb.id" class="feedback-card">
          <div class="feedback-header">
            <div class="fb-sender-group">
              <span class="fb-sender">{{ fb.sender }}</span>
              <span v-if="fb.senderDomain" class="fb-domain-badge">@{{ fb.senderDomain }}</span>
            </div>
            <button class="btn-delete-fb" @click="handleDeleteFeedback(fb.id)" title="Xóa mẫu dạy này">
              <i class="pi pi-trash"></i>
            </button>
          </div>
          <div class="fb-subject">{{ fb.subject || '(Không có tiêu đề)' }}</div>
          <div v-if="fb.snippet" class="fb-snippet">"{{ fb.snippet }}"</div>
          <div class="fb-footer">
            <div class="fb-tags">
              <span v-for="tag in fb.tags" :key="tag" class="fb-tag-pill">{{ tag }}</span>
            </div>
            <div class="fb-reason">
              <i class="pi pi-comment"></i> {{ fb.reason }}
            </div>
            <div class="fb-date">
              <i class="pi pi-clock"></i> {{ formatDate(fb.createdAt) }}
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Empty State for Rules -->
    <div v-else-if="filteredRules.length === 0" class="empty-state">
      <div class="empty-icon"><i class="pi pi-inbox"></i></div>
      <h3>Không tìm thấy quy tắc dọn dẹp nào</h3>
      <p v-if="searchQuery || activeFilterTab !== 'all'">
        Thử thay đổi từ khóa tìm kiếm hoặc chuyển sang bộ lọc khác.
      </p>
      <p v-else>
        Chưa có quy tắc dọn dẹp nào. Bạn có thể bấm "Tạo quy tắc mới" hoặc chờ AI tự động học pattern.
      </p>
      <button class="secondary-btn" @click="openCreateModal" style="margin-top: 1rem;">
        <i class="pi pi-plus"></i> Tạo quy tắc đầu tiên
      </button>
    </div>

    <!-- Rules Grid -->
    <div v-else class="rules-grid">
      <div 
        v-for="rule in filteredRules" 
        :key="rule.id" 
        class="rule-card" 
        :class="{ 'inactive': !rule.isActive }"
      >
        <!-- Card Header -->
        <div class="card-header">
          <div class="rule-title-group">
            <span class="rule-name">{{ rule.ruleName }}</span>
            <div class="rule-badges">
              <span v-if="rule.isAutoLearned" class="tag-badge tag-ai">
                <i class="pi pi-sparkles"></i> AI Tự học
              </span>
              <span v-else-if="rule.useAI" class="tag-badge tag-ai-manual">
                <i class="pi pi-bolt"></i> AI Filter
              </span>
              <span v-else class="tag-badge tag-manual">
                <i class="pi pi-cog"></i> Thủ công
              </span>

              <span class="action-badge" :class="rule.action === 0 ? 'badge-trash' : 'badge-archive'">
                <i class="pi" :class="rule.action === 0 ? 'pi-trash' : 'pi-inbox'"></i>
                {{ rule.action === 0 ? 'Xóa rác' : 'Lưu trữ' }}
              </span>
            </div>
          </div>

          <!-- Active Toggle Switch -->
          <label class="switch" :title="rule.isActive ? 'Bấm để tắt quy tắc' : 'Bấm để bật quy tắc'">
            <input type="checkbox" :checked="rule.isActive" @change="handleToggle(rule.id)" />
            <span class="slider round"></span>
          </label>
        </div>

        <!-- Card Body / Details -->
        <div class="card-body">
          <!-- AI Prompt Condition -->
          <div v-if="rule.useAI" class="detail-item ai-item">
            <div class="detail-label"><i class="pi pi-sparkles"></i> Prompt Điều kiện AI:</div>
            <div class="code-block ai-prompt-box">{{ rule.aiPrompt || 'Chưa thiết lập prompt' }}</div>
          </div>

          <!-- Custom Gmail Query -->
          <div v-else-if="rule.customQuery" class="detail-item">
            <div class="detail-label"><i class="pi pi-search"></i> Gmail Search Query:</div>
            <div class="code-block query-box">
              <code>{{ rule.customQuery }}</code>
              <button class="copy-btn" @click="copyText(rule.customQuery)" title="Sao chép query">
                <i class="pi pi-copy"></i>
              </button>
            </div>
          </div>

          <!-- Regex Conditions -->
          <div v-else class="regex-details">
            <div v-if="rule.subjectRegex" class="detail-item">
              <div class="detail-label"><i class="pi pi-align-left"></i> Regex Tiêu đề (Subject):</div>
              <div class="code-block">
                <code>{{ rule.subjectRegex }}</code>
                <button class="copy-btn" @click="copyText(rule.subjectRegex)" title="Sao chép Regex">
                  <i class="pi pi-copy"></i>
                </button>
              </div>
            </div>

            <div v-if="rule.senderRegex" class="detail-item">
              <div class="detail-label"><i class="pi pi-user"></i> Regex Người gửi (Sender):</div>
              <div class="code-block">
                <code>{{ rule.senderRegex }}</code>
                <button class="copy-btn" @click="copyText(rule.senderRegex)" title="Sao chép Regex">
                  <i class="pi pi-copy"></i>
                </button>
              </div>
            </div>

            <div v-if="rule.bodyRegex" class="detail-item">
              <div class="detail-label"><i class="pi pi-file"></i> Regex Nội dung (Body):</div>
              <div class="code-block">
                <code>{{ rule.bodyRegex }}</code>
                <button class="copy-btn" @click="copyText(rule.bodyRegex)" title="Sao chép Regex">
                  <i class="pi pi-copy"></i>
                </button>
              </div>
            </div>

            <div v-if="!rule.subjectRegex && !rule.senderRegex && !rule.bodyRegex" class="detail-item fallback-item">
              <span class="text-muted"><i class="pi pi-info-circle"></i> Quy tắc dọn dẹp mặc định cho email chưa đọc.</span>
            </div>
          </div>
        </div>

        <!-- Card Actions Footer -->
        <div class="card-footer">
          <span class="status-indicator" :class="rule.isActive ? 'status-online' : 'status-offline'">
            <span class="status-dot"></span>
            {{ rule.isActive ? 'Đang hoạt động' : 'Tắt' }}
          </span>

          <div class="footer-btns">
            <button class="icon-btn edit-btn" @click="openEditModal(rule)" title="Chỉnh sửa quy tắc">
              <i class="pi pi-pencil"></i>
              <span>Sửa</span>
            </button>
            <button class="icon-btn delete-btn" @click="handleDelete(rule.id)" title="Xóa quy tắc">
              <i class="pi pi-trash"></i>
              <span>Xóa</span>
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Create/Edit Modal -->
    <div v-if="showModal" class="modal-overlay" @click.self="closeModal">
      <div class="modal-content glass-panel">
        <div class="modal-header">
          <h3>
            <i class="pi" :class="isEditing ? 'pi-pencil' : 'pi-plus-circle'"></i>
            {{ isEditing ? 'Chỉnh sửa quy tắc dọn dẹp' : 'Tạo quy tắc dọn dẹp mới' }}
          </h3>
          <button class="close-btn" @click="closeModal"><i class="pi pi-times"></i></button>
        </div>

        <form @submit.prevent="handleSubmit" class="modal-form">
          <div class="form-group">
            <label class="required-label">Tên quy tắc</label>
            <input 
              v-model="formData.ruleName" 
              required 
              placeholder="Ví dụ: Xóa email khuyến mãi & quảng cáo" 
              autofocus 
            />
          </div>

          <div class="form-group">
            <label class="required-label">Hành động khi khớp điều kiện</label>
            <select v-model.number="formData.action" required>
              <option :value="0">🗑️ Chuyển vào Thùng rác (Trash)</option>
              <option :value="1">📦 Lưu trữ (Archive / Bỏ khỏi Inbox)</option>
            </select>
          </div>

          <div class="form-group-checkbox">
            <label class="checkbox-container">
              <input type="checkbox" v-model="formData.useAI" />
              <span class="checkmark"></span>
              <span class="checkbox-label">
                <i class="pi pi-sparkles text-purple"></i> Sử dụng Gemini AI phân tích nội dung email
              </span>
            </label>
          </div>

          <div v-if="formData.useAI" class="form-group animate-fade">
            <label>Prompt AI (Mô tả điều kiện dọn dẹp)</label>
            <textarea 
              v-model="formData.aiPrompt" 
              rows="3" 
              placeholder="Ví dụ: Đánh giá xem email này có phải là thông báo khuyến mãi, giảm giá khóa học hoặc spam không."
            ></textarea>
          </div>

          <div v-else class="regex-form-group animate-fade">
            <div class="form-group">
              <label>Regex Tiêu đề (Subject Regex)</label>
              <input v-model="formData.subjectRegex" placeholder="Ví dụ: (?i).*(khuyến mãi|giảm giá|flash sale).*" />
            </div>

            <div class="form-group">
              <label>Regex Người gửi (Sender Regex)</label>
              <input v-model="formData.senderRegex" placeholder="Ví dụ: (?i).*(no-reply|newsletter)@domain\.com" />
            </div>

            <div class="form-group">
              <label>Regex Nội dung (Body Regex)</label>
              <input v-model="formData.bodyRegex" placeholder="Ví dụ: (?i).*(unsubscribe|hủy đăng ký).*" />
            </div>
          </div>

          <div class="modal-actions">
            <button type="button" class="btn-secondary" @click="closeModal">Hủy bỏ</button>
            <button type="submit" class="btn-primary" :disabled="submitting">
              <i v-if="submitting" class="pi pi-spin pi-spinner"></i>
              <span>{{ isEditing ? 'Cập nhật quy tắc' : 'Tạo mới quy tắc' }}</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import api from '@/services/api.service';
import LoadingSpinner from '@/components/common/LoadingSpinner.vue';

const rules = ref<any[]>([]);
const feedbacks = ref<any[]>([]);
const loading = ref(true);
const runningCleanup = ref(false);
const submitting = ref(false);
const searchQuery = ref('');
const activeFilterTab = ref('all');

const showModal = ref(false);
const isEditing = ref(false);
const currentEditId = ref('');
const formData = ref({
  ruleName: '',
  action: 0,
  whitelistDomains: [] as string[],
  customQuery: '',
  useAI: false,
  aiPrompt: '',
  subjectRegex: '',
  senderRegex: '',
  bodyRegex: ''
});

const fetchRules = async () => {
  loading.value = true;
  try {
    const res: any = await api.get('/emailops/rules');
    if (res.success) {
      rules.value = res.data || [];
    }
  } catch (e) {
    console.error('Failed to fetch rules:', e);
  } finally {
    loading.value = false;
  }
};

const fetchFeedbacks = async () => {
  try {
    const res: any = await api.get('/emailops/cleanup/feedback');
    if (res.success) {
      feedbacks.value = res.data || [];
    }
  } catch (e) {
    console.error('Failed to fetch feedbacks:', e);
  }
};

const handleDeleteFeedback = async (id: string) => {
  if (confirm('Bạn có chắc muốn xóa mẫu dạy AI này không?')) {
    try {
      await api.delete(`/emailops/cleanup/feedback/${id}`);
      feedbacks.value = feedbacks.value.filter(f => f.id !== id);
    } catch (e) {
      alert('Lỗi khi xóa mẫu feedback');
    }
  }
};

const formatDate = (dateStr: string) => {
  if (!dateStr) return '';
  const d = new Date(dateStr);
  return d.toLocaleString('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  });
};

// Computed Stats
const totalRules = computed(() => rules.value.length);
const activeRulesCount = computed(() => rules.value.filter(r => r.isActive).length);
const autoLearnedCount = computed(() => rules.value.filter(r => r.isAutoLearned).length);
const trashRulesCount = computed(() => rules.value.filter(r => r.action === 0).length);

const filterTabs = computed(() => [
  { id: 'all', label: 'Tất cả quy tắc', count: totalRules.value },
  { id: 'active', label: 'Đang bật', count: activeRulesCount.value },
  { id: 'auto', label: 'AI Tự động học', count: autoLearnedCount.value },
  { id: 'feedback', label: '🧠 Mẫu đã dạy AI', count: feedbacks.value.length },
  { id: 'inactive', label: 'Đã tắt', count: totalRules.value - activeRulesCount.value }
]);

const filteredFeedbacks = computed(() => {
  if (!searchQuery.value.trim()) return feedbacks.value;
  const q = searchQuery.value.toLowerCase().trim();
  return feedbacks.value.filter(f =>
    f.sender?.toLowerCase().includes(q) ||
    f.subject?.toLowerCase().includes(q) ||
    f.reason?.toLowerCase().includes(q) ||
    (f.tags && f.tags.some((t: string) => t.toLowerCase().includes(q)))
  );
});

const filteredRules = computed(() => {
  return rules.value.filter(rule => {
    // 1. Tab Filter
    if (activeFilterTab.value === 'active' && !rule.isActive) return false;
    if (activeFilterTab.value === 'inactive' && rule.isActive) return false;
    if (activeFilterTab.value === 'auto' && !rule.isAutoLearned) return false;

    // 2. Search Query Filter
    if (searchQuery.value.trim()) {
      const q = searchQuery.value.toLowerCase().trim();
      const matchName = rule.ruleName?.toLowerCase().includes(q);
      const matchSubject = rule.subjectRegex?.toLowerCase().includes(q);
      const matchSender = rule.senderRegex?.toLowerCase().includes(q);
      const matchBody = rule.bodyRegex?.toLowerCase().includes(q);
      const matchQuery = rule.customQuery?.toLowerCase().includes(q);
      const matchPrompt = rule.aiPrompt?.toLowerCase().includes(q);
      return matchName || matchSubject || matchSender || matchBody || matchQuery || matchPrompt;
    }

    return true;
  });
});

const handleRunAll = async () => {
  runningCleanup.value = true;
  try {
    const res: any = await api.post('/emailops/rules/run', {});
    if (res.success) {
      alert(`✅ Thực thi Dọn dẹp Inbox thành công!\n• Đã dọn dẹp/chuyển vào Thùng rác: ${res.data.totalTrashed} email\n• Đã lưu trữ: ${res.data.totalArchived} email\n• Thời gian thực thi: ${res.data.totalDurationMs}ms`);
      fetchRules();
    }
  } catch (e) {
    alert('Lỗi thực thi quy tắc dọn dẹp inbox');
  } finally {
    runningCleanup.value = false;
  }
};

const openCreateModal = () => {
  isEditing.value = false;
  currentEditId.value = '';
  formData.value = { 
    ruleName: '', action: 0, 
    whitelistDomains: [], customQuery: '', useAI: false, aiPrompt: '', 
    subjectRegex: '', senderRegex: '', bodyRegex: '' 
  };
  showModal.value = true;
};

const openEditModal = (rule: any) => {
  isEditing.value = true;
  currentEditId.value = rule.id;
  formData.value = { 
    ruleName: rule.ruleName,
    action: rule.action ?? 0,
    whitelistDomains: rule.whitelistDomains || [],
    customQuery: rule.customQuery || '',
    useAI: rule.useAI || false,
    aiPrompt: rule.aiPrompt || '',
    subjectRegex: rule.subjectRegex || '',
    senderRegex: rule.senderRegex || '',
    bodyRegex: rule.bodyRegex || ''
  };
  showModal.value = true;
};

const closeModal = () => {
  showModal.value = false;
};

const handleSubmit = async () => {
  submitting.value = true;
  try {
    if (isEditing.value) {
      await api.put(`/emailops/rules/${currentEditId.value}`, formData.value);
    } else {
      await api.post('/emailops/rules', formData.value);
    }
    closeModal();
    fetchRules();
  } catch (e) {
    alert('Lỗi khi lưu quy tắc');
  } finally {
    submitting.value = false;
  }
};

const handleDelete = async (id: string) => {
  if (confirm('Bạn có chắc chắn muốn xóa quy tắc dọn dẹp này không?')) {
    try {
      await api.delete(`/emailops/rules/${id}`);
      fetchRules();
    } catch (e) {
      alert('Lỗi khi xóa quy tắc');
    }
  }
};

const handleToggle = async (id: string) => {
  try {
    await api.patch(`/emailops/rules/${id}/toggle`, {});
    // Optimistic toggle locally
    const rule = rules.value.find(r => r.id === id);
    if (rule) rule.isActive = !rule.isActive;
  } catch (e) {
    alert('Lỗi khi chuyển trạng thái quy tắc');
    fetchRules();
  }
};

const copyText = (text: string) => {
  if (!text) return;
  navigator.clipboard.writeText(text);
  alert(`Đã sao chép vào bộ nhớ tạm:\n${text}`);
};

onMounted(() => {
  fetchRules();
  fetchFeedbacks();
});
</script>

<style scoped lang="scss">
.cleanup-rules-container {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

/* Header */
.header-actions {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  flex-wrap: wrap;
  gap: 1rem;
}

.header-title-group {
  h2 {
    font-size: 1.35rem;
    font-weight: 700;
    color: #f8fafc;
    display: flex;
    align-items: center;
    gap: 0.6rem;
    margin: 0 0 0.35rem 0;

    .title-icon {
      color: #6366f1;
    }
  }

  .subtitle {
    font-size: 0.85rem;
    color: #94a3b8;
    margin: 0;
  }
}

.header-btns {
  display: flex;
  gap: 0.75rem;
}

.primary-btn {
  background: linear-gradient(135deg, #6366f1 0%, #4f46e5 100%);
  color: #fff;
  border: none;
  padding: 0.65rem 1.35rem;
  border-radius: 0.6rem;
  font-weight: 600;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 0.5rem;
  box-shadow: 0 4px 14px rgba(99, 102, 241, 0.35);
  transition: all 0.2s ease;

  &:hover:not(:disabled) {
    transform: translateY(-1px);
    box-shadow: 0 6px 18px rgba(99, 102, 241, 0.45);
  }

  &:disabled {
    opacity: 0.6;
    cursor: not-allowed;
  }
}

.secondary-btn {
  background: rgba(30, 41, 59, 0.8);
  color: #f8fafc;
  border: 1px solid rgba(255, 255, 255, 0.12);
  padding: 0.65rem 1.25rem;
  border-radius: 0.6rem;
  font-weight: 600;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 0.5rem;
  backdrop-filter: blur(8px);
  transition: all 0.2s ease;

  &:hover {
    background: rgba(255, 255, 255, 0.1);
    border-color: rgba(255, 255, 255, 0.25);
  }
}

/* Stats Overview */
.stats-overview {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 1rem;
}

.stat-card {
  background: rgba(30, 41, 59, 0.6);
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 0.75rem;
  padding: 1rem 1.25rem;
  display: flex;
  align-items: center;
  gap: 1rem;
  backdrop-filter: blur(12px);
}

.stat-icon {
  width: 2.75rem;
  height: 2.75rem;
  border-radius: 0.6rem;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.25rem;

  &.icon-indigo { background: rgba(99, 102, 241, 0.15); color: #818cf8; }
  &.icon-emerald { background: rgba(16, 185, 129, 0.15); color: #34d399; }
  &.icon-purple { background: rgba(168, 85, 247, 0.15); color: #c084fc; }
  &.icon-rose { background: rgba(244, 63, 94, 0.15); color: #fb7185; }
}

.stat-info {
  display: flex;
  flex-direction: column;

  .stat-value {
    font-size: 1.4rem;
    font-weight: 800;
    color: #f8fafc;
    line-height: 1.2;
  }

  .stat-label {
    font-size: 0.78rem;
    color: #94a3b8;
  }
}

/* Filter & Search Bar */
.filter-bar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 1rem;
  background: rgba(15, 23, 42, 0.6);
  padding: 0.75rem 1rem;
  border-radius: 0.75rem;
  border: 1px solid rgba(255, 255, 255, 0.06);
}

.search-box {
  position: relative;
  flex: 1;
  min-width: 260px;

  .search-icon {
    position: absolute;
    left: 0.85rem;
    top: 50%;
    transform: translateY(-50%);
    color: #64748b;
  }

  input {
    width: 100%;
    background: #0f172a;
    border: 1px solid rgba(255, 255, 255, 0.1);
    color: #f8fafc;
    padding: 0.5rem 2.2rem;
    border-radius: 0.5rem;
    font-size: 0.88rem;

    &:focus {
      outline: none;
      border-color: #6366f1;
    }
  }

  .clear-search {
    position: absolute;
    right: 0.6rem;
    top: 50%;
    transform: translateY(-50%);
    background: none;
    border: none;
    color: #64748b;
    cursor: pointer;
    &:hover { color: #f8fafc; }
  }
}

.filter-tabs {
  display: flex;
  gap: 0.4rem;
  background: #0f172a;
  padding: 0.25rem;
  border-radius: 0.5rem;
  border: 1px solid rgba(255, 255, 255, 0.06);
}

.filter-tab {
  background: transparent;
  border: none;
  color: #94a3b8;
  padding: 0.4rem 0.85rem;
  border-radius: 0.35rem;
  font-size: 0.82rem;
  font-weight: 600;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 0.4rem;
  transition: all 0.2s ease;

  &:hover { color: #f8fafc; }

  &.active {
    background: #1e293b;
    color: #818cf8;
    box-shadow: 0 2px 6px rgba(0, 0, 0, 0.2);
  }

  .tab-count {
    background: rgba(255, 255, 255, 0.08);
    padding: 0.1rem 0.4rem;
    border-radius: 0.25rem;
    font-size: 0.72rem;
  }
}

/* Empty State */
.empty-state {
  text-align: center;
  padding: 3rem 1.5rem;
  background: rgba(30, 41, 59, 0.3);
  border: 1px dashed rgba(255, 255, 255, 0.1);
  border-radius: 1rem;

  .empty-icon {
    font-size: 2.5rem;
    color: #64748b;
    margin-bottom: 1rem;
  }

  h3 { color: #f8fafc; margin: 0 0 0.5rem 0; font-size: 1.1rem; }
  p { color: #94a3b8; font-size: 0.85rem; margin: 0; }
}

/* Rules Grid */
.rules-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
  gap: 1.25rem;
}

.rule-card {
  background: rgba(30, 41, 59, 0.7);
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 0.85rem;
  padding: 1.25rem;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  backdrop-filter: blur(12px);
  transition: all 0.25s ease;

  &:hover {
    border-color: rgba(99, 102, 241, 0.3);
    transform: translateY(-2px);
    box-shadow: 0 8px 24px rgba(0, 0, 0, 0.25);
  }

  &.inactive {
    opacity: 0.55;
    filter: grayscale(0.2);
  }
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 1rem;
}

.rule-title-group {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;

  .rule-name {
    font-weight: 700;
    font-size: 0.98rem;
    color: #f8fafc;
    line-height: 1.35;
  }
}

.rule-badges {
  display: flex;
  align-items: center;
  gap: 0.4rem;
  flex-wrap: wrap;
}

.tag-badge {
  font-size: 0.7rem;
  padding: 0.2rem 0.5rem;
  border-radius: 0.3rem;
  font-weight: 600;
  display: inline-flex;
  align-items: center;
  gap: 0.25rem;

  &.tag-ai { background: rgba(168, 85, 247, 0.2); color: #d8b4fe; border: 1px solid rgba(168, 85, 247, 0.3); }
  &.tag-ai-manual { background: rgba(99, 102, 241, 0.2); color: #a5b4fc; border: 1px solid rgba(99, 102, 241, 0.3); }
  &.tag-manual { background: rgba(148, 163, 184, 0.15); color: #cbd5e1; border: 1px solid rgba(148, 163, 184, 0.2); }
}

.action-badge {
  font-size: 0.7rem;
  padding: 0.2rem 0.5rem;
  border-radius: 0.3rem;
  font-weight: 700;
  display: inline-flex;
  align-items: center;
  gap: 0.25rem;

  &.badge-trash { background: rgba(239, 68, 68, 0.18); color: #fca5a5; border: 1px solid rgba(239, 68, 68, 0.3); }
  &.badge-archive { background: rgba(59, 130, 246, 0.18); color: #93c5fd; border: 1px solid rgba(59, 130, 246, 0.3); }
}

/* Switch Toggle */
.switch {
  position: relative;
  display: inline-block;
  width: 38px;
  height: 22px;
  flex-shrink: 0;

  input { opacity: 0; width: 0; height: 0; }

  .slider {
    position: absolute;
    cursor: pointer;
    top: 0; left: 0; right: 0; bottom: 0;
    background-color: #334155;
    transition: .3s;
    border-radius: 22px;

    &:before {
      position: absolute;
      content: "";
      height: 16px; width: 16px;
      left: 3px; bottom: 3px;
      background-color: white;
      transition: .3s;
      border-radius: 50%;
    }
  }

  input:checked + .slider {
    background-color: #10b981;
  }

  input:checked + .slider:before {
    transform: translateX(16px);
  }
}

/* Card Body */
.card-body {
  margin-bottom: 1rem;
}

.regex-details {
  display: flex;
  flex-direction: column;
  gap: 0.6rem;
}

.detail-item {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;

  .detail-label {
    font-size: 0.75rem;
    color: #94a3b8;
    font-weight: 600;
    display: flex;
    align-items: center;
    gap: 0.35rem;
  }
}

.code-block {
  background: #0f172a;
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 0.4rem;
  padding: 0.45rem 0.65rem;
  font-family: 'Fira Code', monospace, sans-serif;
  font-size: 0.78rem;
  color: #38bdf8;
  display: flex;
  justify-content: space-between;
  align-items: center;
  word-break: break-all;

  code { font-family: inherit; }

  .copy-btn {
    background: none;
    border: none;
    color: #64748b;
    cursor: pointer;
    padding: 0.2rem;
    margin-left: 0.4rem;
    font-size: 0.85rem;
    &:hover { color: #38bdf8; }
  }
}

.ai-prompt-box {
  color: #c084fc;
  font-family: inherit;
  font-size: 0.8rem;
  line-height: 1.4;
}

.fallback-item {
  font-size: 0.78rem;
  color: #64748b;
}

/* Card Footer */
.card-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
  border-top: 1px solid rgba(255, 255, 255, 0.06);
  padding-top: 0.75rem;
}

.status-indicator {
  font-size: 0.75rem;
  display: flex;
  align-items: center;
  gap: 0.4rem;

  .status-dot {
    width: 6px;
    height: 6px;
    border-radius: 50%;
  }

  &.status-online {
    color: #34d399;
    .status-dot { background: #34d399; box-shadow: 0 0 8px #34d399; }
  }

  &.status-offline {
    color: #64748b;
    .status-dot { background: #64748b; }
  }
}

.footer-btns {
  display: flex;
  gap: 0.4rem;
}

.icon-btn {
  background: transparent;
  border: 1px solid rgba(255, 255, 255, 0.08);
  color: #cbd5e1;
  padding: 0.35rem 0.6rem;
  border-radius: 0.4rem;
  font-size: 0.78rem;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 0.35rem;
  transition: all 0.2s ease;

  &.edit-btn:hover {
    background: rgba(99, 102, 241, 0.15);
    color: #a5b4fc;
    border-color: rgba(99, 102, 241, 0.3);
  }

  &.delete-btn:hover {
    background: rgba(239, 68, 68, 0.15);
    color: #fca5a5;
    border-color: rgba(239, 68, 68, 0.3);
  }
}

/* Modal Styles */
.modal-overlay {
  position: fixed;
  top: 0; left: 0; right: 0; bottom: 0;
  background: rgba(0, 0, 0, 0.75);
  backdrop-filter: blur(6px);
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
  padding: 1.75rem;
  width: 100%;
  max-width: 480px;
  box-shadow: 0 20px 40px rgba(0, 0, 0, 0.5);
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1.25rem;

  h3 {
    margin: 0;
    font-size: 1.15rem;
    color: #f8fafc;
    display: flex;
    align-items: center;
    gap: 0.5rem;
  }

  .close-btn {
    background: none;
    border: none;
    color: #64748b;
    font-size: 1.1rem;
    cursor: pointer;
    &:hover { color: #f8fafc; }
  }
}

.modal-form {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;

  label {
    font-size: 0.82rem;
    color: #cbd5e1;
    font-weight: 600;

    &.required-label::after {
      content: ' *';
      color: #f43f5e;
    }
  }

  input, select, textarea {
    background: #0f172a;
    border: 1px solid rgba(255, 255, 255, 0.12);
    color: #f8fafc;
    padding: 0.6rem 0.75rem;
    border-radius: 0.5rem;
    font-family: inherit;
    font-size: 0.88rem;

    &:focus {
      outline: none;
      border-color: #6366f1;
    }
  }
}

.checkbox-container {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  cursor: pointer;
  font-size: 0.85rem;
  color: #cbd5e1;

  input { width: 1rem; height: 1rem; cursor: pointer; }
}

.modal-actions {
  display: flex;
  justify-content: flex-end;
  gap: 0.75rem;
  margin-top: 1rem;
  padding-top: 1rem;
  border-top: 1px solid rgba(255, 255, 255, 0.08);
}

.btn-primary {
  background: #6366f1;
  color: #fff;
  border: none;
  padding: 0.6rem 1.25rem;
  border-radius: 0.5rem;
  font-weight: 600;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 0.4rem;

  &:hover { background: #4f46e5; }
}

.btn-secondary {
  background: transparent;
  border: 1px solid rgba(255, 255, 255, 0.1);
  color: #94a3b8;
  padding: 0.6rem 1.1rem;
  border-radius: 0.5rem;
  font-weight: 600;
  cursor: pointer;

  &:hover { color: #f8fafc; background: rgba(255, 255, 255, 0.05); }
}

/* Feedback Management Grid */
.feedbacks-container {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.feedbacks-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(360px, 1fr));
  gap: 1rem;
}

.feedback-card {
  background: rgba(18, 24, 38, 0.7);
  border: 1px solid rgba(139, 92, 246, 0.25);
  border-radius: 12px;
  padding: 1.1rem 1.25rem;
  display: flex;
  flex-direction: column;
  gap: 0.6rem;
  transition: all 0.2s cubic-bezier(0.16, 1, 0.3, 1);
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.2);

  &:hover {
    border-color: rgba(168, 85, 247, 0.45);
    box-shadow: 0 8px 24px rgba(139, 92, 246, 0.15);
    transform: translate3d(0, -2px, 0);
  }
}

.feedback-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 0.5rem;
}

.fb-sender-group {
  display: flex;
  align-items: center;
  gap: 0.4rem;
  overflow: hidden;
}

.fb-sender {
  font-size: 0.85rem;
  font-weight: 600;
  color: #38bdf8;
  white-space: nowrap;
  text-overflow: ellipsis;
  overflow: hidden;
}

.fb-domain-badge {
  font-size: 0.7rem;
  font-family: monospace;
  background: rgba(56, 189, 248, 0.15);
  border: 1px solid rgba(56, 189, 248, 0.3);
  color: #7dd3fc;
  padding: 0.1rem 0.4rem;
  border-radius: 6px;
  white-space: nowrap;
}

.btn-delete-fb {
  background: transparent;
  border: none;
  color: #94a3b8;
  padding: 0.35rem 0.5rem;
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.15s;

  &:hover {
    background: rgba(239, 68, 68, 0.15);
    color: #f87171;
  }
}

.fb-subject {
  font-size: 0.9rem;
  font-weight: 600;
  color: #f1f5f9;
  line-height: 1.35;
}

.fb-snippet {
  font-size: 0.8rem;
  color: #94a3b8;
  font-style: italic;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
  line-height: 1.35;
}

.fb-footer {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
  margin-top: 0.25rem;
  padding-top: 0.6rem;
  border-top: 1px solid rgba(255, 255, 255, 0.06);
}

.fb-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 0.35rem;
}

.fb-tag-pill {
  font-size: 0.75rem;
  padding: 0.2rem 0.55rem;
  background: rgba(139, 92, 246, 0.15);
  border: 1px solid rgba(139, 92, 246, 0.3);
  color: #c084fc;
  border-radius: 12px;
  font-weight: 500;
}

.fb-reason {
  font-size: 0.82rem;
  color: #cbd5e1;
  display: flex;
  align-items: center;
  gap: 0.35rem;

  i {
    color: #a855f7;
    font-size: 0.8rem;
  }
}

.fb-date {
  font-size: 0.725rem;
  color: #64748b;
  display: flex;
  align-items: center;
  gap: 0.3rem;
}
</style>
