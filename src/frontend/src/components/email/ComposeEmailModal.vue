<template>
  <div class="modal-overlay" @click.self="$emit('close')">
    <div class="modal-content">
      <div class="modal-header">
        <div class="title-group">
          <h3><i class="pi pi-send"></i> Soạn Email Mới</h3>
          <p class="modal-subtitle">Gửi trực tiếp từ tài khoản Google cá nhân của bạn</p>
        </div>
        <button class="close-btn" @click="$emit('close')"><i class="pi pi-times"></i></button>
      </div>

      <!-- AI Assistant Bar -->
      <div class="ai-assist-box">
        <div class="ai-box-header" @click="showAiPrompt = !showAiPrompt">
          <span class="ai-badge"><i class="pi pi-sparkles"></i> AI Writing Assistant</span>
          <span class="toggle-hint">{{ showAiPrompt ? 'Ẩn' : 'Bật trợ lý viết nháp' }}</span>
        </div>
        <div v-if="showAiPrompt" class="ai-input-group mt-2">
          <input
            v-model="aiPrompt"
            type="text"
            placeholder="Ví dụ: Soạn email xin nghỉ phép 1 ngày gửi anh Minh quản lý..."
            @keyup.enter="handleAiGenerate"
          />
          <button class="btn-ai" @click="handleAiGenerate" :disabled="generatingAi || !aiPrompt.trim()">
            <i class="pi" :class="generatingAi ? 'pi-spin pi-spinner' : 'pi-sparkles'"></i>
            {{ generatingAi ? 'Đang viết...' : 'AI Viết hộ' }}
          </button>
        </div>
      </div>

      <form @submit.prevent="handleSend" class="compose-form">
        <div class="form-group">
          <div class="field-label-row">
            <label>Người nhận (To) <span class="required">*</span></label>
            <div class="cc-bcc-toggles">
              <button type="button" class="btn-toggle-field" :class="{ active: showCc }" @click="showCc = !showCc">Cc</button>
              <button type="button" class="btn-toggle-field" :class="{ active: showBcc }" @click="showBcc = !showBcc">Bcc</button>
            </div>
          </div>
          <input
            v-model="form.to"
            type="email"
            placeholder="nguoinhan@example.com"
            required
          />
        </div>

        <div v-if="showCc" class="form-group">
          <label>Cc (Đồng kính gửi)</label>
          <input
            v-model="form.cc"
            type="text"
            placeholder="cc1@example.com, cc2@example.com"
          />
        </div>

        <div v-if="showBcc" class="form-group">
          <label>Bcc (Kính gửi ẩn danh)</label>
          <input
            v-model="form.bcc"
            type="text"
            placeholder="bcc1@example.com, bcc2@example.com"
          />
        </div>

        <div class="form-group">
          <label>Tiêu đề (Subject) <span class="required">*</span></label>
          <input
            v-model="form.subject"
            type="text"
            placeholder="Nhập tiêu đề email..."
            required
          />
        </div>

        <div class="form-group">
          <label>Nội dung thư</label>
          <Editor
            v-model="form.body"
            editorStyle="height: 220px"
            placeholder="Nội dung thư của bạn..."
          />
        </div>

        <div class="modal-actions">
          <button type="button" class="btn-cancel" @click="$emit('close')">Hủy bỏ</button>
          <button type="submit" class="btn-send" :disabled="sending || !form.to || !form.subject">
            <i class="pi" :class="sending ? 'pi-spin pi-spinner' : 'pi-send'"></i>
            {{ sending ? 'Đang gửi...' : 'Gửi Email Ngay' }}
          </button>
        </div>
      </form>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, defineAsyncComponent } from 'vue';
import api from '@/services/api.service';
import { showToast } from '@/services/notification.service';

const Editor = defineAsyncComponent(() => import('primevue/editor'));

const emit = defineEmits(['close', 'sent']);

const form = ref({
  to: '',
  cc: '',
  bcc: '',
  subject: '',
  body: '',
});

const showCc = ref(false);
const showBcc = ref(false);
const showAiPrompt = ref(true);
const aiPrompt = ref('');
const generatingAi = ref(false);
const sending = ref(false);

