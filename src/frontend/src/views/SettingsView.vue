<template>
  <div class="settings-page">
    <div class="ambient-glow" aria-hidden="true"></div>
    <div class="page-top-bar">
      <div class="title-group">
        <div class="title-icon-badge">
          <i class="pi pi-cog"></i>
        </div>
        <div class="title-text">
          <h1>Cài đặt Hệ thống <span class="version-tag">Ops Console</span></h1>
          <p class="subtitle">Quản lý toàn diện chu kỳ tác vụ chạy ngầm, thông báo đa kênh, trợ lý AI và lưu trữ Drive</p>
        </div>
      </div>
      <div class="top-actions">
        <button class="btn-reset" @click="fetchSettings" :disabled="loading || saving || savingCurrentSection" title="Tải lại cài đặt từ máy chủ">
          <i class="pi pi-refresh"></i>
          <span>Tải lại</span>
        </button>
        <button
          class="btn-save-section"
          @click="saveCurrentSection"
          :disabled="loading || saving || savingCurrentSection || !isCurrentTabDirty"
          :title="isCurrentTabDirty ? 'Lưu ngay các thay đổi trong mục hiện tại' : 'Chưa có thay đổi nào trong mục này'"
        >
          <i class="pi" :class="savingCurrentSection ? 'pi-spin pi-spinner' : 'pi-bolt'"></i>
          <span>{{ savingCurrentSection ? 'Đang lưu...' : 'Lưu mục này' }}</span>
        </button>
        <button class="btn-save" @click="saveSettings" :disabled="loading || saving || savingCurrentSection" title="Lưu toàn bộ cài đặt">
          <i class="pi" :class="saving ? 'pi-spin pi-spinner' : 'pi-save'"></i>
          <span>{{ saving ? 'Đang lưu tất cả...' : 'Lưu tất cả' }}</span>
        </button>
      </div>
    </div>

    <!-- Tab Bar Navigation -->
    <div class="tabs-nav-wrapper">
      <div class="tabs-nav">
        <button :class="{ active: activeTab === 'jobs' }" @click="requestTabSwitch('jobs')">
          <i class="pi pi-clock"></i>
          <span>Tác vụ & Chu kỳ</span>
          <span v-if="isSectionDirty('jobs')" class="dirty-badge" title="Mục này có thay đổi chưa lưu"></span>
        </button>
        <button :class="{ active: activeTab === 'alerts' }" @click="requestTabSwitch('alerts')">
          <i class="pi pi-bell"></i>
          <span>Kênh Thông báo</span>
          <span v-if="isSectionDirty('alerts')" class="dirty-badge" title="Mục này có thay đổi chưa lưu"></span>
        </button>
        <button :class="{ active: activeTab === 'ai' }" @click="requestTabSwitch('ai')">
          <i class="pi pi-sparkles"></i>
          <span>Trí tuệ AI & Quota</span>
          <span v-if="isSectionDirty('ai')" class="dirty-badge" title="Mục này có thay đổi chưa lưu"></span>
        </button>
        <button :class="{ active: activeTab === 'storage' }" @click="requestTabSwitch('storage')">
          <i class="pi pi-folder"></i>
          <span>Lưu trữ & Whitelist</span>
          <span v-if="isSectionDirty('storage')" class="dirty-badge" title="Mục này có thay đổi chưa lưu"></span>
        </button>
      </div>
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

        <div class="section-footer-actions">
          <span v-if="isSectionDirty('jobs')" class="dirty-notice">
            <i class="pi pi-exclamation-circle"></i> Chu kỳ quét hoặc cấu hình Keep-Alive có thay đổi chưa lưu.
          </span>
          <button
            class="btn-save-section-bottom"
            @click="saveCurrentSection"
            :disabled="savingCurrentSection || !isSectionDirty('jobs')"
          >
            <i class="pi" :class="savingCurrentSection ? 'pi-spin pi-spinner' : 'pi-sync'"></i>
            {{ savingCurrentSection ? 'Đang lưu chu kỳ...' : 'Lưu chu kỳ tác vụ ngầm & Cập nhật Hangfire' }}
          </button>
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

        <div class="section-footer-actions">
          <span v-if="isSectionDirty('alerts')" class="dirty-notice">
            <i class="pi pi-exclamation-circle"></i> Kênh thông báo có thay đổi chưa lưu.
          </span>
          <button
            class="btn-save-section-bottom"
            @click="saveCurrentSection"
            :disabled="savingCurrentSection || !isSectionDirty('alerts')"
          >
            <i class="pi" :class="savingCurrentSection ? 'pi-spin pi-spinner' : 'pi-save'"></i>
            {{ savingCurrentSection ? 'Đang lưu...' : 'Lưu cấu hình Kênh Thông báo' }}
          </button>
        </div>
      </div>

      <!-- TAB 3: AI Assistant & Quota Guard -->
      <div v-if="activeTab === 'ai'" class="tab-panel">
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
          <span v-if="isSectionDirty('ai')" class="dirty-notice">
            <i class="pi pi-exclamation-circle"></i> Cấu hình Trí tuệ AI có thay đổi chưa lưu.
          </span>
          <button
            class="btn-save-section-bottom"
            @click="saveCurrentSection"
            :disabled="savingCurrentSection || !isSectionDirty('ai')"
          >
            <i class="pi" :class="savingCurrentSection ? 'pi-spin pi-spinner' : 'pi-save'"></i>
            {{ savingCurrentSection ? 'Đang lưu...' : 'Lưu cấu hình Trợ lý AI' }}
          </button>
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

        <div class="section-footer-actions">
          <span v-if="isSectionDirty('storage')" class="dirty-notice">
            <i class="pi pi-exclamation-circle"></i> Cấu hình Drive & Whitelist có thay đổi chưa lưu.
          </span>
          <button
            class="btn-save-section-bottom"
            @click="saveCurrentSection"
            :disabled="savingCurrentSection || !isSectionDirty('storage')"
          >
            <i class="pi" :class="savingCurrentSection ? 'pi-spin pi-spinner' : 'pi-save'"></i>
            {{ savingCurrentSection ? 'Đang lưu...' : 'Lưu cấu hình Drive & Whitelist' }}
          </button>
        </div>
      </div>
    </div>

    <!-- Popup Modal Cảnh báo Thay đổi Chưa Lưu khi Chuyển Tab -->
    <Teleport to="body">
      <Transition name="fade">
        <div v-if="showUnsavedModal" class="unsaved-modal-overlay" @click.self="showUnsavedModal = false">
          <div class="unsaved-modal-dialog">
            <div class="unsaved-modal-header">
              <div class="header-title">
                <i class="pi pi-exclamation-triangle warning-icon"></i>
                <h3>Cảnh báo: Thay đổi chưa được lưu!</h3>
              </div>
              <button type="button" class="btn-close-modal" @click="showUnsavedModal = false" title="Đóng">
                <i class="pi pi-times"></i>
              </button>
            </div>
            <div class="unsaved-modal-body">
              <p>
                Bạn vừa chỉnh sửa cấu hình trong mục <strong>{{ getSectionTitle(activeTab) }}</strong> nhưng chưa bấm lưu.
              </p>
              <p class="sub-desc">
                Bạn có muốn lưu các thay đổi này trước khi chuyển sang mục <strong>{{ getSectionTitle(pendingTargetTab) }}</strong> không?
              </p>
            </div>
            <div class="unsaved-modal-footer">
              <button type="button" class="btn-modal-cancel" @click="showUnsavedModal = false">
                Ở lại trang
              </button>
              <button type="button" class="btn-modal-discard" @click="confirmDiscardAndSwitch">
                <i class="pi pi-trash"></i> Bỏ qua thay đổi
              </button>
              <button
                type="button"
                class="btn-modal-save"
                @click="confirmSaveAndSwitch"
                :disabled="savingCurrentSection"
              >
                <i class="pi" :class="savingCurrentSection ? 'pi-spin pi-spinner' : 'pi-check'"></i>
                {{ savingCurrentSection ? 'Đang lưu...' : 'Lưu & Chuyển tiếp' }}
              </button>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>
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
const savingCurrentSection = ref(false);
const testingTelegram = ref(false);
const testingModel = ref(false);
const testingPing = ref(false);
const newWhitelistDomain = ref('');
const loadingAiUsage = ref(false);

