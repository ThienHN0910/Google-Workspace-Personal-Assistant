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
      <SettingsJobsTab
        v-if="activeTab === 'jobs'"
        :form="form"
        :saving-current-section="savingCurrentSection"
        :is-dirty="isSectionDirty('jobs')"
        @save="saveCurrentSection"
        @update:dirty="(val: boolean) => handleTabDirty('jobs', val)"
      />
      <SettingsAlertsTab
        v-if="activeTab === 'alerts'"
        :form="form"
        :saving-current-section="savingCurrentSection"
        :is-dirty="isSectionDirty('alerts')"
        @save="saveCurrentSection"
        @update:dirty="(val: boolean) => handleTabDirty('alerts', val)"
      />
      <SettingsAiTab
        v-if="activeTab === 'ai'"
        :form="form"
        :saving-current-section="savingCurrentSection"
        :is-dirty="isSectionDirty('ai')"
        @save="saveCurrentSection"
        @update:dirty="(val: boolean) => handleTabDirty('ai', val)"
      />
      <SettingsStorageTab
        v-if="activeTab === 'storage'"
        :form="form"
        :saving-current-section="savingCurrentSection"
        :is-dirty="isSectionDirty('storage')"
        @save="saveCurrentSection"
        @update:dirty="(val: boolean) => handleTabDirty('storage', val)"
      />
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
import SettingsJobsTab from '@/components/settings/SettingsJobsTab.vue';
import SettingsAlertsTab from '@/components/settings/SettingsAlertsTab.vue';
import SettingsAiTab from '@/components/settings/SettingsAiTab.vue';
import SettingsStorageTab from '@/components/settings/SettingsStorageTab.vue';

const LoadingSpinner = defineAsyncComponent(() => import('@/components/common/LoadingSpinner.vue'));

const activeTab = ref('jobs');
const loading = ref(false);
const saving = ref(false);
const savingCurrentSection = ref(false);

const originalForm = ref<any>(null);
const showUnsavedModal = ref(false);
const pendingTargetTab = ref('');

export interface SettingsFormData {
  // Jobs
  driveGuardIntervalMinutes: number;
  bankTelemetryIntervalMinutes: number;
  emailCleanupIntervalHours: number;
  calendarExtractorIntervalHours: number;
  bulkDeleteThreshold: number;

  // Alerts
  enableTelegram: boolean;
  telegramBotToken: string;
  telegramChatId: string;
  enableDiscord: boolean;
  discordWebhookUrl: string;

  // AI
  geminiModel: string;
  defaultLanguage: string;
  defaultTone: string;
  maxRequestsPerMinute: number;
  maxRequestsPerDay: number;
  aiMonthlyTokenQuota: number;
  aiWarningTokenThreshold: number;

  // Storage
  financeFolderId: string;
  financeSpreadsheetId: string;
  financeFileNamePattern: string;
  emailWhitelistDomains: string[];

  // Anti-Sleep Keep-Alive
  keepAliveKey: string;
}

const form = ref<SettingsFormData>({
  // Jobs
  driveGuardIntervalMinutes: 120,
  bankTelemetryIntervalMinutes: 120,
  emailCleanupIntervalHours: 2,
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

const tabDirtyMap = ref<Record<string, boolean>>({
  jobs: false,
  alerts: false,
  ai: false,
  storage: false,
});

const handleTabDirty = (tabKey: string, isDirty: boolean) => {
  tabDirtyMap.value[tabKey] = isDirty;
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

onMounted(() => {
  fetchSettings();
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
</style>
