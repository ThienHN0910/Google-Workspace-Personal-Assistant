<template>
  <div class="tab-panel">
    <div class="card-box">
      <div class="box-header">
        <h3><i class="pi pi-sparkles"></i> Trí tuệ Nhân tạo Gemini AI</h3>
        <span class="badge-safe">🛡️ Tự động bảo vệ Quota 14 RPM / 498 RPD / 240k TPM</span>
      </div>
      <p class="box-desc">
        Cấu hình mô hình xử lý sinh bản nháp email, bóc tách hóa đơn ngân hàng và trích xuất lịch hẹn.
      </p>

      <div class="settings-grid">
        <div class="setting-item">
          <label>
            <span>Mô hình Gemini (Model)</span>
            <span class="field-hint">Điền trực tiếp tên model (VD: gemini-3.5-flash-lite, gemini-2.5-flash...)</span>
          </label>
          <div class="model-input-row">
            <input
              type="text"
              v-model="form.geminiModel"
              placeholder="gemini-3.5-flash-lite"
              required
            />
            <button
              type="button"
              class="btn-test-model"
              @click="testGeminiModel"
              :disabled="testingModel || !form.geminiModel?.trim()"
              title="Kiểm tra kết nối và độ tồn tại của model trên Google AI"
            >
              <i class="pi" :class="testingModel ? 'pi-spin pi-spinner' : 'pi-sparkles'"></i>
              {{ testingModel ? 'Đang test...' : 'Kiểm tra Model' }}
            </button>
          </div>
        </div>

        <div class="setting-item">
          <label>
            <span>Ngôn ngữ phản hồi mặc định</span>
            <span class="field-hint">Ngôn ngữ ưu tiên khi AI soạn bản nháp trả lời</span>
          </label>
          <select v-model="form.defaultLanguage">
            <option value="vi">Tiếng Việt</option>
            <option value="en">English</option>
          </select>
        </div>

        <div class="setting-item full-width">
          <label>
            <span>Phong cách trả lời (Tone)</span>
            <span class="field-hint">Định hình văn phong phản hồi thư của trợ lý AI</span>
          </label>
          <select v-model="form.defaultTone">
            <option value="polite">Lịch sự & Chuyên nghiệp (Polite & Professional)</option>
            <option value="casual">Thân thiện & Ngắn gọn (Casual & Concise)</option>
            <option value="executive">Trực diện & Quyết đoán (Executive & Decisive)</option>
          </select>
        </div>
      </div>

      <div class="quota-info-box">
        <div class="quota-header">
          <i class="pi pi-shield"></i>
          <strong>Hàng rào kiểm soát hạn mức GeminiRateLimiter:</strong>
        </div>
        <ul>
          <li><strong>Tối đa 14 yêu cầu / phút (14 RPM)</strong>: Điều phối hàng đợi trượt, tạo khoảng đệm an toàn phòng ngừa lỗi <code>429 Too Many Requests</code>.</li>
          <li><strong>Tối đa 240.000 Input Token / phút (240K TPM)</strong>: Kiểm soát kích thước prompt trượt 60 giây, cảnh báo Telegram khi tải chạm đỉnh &ge; 200k TPM.</li>
          <li><strong>Tối đa 498 yêu cầu / ngày (498 RPD)</strong>: Duy trì trần an toàn dưới 500 RPD của gói miễn phí Google AI.</li>
        </ul>
      </div>
    </div>

    <!-- Token AI Quota Monitoring -->
    <div class="card-box mt-3">
      <div class="box-header">
        <h3><i class="pi pi-chart-bar"></i> Token AI — Thống kê & Giám sát Tải</h3>
        <button class="btn-refresh-usage" @click="fetchAiUsage" :disabled="loadingAiUsage">
          <i class="pi" :class="loadingAiUsage ? 'pi-spin pi-spinner' : 'pi-refresh'"></i>
        </button>
      </div>
      <p class="box-desc">
        Theo dõi tổng lượng token Gemini AI đã sử dụng trong tháng. Tự động cảnh báo Telegram khi có lỗi HTTP 429 hoặc khi input token trong 1 phút đạt đỉnh &ge; 200K. Không áp dụng giới hạn chặn tháng.
      </p>

      <div v-if="loadingAiUsage" class="usage-loading">
        <i class="pi pi-spin pi-spinner"></i> Đang tải dữ liệu quota...
      </div>
      <div v-else class="token-usage-panel">
        <!-- Progress Bar -->
        <div class="usage-progress-container">
          <div class="usage-labels">
            <span class="usage-month">📅 {{ aiUsage.yearMonth || 'N/A' }}</span>
            <span class="usage-count">{{ formatTokens(aiUsage.totalTokens) }} tokens tháng này</span>
          </div>
          <div class="progress-bar-track">
            <div
              class="progress-bar-fill bg-indigo"
              style="width: 100%"
            ></div>
          </div>
          <div class="usage-stats-row">
            <span class="usage-pct text-indigo">{{ aiUsage.callCount }} cuộc gọi AI</span>
            <span class="usage-remaining">Prompt: {{ formatTokens(aiUsage.promptTokens) }} | Candidates: {{ formatTokens(aiUsage.candidatesTokens) }}</span>
          </div>
        </div>

        <!-- Status Badges -->
        <div class="usage-badges">
          <span class="badge-ok">🟢 Background AI: Hoạt động liên tục (Không giới hạn tháng)</span>
          <span class="badge-safe">🛡️ Cảnh báo Real-time: HTTP 429 &amp; Peak &ge; 200k TPM</span>
        </div>

        <!-- Feature Breakdown -->
        <div v-if="Object.keys(aiUsage.featureBreakdown || {}).length > 0" class="feature-breakdown">
          <h4><i class="pi pi-list"></i> Phân bổ theo tính năng</h4>
          <div class="breakdown-list">
            <div
              v-for="(tokens, feature) in aiUsage.featureBreakdown"
              :key="feature"
              class="breakdown-item"
            >
              <span class="breakdown-feature">{{ feature }}</span>
              <div class="breakdown-bar-track">
                <div
                  class="breakdown-bar-fill"
                  :style="{ width: featurePercentage(tokens) + '%' }"
                ></div>
              </div>
              <span class="breakdown-tokens">{{ formatTokens(tokens) }}</span>
            </div>
          </div>
        </div>

        <!-- Summary Stats -->
        <div class="usage-summary-grid">
          <div class="usage-stat-card">
            <span class="stat-label">Tổng lượt gọi</span>
            <span class="stat-value">{{ aiUsage.callCount }}</span>
          </div>
          <div class="usage-stat-card">
            <span class="stat-label">Prompt Tokens</span>
            <span class="stat-value">{{ formatTokens(aiUsage.promptTokens) }}</span>
          </div>
          <div class="usage-stat-card">
            <span class="stat-label">Response Tokens</span>
            <span class="stat-value">{{ formatTokens(aiUsage.candidatesTokens) }}</span>
          </div>
          <div class="usage-stat-card">
            <span class="stat-label">Reset đầu tháng sau</span>
            <span class="stat-value">{{ nextResetDate }}</span>
          </div>
        </div>

        <!-- Quota Settings -->
        <div class="settings-grid" style="margin-top: 1.25rem;">
          <div class="setting-item">
            <label>
              <span>Giới hạn Token hàng tháng</span>
              <span class="field-hint">Khóa AI chạy ngầm khi vượt ngưỡng này</span>
            </label>
            <div class="input-with-unit">
              <input type="number" v-model.number="form.aiMonthlyTokenQuota" min="10000" max="10000000" />
              <span class="unit">tokens</span>
            </div>
          </div>
          <div class="setting-item">
            <label>
              <span>Ngưỡng cảnh báo Telegram</span>
              <span class="field-hint">Gửi cảnh báo khi đạt mốc này</span>
            </label>
            <div class="input-with-unit">
              <input type="number" v-model.number="form.aiWarningTokenThreshold" min="5000" max="10000000" />
              <span class="unit">tokens</span>
            </div>
          </div>
        </div>
      </div>
    </div>

    <div class="section-footer-actions">
      <span v-if="isDirty" class="dirty-notice">
        <i class="pi pi-exclamation-circle"></i> Cấu hình Trí tuệ AI có thay đổi chưa lưu.
      </span>
      <button
        class="btn-save-section-bottom"
        @click="emit('save')"
        :disabled="savingCurrentSection || !isDirty"
      >
        <i class="pi" :class="savingCurrentSection ? 'pi-spin pi-spinner' : 'pi-save'"></i>
        {{ savingCurrentSection ? 'Đang lưu...' : 'Lưu cấu hình Trợ lý AI' }}
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue';
import api from '@/services/api.service';
import { showToast } from '@/services/notification.service';

