<template>
  <div class="tab-panel">
    <div class="card-box">
      <div class="box-header">
        <h3><i class="pi pi-sync"></i> Chu kỳ thực thi Tác vụ chạy ngầm</h3>
        <span class="badge-dynamic">⚡ Tự động cập nhật Hangfire lập tức</span>
      </div>
      <p class="box-desc">
        Khi thay đổi chu kỳ và bấm Lưu, hệ thống sẽ tự động cập nhật lại lịch chạy của các Hangfire Recurring Jobs ngay lập tức mà không cần khởi động lại máy chủ.
      </p>

      <div class="settings-grid">
        <div class="setting-item">
          <label>
            <span>🛡️ Chu kỳ quét Drive Guard (phút)</span>
            <span class="field-hint">Kiểm tra file mới, xóa file và cảnh báo an ninh</span>
          </label>
          <div class="input-with-unit">
            <input type="number" v-model.number="form.driveGuardIntervalMinutes" min="1" max="1440" required />
            <span class="unit">phút</span>
          </div>
        </div>

        <div class="setting-item">
          <label>
            <span>💳 Chu kỳ quét Biến động số dư (phút)</span>
            <span class="field-hint">Quét email ngân hàng và đồng bộ Google Sheets</span>
          </label>
          <div class="input-with-unit">
            <input type="number" v-model.number="form.bankTelemetryIntervalMinutes" min="1" max="1440" required />
            <span class="unit">phút</span>
          </div>
        </div>

        <div class="setting-item">
          <label>
            <span>🧹 Chu kỳ Tự động Dọn dẹp Email (giờ)</span>
            <span class="field-hint">Dọn dẹp thư rác theo quy tắc CleanupRules</span>
          </label>
          <div class="input-with-unit">
            <input type="number" v-model.number="form.emailCleanupIntervalHours" min="1" max="168" required />
            <span class="unit">giờ</span>
          </div>
        </div>

        <div class="setting-item">
          <label>
            <span>📅 Chu kỳ Trích xuất Lịch hẹn (giờ)</span>
            <span class="field-hint">Tìm email cuộc họp/phỏng vấn tạo bản nháp lịch</span>
          </label>
          <div class="input-with-unit">
            <input type="number" v-model.number="form.calendarExtractorIntervalHours" min="1" max="168" required />
            <span class="unit">giờ</span>
          </div>
        </div>

        <div class="setting-item full-width">
          <label>
            <span>🚨 Ngưỡng cảnh báo Xóa hàng loạt Drive (Bulk Delete Threshold)</span>
            <span class="field-hint">Kích hoạt cảnh báo nguy cấp khi số file bị xóa cùng lúc đạt ngưỡng</span>
          </label>
          <div class="input-with-unit">
            <input type="number" v-model.number="form.bulkDeleteThreshold" min="2" max="100" required />
            <span class="unit">files</span>
          </div>
        </div>
      </div>
    </div>

    <!-- Anti-Sleep Keep-Alive (MonsterASP Free Tier) -->
    <div class="card-box mt-3">
      <div class="box-header">
        <h3><i class="pi pi-heart"></i> Duy trì Máy chủ & Tác vụ ngầm (Keep-Alive Anti-Sleep)</h3>
        <span class="badge-safe">🛡️ Chống ngủ IIS AppPool 20 phút</span>
      </div>
      <p class="box-desc">
        Trên các gói hosting miễn phí (như MonsterASP free tier), máy chủ IIS sẽ tự động cho ứng dụng <strong>ngủ (sleep) sau 20 phút không có người truy cập</strong>, khiến Hangfire ngưng chạy ngầm. Cấu hình ping endpoint này mỗi <strong>10 - 14 phút</strong> trên <strong>Cron-job.org</strong>, <strong>UptimeRobot</strong> (miễn phí 100%) hoặc <strong>GitHub Actions</strong> để giữ máy chủ và các tác vụ ngầm luôn thức 24/7.
      </p>

      <div class="settings-grid">
        <div class="setting-item full-width">
          <label>
            <span>Mã khóa bảo vệ Ping (Keep-Alive Secret Key)</span>
            <span class="field-hint">Khóa bí mật để xác thực khi cron ping tới, ngăn ngừa bot quét rác bên ngoài</span>
          </label>
          <div class="key-input-row">
            <input type="text" v-model="form.keepAliveKey" placeholder="Để trống nếu không cần khóa, hoặc nhập/bấm tạo khóa..." />
            <button type="button" class="btn-generate-key" @click="generateKeepAliveKey">
              <i class="pi pi-refresh"></i> Tạo khóa ngẫu nhiên
            </button>
          </div>
        </div>

        <div class="setting-item full-width">
          <label>
            <span>Đường link Cron Ping Keep-Alive (Webhook URL)</span>
            <span class="field-hint">Dán URL này vào Cron-job.org hoặc UptimeRobot với chu kỳ 14 phút/lần (HTTP GET)</span>
          </label>
          <div class="copy-input-row">
            <input type="text" readonly :value="computedKeepAliveUrl" />
            <button type="button" class="btn-copy" @click="copyKeepAliveUrl">
              <i class="pi pi-copy"></i> Sao chép
            </button>
            <button type="button" class="btn-test-ping" @click="testKeepAlivePing" :disabled="testingPing">
              <i class="pi" :class="testingPing ? 'pi-spin pi-spinner' : 'pi-bolt'"></i>
              {{ testingPing ? 'Đang ping...' : 'Ping thử ngay' }}
            </button>
          </div>
        </div>
      </div>
    </div>

    <div class="section-footer-actions">
      <span v-if="isDirty" class="dirty-notice">
        <i class="pi pi-exclamation-circle"></i> Chu kỳ quét hoặc cấu hình Keep-Alive có thay đổi chưa lưu.
      </span>
      <button
        class="btn-save-section-bottom"
        @click="emit('save')"
        :disabled="savingCurrentSection || !isDirty"
      >
        <i class="pi" :class="savingCurrentSection ? 'pi-spin pi-spinner' : 'pi-sync'"></i>
        {{ savingCurrentSection ? 'Đang lưu chu kỳ...' : 'Lưu chu kỳ tác vụ ngầm & Cập nhật Hangfire' }}
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import api from '@/services/api.service';
import { showToast } from '@/services/notification.service';