const originalForm = ref<any>(null);
const showUnsavedModal = ref(false);
const pendingTargetTab = ref('');

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
  geminiModel: 'gemini-3.5-flash-lite',
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

const sectionFields: Record<string, string[]> = {
  jobs: ['driveGuardIntervalMinutes', 'bankTelemetryIntervalMinutes', 'emailCleanupIntervalHours', 'calendarExtractorIntervalHours', 'bulkDeleteThreshold', 'keepAliveKey'],
  alerts: ['enableTelegram', 'telegramBotToken', 'telegramChatId', 'enableDiscord', 'discordWebhookUrl'],
  ai: ['geminiModel', 'defaultLanguage', 'defaultTone', 'maxRequestsPerMinute', 'maxRequestsPerDay', 'aiMonthlyTokenQuota', 'aiWarningTokenThreshold'],
  storage: ['financeFolderId', 'financeSpreadsheetId', 'financeFileNamePattern', 'emailWhitelistDomains'],
};

const getSectionTitle = (tabKey: string) => {
  switch (tabKey) {
    case 'jobs': return 'Tác vụ & Chu kỳ quét';
    case 'alerts': return 'Kênh Thông báo';
    case 'ai': return 'Trí tuệ AI & Quota';
    case 'storage': return 'Lưu trữ Drive & Whitelist';
    default: return tabKey;
  }
};

