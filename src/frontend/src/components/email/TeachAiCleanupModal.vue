<template>
  <div v-if="visible" class="modal-overlay" @click.self="$emit('close')">
    <div class="teach-modal-card">
      <div class="modal-header">
        <div class="header-icon-title">
          <div class="icon-badge">
            <i class="pi pi-sparkles"></i>
          </div>
          <div>
            <h3>Dọn dẹp & Huấn luyện AI</h3>
            <p class="header-subtitle">Dạy AI khẩu vị dọn dẹp để tự động tối ưu các đợt quét tới</p>
          </div>
        </div>
        <button class="btn-close" @click="$emit('close')">
          <i class="pi pi-times"></i>
        </button>
      </div>

      <div class="email-preview-box">
        <div class="preview-row">
          <span class="preview-label">Người gửi:</span>
          <span class="preview-val sender-val">{{ email?.from }}</span>
        </div>
        <div class="preview-row">
          <span class="preview-label">Tiêu đề:</span>
          <span class="preview-val subject-val">{{ email?.subject }}</span>
        </div>
        <div v-if="email?.snippet" class="preview-snippet">
          "{{ email?.snippet }}"
        </div>
      </div>

      <div class="modal-body">
        <div class="tags-group">
          <label class="section-title">
            <i class="pi pi-tags"></i> Chọn lý do nhanh (1 chạm):
          </label>
          <div class="preset-tags">
            <button
              v-for="tag in presetTags"
              :key="tag"
              type="button"
              class="preset-tag-chip"
              :class="{ active: selectedTags.includes(tag) }"
              @click="toggleTag(tag)"
            >
              <i class="pi" :class="selectedTags.includes(tag) ? 'pi-check-circle' : 'pi-plus'"></i>
              <span>{{ tag }}</span>
            </button>
          </div>
        </div>

        <div class="reason-group">
          <label class="section-title">
            <i class="pi pi-comment"></i> Ghi chú lý do cụ thể (tùy chọn):
          </label>
          <textarea
            v-model="customReason"
            rows="3"
            placeholder="Ví dụ: Tài khoản dịch vụ này không còn dùng, quảng cáo quá nhiều lần..."
            class="reason-textarea"
          ></textarea>
        </div>

        <div class="info-alert">
          <i class="pi pi-info-circle"></i>
          <div>
            Hệ thống sẽ <b>chuyển email này vào Thùng rác ngay</b> và nạp mẫu này vào ngữ cảnh huấn luyện (Few-shot learning) của AI cho lần dọn dẹp tiếp theo.
          </div>
        </div>
      </div>

      <div class="modal-actions">
        <button type="button" class="btn-cancel" @click="$emit('close')" :disabled="submitting">
          Hủy bỏ
        </button>
        <button
          type="button"
          class="btn-teach-submit"
          :disabled="submitting || (selectedTags.length === 0 && !customReason.trim())"
          @click="handleSubmit"
        >
          <i class="pi" :class="submitting ? 'pi-spin pi-spinner' : 'pi-trash'"></i>
          <span>{{ submitting ? 'Đang xử lý...' : 'Xóa & Huấn luyện AI' }}</span>
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import api from '@/services/api.service';
import { showToast } from '@/services/notification.service';

const props = defineProps<{
  email: any;
  visible: boolean;
}>();

const emit = defineEmits<{
  (e: 'close'): void;
  (e: 'submitted', emailId: string): void;
}>();

const presetTags = [
  'Quảng cáo / Khuyến mãi',
  'Bản tin không đọc',
  'Thông báo tự động vô ích',
  'Báo cáo / Cảnh báo định kỳ'
];

const selectedTags = ref<string[]>(['Quảng cáo / Khuyến mãi']);
const customReason = ref('');
const submitting = ref(false);

const toggleTag = (tag: string) => {
  const index = selectedTags.value.indexOf(tag);
  if (index >= 0) {
    selectedTags.value.splice(index, 1);
  } else {
    selectedTags.value.push(tag);
  }
};

