<template>
  <div class="email-ops-page">
    <div class="tabs">
      <button :class="{ active: activeTab === 'drafts' }" @click="activeTab = 'drafts'">
        ✨ AI Drafts chờ duyệt ({{ drafts.length }})
      </button>
      <button :class="{ active: activeTab === 'rules' }" @click="activeTab = 'rules'">
        🧹 Quy tắc dọn Inbox
      </button>
    </div>

    <!-- Tab 1: AI Drafts -->
    <div v-if="activeTab === 'drafts'" class="tab-content">
      <div v-if="loading" class="loading">Đang tải bản nháp...</div>
      <div v-else-if="drafts.length === 0" class="empty-state">
        <i class="pi pi-check-circle"></i>
        <p>Không có bản nháp AI nào cần phê duyệt!</p>
      </div>
      <div v-else>
        <DraftReviewCard
          v-for="draft in drafts"
          :key="draft.id"
          :draft="draft"
          @approve="handleApprove"
          @reject="handleReject"
        />
      </div>
    </div>

    <!-- Tab 2: Cleanup Rules -->
    <div v-else class="tab-content">
      <CleanupRuleList />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import api from '@/services/api.service';
import DraftReviewCard from '@/components/email/DraftReviewCard.vue';
import CleanupRuleList from '@/components/email/CleanupRuleList.vue';

const activeTab = ref('drafts');
const drafts = ref<any[]>([]);
const loading = ref(true);

const fetchDrafts = async () => {
  loading.value = true;
  try {
    const res: any = await api.get('/emailops/drafts/pending');
    if (res.success && res.data) {
      drafts.value = res.data.items;
    }
  } catch (e) {
    console.error('Failed to fetch pending drafts:', e);
  } finally {
    loading.value = false;
  }
};

const handleApprove = async ({ id, content }: { id: string; content: string }) => {
  try {
    const res: any = await api.post(`/emailops/drafts/${id}/approve`, { customContent: content });
    if (res.success) {
      drafts.value = drafts.value.filter((d) => d.id !== id);
    }
  } catch (e) {
    alert('Lỗi phê duyệt bản nháp');
  }
};

const handleReject = async ({ id }: { id: string }) => {
  try {
    const res: any = await api.post(`/emailops/drafts/${id}/reject`, { reason: 'Từ chối bởi Admin' });
    if (res.success) {
      drafts.value = drafts.value.filter((d) => d.id !== id);
    }
  } catch (e) {
    alert('Lỗi từ chối bản nháp');
  }
};

onMounted(fetchDrafts);
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
</style>
