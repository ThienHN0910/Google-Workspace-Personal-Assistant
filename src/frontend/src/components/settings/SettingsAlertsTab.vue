<template>
  <div class="tab-panel">
    <div class="card-box">
      <div class="box-header">
        <h3><i class="pi pi-send"></i> Kênh Telegram Bot</h3>
        <label class="switch-toggle">
          <input type="checkbox" v-model="form.enableTelegram" />
          <span class="slider"></span>
        </label>
      </div>
      <p class="box-desc">
        Nhận thông báo khẩn cấp, phát hiện file nguy hiểm Drive Guard, biến động số dư ngân hàng và báo cáo dọn dẹp trực tiếp qua tin nhắn Telegram.
      </p>

      <div class="settings-grid" :class="{ 'disabled-section': !form.enableTelegram }">
        <div class="setting-item">
          <label>
            <span>Telegram Bot Token</span>
            <span class="field-hint">Token do @BotFather cấp (e.g. 871026...:AAH...)</span>
          </label>
          <input type="password" v-model="form.telegramBotToken" placeholder="Nhập Telegram Bot Token..." />
        </div>

        <div class="setting-item">
          <label>
            <span>Telegram Chat ID</span>
            <span class="field-hint">ID chat của bạn hoặc ID nhóm (e.g. 5772252848)</span>
          </label>
          <input type="text" v-model="form.telegramChatId" placeholder="Nhập Telegram Chat ID..." />
        </div>

        <div class="setting-item full-width test-row">
          <button
            class="btn-test"
            @click="testTelegram"
            :disabled="testingTelegram || !form.telegramBotToken || !form.telegramChatId"
          >
            <i class="pi" :class="testingTelegram ? 'pi-spin pi-spinner' : 'pi-telegram'"></i>
            {{ testingTelegram ? 'Đang gửi ping...' : 'Gửi tin nhắn thử nghiệm (Test Ping)' }}
          </button>
          <span class="test-hint">Thử nghiệm gửi tin nhắn tức thì để kiểm tra kết nối với Bot.</span>
        </div>
      </div>
    </div>

    <div class="card-box mt-3">
      <div class="box-header">
        <h3><i class="pi pi-discord"></i> Kênh Discord Webhook</h3>
        <label class="switch-toggle">
          <input type="checkbox" v-model="form.enableDiscord" />
          <span class="slider"></span>
        </label>
      </div>
      <p class="box-desc">
        Tự động đẩy thông báo kèm màu sắc phân loại (Embeds) về kênh Discord qua Webhook.
      </p>

      <div class="settings-grid" :class="{ 'disabled-section': !form.enableDiscord }">
        <div class="setting-item full-width">
          <label>
            <span>Discord Webhook URL</span>
            <span class="field-hint">Đường link webhook được tạo từ Channel Settings trong Discord</span>
          </label>
          <input type="text" v-model="form.discordWebhookUrl" placeholder="https://discord.com/api/webhooks/..." />
        </div>
      </div>
    </div>

    <div class="section-footer-actions">
      <span v-if="isDirty" class="dirty-notice">
        <i class="pi pi-exclamation-circle"></i> Kênh thông báo có thay đổi chưa lưu.
      </span>
      <button
        class="btn-save-section-bottom"
        @click="emit('save')"
        :disabled="savingCurrentSection || !isDirty"
      >
        <i class="pi" :class="savingCurrentSection ? 'pi-spin pi-spinner' : 'pi-save'"></i>
        {{ savingCurrentSection ? 'Đang lưu...' : 'Lưu cấu hình Kênh Thông báo' }}
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import api from '@/services/api.service';
import { showToast } from '@/services/notification.service';

export interface AlertsSettingsForm {
  enableTelegram: boolean;
  telegramBotToken: string;
  telegramChatId: string;
  enableDiscord: boolean;
  discordWebhookUrl: string;
}

const props = defineProps<{
  form: AlertsSettingsForm;
  savingCurrentSection: boolean;
  isDirty: boolean;
}>();