export interface JobsSettingsForm {
  driveGuardIntervalMinutes: number;
  bankTelemetryIntervalMinutes: number;
  emailCleanupIntervalHours: number;
  calendarExtractorIntervalHours: number;
  bulkDeleteThreshold: number;
  keepAliveKey: string;
}

const props = defineProps<{
  form: JobsSettingsForm;
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

const testingPing = ref(false);

const computedKeepAliveUrl = computed(() => {
  const origin = window.location.origin;
  const keyParam = props.form.keepAliveKey ? `?key=${encodeURIComponent(props.form.keepAliveKey)}` : '';
  return `${origin}/api/v1/public/keep-alive${keyParam}`;
});

const generateKeepAliveKey = () => {
  const randomStr = Math.random().toString(36).substring(2, 12) + Math.random().toString(36).substring(2, 12);
  props.form.keepAliveKey = `ka_${randomStr}`;
};

const copyKeepAliveUrl = async () => {
  try {
    await navigator.clipboard.writeText(computedKeepAliveUrl.value);
    showToast({
      severity: 'success',
      summary: 'Đã sao chép URL',
      detail: 'Đã sao chép link Cron Ping Keep-Alive vào bộ nhớ tạm!',
    });
  } catch {
    showToast({
      severity: 'info',
      summary: 'URL Keep-Alive',
      detail: computedKeepAliveUrl.value,
    });
  }
};

const testKeepAlivePing = async () => {
  testingPing.value = true;
  try {
    const keyParam = props.form.keepAliveKey ? `?key=${encodeURIComponent(props.form.keepAliveKey)}` : '';
    const res: any = await api.get(`/public/keep-alive${keyParam}`);
    if (res.success) {
      showToast({
        severity: 'success',
        summary: 'Ping thành công (200 OK)!',
        detail: `Phản hồi: ${res.data?.message || 'Alive'} — Uptime: ${res.data?.uptime || 'Vừa khởi động'}`,
      });
    } else {
      showToast({
        severity: 'error',
        summary: 'Ping thất bại',
        detail: res.message || 'Máy chủ trả về lỗi khi ping.',
      });
    }
  } catch (err: any) {
    showToast({
      severity: 'error',
      summary: 'Lỗi Ping Keep-Alive',
      detail: err.message || 'Không thể kết nối đến endpoint Keep-Alive.',
    });
  } finally {
    testingPing.value = false;
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

    .badge-dynamic {
      font-size: 0.725rem;
      font-weight: 700;
      color: #34d399;
      background: rgba(16, 185, 129, 0.14);
      border: 1px solid rgba(16, 185, 129, 0.3);
      padding: 0.15rem 0.55rem;
      border-radius: 9999px;
      display: inline-flex;
      align-items: center;
      gap: 0.3rem;
    }

    .badge-safe {
      font-size: 0.725rem;
      font-weight: 700;
      color: #60a5fa;
      background: rgba(59, 130, 246, 0.14);
      border: 1px solid rgba(59, 130, 246, 0.3);
      padding: 0.15rem 0.55rem;
      border-radius: 9999px;
      display: inline-flex;
      align-items: center;
      gap: 0.3rem;
    }
  }

  .box-desc {
    font-size: 0.8rem;
    color: #94a3b8;
    line-height: 1.5;
    margin: 0 0 1rem 0;
  }
}

.settings-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 0.95rem;

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

    .input-with-unit {
      display: flex;
      align-items: center;
      height: 38px;
      background: rgba(11, 17, 32, 0.85);
      border: 1px solid rgba(148, 163, 184, 0.2);
      border-radius: 0.5rem;
      padding-right: 0.75rem;
      transition: border-color 0.15s ease, box-shadow 0.15s ease;

      input {
        flex: 1;
        border: none;
        background: transparent;
        height: 100%;
        padding-right: 0.4rem;

        &:focus {
          box-shadow: none;
        }
      }

      .unit {
        color: #818cf8;
        font-size: 0.75rem;
        font-weight: 700;
        text-transform: uppercase;
        letter-spacing: 0.05em;
        background: rgba(99, 102, 241, 0.12);
        padding: 0.15rem 0.45rem;
        border-radius: 0.25rem;
      }

      &:focus-within {
        border-color: #6366f1;
        box-shadow: 0 0 0 2px rgba(99, 102, 241, 0.25);
      }
    }
  }
}

