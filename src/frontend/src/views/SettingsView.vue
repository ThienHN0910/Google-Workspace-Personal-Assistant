<template>
  <div class="settings-page">
    <div class="page-top-bar">
      <div class="title-group">
        <h1><i class="pi pi-cog"></i> Cài đặt Hệ thống (System Settings)</h1>
        <p class="subtitle">Quản lý toàn diện chu kỳ tác vụ chạy ngầm, thông báo đa kênh, trợ lý AI và lưu trữ Drive</p>
      </div>
      <div class="top-actions">
        <button class="btn-reset" @click="fetchSettings" :disabled="loading || saving">
          <i class="pi pi-refresh"></i> Tải lại
        </button>
        <button class="btn-save" @click="saveSettings" :disabled="loading || saving">
          <i class="pi" :class="saving ? 'pi-spin pi-spinner' : 'pi-save'"></i>
          {{ saving ? 'Đang lưu...' : 'Lưu cài đặt' }}
        </button>
      </div>
    </div>

    <!-- Tab Bar Navigation -->
    <div class="tabs-nav">
      <button :class="{ active: activeTab === 'jobs' }" @click="activeTab = 'jobs'">
        <i class="pi pi-clock"></i> Tác vụ & Chu kỳ quét
      </button>
      <button :class="{ active: activeTab === 'alerts' }" @click="activeTab = 'alerts'">
        <i class="pi pi-bell"></i> Kênh Thông báo (Telegram / Discord)
      </button>
      <button :class="{ active: activeTab === 'ai' }" @click="activeTab = 'ai'">
        <i class="pi pi-sparkles"></i> Trí tuệ AI & Quota
      </button>
      <button :class="{ active: activeTab === 'storage' }" @click="activeTab = 'storage'">
        <i class="pi pi-folder"></i> Lưu trữ Drive & Whitelist Email
      </button>
    </div>

    <LoadingSpinner v-if="loading" text="Đang tải cấu hình hệ thống..." />

    <div v-else class="tab-content-container">
      <!-- TAB 1: Background Jobs & Intervals -->
      <div v-if="activeTab === 'jobs'" class="tab-panel">
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
      </div>

      <!-- TAB 2: Multi-Channel Alerting -->
      <div v-if="activeTab === 'alerts'" class="tab-panel">
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
      </div>

      <!-- TAB 3: AI Assistant & Quota Guard -->
      <div v-if="activeTab === 'ai'" class="tab-panel">
        <div class="card-box">
          <div class="box-header">
            <h3><i class="pi pi-sparkles"></i> Trí tuệ Nhân tạo Gemini AI</h3>
            <span class="badge-safe">🛡️ Tự động bảo vệ Quota 15 RPM / 500 RPD</span>
          </div>
          <p class="box-desc">
            Cấu hình mô hình xử lý sinh bản nháp email, bóc tách hóa đơn ngân hàng và trích xuất lịch hẹn.
          </p>

          <div class="settings-grid">
            <div class="setting-item">
              <label>
                <span>Mô hình Gemini (Model)</span>
                <span class="field-hint">Mô hình AI được sử dụng cho toàn bộ dự án</span>
              </label>
              <select v-model="form.geminiModel">
                <option value="gemini-3.1-flash-lite">gemini-3.1-flash-lite (Tốc độ cao, tối ưu quota)</option>
                <option value="gemini-1.5-flash">gemini-1.5-flash (Cân bằng hiệu năng)</option>
                <option value="gemini-1.5-pro">gemini-1.5-pro (Suy luận sâu sắc)</option>
              </select>
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
              <li><strong>Tối đa 15 yêu cầu / phút (15 RPM)</strong>: Các tác vụ chạy ngầm tự động điều phối hàng đợi, không bao giờ gây lỗi <code>429 Too Many Requests</code>.</li>
              <li><strong>Tối đa 500 yêu cầu / ngày (500 RPD)</strong>: Ngăn chặn vượt hạn mức gói miễn phí của Google AI.</li>
            </ul>
          </div>
        </div>
      </div>

      <!-- TAB 4: Storage & Email Whitelist -->
      <div v-if="activeTab === 'storage'" class="tab-panel">
        <div class="card-box">
          <div class="box-header">
            <h3><i class="pi pi-table"></i> Cấu hình Xuất Dữ liệu Google Sheets & Drive</h3>
          </div>
          <p class="box-desc">
            Cấu hình thư mục lưu trữ báo cáo tài chính và quy tắc đặt tên file tự động theo tháng.
          </p>

          <div class="settings-grid">
            <div class="setting-item">
              <label>
                <span>Google Drive Folder ID</span>
                <span class="field-hint">Thư mục Drive chứa các bảng tính báo cáo thu chi</span>
              </label>
              <input type="text" v-model="form.financeFolderId" placeholder="VD: 1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms" />
            </div>

            <div class="setting-item">
              <label>
                <span>Mẫu đặt tên file Google Sheets</span>
                <span class="field-hint">Sử dụng placeholder {yyyy_MM} để tự động sinh theo tháng</span>
              </label>
              <input type="text" v-model="form.financeFileNamePattern" placeholder="BaoCaoTaiChinh_{yyyy_MM}" />
            </div>

            <div class="setting-item full-width">
              <label>
                <span>Google Spreadsheet ID Cố định (Tùy chọn)</span>
                <span class="field-hint">Nếu muốn dùng chung 1 file Sheet duy nhất thay vì tạo mới theo tháng</span>
              </label>
              <input type="text" v-model="form.financeSpreadsheetId" placeholder="Để trống để tự động tạo file mới theo tháng" />
            </div>
          </div>
        </div>

        <div class="card-box mt-3">
          <div class="box-header">
            <h3><i class="pi pi-verified"></i> Danh sách Tên miền Email an toàn (Whitelist Domains)</h3>
          </div>
          <p class="box-desc">
            Các email nhận từ những tên miền này sẽ <strong>TUYỆT ĐỐI KHÔNG BAO GIỜ</strong> bị tính năng dọn dẹp tự động (Auto-Clean) chuyển vào thùng rác hay lưu trữ.
          </p>

          <div class="whitelist-input-group">
            <input
              type="text"
              v-model="newWhitelistDomain"
              placeholder="VD: google.com, fpt.edu.vn, company.com..."
              @keyup.enter="addWhitelistDomain"
            />
            <button class="btn-add-domain" @click="addWhitelistDomain" :disabled="!newWhitelistDomain.trim()">
              <i class="pi pi-plus"></i> Thêm Domain
            </button>
          </div>

          <div v-if="form.emailWhitelistDomains.length === 0" class="empty-hint">
            Chưa có tên miền nào trong Whitelist. Nhập tên miền phía trên để thêm bảo vệ.
          </div>
          <div v-else class="chips-container">
            <div v-for="(domain, idx) in form.emailWhitelistDomains" :key="idx" class="domain-chip">
              <i class="pi pi-globe"></i>
              <span>{{ domain }}</span>
              <button class="btn-remove-chip" @click="removeWhitelistDomain(idx)" title="Xóa domain">
                <i class="pi pi-times"></i>
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, defineAsyncComponent } from 'vue';
import api from '@/services/api.service';
import { showToast } from '@/services/notification.service';