const handleSubmit = async () => {
  if (!props.email?.id) return;
  submitting.value = true;
  try {
    const payload = {
      emailId: props.email.id,
      sender: props.email.from || '',
      subject: props.email.subject || '',
      snippet: props.email.snippet || '',
      reason: customReason.value.trim() || selectedTags.value.join(', '),
      tags: selectedTags.value
    };

    const res: any = await api.post('/emailops/cleanup/feedback', payload);
    if (res.success) {
      showToast({
        severity: 'success',
        summary: 'Dọn & Dạy AI',
        detail: 'Đã chuyển email vào thùng rác và lưu mẫu dạy AI thành công!'
      });
      emit('submitted', props.email.id);
      emit('close');
    } else {
      showToast({
        severity: 'error',
        summary: 'Lỗi',
        detail: res.message || 'Lỗi khi gửi phản hồi dạy AI.'
      });
    }
  } catch (err: any) {
    showToast({
      severity: 'error',
      summary: 'Lỗi kết nối',
      detail: err.message || 'Không thể kết nối đến máy chủ.'
    });
  } finally {
    submitting.value = false;
  }
};
</script>

<style scoped>
.modal-overlay {
  position: fixed;
  inset: 0;
  background: rgba(10, 15, 30, 0.75);
  backdrop-filter: blur(8px);
  z-index: 1100;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 1rem;
}

.teach-modal-card {
  width: 100%;
  max-width: 540px;
  background: rgba(18, 24, 38, 0.95);
  border: 1px solid rgba(139, 92, 246, 0.3);
  box-shadow: 0 20px 40px rgba(0, 0, 0, 0.6), 0 0 30px rgba(139, 92, 246, 0.15);
  border-radius: 16px;
  overflow: hidden;
  animation: modalScale 0.25s cubic-bezier(0.16, 1, 0.3, 1);
}

@keyframes modalScale {
  from {
    opacity: 0;
    transform: scale(0.95) translateY(10px);
  }
  to {
    opacity: 1;
    transform: scale(1) translateY(0);
  }
}

.modal-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 1.25rem 1.5rem;
  border-bottom: 1px solid rgba(255, 255, 255, 0.08);
  background: rgba(255, 255, 255, 0.02);
}

.header-icon-title {
  display: flex;
  align-items: center;
  gap: 0.85rem;
}

.icon-badge {
  width: 40px;
  height: 40px;
  border-radius: 10px;
  background: linear-gradient(135deg, rgba(139, 92, 246, 0.3), rgba(236, 72, 153, 0.3));
  border: 1px solid rgba(139, 92, 246, 0.4);
  color: #c084fc;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.2rem;
}

.modal-header h3 {
  margin: 0;
  font-size: 1.15rem;
  font-weight: 700;
  color: #f1f5f9;
}

.header-subtitle {
  margin: 0.2rem 0 0;
  font-size: 0.8rem;
  color: #94a3b8;
}

.btn-close {
  background: transparent;
  border: none;
  color: #94a3b8;
  font-size: 1.1rem;
  cursor: pointer;
  padding: 0.5rem;
  border-radius: 8px;
  transition: all 0.2s;
}

.btn-close:hover {
  background: rgba(255, 255, 255, 0.08);
  color: #f8fafc;
}

.email-preview-box {
  margin: 1.25rem 1.5rem 0.5rem;
  padding: 0.85rem 1rem;
  background: rgba(15, 23, 42, 0.6);
  border: 1px solid rgba(255, 255, 255, 0.06);
  border-radius: 10px;
}

.preview-row {
  display: flex;
  gap: 0.5rem;
  font-size: 0.85rem;
  margin-bottom: 0.35rem;
  line-height: 1.4;
}

.preview-label {
  color: #64748b;
  min-width: 65px;
}