const emit = defineEmits<{
  (e: 'save'): void;
  (e: 'update:dirty', isDirty: boolean): void;
}>();

watch(
  () => props.isDirty,
  (val) => {
    emit('update:dirty', val);
  },
  { immediate: true }
);

const testingTelegram = ref(false);

const testTelegram = async () => {
  if (!props.form.telegramBotToken || !props.form.telegramChatId) {
    showToast({
      severity: 'warn',
      summary: 'Thiếu thông tin',
      detail: 'Vui lòng nhập Bot Token và Chat ID trước khi thử nghiệm.',
    });
    return;
  }

  testingTelegram.value = true;
  try {
    const res: any = await api.post('/settings/test-telegram', {
      botToken: props.form.telegramBotToken,
      chatId: props.form.telegramChatId,
    });

    if (res.success) {
      showToast({
        severity: 'success',
        summary: 'Gửi thành công',
        detail: 'Đã gửi tin nhắn thử nghiệm tới Telegram của bạn! Hãy mở ứng dụng kiểm tra.',
      });
    } else {
      showToast({
        severity: 'error',
        summary: 'Lỗi kết nối',
        detail: res.message || 'Không thể gửi tin nhắn Telegram.',
      });
    }
  } catch (e: any) {
    showToast({
      severity: 'error',
      summary: 'Lỗi Telegram',
      detail: e.message || 'Lỗi khi gửi ping thử nghiệm tới Telegram API.',
    });
  } finally {
    testingTelegram.value = false;
  }
};
</script>

<style scoped lang="scss">
.card-box {
  background: linear-gradient(145deg, rgba(30, 41, 59, 0.72) 0%, rgba(15, 23, 42, 0.88) 100%);
  border: 1px solid rgba(148, 163, 184, 0.12);
  border-top: 1px solid rgba(255, 255, 255, 0.16);
  border-radius: 0.875rem;
  padding: 1.2rem;
  backdrop-filter: blur(16px);
  box-shadow: 0 12px 30px -6px rgba(0, 0, 0, 0.4), 0 0 16px rgba(99, 102, 241, 0.03);
  transition: border-color 0.2s ease;

  &:hover {
    border-color: rgba(148, 163, 184, 0.22);
  }

  &.mt-3 { margin-top: 1rem; }

  .box-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 0.35rem;

    h3 {
      font-size: 1.05rem;
      font-weight: 700;
      color: #f8fafc;
      margin: 0;
      display: flex;
      align-items: center;
      gap: 0.5rem;

      i { color: #818cf8; }
    }
  }

  .box-desc {
    font-size: 0.8rem;
    color: #94a3b8;
    line-height: 1.5;
    margin: 0 0 1rem 0;
  }
}

.switch-toggle {
  position: relative;
  display: inline-block;
  width: 42px;
  height: 22px;

  input {
    opacity: 0;
    width: 0;
    height: 0;
  }

  .slider {
    position: absolute;
    cursor: pointer;
    inset: 0;
    background-color: rgba(255, 255, 255, 0.15);
    border-radius: 22px;
    transition: background-color 0.2s cubic-bezier(0.4, 0, 0.2, 1);

    &:before {
      position: absolute;
      content: "";
      height: 16px;
      width: 16px;
      left: 3px;
      bottom: 3px;
      background-color: #fff;
      border-radius: 50%;
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.3);
      transition: transform 0.2s cubic-bezier(0.4, 0, 0.2, 1);
    }
  }

  input:checked + .slider {
    background-color: #10b981;
    box-shadow: 0 0 10px rgba(16, 185, 129, 0.4);
  }

  input:checked + .slider:before {
    transform: translate3d(20px, 0, 0);
  }
}