const LoadingSpinner = defineAsyncComponent(() => import('@/components/common/LoadingSpinner.vue'));

const activeTab = ref('jobs');
const loading = ref(false);
const saving = ref(false);
const testingTelegram = ref(false);
const newWhitelistDomain = ref('');

const form = ref({
  // Jobs
  driveGuardIntervalMinutes: 5,
  bankTelemetryIntervalMinutes: 15,
  emailCleanupIntervalHours: 6,
  calendarExtractorIntervalHours: 1,
  bulkDeleteThreshold: 5,

  // Alerts
  enableTelegram: true,
  telegramBotToken: '',
  telegramChatId: '',
  enableDiscord: true,
  discordWebhookUrl: '',

  // AI
  geminiModel: 'gemini-3.1-flash-lite',
  defaultLanguage: 'vi',
  defaultTone: 'polite',
  maxRequestsPerMinute: 15,
  maxRequestsPerDay: 500,

  // Storage
  financeFolderId: '',
  financeSpreadsheetId: '',
  financeFileNamePattern: 'BaoCaoTaiChinh_{yyyy_MM}',
  emailWhitelistDomains: [] as string[],
});

const fetchSettings = async () => {
  loading.value = true;
  try {
    const res: any = await api.get('/settings');
    if (res.success && res.data) {
      form.value = {
        ...form.value,
        ...res.data,
      };
    }
  } catch (e: any) {
    showToast({
      severity: 'error',
      summary: 'Lỗi tải cấu hình',
      detail: e.message || 'Không thể lấy dữ liệu cài đặt từ máy chủ.',
    });
  } finally {
    loading.value = false;
  }
};

const saveSettings = async () => {
  saving.value = true;
  try {
    const res: any = await api.put('/settings', form.value);
    if (res.success) {
      showToast({
        severity: 'success',
        summary: 'Đã lưu cài đặt',
        detail: 'Cấu hình hệ thống và lịch chạy ngầm đã được cập nhật thành công.',
      });
    } else {
      showToast({
        severity: 'error',
        summary: 'Lưu thất bại',
        detail: res.message || 'Không thể lưu cài đặt.',
      });
    }
  } catch (e: any) {
    showToast({
      severity: 'error',
      summary: 'Lỗi',
      detail: e.message || 'Có lỗi xảy ra khi lưu cấu hình.',
    });
  } finally {
    saving.value = false;
  }
};