.preview-val {
  color: #e2e8f0;
  font-weight: 500;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.sender-val {
  color: #38bdf8;
}

.preview-snippet {
  font-size: 0.8rem;
  color: #94a3b8;
  font-style: italic;
  margin-top: 0.4rem;
  line-height: 1.35;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.modal-body {
  padding: 1rem 1.5rem;
  display: flex;
  flex-direction: column;
  gap: 1.2rem;
}

.section-title {
  display: flex;
  align-items: center;
  gap: 0.45rem;
  font-size: 0.85rem;
  font-weight: 600;
  color: #cbd5e1;
  margin-bottom: 0.5rem;
}

.section-title i {
  color: #a855f7;
}

.preset-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}

.preset-tag-chip {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  padding: 0.45rem 0.85rem;
  border-radius: 20px;
  font-size: 0.82rem;
  cursor: pointer;
  background: rgba(30, 41, 59, 0.7);
  border: 1px solid rgba(148, 163, 184, 0.2);
  color: #94a3b8;
  transition: all 0.2s cubic-bezier(0.16, 1, 0.3, 1);
}

.preset-tag-chip:hover {
  background: rgba(139, 92, 246, 0.15);
  border-color: rgba(139, 92, 246, 0.4);
  color: #e2e8f0;
}

.preset-tag-chip.active {
  background: linear-gradient(135deg, rgba(139, 92, 246, 0.3), rgba(168, 85, 247, 0.25));
  border-color: #a855f7;
  color: #f1f5f9;
  box-shadow: 0 0 12px rgba(168, 85, 247, 0.3);
}

.reason-textarea {
  width: 100%;
  box-sizing: border-box;
  background: rgba(15, 23, 42, 0.7);
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 10px;
  padding: 0.75rem 1rem;
  color: #f1f5f9;
  font-size: 0.88rem;
  font-family: inherit;
  resize: vertical;
  transition: border-color 0.2s;
}

.reason-textarea:focus {
  outline: none;
  border-color: #a855f7;
  box-shadow: 0 0 0 2px rgba(168, 85, 247, 0.2);
}

.info-alert {
  display: flex;
  align-items: flex-start;
  gap: 0.65rem;
  padding: 0.75rem 0.9rem;
  background: rgba(14, 165, 233, 0.1);
  border: 1px solid rgba(14, 165, 233, 0.25);
  border-radius: 10px;
  font-size: 0.8rem;
  color: #7dd3fc;
  line-height: 1.4;
}

.info-alert i {
  font-size: 1rem;
  margin-top: 0.1rem;
  color: #38bdf8;
}

.modal-actions {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 0.75rem;
  padding: 1.1rem 1.5rem;
  border-top: 1px solid rgba(255, 255, 255, 0.08);
  background: rgba(255, 255, 255, 0.02);
}

.btn-cancel {
  padding: 0.6rem 1.2rem;
  background: transparent;
  border: 1px solid rgba(255, 255, 255, 0.15);
  border-radius: 8px;
  color: #94a3b8;
  font-size: 0.88rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-cancel:hover:not(:disabled) {
  background: rgba(255, 255, 255, 0.05);
  color: #f8fafc;
}

.btn-teach-submit {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.6rem 1.4rem;
  background: linear-gradient(135deg, #7c3aed, #9333ea);
  border: 1px solid rgba(192, 132, 252, 0.4);
  border-radius: 8px;
  color: #ffffff;
  font-size: 0.88rem;
  font-weight: 600;
  cursor: pointer;
  box-shadow: 0 4px 14px rgba(124, 58, 237, 0.35);
  transition: all 0.2s cubic-bezier(0.16, 1, 0.3, 1);
}

.btn-teach-submit:hover:not(:disabled) {
  background: linear-gradient(135deg, #6d28d9, #7e22ce);
  box-shadow: 0 6px 20px rgba(124, 58, 237, 0.5);
  transform: translateY(-1px);
}

.btn-teach-submit:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
</style>
