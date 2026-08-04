<template>
  <div class="cleanup-rules">
    <div class="header-actions">
      <h2>Quy tắc dọn dẹp Email (UC01)</h2>
      <div class="header-btns">
        <button class="secondary-btn" @click="openCreateModal">
          <i class="pi pi-plus"></i> Tạo quy tắc
        </button>
        <button class="primary-btn" @click="handleRunAll">
          <i class="pi pi-play"></i> Chạy dọn dẹp
        </button>
      </div>
    </div>

    <div v-if="loading" class="loading">Đang tải quy tắc...</div>

    <div v-else class="rules-grid">
      <div v-for="rule in rules" :key="rule.id" class="rule-card" :class="{ 'inactive': !rule.isActive }">
        <div class="rule-header">
          <span class="rule-name">{{ rule.ruleName }}</span>
          <span class="badge" :class="rule.action === 0 ? 'trash' : 'archive'">
            {{ rule.action === 0 ? 'Xóa tạm' : 'Lưu trữ' }}
          </span>
        </div>
        <div class="rule-details">
          <div v-if="rule.useAI"><i class="pi pi-sparkles"></i> AI: <strong>{{ rule.aiPrompt }}</strong></div>
          <div v-else-if="rule.customQuery"><i class="pi pi-search"></i> Truy vấn: <strong>{{ rule.customQuery }}</strong></div>
          <div v-else>
            <span v-if="rule.subjectRegex"><i class="pi pi-align-left"></i> Tiêu đề: <strong>{{ rule.subjectRegex }}</strong><br></span>
            <span v-if="rule.bodyRegex"><i class="pi pi-file"></i> Nội dung: <strong>{{ rule.bodyRegex }}</strong></span>
          </div>
        </div>
        <div class="rule-actions">
          <button class="action-btn" :class="rule.isActive ? 'text-green' : 'text-gray'" @click="handleToggle(rule.id)" :title="rule.isActive ? 'Tắt quy tắc' : 'Bật quy tắc'">
            <i class="pi" :class="rule.isActive ? 'pi-check-circle' : 'pi-minus-circle'"></i>
          </button>
          <button class="action-btn text-blue" @click="openEditModal(rule)" title="Sửa">
            <i class="pi pi-pencil"></i>
          </button>
          <button class="action-btn text-red" @click="handleDelete(rule.id)" title="Xóa">
            <i class="pi pi-trash"></i>
          </button>
        </div>
      </div>
    </div>

    <!-- Create/Edit Modal -->
    <div v-if="showModal" class="modal-overlay" @click.self="closeModal">
      <div class="modal-content">
        <h3>{{ isEditing ? 'Sửa quy tắc' : 'Tạo quy tắc mới' }}</h3>
        <form @submit.prevent="handleSubmit">

          <div class="form-group">
            <label>Hành động</label>
            <select v-model.number="formData.action" required>
              <option :value="0">Xóa vào thùng rác (Trash)</option>
              <option :value="1">Lưu trữ (Archive)</option>
            </select>
          </div>

          <div class="form-group-checkbox">
            <label>
              <input type="checkbox" v-model="formData.useAI" />
              Sử dụng AI để quyết định (Giới hạn 10 calls/phút)
            </label>
          </div>

          <div v-if="formData.useAI" class="form-group">
            <label>Prompt AI (Điều kiện xóa)</label>
            <textarea v-model="formData.aiPrompt" rows="3" placeholder="Ví dụ: Email này là quảng cáo khóa học hoặc giảm giá"></textarea>
          </div>

          <div v-if="!formData.useAI">
            <div class="form-group">
              <label>Regex Tiêu đề (Tùy chọn)</label>
              <input v-model="formData.subjectRegex" placeholder="Ví dụ: ^\[Quảng cáo\]" />
            </div>
            <div class="form-group">
              <label>Regex Nội dung (Tùy chọn)</label>
              <input v-model="formData.bodyRegex" placeholder="Ví dụ: unsubscribe|hủy đăng ký" />
            </div>
          </div>

          <div class="modal-actions">
            <button type="button" class="btn-cancel" @click="closeModal">Hủy</button>
            <button type="submit" class="btn-submit">Lưu</button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import api from '@/services/api.service';

const rules = ref<any[]>([]);
const loading = ref(true);

const showModal = ref(false);
const isEditing = ref(false);
const currentEditId = ref('');
const formData = ref({
  ruleName: '',
  action: 0,
  whitelistDomains: [],
  customQuery: '',
  useAI: false,
  aiPrompt: '',
  subjectRegex: '',
  bodyRegex: ''
});

const fetchRules = async () => {
  loading.value = true;
  try {
    const res: any = await api.get('/emailops/rules');
    if (res.success) {
      rules.value = res.data;
    }
  } catch (e) {
    console.error('Failed to fetch rules:', e);
  } finally {
    loading.value = false;
  }
};