.key-input-row,
.copy-input-row {
  display: flex;
  gap: 0.5rem;
  align-items: stretch;

  input {
    flex: 1;
    min-width: 220px;
    height: 38px;
    background: rgba(11, 17, 32, 0.85);
    border: 1px solid rgba(148, 163, 184, 0.2);
    border-radius: 0.5rem;
    padding: 0.5rem 0.75rem;
    color: #f8fafc;
    font-size: 0.85rem;
  }

  button {
    height: 38px;
    padding: 0 1rem;
    border-radius: 0.5rem;
    font-size: 0.825rem;
    font-weight: 600;
    cursor: pointer;
    display: flex;
    align-items: center;
    gap: 0.4rem;
    white-space: nowrap;
    transition: all 0.15s ease;
  }

  .btn-generate-key {
    background: rgba(99, 102, 241, 0.15);
    border: 1px solid rgba(99, 102, 241, 0.35);
    color: #818cf8;
    &:hover { background: #6366f1; color: #fff; transform: translate3d(0, -1px, 0); }
  }

  .btn-copy {
    background: rgba(255, 255, 255, 0.08);
    border: 1px solid rgba(255, 255, 255, 0.15);
    color: #cbd5e1;
    &:hover { background: rgba(255, 255, 255, 0.15); color: #fff; transform: translate3d(0, -1px, 0); }
  }

  .btn-test-ping {
    background: rgba(16, 185, 129, 0.15);
    border: 1px solid rgba(16, 185, 129, 0.35);
    color: #34d399;
    &:hover:not(:disabled) { background: #10b981; color: #fff; transform: translate3d(0, -1px, 0); }
    &:disabled { opacity: 0.45; cursor: not-allowed; }
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

  .key-input-row,
  .copy-input-row {
    flex-direction: column;

    input {
      min-width: 100%;
      width: 100%;
    }

    button {
      width: 100%;
      justify-content: center;
    }
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
