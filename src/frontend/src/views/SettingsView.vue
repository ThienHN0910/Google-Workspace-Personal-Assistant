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

        <!-- Token AI Quota Monitoring -->
        <div class="card-box mt-3">
          <div class="box-header">
            <h3><i class="pi pi-chart-bar"></i> Token AI Quota — Theo dõi hạn mức tháng</h3>
            <button class="btn-refresh-usage" @click="fetchAiUsage" :disabled="loadingAiUsage">
              <i class="pi" :class="loadingAiUsage ? 'pi-spin pi-spinner' : 'pi-refresh'"></i>
            </button>
          </div>
          <p class="box-desc">
            Theo dõi lượng token Gemini AI đã sử dụng trong tháng. Cảnh báo Telegram khi đạt 200K, khóa tác vụ AI chạy ngầm khi đạt 250K.
          </p>

          <div v-if="loadingAiUsage" class="usage-loading">
            <i class="pi pi-spin pi-spinner"></i> Đang tải dữ liệu quota...
          </div>
          <div v-else class="token-usage-panel">
            <!-- Progress Bar -->
            <div class="usage-progress-container">
              <div class="usage-labels">
                <span class="usage-month">📅 {{ aiUsage.yearMonth || 'N/A' }}</span>
                <span class="usage-count">{{ formatTokens(aiUsage.totalTokens) }} / {{ formatTokens(aiUsage.monthlyQuotaLimit) }} tokens</span>
              </div>
              <div class="progress-bar-track">
                <div
                  class="progress-bar-fill"
                  :class="usageBarClass"
                  :style="{ width: Math.min(aiUsage.usagePercentage, 100) + '%' }"
                ></div>
                <div class="progress-marker warning-marker" :style="{ left: warningPercentage + '%' }" title="Ngưỡng cảnh báo 200K"></div>
              </div>
              <div class="usage-stats-row">
                <span :class="['usage-pct', usageBarClass]">{{ aiUsage.usagePercentage }}%</span>
                <span class="usage-remaining">Còn lại: {{ formatTokens(aiUsage.remainingTokens) }} tokens</span>
              </div>
            </div>

            <!-- Status Badges -->
            <div class="usage-badges">
              <span v-if="aiUsage.quotaExceeded" class="badge-danger">🚫 Đã vượt quota — AI ngầm bị khóa</span>
              <span v-else-if="aiUsage.warningSent" class="badge-warning">⚠️ Đã cảnh báo — Gần đạt giới hạn</span>
              <span v-else class="badge-ok">✅ Trong giới hạn an toàn</span>
              <span v-if="!aiUsage.canRunBackgroundAi" class="badge-danger">🔒 Background AI: Đã khóa</span>
              <span v-else class="badge-ok">🟢 Background AI: Hoạt động</span>
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
import { ref, computed, onMounted, defineAsyncComponent } from 'vue';
import api from '@/services/api.service';
import { showToast } from '@/services/notification.service';

const LoadingSpinner = defineAsyncComponent(() => import('@/components/common/LoadingSpinner.vue'));

const activeTab = ref('jobs');
const loading = ref(false);
const saving = ref(false);
const testingTelegram = ref(false);
const testingPing = ref(false);
const newWhitelistDomain = ref('');
const loadingAiUsage = ref(false);

const form = ref({
  // Jobs
  driveGuardIntervalMinutes: 50,
  bankTelemetryIntervalMinutes: 30,
  emailCleanupIntervalHours: 12,
  calendarExtractorIntervalHours: 2,
  bulkDeleteThreshold: 3,

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
  aiMonthlyTokenQuota: 250000,
  aiWarningTokenThreshold: 200000,

  // Storage
  financeFolderId: '',
  financeSpreadsheetId: '',
  financeFileNamePattern: 'BaoCaoTaiChinh_{yyyy_MM}',
  emailWhitelistDomains: [] as string[],

  // Anti-Sleep Keep-Alive
  keepAliveKey: '',
});

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

const usageBarClass = computed(() => {
  const pct = aiUsage.value.usagePercentage;
  if (pct >= 100) return 'bar-danger';
  if (pct >= 80) return 'bar-warning';
  return 'bar-safe';
});