const handleAiGenerate = async () => {
  if (!aiPrompt.value.trim() || generatingAi.value) return;

  generatingAi.value = true;
  try {
    const res: any = await api.post('/EmailOps/compose-ai', {
      Prompt: aiPrompt.value,
      RecipientHint: form.value.to || undefined,
    });

    if (res.success && res.data) {
      form.value.body = res.data.draftContent || '';
      showToast({
        severity: 'success',
        summary: 'AI Hoàn thành',
        detail: 'Đã sinh nội dung email thành công. Bạn có thể chỉnh sửa lại trước khi gửi.',
      });

      // Kiểm tra quota để thông báo cho người dùng
      try {
        const quotaRes: any = await api.get('/settings/ai-usage');
        if (quotaRes.success && quotaRes.data) {
          const d = quotaRes.data;
          if (d.quotaExceeded) {
            showToast({
              severity: 'warn',
              summary: 'Lưu ý Hạn ngạch AI',
              detail: `Đã dùng ${d.totalTokens.toLocaleString()} / ${d.monthlyQuotaLimit.toLocaleString()} token (vượt hạn mức ngầm). Thao tác tay vẫn được ưu tiên phục vụ.`,
            });
          } else if (d.usagePercentage >= 80) {
            showToast({
              severity: 'info',
              summary: 'Lượng Token còn lại',
              detail: `Còn lại ${d.remainingTokens.toLocaleString()} token trong tháng (${d.usagePercentage}% đã dùng).`,
            });
          }
        }
      } catch {
        // bỏ qua nếu không lấy được quota
      }
    } else {
      showToast({
        severity: 'warn',
        summary: 'Không tạo được nháp',
        detail: res.message || 'Vui lòng thử lại với prompt cụ thể hơn.',
      });
    }
  } catch (err: any) {
    console.error('AI generation failed:', err);
    showToast({
      severity: 'error',
      summary: 'Lỗi AI',
      detail: err.message || 'Lỗi khi kết nối với Gemini AI.',
    });
  } finally {
    generatingAi.value = false;
  }
};

const handleSend = async () => {
  if (sending.value || !form.value.to) return;

  sending.value = true;
  try {
    const res: any = await api.post('/EmailOps/send', {
      To: form.value.to,
      Cc: form.value.cc.trim() || undefined,
      Bcc: form.value.bcc.trim() || undefined,
      Subject: form.value.subject,
      Body: form.value.body,
    });

    if (res.success) {
      showToast({
        severity: 'success',
        summary: 'Đã gửi thư',
        detail: `Email đã được gửi thành công tới ${form.value.to}`,
      });
      emit('sent');
      emit('close');
    } else {
      showToast({
        severity: 'error',
        summary: 'Lỗi gửi thư',
        detail: res.message || 'Không thể gửi email qua Gmail API.',
      });
    }
  } catch (err: any) {
    console.error('Failed to send email:', err);
    showToast({
      severity: 'error',
      summary: 'Lỗi hệ thống',
      detail: err.message || 'Có lỗi xảy ra trong quá trình gửi.',
    });
  } finally {
    sending.value = false;
  }
};
</script>