const testTelegram = async () => {
  if (!form.value.telegramBotToken || !form.value.telegramChatId) {
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
      botToken: form.value.telegramBotToken,
      chatId: form.value.telegramChatId,
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

const addWhitelistDomain = () => {
  const domain = newWhitelistDomain.value.trim().toLowerCase().replace(/^@/, '');
  if (domain && !form.value.emailWhitelistDomains.includes(domain)) {
    form.value.emailWhitelistDomains.push(domain);
    newWhitelistDomain.value = '';
  }
};

const removeWhitelistDomain = (idx: number) => {
  form.value.emailWhitelistDomains.splice(idx, 1);
};

onMounted(() => {
  fetchSettings();
});
</script>

<style scoped lang="scss">
.settings-page {
  max-width: 1000px;
  margin: 0 auto;
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.page-top-bar {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  flex-wrap: wrap;
  gap: 1rem;

  .title-group {
    h1 {
      font-size: 1.6rem;
      font-weight: 800;
      color: #f8fafc;
      display: flex;
      align-items: center;
      gap: 0.6rem;
      margin: 0;

      i { color: #818cf8; }
    }

    .subtitle {
      font-size: 0.875rem;
      color: #94a3b8;
      margin-top: 0.35rem;
    }
  }

  .top-actions {
    display: flex;
    gap: 0.75rem;

    button {
      padding: 0.6rem 1.25rem;
      border-radius: 0.5rem;
      font-weight: 600;
      font-size: 0.875rem;
      cursor: pointer;
      display: flex;
      align-items: center;
      gap: 0.45rem;
      transition: all 0.2s;
    }

    .btn-reset {
      background: rgba(255, 255, 255, 0.06);
      border: 1px solid rgba(255, 255, 255, 0.12);
      color: #cbd5e1;
      &:hover:not(:disabled) { background: rgba(255, 255, 255, 0.12); color: #fff; }
    }

    .btn-save {
      background: linear-gradient(135deg, #6366f1, #818cf8);
      border: none;
      color: #fff;
      box-shadow: 0 4px 12px rgba(99, 102, 241, 0.3);
      &:hover:not(:disabled) { filter: brightness(1.1); transform: translateY(-1px); }
      &:disabled { opacity: 0.5; cursor: not-allowed; }
    }
  }
}

.tabs-nav {
  display: flex;
  gap: 0.5rem;
  background: #1e293b;
  padding: 0.35rem;
  border-radius: 0.75rem;
  border: 1px solid rgba(255, 255, 255, 0.08);
  overflow-x: auto;

  button {
    flex: 1;
    background: transparent;
    border: none;
    color: #94a3b8;
    padding: 0.65rem 1rem;
    font-size: 0.875rem;
    font-weight: 600;
    border-radius: 0.5rem;
    cursor: pointer;
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 0.45rem;
    white-space: nowrap;
    transition: all 0.2s;

    i { font-size: 1rem; }

    &:hover {
      color: #f8fafc;
      background: rgba(255, 255, 255, 0.04);
    }

    &.active {
      background: #6366f1;
      color: #fff;
      box-shadow: 0 4px 12px rgba(99, 102, 241, 0.25);
    }
  }
}

.card-box {
  background: #1e293b;
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 1rem;
  padding: 1.5rem;

  &.mt-3 { margin-top: 1.25rem; }

  .box-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 0.5rem;

    h3 {
      font-size: 1.15rem;
      font-weight: 700;
      color: #f8fafc;
      margin: 0;
      display: flex;
      align-items: center;
      gap: 0.5rem;

      i { color: #818cf8; }
    }

    .badge-dynamic {
      font-size: 0.75rem;
      font-weight: 700;
      color: #34d399;
      background: rgba(16, 185, 129, 0.15);
      border: 1px solid rgba(16, 185, 129, 0.25);
      padding: 0.2rem 0.6rem;
      border-radius: 1rem;
    }

    .badge-safe {
      font-size: 0.75rem;
      font-weight: 700;
      color: #60a5fa;
      background: rgba(59, 130, 246, 0.15);
      border: 1px solid rgba(59, 130, 246, 0.25);
      padding: 0.2rem 0.6rem;
      border-radius: 1rem;
    }
  }

  .box-desc {
    font-size: 0.825rem;
    color: #94a3b8;
    line-height: 1.5;
    margin: 0 0 1.25rem 0;
  }
}

.settings-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 1.25rem;

  &.disabled-section {
    opacity: 0.45;
    pointer-events: none;
  }

  .setting-item {
    display: flex;
    flex-direction: column;
    gap: 0.4rem;

    &.full-width { grid-column: span 2; }

    label {
      font-size: 0.85rem;
      font-weight: 600;
      color: #cbd5e1;
      display: flex;
      flex-direction: column;
      gap: 0.15rem;

      .field-hint {
        font-size: 0.75rem;
        color: #64748b;
        font-weight: 400;
      }
    }

    input, select {
      background: #0f172a;
      border: 1px solid rgba(255, 255, 255, 0.12);
      border-radius: 0.5rem;
      padding: 0.65rem 0.85rem;
      color: #f8fafc;
      font-size: 0.875rem;
      transition: border-color 0.2s;

      &:focus {
        outline: none;
        border-color: #6366f1;
      }
    }

    .input-with-unit {
      display: flex;
      align-items: center;
      background: #0f172a;
      border: 1px solid rgba(255, 255, 255, 0.12);
      border-radius: 0.5rem;
      padding-right: 0.85rem;

      input {
        flex: 1;
        border: none;
        background: transparent;
      }

      .unit {
        color: #94a3b8;
        font-size: 0.8rem;
        font-weight: 600;
      }
    }
  }

  .test-row {
    display: flex;
    align-items: center;
    gap: 1rem;
    padding-top: 0.5rem;

    .btn-test {
      background: rgba(99, 102, 241, 0.15);
      border: 1px solid rgba(99, 102, 241, 0.35);
      color: #818cf8;
      padding: 0.6rem 1.15rem;
      border-radius: 0.5rem;
      font-size: 0.85rem;
      font-weight: 600;
      cursor: pointer;
      display: flex;
      align-items: center;
      gap: 0.45rem;
      white-space: nowrap;
      transition: all 0.2s;

      &:hover:not(:disabled) {
        background: #6366f1;
        color: #fff;
      }

      &:disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }
    }

    .test-hint {
      font-size: 0.775rem;
      color: #94a3b8;
    }
  }
}

.switch-toggle {
  position: relative;
  display: inline-block;
  width: 44px;
  height: 24px;

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
    border-radius: 24px;
    transition: 0.2s;

    &:before {
      position: absolute;
      content: "";
      height: 18px;
      width: 18px;
      left: 3px;
      bottom: 3px;
      background-color: white;
      border-radius: 50%;
      transition: 0.2s;
    }
  }

  input:checked + .slider {
    background-color: #10b981;
  }

  input:checked + .slider:before {
    transform: translateX(20px);
  }
}

.quota-info-box {
  background: rgba(59, 130, 246, 0.08);
  border: 1px solid rgba(59, 130, 246, 0.2);
  border-radius: 0.75rem;
  padding: 1rem;
  margin-top: 1.25rem;

  .quota-header {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    color: #60a5fa;
    font-size: 0.875rem;
    margin-bottom: 0.5rem;
  }

  ul {
    margin: 0;
    padding-left: 1.25rem;
    font-size: 0.8rem;
    color: #cbd5e1;
    line-height: 1.6;
  }
}

.whitelist-input-group {
  display: flex;
  gap: 0.75rem;
  margin-bottom: 1rem;

  input {
    flex: 1;
    background: #0f172a;
    border: 1px solid rgba(255, 255, 255, 0.12);
    border-radius: 0.5rem;
    padding: 0.65rem 0.85rem;
    color: #f8fafc;
    font-size: 0.875rem;
    &:focus { outline: none; border-color: #6366f1; }
  }

  .btn-add-domain {
    background: #6366f1;
    border: none;
    color: #fff;
    padding: 0.65rem 1.15rem;
    border-radius: 0.5rem;
    font-size: 0.85rem;
    font-weight: 600;
    cursor: pointer;
    display: flex;
    align-items: center;
    gap: 0.35rem;
    white-space: nowrap;
    &:hover:not(:disabled) { background: #4f46e5; }
    &:disabled { opacity: 0.5; cursor: not-allowed; }
  }
}

.empty-hint {
  font-size: 0.8rem;
  color: #64748b;
  font-style: italic;
}

.chips-container {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;

  .domain-chip {
    background: rgba(99, 102, 241, 0.15);
    border: 1px solid rgba(99, 102, 241, 0.3);
    color: #a5b4fc;
    padding: 0.3rem 0.65rem;
    border-radius: 2rem;
    font-size: 0.8rem;
    display: flex;
    align-items: center;
    gap: 0.4rem;

    .btn-remove-chip {
      background: none;
      border: none;
      color: #94a3b8;
      cursor: pointer;
      padding: 0;
      font-size: 0.75rem;
      display: flex;
      align-items: center;
      &:hover { color: #f87171; }
    }
  }
}

@media (max-width: 768px) {
  .settings-grid {
    grid-template-columns: 1fr;
    .setting-item.full-width { grid-column: span 1; }
  }
}
</style>