const isSectionDirty = (sectionKey: string): boolean => {
  if (!originalForm.value) return false;
  const fields = sectionFields[sectionKey];
  if (!fields) return false;

  return fields.some((key) => {
    const currentVal = (form.value as any)[key];
    const origVal = (originalForm.value as any)[key];
    if (Array.isArray(currentVal) && Array.isArray(origVal)) {
      return JSON.stringify(currentVal) !== JSON.stringify(origVal);
    }
    return currentVal !== origVal;
  });
};

const isCurrentTabDirty = computed(() => isSectionDirty(activeTab.value));

const requestTabSwitch = (targetTab: string) => {
  if (targetTab === activeTab.value) return;
  if (isSectionDirty(activeTab.value)) {
    pendingTargetTab.value = targetTab;
    showUnsavedModal.value = true;
  } else {
    activeTab.value = targetTab;
  }
};

const confirmDiscardAndSwitch = () => {
  const fields = sectionFields[activeTab.value] || [];
  for (const f of fields) {
    if (originalForm.value && f in originalForm.value) {
      (form.value as any)[f] = JSON.parse(JSON.stringify((originalForm.value as any)[f]));
    }
  }
  showUnsavedModal.value = false;
  if (pendingTargetTab.value) {
    activeTab.value = pendingTargetTab.value;
    pendingTargetTab.value = '';
  }
};

const saveSection = async (sectionKey: string): Promise<boolean> => {
  savingCurrentSection.value = true;
  try {
    const res: any = await api.put(`/settings/section/${sectionKey}`, form.value);
    if (res.success) {
      const fields = sectionFields[sectionKey] || [];
      if (!originalForm.value) originalForm.value = {};
      for (const f of fields) {
        originalForm.value[f] = JSON.parse(JSON.stringify((form.value as any)[f]));
      }

      showToast({
        severity: 'success',
        summary: 'Đã lưu cài đặt',
        detail: sectionKey === 'jobs'
          ? 'Đã cập nhật chu kỳ và làm mới lịch chạy Hangfire thành công!'
          : `Đã lưu cấu hình ${getSectionTitle(sectionKey)} thành công.`,
      });
      return true;
    } else {
      showToast({
        severity: 'error',
        summary: 'Lưu thất bại',
        detail: res.message || 'Không thể lưu cài đặt.',
      });
      return false;
    }
  } catch (e: any) {
    showToast({
      severity: 'error',
      summary: 'Lỗi',
      detail: e.message || 'Có lỗi xảy ra khi lưu cấu hình.',
    });
    return false;
  } finally {
    savingCurrentSection.value = false;
  }
};

const saveCurrentSection = async () => {
  await saveSection(activeTab.value);
};

const confirmSaveAndSwitch = async () => {
  const success = await saveSection(activeTab.value);
  if (success) {
    showUnsavedModal.value = false;
    if (pendingTargetTab.value) {
      activeTab.value = pendingTargetTab.value;
      pendingTargetTab.value = '';
    }
  }
};

