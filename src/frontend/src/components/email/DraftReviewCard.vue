<template>
  <div class="draft-card">
    <div class="email-info">
      <div class="sender">{{ draft.originalEmail?.from || 'Không rõ người gửi' }}</div>
      <div class="subject">{{ draft.originalEmail?.subject || '(Không có tiêu đề)' }}</div>
      <div class="snippet">{{ draft.originalEmail?.snippet || '' }}</div>
    </div>

    <div class="ai-generated">
      <div class="ai-header">
        <span>✨ AI Draft (Confidence: {{ Math.round(draft.confidenceScore * 100) }}%)</span>
      </div>
      <textarea v-model="editedContent" rows="4" class="content-editor"></textarea>
    </div>

    <div class="card-actions">
      <button class="approve-btn" @click="handleApprove">
        <i class="pi pi-check"></i> Phê duyệt & Gửi nháp
      </button>
      <button class="reject-btn" @click="handleReject">
        <i class="pi pi-times"></i> Từ chối
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';

const props = defineProps<{ draft: any }>();
const emit = defineEmits(['approve', 'reject']);

const editedContent = ref(props.draft.draftContent);

const handleApprove = () => {
  emit('approve', { id: props.draft.id, content: editedContent.value });
};

const handleReject = () => {
  emit('reject', { id: props.draft.id });
};
</script>

<style scoped lang="scss">
.draft-card {
  background: #1e293b;
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 1rem;
  padding: 1.5rem;
  margin-bottom: 1rem;
}

.sender { font-weight: 700; color: #818cf8; font-size: 0.9rem; }
.subject { font-size: 1.1rem; font-weight: 800; margin: 0.25rem 0; }
.snippet { color: #94a3b8; font-size: 0.85rem; margin-bottom: 1rem; }

.ai-generated {
  background: rgba(99, 102, 241, 0.05);
  border: 1px solid rgba(99, 102, 241, 0.2);
  border-radius: 0.75rem;
  padding: 1rem;
  margin-bottom: 1rem;
}

.ai-header {
  font-size: 0.8rem;
  font-weight: 600;
  color: #c084fc;
  margin-bottom: 0.5rem;
}

.content-editor {
  width: 100%;
  background: #0f172a;
  color: #f8fafc;
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 0.5rem;
  padding: 0.75rem;
  font-family: inherit;
  font-size: 0.9rem;
  resize: vertical;
}

.card-actions {
  display: flex;
  gap: 0.75rem;
}

button {
  padding: 0.5rem 1rem;
  border-radius: 0.5rem;
  font-weight: 600;
  cursor: pointer;
  border: none;
  display: flex;
  align-items: center;
  gap: 0.35rem;
  font-size: 0.85rem;
}

.approve-btn { background: #10b981; color: #fff; &:hover { background: #059669; } }
.reject-btn { background: rgba(239, 68, 68, 0.2); color: #fca5a5; &:hover { background: rgba(239, 68, 68, 0.4); } }
</style>
