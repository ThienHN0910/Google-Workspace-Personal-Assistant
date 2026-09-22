<template>
  <div class="draft-card">
    <div class="email-info">
      <div class="sender-row">
        <span class="sender">{{ draft.originalEmail?.from || 'Không rõ người gửi' }}</span>
        <span class="confidence-badge">
          <i class="pi pi-sparkles"></i>
          AI Confidence {{ Math.round((draft.confidenceScore || 0) * 100) }}%
        </span>
      </div>
      <div class="subject">{{ draft.originalEmail?.subject || '(Không có tiêu đề)' }}</div>
      <div class="snippet">{{ draft.originalEmail?.snippet || '' }}</div>
    </div>

    <div class="ai-generated">
      <div class="ai-header">
        <span class="ai-tag">
          <span class="pulse-dot"></span>
          Phản hồi gợi ý tự động (Editable)
        </span>
      </div>
      <textarea v-model="editedContent" rows="3" class="content-editor" placeholder="Nội dung bản nháp..."></textarea>
    </div>

    <div class="card-actions">
      <button class="approve-btn" @click="handleApprove">
        <i class="pi pi-check"></i> Phê duyệt & Lưu nháp Gmail
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
  background: rgba(15, 23, 42, 0.72);
  border: 1px solid rgba(139, 92, 246, 0.25);
  border-top: 1px solid rgba(255, 255, 255, 0.12);
  border-radius: 0.85rem;
  padding: 1.15rem;
  backdrop-filter: blur(14px);
  -webkit-backdrop-filter: blur(14px);
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.3);
  transition: all 0.15s ease;

  &:hover {
    border-color: rgba(139, 92, 246, 0.45);
    box-shadow: 0 6px 20px rgba(0, 0, 0, 0.4), 0 0 16px rgba(139, 92, 246, 0.12);
  }
}

.email-info {
  margin-bottom: 0.85rem;

  .sender-row {
    display: flex;
    justify-content: space-between;
    align-items: center;
    flex-wrap: wrap;
    gap: 0.5rem;
    margin-bottom: 0.25rem;

    .sender {
      font-weight: 700;
      color: #38bdf8;
      font-size: 0.825rem;
    }

    .confidence-badge {
      display: inline-flex;
      align-items: center;
      gap: 0.35rem;
      padding: 0.2rem 0.6rem;
      background: rgba(139, 92, 246, 0.15);
      border: 1px solid rgba(139, 92, 246, 0.35);
      color: #c084fc;
      border-radius: 9999px;
      font-size: 0.725rem;
      font-weight: 700;
      letter-spacing: 0.02em;

      i { font-size: 0.75rem; }
    }
  }

  .subject {
    font-size: 0.95rem;
    font-weight: 700;
    color: #f8fafc;
    margin-bottom: 0.25rem;
  }

  .snippet {
    color: #94a3b8;
    font-size: 0.775rem;
    line-height: 1.4;
  }
}

.ai-generated {
  background: rgba(11, 17, 32, 0.7);
  border: 1px solid rgba(139, 92, 246, 0.2);
  border-radius: 0.65rem;
  padding: 0.85rem;
  margin-bottom: 0.85rem;

  .ai-header {
    display: flex;
    align-items: center;
    margin-bottom: 0.5rem;

    .ai-tag {
      display: inline-flex;
      align-items: center;
      gap: 0.4rem;
      font-size: 0.725rem;
      font-weight: 600;
      color: #a78bfa;

      .pulse-dot {
        width: 6px;
        height: 6px;
        border-radius: 50%;
        background: #a855f7;
        box-shadow: 0 0 6px #a855f7;
        animation: pulse-dot 2s infinite ease-in-out;
      }
    }
  }

  .content-editor {
    width: 100%;
    background: rgba(15, 23, 42, 0.8);
    color: #f1f5f9;
    border: 1px solid rgba(148, 163, 184, 0.15);
    border-radius: 0.45rem;
    padding: 0.65rem 0.75rem;
    font-family: inherit;
    font-size: 0.825rem;
    line-height: 1.45;
    resize: vertical;
    outline: none;
    transition: all 0.15s ease;

    &:focus {
      border-color: rgba(139, 92, 246, 0.45);
      box-shadow: 0 0 10px rgba(139, 92, 246, 0.15);
    }
  }
}

.card-actions {
  display: flex;
  align-items: center;
  gap: 0.55rem;
  flex-wrap: wrap;

  button {
    height: 36px;
    padding: 0 0.95rem;
    border-radius: 0.45rem;
    font-weight: 600;
    font-size: 0.8rem;
    cursor: pointer;
    display: inline-flex;
    align-items: center;
    gap: 0.4rem;
    transition: all 0.15s ease;
    border: 1px solid transparent;
  }

  .approve-btn {
    background: linear-gradient(135deg, #10b981 0%, #059669 100%);
    border-color: rgba(52, 211, 153, 0.3);
    color: #fff;
    box-shadow: 0 2px 8px rgba(16, 185, 129, 0.25);

    &:hover {
      filter: brightness(1.1);
      transform: translate3d(0, -1px, 0);
    }
  }

  .reject-btn {
    background: rgba(244, 63, 94, 0.12);
    border-color: rgba(244, 63, 94, 0.25);
    color: #fb7185;

    &:hover {
      background: #f43f5e;
      color: #fff;
      transform: translate3d(0, -1px, 0);
    }
  }
}
</style>
