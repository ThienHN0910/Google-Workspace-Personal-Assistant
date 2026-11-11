<template>
  <div class="tab-panel">
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
      <span v-if="isDirty" class="dirty-notice">
        <i class="pi pi-exclamation-circle"></i> Cấu hình Drive & Whitelist có thay đổi chưa lưu.
      </span>
      <button
        class="btn-save-section-bottom"
        @click="emit('save')"
        :disabled="savingCurrentSection || !isDirty"
      >
        <i class="pi" :class="savingCurrentSection ? 'pi-spin pi-spinner' : 'pi-save'"></i>
        {{ savingCurrentSection ? 'Đang lưu...' : 'Lưu cấu hình Drive & Whitelist' }}
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';

export interface StorageSettingsForm {
  financeFolderId: string;
  financeSpreadsheetId: string;
  financeFileNamePattern: string;
  emailWhitelistDomains: string[];
}

const props = defineProps<{
  form: StorageSettingsForm;
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

const newWhitelistDomain = ref('');

const addWhitelistDomain = () => {
  const domain = newWhitelistDomain.value.trim().toLowerCase().replace(/^@/, '');
  if (domain && !props.form.emailWhitelistDomains.includes(domain)) {
    props.form.emailWhitelistDomains.push(domain);
    newWhitelistDomain.value = '';
  }
};

const removeWhitelistDomain = (idx: number) => {
  props.form.emailWhitelistDomains.splice(idx, 1);
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