export interface AiSettingsForm {
  geminiModel: string;
  defaultLanguage: string;
  defaultTone: string;
  maxRequestsPerMinute?: number;
  maxRequestsPerDay?: number;
  aiMonthlyTokenQuota: number;
  aiWarningTokenThreshold: number;
}

const props = defineProps<{
  form: AiSettingsForm;
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

const testingModel = ref(false);
const loadingAiUsage = ref(false);

const testGeminiModel = async () => {
  if (!props.form.geminiModel || !props.form.geminiModel.trim()) {
    showToast({
      severity: 'warn',
      summary: 'Thiếu thông tin',
      detail: 'Vui lòng nhập tên model trước khi kiểm tra.',
    });
    return;
  }

  testingModel.value = true;
  try {
    const res: any = await api.post('/settings/test-gemini-model', {
      model: props.form.geminiModel.trim(),
    });

    if (res.success && res.data) {
      if (res.data.success) {
        showToast({
          severity: 'success',
          summary: 'Kết nối thành công',
          detail: res.data.message || `Model '${props.form.geminiModel}' hoạt động tốt!`,
        });
      } else {
        showToast({
          severity: 'error',
          summary: 'Model không khả dụng',
          detail: res.data.message || 'Không thể kết nối tới model này.',
        });
      }
    } else {
      showToast({
        severity: 'error',
        summary: 'Lỗi kiểm tra',
        detail: res.message || 'Không thể kiểm tra model.',
      });
    }
  } catch (e: any) {
    showToast({
      severity: 'error',
      summary: 'Lỗi kiểm tra model',
      detail: e.message || 'Không thể kết nối đến máy chủ.',
    });
  } finally {
    testingModel.value = false;
  }
};

const aiUsage = ref({
  yearMonth: '',
  totalTokens: 0,
  promptTokens: 0,
  candidatesTokens: 0,
  featureBreakdown: {} as Record<string, number>,
  callCount: 0,
  monthlyQuotaLimit: 250000,
  warningThreshold: 200000,
  warningSent: false,
  quotaExceeded: false,
  remainingTokens: 250000,
  canRunBackgroundAi: true,
  usagePercentage: 0,
});

const nextResetDate = computed(() => {
  const now = new Date();
  const next = new Date(now.getFullYear(), now.getMonth() + 1, 1);
  return next.toLocaleDateString('vi-VN');
});

const formatTokens = (val: number) => {
  if (!val && val !== 0) return '0';
  if (val >= 1000) return (val / 1000).toFixed(1).replace(/\.0$/, '') + 'K';
  return val.toString();
};

const featurePercentage = (tokens: number) => {
  if (aiUsage.value.totalTokens <= 0) return 0;
  return Math.round((tokens / aiUsage.value.totalTokens) * 100);
};

const fetchAiUsage = async () => {
  loadingAiUsage.value = true;
  try {
    const res: any = await api.get('/settings/ai-usage');
    if (res.success && res.data) {
      aiUsage.value = { ...aiUsage.value, ...res.data };
    }
  } catch (e: any) {
    console.error('Failed to load AI usage:', e);
  } finally {
    loadingAiUsage.value = false;
  }
};

onMounted(() => {
  fetchAiUsage();
});
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

    input, select {
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

.model-input-row {
  display: flex;
  gap: 0.5rem;
  align-items: stretch;

  input {
    flex: 1;
  }

  .btn-test-model {
    height: 38px;
    padding: 0 1rem;
    border-radius: 0.5rem;
    font-size: 0.825rem;
    font-weight: 600;
    cursor: pointer;
    display: flex;
    align-items: center;
    gap: 0.45rem;
    white-space: nowrap;
    background: rgba(99, 102, 241, 0.16);
    border: 1px solid rgba(99, 102, 241, 0.35);
    color: #a5b4fc;
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
}

.quota-info-box {
  background: rgba(59, 130, 246, 0.08);
  border: 1px solid rgba(59, 130, 246, 0.2);
  border-radius: 0.65rem;
  padding: 0.85rem;
  margin-top: 1rem;

  .quota-header {
    display: flex;
    align-items: center;
    gap: 0.45rem;
    color: #60a5fa;
    font-size: 0.825rem;
    margin-bottom: 0.4rem;
  }

  ul {
    margin: 0;
    padding-left: 1.15rem;
    font-size: 0.775rem;
    color: #cbd5e1;
    line-height: 1.55;
  }
}

.btn-refresh-usage {
  background: rgba(30, 41, 59, 0.6);
  border: 1px solid rgba(148, 163, 184, 0.2);
  color: #94a3b8;
  width: 34px;
  height: 34px;
  border-radius: 0.5rem;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.15s ease;
  &:hover:not(:disabled) { background: rgba(51, 65, 85, 0.8); color: #fff; transform: translate3d(0, -1px, 0); }
  &:disabled { opacity: 0.45; cursor: not-allowed; }
}

.usage-loading {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  color: #94a3b8;
  padding: 1.5rem 0;
  font-size: 0.825rem;
}

.token-usage-panel {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.usage-progress-container {
  .usage-labels {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 0.4rem;

    .usage-month {
      font-size: 0.825rem;
      font-weight: 600;
      color: #cbd5e1;
    }

    .usage-count {
      font-size: 0.775rem;
      color: #94a3b8;
      font-weight: 500;
    }
  }

  .progress-bar-track {
    position: relative;
    height: 10px;
    background: rgba(255, 255, 255, 0.08);
    border-radius: 5px;
    overflow: visible;

    .progress-bar-fill {
      height: 100%;
      border-radius: 5px;
      transition: width 0.6s cubic-bezier(0.16, 1, 0.3, 1);

      &.bar-safe { background: linear-gradient(90deg, #10b981, #34d399); }
      &.bar-warning { background: linear-gradient(90deg, #f59e0b, #fbbf24); }
      &.bar-danger { background: linear-gradient(90deg, #ef4444, #f87171); }
    }

    .warning-marker {
      position: absolute;
      top: -3px;
      width: 2px;
      height: 16px;
      background: #fbbf24;
      border-radius: 1px;
    }
  }

  .usage-stats-row {
    display: flex;
    justify-content: space-between;
    margin-top: 0.35rem;

    .usage-pct {
      font-size: 0.825rem;
      font-weight: 700;
      &.bar-safe { color: #34d399; }
      &.bar-warning { color: #fbbf24; }
      &.bar-danger { color: #ef4444; }
    }

    .usage-remaining {
      font-size: 0.775rem;
      color: #94a3b8;
    }
  }
}

.usage-badges {
  display: flex;
  flex-wrap: wrap;
  gap: 0.45rem;

  .badge-ok,
  .badge-warning,
  .badge-danger {
    font-size: 0.725rem;
    font-weight: 600;
    padding: 0.2rem 0.65rem;
    border-radius: 9999px;
  }

  .badge-ok {
    background: rgba(16, 185, 129, 0.12);
    border: 1px solid rgba(16, 185, 129, 0.3);
    color: #34d399;
  }

  .badge-warning {
    background: rgba(245, 158, 11, 0.12);
    border: 1px solid rgba(245, 158, 11, 0.3);
    color: #fbbf24;
  }

  .badge-danger {
    background: rgba(239, 68, 68, 0.12);
    border: 1px solid rgba(239, 68, 68, 0.3);
    color: #f87171;
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

.feature-breakdown {
  h4 {
    font-size: 0.825rem;
    font-weight: 600;
    color: #cbd5e1;
    margin: 0 0 0.65rem 0;
    display: flex;
    align-items: center;
    gap: 0.4rem;
    i { color: #818cf8; }
  }

  .breakdown-list {
    display: flex;
    flex-direction: column;
    gap: 0.45rem;
  }

  .breakdown-item {
    display: grid;
    grid-template-columns: 140px 1fr 70px;
    align-items: center;
    gap: 0.65rem;

    .breakdown-feature {
      font-size: 0.775rem;
      color: #94a3b8;
      font-weight: 500;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .breakdown-bar-track {
      height: 6px;
      background: rgba(255, 255, 255, 0.06);
      border-radius: 3px;
      overflow: hidden;

      .breakdown-bar-fill {
        height: 100%;
        background: linear-gradient(90deg, #6366f1, #818cf8);
        border-radius: 3px;
        transition: width 0.4s ease;
      }
    }

    .breakdown-tokens {
      font-size: 0.75rem;
      color: #cbd5e1;
      font-weight: 600;
      text-align: right;
    }
  }
}

.usage-summary-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 0.65rem;

  .usage-stat-card {
    background: rgba(11, 17, 32, 0.65);
    border: 1px solid rgba(148, 163, 184, 0.12);
    border-radius: 0.65rem;
    padding: 0.65rem 0.75rem;
    text-align: center;
    transition: border-color 0.15s ease, transform 0.15s ease;

    &:hover {
      border-color: rgba(99, 102, 241, 0.35);
      transform: translate3d(0, -1px, 0);
    }

    .stat-label {
      display: block;
      font-size: 0.68rem;
      color: #64748b;
      font-weight: 500;
      margin-bottom: 0.2rem;
    }

    .stat-value {
      display: block;
      font-size: 0.95rem;
      font-weight: 700;
      color: #f8fafc;
      font-feature-settings: "tnum";
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
  .usage-summary-grid {
    grid-template-columns: repeat(2, 1fr);
  }

  .settings-grid {
    grid-template-columns: 1fr;
    gap: 0.85rem;

    .setting-item.full-width {
      grid-column: span 1;
    }
  }

  .feature-breakdown .breakdown-item {
    grid-template-columns: 110px 1fr 60px;
  }
}

@media (max-width: 640px) {
  .card-box {
    padding: 1rem;
    border-radius: 0.75rem;
  }

  .model-input-row {
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