<style scoped lang="scss">
.modal-overlay {
  position: fixed;
  inset: 0;
  background: rgba(15, 23, 42, 0.75);
  backdrop-filter: blur(4px);
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
  width: 100%;
  max-width: 720px;
  max-height: 90vh;
  overflow-y: auto;
  box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.5);
  padding: 1.75rem;
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 1.25rem;
  padding-bottom: 0.75rem;
  border-bottom: 1px solid rgba(255, 255, 255, 0.08);

  h3 {
    font-size: 1.25rem;
    font-weight: 700;
    color: #f8fafc;
    display: flex;
    align-items: center;
    gap: 0.5rem;
    margin: 0;

    i { color: #818cf8; }
  }

  .modal-subtitle {
    font-size: 0.825rem;
    color: #94a3b8;
    margin: 0.25rem 0 0 0;
  }

  .close-btn {
    background: none;
    border: none;
    color: #94a3b8;
    font-size: 1.1rem;
    cursor: pointer;
    padding: 0.25rem;
    &:hover { color: #f8fafc; }
  }
}

.ai-assist-box {
  background: linear-gradient(135deg, rgba(99, 102, 241, 0.12), rgba(168, 85, 247, 0.12));
  border: 1px solid rgba(168, 85, 247, 0.25);
  border-radius: 0.75rem;
  padding: 0.875rem 1rem;
  margin-bottom: 1.25rem;

  .ai-box-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    cursor: pointer;
  }

  .ai-badge {
    font-size: 0.85rem;
    font-weight: 700;
    color: #c084fc;
    display: flex;
    align-items: center;
    gap: 0.4rem;
  }

  .toggle-hint {
    font-size: 0.75rem;
    color: #94a3b8;
  }

  .ai-input-group {
    display: flex;
    gap: 0.5rem;
    margin-top: 0.6rem;

    input {
      flex: 1;
      background: #0f172a;
      border: 1px solid rgba(255, 255, 255, 0.12);
      border-radius: 0.5rem;
      padding: 0.6rem 0.875rem;
      color: #f8fafc;
      font-size: 0.85rem;

      &:focus {
        outline: none;
        border-color: #a855f7;
      }
    }

    .btn-ai {
      background: linear-gradient(135deg, #6366f1, #a855f7);
      color: #fff;
      border: none;
      border-radius: 0.5rem;
      padding: 0.6rem 1rem;
      font-weight: 600;
      font-size: 0.85rem;
      cursor: pointer;
      display: flex;
      align-items: center;
      gap: 0.35rem;
      white-space: nowrap;

      &:disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }
      &:hover:not(:disabled) {
        filter: brightness(1.1);
      }
    }
  }
}

.compose-form {
  display: flex;
  flex-direction: column;
  gap: 1rem;

  .form-group {
    display: flex;
    flex-direction: column;
    gap: 0.35rem;

    .field-label-row {
      display: flex;
      justify-content: space-between;
      align-items: center;

      .cc-bcc-toggles {
        display: flex;
        gap: 0.35rem;

        .btn-toggle-field {
          background: rgba(255, 255, 255, 0.06);
          border: 1px solid rgba(255, 255, 255, 0.12);
          color: #94a3b8;
          font-size: 0.75rem;
          font-weight: 700;
          padding: 0.15rem 0.45rem;
          border-radius: 0.3rem;
          cursor: pointer;
          transition: all 0.15s;

          &:hover { color: #f8fafc; border-color: rgba(255, 255, 255, 0.25); }
          &.active {
            background: rgba(99, 102, 241, 0.2);
            border-color: #6366f1;
            color: #818cf8;
          }
        }
      }
    }

    label {
      font-size: 0.85rem;
      font-weight: 600;
      color: #cbd5e1;

      .required { color: #f87171; }
    }

    input {
      background: #0f172a;
      border: 1px solid rgba(255, 255, 255, 0.1);
      border-radius: 0.5rem;
      padding: 0.65rem 0.875rem;
      color: #f8fafc;
      font-size: 0.9rem;

      &:focus {
        outline: none;
        border-color: #6366f1;
      }
    }
  }

  .modal-actions {
    display: flex;
    justify-content: flex-end;
    gap: 0.75rem;
    margin-top: 1rem;
    padding-top: 1rem;
    border-top: 1px solid rgba(255, 255, 255, 0.08);

    button {
      padding: 0.65rem 1.25rem;
      border-radius: 0.5rem;
      font-size: 0.9rem;
      font-weight: 600;
      cursor: pointer;
      display: flex;
      align-items: center;
      gap: 0.4rem;
      border: none;
    }

    .btn-cancel {
      background: rgba(255, 255, 255, 0.08);
      color: #cbd5e1;
      &:hover { background: rgba(255, 255, 255, 0.14); }
    }

    .btn-send {
      background: #10b981;
      color: #fff;
      &:hover:not(:disabled) { background: #059669; }
      &:disabled { opacity: 0.5; cursor: not-allowed; }
    }
  }
}
</style>
