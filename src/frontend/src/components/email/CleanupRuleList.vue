<template>
  <div class="cleanup-rules">
    <div class="header-actions">
      <h2>Quy tắc dọn dẹp Email (UC01)</h2>
      <button class="primary-btn" @click="handleRunAll">
        <i class="pi pi-play"></i>
        <span>Chạy dọn dẹp ngay</span>
      </button>
    </div>

    <div v-if="loading" class="loading">Đang tải quy tắc...</div>

    <div v-else class="rules-grid">
      <div v-for="rule in rules" :key="rule.id" class="rule-card">
        <div class="rule-header">
          <span class="rule-name">{{ rule.ruleName }}</span>
          <span class="badge" :class="rule.action === 0 ? 'trash' : 'archive'">
            {{ rule.action === 0 ? 'Xóa tạm' : 'Lưu trữ' }}
          </span>
        </div>
        <div class="rule-details">
          <div><i class="pi pi-folder"></i> Danh mục: <strong>{{ rule.category }}</strong></div>
          <div><i class="pi pi-clock"></i> Cũ hơn: <strong>{{ rule.olderThanDays }} ngày</strong></div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import api from '@/services/api.service';

const rules = ref<any[]>([]);
const loading = ref(true);

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

onMounted(fetchRules);
</script>

<style scoped lang="scss">
.header-actions {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1.5rem;
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
}
</style>