const handleRunAll = async () => {
  try {
    const res: any = await api.post('/emailops/rules/run', {});
    if (res.success) {
      alert(`Đã thực thi! Xóa: ${res.data.totalTrashed}, Lưu trữ: ${res.data.totalArchived}`);
    }
  } catch (e) {
    alert('Lỗi thực thi quy tắc dọn dẹp');
  }
};

const openCreateModal = () => {
  isEditing.value = false;
  formData.value = { 
    ruleName: '', category: 'promotions', olderThanDays: 7, action: 0, 
    whitelistDomains: [], customQuery: '', useAI: false, aiPrompt: '', 
    subjectRegex: '', bodyRegex: '' 
  };
  showModal.value = true;
};

const openEditModal = (rule: any) => {
  isEditing.value = true;
  currentEditId.value = rule.id;
  formData.value = { ...rule };
  showModal.value = true;
};

const closeModal = () => {
  showModal.value = false;
};

const handleSubmit = async () => {
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
  }
};

const handleDelete = async (id: string) => {
  if (confirm('Bạn có chắc chắn muốn xóa quy tắc này?')) {
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
    fetchRules();
  } catch (e) {
    alert('Lỗi khi chuyển trạng thái quy tắc');
  }
};

onMounted(fetchRules);
</script>

<style scoped lang="scss">
.header-actions {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1.5rem;
}

.header-btns {
  display: flex;
  gap: 0.75rem;
}

.primary-btn {
  background: #6366f1;
  color: #fff;
  border: none;
  padding: 0.625rem 1.25rem;
  border-radius: 0.5rem;
  font-weight: 600;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 0.5rem;
  &:hover { background: #4f46e5; }
}

.secondary-btn {
  background: rgba(255, 255, 255, 0.1);
  color: #f8fafc;
  border: 1px solid rgba(255, 255, 255, 0.2);
  padding: 0.625rem 1.25rem;
  border-radius: 0.5rem;
  font-weight: 600;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 0.5rem;
  &:hover { background: rgba(255, 255, 255, 0.15); }
}

.rules-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: 1rem;
}

.rule-card {
  background: #1e293b;
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 0.75rem;
  padding: 1.25rem;
  transition: opacity 0.3s;
  &.inactive {
    opacity: 0.6;
  }
}

.rule-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 0.75rem;
}

.rule-name { font-weight: 700; font-size: 1rem; }

.badge {
  font-size: 0.75rem;
  padding: 0.25rem 0.5rem;
  border-radius: 0.25rem;
  font-weight: 600;
  &.trash { background: rgba(239, 68, 68, 0.2); color: #fca5a5; }
  &.archive { background: rgba(59, 130, 246, 0.2); color: #93c5fd; }
}

.rule-details {
  font-size: 0.85rem;
  color: #94a3b8;
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
  margin-bottom: 1rem;
}

.rule-actions {
  display: flex;
  gap: 0.5rem;
  border-top: 1px solid rgba(255, 255, 255, 0.1);
  padding-top: 0.75rem;
  justify-content: flex-end;
}

.action-btn {
  background: none;
  border: none;
  cursor: pointer;
  padding: 0.25rem 0.5rem;
  border-radius: 0.25rem;
  font-size: 1.1rem;
  transition: background 0.2s;
  &:hover { background: rgba(255, 255, 255, 0.1); }
  &.text-green { color: #34d399; }
  &.text-blue { color: #60a5fa; }
  &.text-red { color: #f87171; }
  &.text-gray { color: #94a3b8; }
}

/* Modal Styles */
.modal-overlay {
  position: fixed;
  top: 0; left: 0; right: 0; bottom: 0;
  background: rgba(0, 0, 0, 0.6);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.modal-content {
  background: #1e293b;
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 1rem;
  padding: 2rem;
  width: 100%;
  max-width: 400px;
}

.modal-content h3 {
  margin-top: 0;
  margin-bottom: 1.5rem;
}

.form-group {
  margin-bottom: 1rem;
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
  
  label {
    font-size: 0.85rem;
    color: #cbd5e1;
  }
  
    input, select, textarea {
    background: #0f172a;
    border: 1px solid rgba(255, 255, 255, 0.15);
    color: #f8fafc;
    padding: 0.5rem;
    border-radius: 0.35rem;
    font-family: inherit;
  }
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
    input { width: 1.1rem; height: 1.1rem; cursor: pointer; }
  }
}

.modal-actions {
  display: flex;
  justify-content: flex-end;
  gap: 0.75rem;
  margin-top: 1.5rem;
}

.btn-cancel {
  background: transparent;
  border: none;
  color: #94a3b8;
  cursor: pointer;
  padding: 0.5rem 1rem;
  &:hover { color: #f8fafc; }
}

.btn-submit {
  background: #6366f1;
  color: #fff;
  border: none;
  padding: 0.5rem 1.25rem;
  border-radius: 0.35rem;
  font-weight: 600;
  cursor: pointer;
  &:hover { background: #4f46e5; }
}
</style>