const testGeminiModel = async () => {
  if (!form.value.geminiModel || !form.value.geminiModel.trim()) {
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
      model: form.value.geminiModel.trim(),
    });

    if (res.success && res.data) {
      if (res.data.success) {
        showToast({
          severity: 'success',
          summary: 'Kết nối thành công',
          detail: res.data.message || `Model '${form.value.geminiModel}' hoạt động tốt!`,
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
      originalForm.value = JSON.parse(JSON.stringify(form.value));
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
      originalForm.value = JSON.parse(JSON.stringify(form.value));
      showToast({
        severity: 'success',
        summary: 'Đã lưu tất cả cài đặt',
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
  position: relative;
  max-width: 1100px;
  margin: 0 auto;
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.ambient-glow {
  position: absolute;
  top: -80px;
  left: 50%;
  transform: translate3d(-50%, 0, 0);
  width: 720px;
  height: 320px;
  background: radial-gradient(ellipse at center, rgba(99, 102, 241, 0.12) 0%, rgba(6, 182, 212, 0.05) 45%, transparent 70%);
  pointer-events: none;
  z-index: 0;
  filter: blur(40px);
}

.page-top-bar {
  position: relative;
  z-index: 1;
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 1rem;

  .title-group {
    display: flex;
    align-items: center;
    gap: 0.85rem;

    .title-icon-badge {
      width: 44px;
      height: 44px;
      border-radius: 0.75rem;
      background: linear-gradient(135deg, rgba(99, 102, 241, 0.25), rgba(6, 182, 212, 0.2));
      border: 1px solid rgba(129, 140, 248, 0.4);
      display: flex;
      align-items: center;
      justify-content: center;
      box-shadow: 0 0 16px rgba(99, 102, 241, 0.25);
      flex-shrink: 0;

      i {
        font-size: 1.35rem;
        color: #a5b4fc;
      }
    }

    .title-text {
      h1 {
        font-size: 1.5rem;
        font-weight: 800;
        color: #f8fafc;
        letter-spacing: -0.02em;
        margin: 0;
        display: flex;
        align-items: center;
        gap: 0.6rem;
        flex-wrap: wrap;

        .version-tag {
          font-size: 0.68rem;
          font-weight: 700;
          text-transform: uppercase;
          letter-spacing: 0.08em;
          padding: 0.15rem 0.5rem;
          border-radius: 0.35rem;
          background: rgba(99, 102, 241, 0.18);
          border: 1px solid rgba(99, 102, 241, 0.35);
          color: #818cf8;
        }
      }

      .subtitle {
        font-size: 0.825rem;
        color: #94a3b8;
        margin: 0.2rem 0 0 0;
      }
    }
  }

  .top-actions {
    display: flex;
    align-items: center;
    gap: 0.6rem;
    flex-wrap: wrap;

    button {
      height: 38px;
      padding: 0 1rem;
      border-radius: 0.5rem;
      font-weight: 600;
      font-size: 0.825rem;
      cursor: pointer;
      display: flex;
      align-items: center;
      gap: 0.45rem;
      white-space: nowrap;
      transition: transform 0.15s ease, background 0.15s ease, box-shadow 0.15s ease, border-color 0.15s ease;

      &:active:not(:disabled) {
        transform: translate3d(0, 0.5px, 0) scale(0.98);
      }
    }

    .btn-reset {
      background: rgba(30, 41, 59, 0.65);
      border: 1px solid rgba(148, 163, 184, 0.2);
      color: #cbd5e1;
      backdrop-filter: blur(8px);
      &:hover:not(:disabled) {
        background: rgba(51, 65, 85, 0.85);
        color: #fff;
        transform: translate3d(0, -1px, 0);
      }
      &:disabled { opacity: 0.4; cursor: not-allowed; }
    }

    .btn-save-section {
      background: rgba(16, 185, 129, 0.16);
      border: 1px solid rgba(16, 185, 129, 0.4);
      color: #34d399;
      box-shadow: 0 2px 8px rgba(16, 185, 129, 0.15);
      &:hover:not(:disabled) {
        background: #10b981;
        color: #fff;
        box-shadow: 0 4px 14px rgba(16, 185, 129, 0.35);
        transform: translate3d(0, -1.5px, 0);
      }
      &:disabled {
        opacity: 0.35;
        cursor: not-allowed;
        box-shadow: none;
      }
    }

    .btn-save {
      background: linear-gradient(135deg, #6366f1 0%, #4f46e5 100%);
      border: 1px solid rgba(129, 140, 248, 0.3);
      color: #fff;
      box-shadow: 0 4px 14px rgba(99, 102, 241, 0.35);
      &:hover:not(:disabled) {
        filter: brightness(1.12);
        box-shadow: 0 6px 18px rgba(99, 102, 241, 0.5);
        transform: translate3d(0, -1.5px, 0);
      }
      &:disabled { opacity: 0.45; cursor: not-allowed; box-shadow: none; }
    }
  }
}

.tabs-nav-wrapper {
  position: relative;
  z-index: 1;
  border-radius: 0.75rem;
  background: rgba(15, 23, 42, 0.75);
  border: 1px solid rgba(255, 255, 255, 0.08);
  backdrop-filter: blur(12px);
  padding: 0.3rem;
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.25);

  .tabs-nav {
    display: flex;
    gap: 0.35rem;
    overflow-x: auto;
    scrollbar-width: none;
    -ms-overflow-style: none;
    &::-webkit-scrollbar { display: none; }

    button {
      flex: 1;
      min-width: max-content;
      background: transparent;
      border: none;
      color: #94a3b8;
      padding: 0.6rem 1rem;
      font-size: 0.825rem;
      font-weight: 600;
      border-radius: 0.5rem;
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.45rem;
      white-space: nowrap;
      transition: color 0.15s ease, background 0.15s ease, box-shadow 0.15s ease, transform 0.15s ease;

      i { font-size: 0.95rem; }

      .dirty-badge {
        display: inline-block;
        width: 6px;
        height: 6px;
        border-radius: 50%;
        background: #f59e0b;
        box-shadow: 0 0 8px #f59e0b;
        animation: pulse-dot 2.2s infinite ease-in-out;
      }

      &:hover {
        color: #f1f5f9;
        background: rgba(255, 255, 255, 0.05);
      }

      &.active {
        background: linear-gradient(135deg, #6366f1, #4f46e5);
        color: #fff;
        box-shadow: 0 4px 14px rgba(99, 102, 241, 0.35);
      }
    }
  }
}

@keyframes pulse-dot {
  0%, 100% { opacity: 1; transform: scale(1); }
  50% { opacity: 0.4; transform: scale(1.3); }
}

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

.whitelist-input-group {
  display: flex;
  gap: 0.6rem;
  margin-bottom: 0.85rem;

  input {
    flex: 1;
    height: 38px;
    background: rgba(11, 17, 32, 0.85);
    border: 1px solid rgba(148, 163, 184, 0.2);
    border-radius: 0.5rem;
    padding: 0.5rem 0.75rem;
    color: #f8fafc;
    font-size: 0.85rem;
    &:focus { outline: none; border-color: #6366f1; box-shadow: 0 0 0 2px rgba(99, 102, 241, 0.25); }
  }

  .btn-add-domain {
    height: 38px;
    background: linear-gradient(135deg, #6366f1, #4f46e5);
    border: none;
    color: #fff;
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
    &:hover:not(:disabled) { filter: brightness(1.1); transform: translate3d(0, -1px, 0); }
    &:disabled { opacity: 0.45; cursor: not-allowed; }
  }
}

.empty-hint {
  font-size: 0.775rem;
  color: #64748b;
  font-style: italic;
}

.chips-container {
  display: flex;
  flex-wrap: wrap;
  gap: 0.45rem;

  .domain-chip {
    background: rgba(99, 102, 241, 0.15);
    border: 1px solid rgba(99, 102, 241, 0.3);
    color: #a5b4fc;
    padding: 0.25rem 0.6rem;
    border-radius: 9999px;
    font-size: 0.775rem;
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
      transition: color 0.15s;
      &:hover { color: #f87171; }
    }
  }
}

// Token AI Usage Panel Styles
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

/* Unsaved Changes Modal */
.unsaved-modal-overlay {
  position: fixed;
  inset: 0;
  z-index: 9999;
  background: rgba(3, 7, 18, 0.75);
  backdrop-filter: blur(6px);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 1rem;
}

.unsaved-modal-dialog {
  background: linear-gradient(145deg, #1e293b 0%, #0f172a 100%);
  border: 1px solid rgba(148, 163, 184, 0.2);
  border-top: 1px solid rgba(255, 255, 255, 0.2);
  border-radius: 1rem;
  width: 100%;
  max-width: 480px;
  box-shadow: 0 24px 48px -12px rgba(0, 0, 0, 0.7), 0 0 24px rgba(99, 102, 241, 0.1);
  overflow: hidden;
  animation: modal-spring 0.22s cubic-bezier(0.16, 1, 0.3, 1);

  .unsaved-modal-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 1rem 1.25rem;
    border-bottom: 1px solid rgba(255, 255, 255, 0.08);

    .header-title {
      display: flex;
      align-items: center;
      gap: 0.6rem;

      .warning-icon {
        font-size: 1.25rem;
        color: #f59e0b;
      }

      h3 {
        margin: 0;
        font-size: 1.05rem;
        font-weight: 700;
        color: #f8fafc;
      }
    }

    .btn-close-modal {
      background: transparent;
      border: none;
      color: #94a3b8;
      cursor: pointer;
      font-size: 1rem;
      padding: 0.25rem;
      display: flex;
      align-items: center;
      justify-content: center;
      border-radius: 0.25rem;
      &:hover { color: #fff; background: rgba(255, 255, 255, 0.08); }
    }
  }

  .unsaved-modal-body {
    padding: 1.25rem;
    color: #cbd5e1;
    font-size: 0.9rem;
    line-height: 1.55;

    p { margin: 0 0 0.5rem 0; }
    strong { color: #f8fafc; }
    .sub-desc { font-size: 0.825rem; color: #94a3b8; margin: 0; }
  }

  .unsaved-modal-footer {
    display: flex;
    justify-content: flex-end;
    align-items: center;
    gap: 0.6rem;
    padding: 1rem 1.25rem;
    background: rgba(11, 17, 32, 0.5);
    border-top: 1px solid rgba(255, 255, 255, 0.08);
    flex-wrap: wrap;

    button {
      height: 38px;
      padding: 0 1rem;
      border-radius: 0.5rem;
      font-weight: 600;
      font-size: 0.825rem;
      cursor: pointer;
      display: flex;
      align-items: center;
      gap: 0.4rem;
      transition: all 0.15s ease;
    }

    .btn-modal-cancel {
      background: transparent;
      border: 1px solid rgba(255, 255, 255, 0.15);
      color: #cbd5e1;
      &:hover { background: rgba(255, 255, 255, 0.08); color: #fff; }
    }

    .btn-modal-discard {
      background: rgba(239, 68, 68, 0.15);
      border: 1px solid rgba(239, 68, 68, 0.35);
      color: #f87171;
      &:hover { background: #ef4444; color: #fff; }
    }

    .btn-modal-save {
      background: linear-gradient(135deg, #10b981, #059669);
      border: 1px solid rgba(52, 211, 153, 0.3);
      color: #fff;
      box-shadow: 0 4px 12px rgba(16, 185, 129, 0.25);
      &:hover:not(:disabled) { filter: brightness(1.1); transform: translate3d(0, -1px, 0); }
      &:disabled { opacity: 0.5; cursor: not-allowed; }
    }
  }
}

@keyframes modal-spring {
  from { opacity: 0; transform: translate3d(0, 8px, 0) scale(0.96); }
  to { opacity: 1; transform: translate3d(0, 0, 0) scale(1); }
}

.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}

/* ============================================================
   RESPONSIVE LAYOUT SYSTEM
   ============================================================ */

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
  .settings-page {
    gap: 1rem;
    padding: 0.25rem 0.5rem;
  }

  .page-top-bar {
    flex-direction: column;
    align-items: stretch;
    gap: 0.75rem;

    .title-group {
      gap: 0.65rem;

      .title-icon-badge {
        width: 38px;
        height: 38px;
        border-radius: 0.6rem;
        i { font-size: 1.15rem; }
      }

      .title-text h1 {
        font-size: 1.25rem;
      }
    }

    .top-actions {
      width: 100%;
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 0.5rem;

      .btn-reset {
        grid-column: span 1;
        justify-content: center;
      }

      .btn-save-section {
        grid-column: span 1;
        justify-content: center;
      }

      .btn-save {
        grid-column: span 2;
        justify-content: center;
      }
    }
  }

  .tabs-nav-wrapper .tabs-nav button {
    padding: 0.5rem 0.75rem;
    font-size: 0.775rem;
  }

  .card-box {
    padding: 1rem;
    border-radius: 0.75rem;
  }

  .model-input-row,
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

  .unsaved-modal-footer {
    flex-direction: column-reverse;
    align-items: stretch;

    button {
      width: 100%;
      justify-content: center;
      min-height: 42px;
    }
  }
}

@media (max-width: 420px) {
  .whitelist-input-group {
    flex-direction: column;

    .btn-add-domain {
      width: 100%;
      justify-content: center;
    }
  }
}
</style>