.settings-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 0.95rem;

  &.disabled-section {
    opacity: 0.45;
    pointer-events: none;
  }

  .setting-item {
    display: flex;
    flex-direction: column;
    gap: 0.35rem;

    &.full-width { grid-column: span 2; }

    label {
      font-size: 0.8rem;
      font-weight: 600;
      color: #cbd5e1;
      display: flex;
      flex-direction: column;
      gap: 0.15rem;

      .field-hint {
        font-size: 0.725rem;
        color: #64748b;
        font-weight: 400;
      }
    }

    input {
      height: 38px;
      background: rgba(11, 17, 32, 0.85);
      border: 1px solid rgba(148, 163, 184, 0.2);
      border-radius: 0.5rem;
      padding: 0.5rem 0.75rem;
      color: #f8fafc;
      font-size: 0.85rem;
      transition: border-color 0.15s ease, box-shadow 0.15s ease, background 0.15s ease;

      &:focus {
        outline: none;
        border-color: #6366f1;
        background: rgba(15, 23, 42, 0.95);
        box-shadow: 0 0 0 2px rgba(99, 102, 241, 0.25);
      }
    }
  }

  .test-row {
    display: flex;
    align-items: center;
    gap: 0.85rem;
    padding-top: 0.25rem;

    .btn-test {
      height: 36px;
      background: rgba(99, 102, 241, 0.16);
      border: 1px solid rgba(99, 102, 241, 0.35);
      color: #a5b4fc;
      padding: 0 1rem;
      border-radius: 0.5rem;
      font-size: 0.825rem;
      font-weight: 600;
      cursor: pointer;
      display: flex;
      align-items: center;
      gap: 0.45rem;
      white-space: nowrap;
      transition: all 0.15s ease;

      &:hover:not(:disabled) {
        background: #6366f1;
        color: #fff;
        box-shadow: 0 0 12px rgba(99, 102, 241, 0.35);
        transform: translate3d(0, -1px, 0);
      }

      &:disabled {
        opacity: 0.45;
        cursor: not-allowed;
      }
    }

    .test-hint {
      font-size: 0.75rem;
      color: #94a3b8;
    }
  }
}

.section-footer-actions {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 0.75rem;
  margin-top: 1.25rem;
  padding: 0.85rem 1rem;
  background: rgba(11, 17, 32, 0.7);
  border: 1px solid rgba(148, 163, 184, 0.12);
  border-radius: 0.75rem;
  backdrop-filter: blur(8px);

  .dirty-notice {
    font-size: 0.8rem;
    color: #fbbf24;
    display: flex;
    align-items: center;
    gap: 0.4rem;
    font-weight: 500;

    i { font-size: 0.95rem; }
  }

  .btn-save-section-bottom {
    margin-left: auto;
    height: 38px;
    padding: 0 1.25rem;
    border-radius: 0.5rem;
    font-weight: 600;
    font-size: 0.825rem;
    cursor: pointer;
    display: flex;
    align-items: center;
    gap: 0.45rem;
    background: linear-gradient(135deg, #10b981 0%, #059669 100%);
    border: 1px solid rgba(52, 211, 153, 0.3);
    color: #fff;
    box-shadow: 0 2px 10px rgba(16, 185, 129, 0.25);
    transition: all 0.15s ease;

    &:hover:not(:disabled) {
      filter: brightness(1.1);
      box-shadow: 0 4px 16px rgba(16, 185, 129, 0.4);
      transform: translate3d(0, -1.5px, 0);
    }

    &:disabled {
      opacity: 0.4;
      cursor: not-allowed;
      background: #334155;
      box-shadow: none;
      border-color: transparent;
    }
  }
}

@media (max-width: 860px) {
  .settings-grid {
    grid-template-columns: 1fr;
    gap: 0.85rem;

    .setting-item.full-width {
      grid-column: span 1;
    }
  }
}

@media (max-width: 640px) {
  .card-box {
    padding: 1rem;
    border-radius: 0.75rem;
  }

  .section-footer-actions {
    flex-direction: column;
    align-items: stretch;
    gap: 0.6rem;

    .dirty-notice {
      justify-content: center;
      text-align: center;
    }

    .btn-save-section-bottom {
      margin-left: 0;
      width: 100%;
      justify-content: center;
    }
  }
}
</style>