const warningPercentage = computed(() => {
  if (aiUsage.value.monthlyQuotaLimit <= 0) return 80;
  return Math.round((aiUsage.value.warningThreshold / aiUsage.value.monthlyQuotaLimit) * 100);
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

const computedKeepAliveUrl = computed(() => {
  const origin = window.location.origin;
  const keyParam = form.value.keepAliveKey ? `?key=${encodeURIComponent(form.value.keepAliveKey)}` : '';
  return `${origin}/api/v1/public/keep-alive${keyParam}`;
});

const generateKeepAliveKey = () => {
  const randomStr = Math.random().toString(36).substring(2, 12) + Math.random().toString(36).substring(2, 12);
  form.value.keepAliveKey = `ka_${randomStr}`;
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
    const keyParam = form.value.keepAliveKey ? `?key=${encodeURIComponent(form.value.keepAliveKey)}` : '';
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

onMounted(() => {
  fetchSettings();
  fetchAiUsage();
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

// Token AI Usage Panel Styles
.btn-refresh-usage {
  background: rgba(255, 255, 255, 0.06);
  border: 1px solid rgba(255, 255, 255, 0.12);
  color: #94a3b8;
  width: 36px;
  height: 36px;
  border-radius: 0.5rem;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s;
  &:hover:not(:disabled) { background: rgba(255, 255, 255, 0.12); color: #fff; }
  &:disabled { opacity: 0.5; cursor: not-allowed; }
}

.usage-loading {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  color: #94a3b8;
  padding: 2rem 0;
  font-size: 0.875rem;
}

.token-usage-panel {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.usage-progress-container {
  .usage-labels {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 0.5rem;

    .usage-month {
      font-size: 0.85rem;
      font-weight: 600;
      color: #cbd5e1;
    }

    .usage-count {
      font-size: 0.8rem;
      color: #94a3b8;
      font-weight: 500;
    }
  }

  .progress-bar-track {
    position: relative;
    height: 12px;
    background: rgba(255, 255, 255, 0.08);
    border-radius: 6px;
    overflow: visible;

    .progress-bar-fill {
      height: 100%;
      border-radius: 6px;
      transition: width 0.6s ease;

      &.bar-safe { background: linear-gradient(90deg, #10b981, #34d399); }
      &.bar-warning { background: linear-gradient(90deg, #f59e0b, #fbbf24); }
      &.bar-danger { background: linear-gradient(90deg, #ef4444, #f87171); }
    }

    .warning-marker {
      position: absolute;
      top: -3px;
      width: 2px;
      height: 18px;
      background: #fbbf24;
      border-radius: 1px;
    }
  }

  .usage-stats-row {
    display: flex;
    justify-content: space-between;
    margin-top: 0.4rem;

    .usage-pct {
      font-size: 0.85rem;
      font-weight: 700;
      &.bar-safe { color: #34d399; }
      &.bar-warning { color: #fbbf24; }
      &.bar-danger { color: #ef4444; }
    }

    .usage-remaining {
      font-size: 0.8rem;
      color: #94a3b8;
    }
  }
}

.usage-badges {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;

  .badge-ok,
  .badge-warning,
  .badge-danger {
    font-size: 0.75rem;
    font-weight: 600;
    padding: 0.25rem 0.75rem;
    border-radius: 2rem;
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
}

.feature-breakdown {
  h4 {
    font-size: 0.875rem;
    font-weight: 600;
    color: #cbd5e1;
    margin: 0 0 0.75rem 0;
    display: flex;
    align-items: center;
    gap: 0.4rem;
    i { color: #818cf8; }
  }

  .breakdown-list {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
  }

  .breakdown-item {
    display: grid;
    grid-template-columns: 140px 1fr 70px;
    align-items: center;
    gap: 0.75rem;

    .breakdown-feature {
      font-size: 0.8rem;
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
  gap: 0.75rem;

  .usage-stat-card {
    background: rgba(15, 23, 42, 0.6);
    border: 1px solid rgba(255, 255, 255, 0.08);
    border-radius: 0.75rem;
    padding: 0.75rem;
    text-align: center;

    .stat-label {
      display: block;
      font-size: 0.7rem;
      color: #64748b;
      font-weight: 500;
      margin-bottom: 0.3rem;
    }

    .stat-value {
      display: block;
      font-size: 1rem;
      font-weight: 700;
      color: #f8fafc;
    }
  }
}

.key-input-row,
.copy-input-row {
  display: flex;
  gap: 0.5rem;
  flex-wrap: wrap;

  input {
    flex: 1;
    min-width: 240px;
  }

  button {
    padding: 0.65rem 1rem;
    border-radius: 0.5rem;
    font-size: 0.85rem;
    font-weight: 600;
    cursor: pointer;
    display: flex;
    align-items: center;
    gap: 0.4rem;
    white-space: nowrap;
    transition: all 0.2s;
  }

  .btn-generate-key {
    background: rgba(99, 102, 241, 0.15);
    border: 1px solid rgba(99, 102, 241, 0.35);
    color: #818cf8;
    &:hover { background: #6366f1; color: #fff; }
  }

  .btn-copy {
    background: rgba(255, 255, 255, 0.08);
    border: 1px solid rgba(255, 255, 255, 0.15);
    color: #cbd5e1;
    &:hover { background: rgba(255, 255, 255, 0.15); color: #fff; }
  }

  .btn-test-ping {
    background: rgba(16, 185, 129, 0.15);
    border: 1px solid rgba(16, 185, 129, 0.35);
    color: #34d399;
    &:hover:not(:disabled) { background: #10b981; color: #fff; }
    &:disabled { opacity: 0.5; cursor: not-allowed; }
  }
}

@media (max-width: 768px) {
  .settings-grid {
    grid-template-columns: 1fr;
    .setting-item.full-width { grid-column: span 1; }
  }
}
</style>
