<template>
  <div class="finance-page">
    <header class="page-header">
      <div class="title-group">
        <div class="title-icon-badge">
          <i class="pi pi-credit-card"></i>
        </div>
        <div class="title-text">
          <div class="title-row">
            <h1>Telemetry Tài Chính & Biến Động Số Dư</h1>
            <span class="version-tag">UC04 Finance</span>
          </div>
          <p class="subtitle">Báo cáo thu/chi tự động, trích xuất email ngân hàng & Đồng bộ Google Sheets</p>
        </div>
      </div>
      <div class="header-actions">
        <button class="btn-config" @click="showConfigPanel = !showConfigPanel">
          <i class="pi pi-cog"></i> Cấu hình Drive & Sheets
        </button>
        <div class="sync-controls">
          <select v-model="selectedBank" class="bank-select" :disabled="syncState.isSyncing">
            <option v-for="opt in bankOptions" :key="opt.value" :value="opt.value">
              {{ opt.label }}
            </option>
          </select>
          <button class="btn-sync" @click="syncTransactions" :disabled="syncState.isSyncing">
            <span v-if="!syncState.isSyncing"><i class="pi pi-sync"></i> Đồng bộ ngay</span>
            <span v-else><i class="pi pi-spin pi-spinner"></i> Đang nhờ AI phân tích...</span>
          </button>
        </div>
      </div>
    </header>

    <!-- Custom Bank Input Bar -->
    <div v-if="selectedBank === 'CUSTOM'" class="custom-bank-bar cyber-card">
      <div class="custom-bank-inputs">
        <input v-model="customDomain" placeholder="Domain gửi thư (vd: custombank.com.vn)..." />
        <input v-model="customBankName" placeholder="Tên ngân hàng (vd: MyBank)..." />
      </div>
      <span class="field-hint">Hệ thống sẽ lọc các thư chưa đọc từ domain này và nhờ AI phân tích giao dịch.</span>
    </div>

    <!-- Config Panel -->
    <div v-if="showConfigPanel" class="config-panel cyber-card">
      <div class="config-header">
        <h3><i class="pi pi-sliders-h"></i> Cấu hình Xuất File Google Drive & Sheets</h3>
        <button class="close-config-btn" @click="showConfigPanel = false">
          <i class="pi pi-times"></i>
        </button>
      </div>
      <p class="config-desc">
        Tùy chỉnh thư mục lưu file trên Google Drive, định dạng tên file tự động theo tháng, hoặc chỉ định mã Spreadsheet cố định.
      </p>

      <div class="config-grid">
        <div class="form-group">
          <label>📁 Thư mục Google Drive (Folder ID):</label>
          <input
            v-model="config.folderId"
            type="text"
            placeholder="Ví dụ: 1a2b3c4d5e6f7g8h9i... (Mã folder trên URL Drive)"
          />
          <span class="field-hint">Để trống nếu muốn lưu trực tiếp tại thư mục gốc Drive của bạn.</span>
        </div>

        <div class="form-group">
          <label>📝 Mẫu tên file (FileName Pattern):</label>
          <input
            v-model="config.fileNamePattern"
            type="text"
            placeholder="BaoCaoTaiChinh_{yyyy_MM}"
          />
          <span class="field-hint">Hỗ trợ các thẻ: <code>{yyyy_MM}</code>, <code>{yyyy-MM}</code>, <code>{yyyy}</code>, <code>{MM}</code>. Mỗi tháng sẽ tạo 1 file riêng.</span>
        </div>

        <div class="form-group">
          <label>📊 Mã File Google Sheet cố định (Tùy chọn):</label>
          <input
            v-model="config.spreadsheetId"
            type="text"
            placeholder="Ví dụ: 1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms"
          />
          <span class="field-hint">Chỉ điền nếu bạn muốn dồn TẤT CẢ giao dịch vào 1 file cố định duy nhất thay vì tạo mới theo tháng.</span>
        </div>
      </div>

      <div class="config-actions">
        <button class="btn-save-config" @click="saveConfig" :disabled="savingConfig">
          <span v-if="!savingConfig"><i class="pi pi-save"></i> Lưu Cấu Hình</span>
          <span v-else><i class="pi pi-spin pi-spinner"></i> Đang lưu...</span>
        </button>
        <span v-if="configStatusMsg" class="config-msg">{{ configStatusMsg }}</span>
      </div>
    </div>

    <!-- Sync Banner -->
    <div v-if="syncState.isSyncing" class="sync-banner">
      <div class="banner-pulse"></div>
      <i class="pi pi-spin pi-spinner"></i>
      <span>Hệ thống đang nén tất cả email chưa đọc và gửi cho AI xử lý trong 1 lần. Vui lòng đợi vài giây...</span>
    </div>

    <!-- Summary KPI Bento Grid -->
    <div class="summary-cards">
      <div class="summary-card income">
        <div class="card-icon">
          <i class="pi pi-arrow-up-right"></i>
        </div>
        <div class="card-body">
          <span class="card-label">Tổng Thu (Tháng này)</span>
          <h3 class="card-value">{{ formatCurrency(monthlySummary.totalIncome) }}</h3>
        </div>
      </div>
      <div class="summary-card expense">
        <div class="card-icon">
          <i class="pi pi-arrow-down-right"></i>
        </div>
        <div class="card-body">
          <span class="card-label">Tổng Chi (Tháng này)</span>
          <h3 class="card-value">{{ formatCurrency(monthlySummary.totalExpense) }}</h3>
        </div>
      </div>
      <div class="summary-card balance">
        <div class="card-icon">
          <i class="pi pi-wallet"></i>
        </div>
        <div class="card-body">
          <div class="card-label-row">
            <span class="card-label">Số dư ròng (Net)</span>
            <span class="balance-pill" :class="monthlySummary.netBalance >= 0 ? 'positive' : 'negative'">
              {{ monthlySummary.netBalance >= 0 ? '+ Thặng dư' : '- Thâm hụt' }}
            </span>
          </div>
          <h3 class="card-value" :class="{ positive: monthlySummary.netBalance >= 0, negative: monthlySummary.netBalance < 0 }">
            {{ formatCurrency(monthlySummary.netBalance) }}
          </h3>
        </div>
      </div>
    </div>

    <LoadingSpinner v-if="loading && transactions.length === 0" text="Đang tải giao dịch..." />

    <!-- Transactions Cyber Table -->
    <div v-else class="transaction-table cyber-card">
      <div class="table-toolbar">
        <div class="table-title">
          <i class="pi pi-list"></i>
          <span>Nhật Ký Biến Động Số Dư ({{ transactions.length }} bản ghi)</span>
        </div>
      </div>
      <div class="table-responsive">
        <table>
          <thead>
            <tr>
              <th>Mã GD</th>
              <th>Thời gian</th>
              <th>Ngân hàng</th>
              <th>Loại</th>
              <th>Số tiền</th>
              <th>Phí</th>
              <th>TK Trích</th>
              <th>TK Ghi</th>
              <th>Người hưởng / Đối tác</th>
              <th>Danh mục</th>
              <th>Nội dung</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="t in transactions" :key="t.id">
              <td class="code-col"><code>{{ t.transactionCode || '—' }}</code></td>
              <td class="time-col">{{ formatDate(t.transactionDate) }}</td>
              <td><span class="bank-tag">{{ t.bankName }}</span></td>
              <td>
                <span class="type-tag" :class="t.transactionType === 0 ? 'credit' : 'debit'">
                  <i :class="t.transactionType === 0 ? 'pi pi-arrow-down-left' : 'pi pi-arrow-up-right'"></i>
                  {{ t.transactionType === 0 ? 'Nhận' : 'Chi' }}
                </span>
              </td>
              <td class="amount" :class="t.transactionType === 0 ? 'credit' : 'debit'">
                {{ t.transactionType === 0 ? '+' : '-' }}{{ formatCurrency(t.amount) }}
              </td>
              <td class="fee-col">{{ t.feeAmount ? formatCurrency(t.feeAmount) : '0 ₫' }}</td>
              <td class="account-col"><code>{{ t.sourceAccount || '—' }}</code></td>
              <td class="account-col"><code>{{ t.targetAccount || '—' }}</code></td>
              <td class="beneficiary-col"><strong>{{ t.beneficiaryName || '—' }}</strong></td>
              <td><span class="category-chip">{{ t.category }}</span></td>
              <td class="desc" :title="t.description">{{ t.description }}</td>
            </tr>
          </tbody>
        </table>
      </div>
      <InfiniteScrollObserver :loading="loading" :has-more="hasMore" @load-more="loadMore" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, defineAsyncComponent } from 'vue';
import api from '@/services/api.service';
import LoadingSpinner from '@/components/common/LoadingSpinner.vue';
import { showToast } from '@/services/notification.service';

const InfiniteScrollObserver = defineAsyncComponent(() => import('@/components/common/InfiniteScrollObserver.vue'));

const showConfigPanel = ref(false);
const savingConfig = ref(false);
const configStatusMsg = ref('');
const config = ref({
  folderId: '',
  fileNamePattern: 'BaoCaoTaiChinh_{yyyy_MM}',
  spreadsheetId: ''
});

const bankOptions = [
  { label: '🔄 Tất cả ngân hàng (Khuyên dùng)', value: 'ALL', domain: '' },
  { label: 'VPBank (vpb.com.vn)', value: 'VPB', domain: 'vpb.com.vn', bankName: 'VPBank' },
  { label: 'Vietcombank (vietcombank.com.vn)', value: 'VCB', domain: 'vietcombank.com.vn', bankName: 'Vietcombank' },
  { label: 'Techcombank (techcombank.com.vn)', value: 'TCB', domain: 'techcombank.com.vn', bankName: 'Techcombank' },
  { label: 'MB Bank (mbbank.com.vn)', value: 'MB', domain: 'mbbank.com.vn', bankName: 'MBBank' },
  { label: 'TPBank (tpb.com.vn)', value: 'TPB', domain: 'tpb.com.vn', bankName: 'TPBank' },
  { label: 'MoMo (momo.vn)', value: 'MOMO', domain: 'momo.vn', bankName: 'MoMo' },
  { label: '➕ Tùy chỉnh domain khác...', value: 'CUSTOM', domain: '' }
];

const selectedBank = ref('ALL');
const customDomain = ref('');
const customBankName = ref('');

const monthlySummary = ref({
  totalIncome: 0,
  totalExpense: 0,
  netBalance: 0,
  totalTransactions: 0
});

const fetchConfig = async () => {
  try {
    const res: any = await api.get('/finance/config');
    if (res.success && res.data) {
      config.value = {
        folderId: res.data.folderId || '',
        fileNamePattern: res.data.fileNamePattern || 'BaoCaoTaiChinh_{yyyy_MM}',
        spreadsheetId: res.data.spreadsheetId || ''
      };
    }
  } catch (e) {
    console.error('Failed to fetch finance config:', e);
  }
};

const saveConfig = async () => {
  savingConfig.value = true;
  configStatusMsg.value = '';
  try {
    const res: any = await api.post('/finance/config', {
      FolderId: config.value.folderId,
      FileNamePattern: config.value.fileNamePattern,
      SpreadsheetId: config.value.spreadsheetId
    });
    if (res.success) {
      showToast({
        severity: 'success',
        summary: 'Đã lưu',
        detail: 'Cấu hình Google Drive & Sheets đã được lưu.',
      });
      configStatusMsg.value = '✅ Đã lưu cấu hình thành công!';
      setTimeout(() => { configStatusMsg.value = ''; }, 3000);
    } else {
      showToast({
        severity: 'error',
        summary: 'Lỗi',
        detail: res.message || 'Lỗi khi lưu cấu hình.',
      });
    }
  } catch (e) {
    showToast({
      severity: 'error',
      summary: 'Lỗi',
      detail: 'Không thể lưu cấu hình Drive & Sheets.',
    });
  } finally {
    savingConfig.value = false;
  }
};

const transactions = ref<any[]>([]);
const loading = ref(true);

const syncState = ref({
  isSyncing: false
});

const fetchMonthlySummary = async () => {
  try {
    const res: any = await api.get('/finance/summary-month');
    if (res.success && res.data) {
      monthlySummary.value = res.data;
    }
  } catch (err) {
    console.error('Failed to load monthly finance summary:', err);
  }
};

const syncTransactions = async () => {
  if (syncState.value.isSyncing) return;
  syncState.value.isSyncing = true;

  try {
    const payload: any = {
      SpreadsheetId: config.value.spreadsheetId || ''
    };

    if (selectedBank.value === 'ALL') {
      payload.Targets = [
        { Domain: 'vpb.com.vn', BankName: 'VPBank' },
        { Domain: 'vietcombank.com.vn', BankName: 'Vietcombank' },
        { Domain: 'techcombank.com.vn', BankName: 'Techcombank' },
        { Domain: 'mbbank.com.vn', BankName: 'MBBank' },
        { Domain: 'tpb.com.vn', BankName: 'TPBank' },
        { Domain: 'momo.vn', BankName: 'MoMo' },
      ];
    } else if (selectedBank.value === 'CUSTOM') {
      if (!customDomain.value.trim()) {
        showToast({
          severity: 'warn',
          summary: 'Thiếu domain',
          detail: 'Vui lòng nhập domain email ngân hàng cần quét.',
        });
        syncState.value.isSyncing = false;
        return;
      }
      payload.Domain = customDomain.value.trim();
      payload.BankName = customBankName.value.trim() || 'Custom Bank';
    } else {
      const b = bankOptions.find(opt => opt.value === selectedBank.value);
      if (b) {
        payload.Domain = b.domain;
        payload.BankName = b.bankName;
      }
    }

    const res: any = await api.post('/finance/transactions/sync-batch', payload);
    if (res.success) {
      showToast({
        severity: 'success',
        summary: 'Đồng bộ hoàn tất',
        detail: `Đã quét và đồng bộ ${res.data} giao dịch mới vào Google Sheets!`,
      });
      await fetchTransactions(1);
      await fetchMonthlySummary();
    } else {
      showToast({
        severity: 'error',
        summary: 'Lỗi',
        detail: res.message || 'Lỗi khi đồng bộ.',
      });
    }
  } catch (e: any) {
    showToast({
      severity: 'error',
      summary: 'Lỗi đồng bộ',
      detail: e.message || 'Có lỗi xảy ra trong quá trình đồng bộ.',
    });
  } finally {
    syncState.value.isSyncing = false;
  }
};

const page = ref(1);
const hasMore = ref(true);

const fetchTransactions = async (pageIndex = 1) => {
  loading.value = true;
  try {
    const res: any = await api.get(`/finance/transactions?page=${pageIndex}&pageSize=20`);
    if (res.success && res.data) {
      if (pageIndex === 1) {
        transactions.value = res.data.items;
      } else {
        transactions.value = [...transactions.value, ...res.data.items];
      }
      hasMore.value = pageIndex < res.data.totalPages;
      page.value = pageIndex;
    }
  } catch (e) {
    console.error('Failed to fetch transactions:', e);
  } finally {
    loading.value = false;
  }
};

const loadMore = () => {
  if (!loading.value && hasMore.value) {
    fetchTransactions(page.value + 1);
  }
};

const formatDate = (dateStr: string) => {
  return new Date(dateStr).toLocaleString('vi-VN');
};

const formatCurrency = (val: number) => {
  return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(val);
};

onMounted(() => {
  fetchTransactions(1);
  fetchMonthlySummary();
  fetchConfig();
});
</script>

<style scoped lang="scss">
.finance-page {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

/* ============================================================
   PAGE HEADER
   ============================================================ */
.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 1.25rem;
  padding-bottom: 0.5rem;

  .title-group {
    display: flex;
    align-items: center;
    gap: 0.85rem;

    .title-icon-badge {
      width: 44px;
      height: 44px;
      border-radius: 0.75rem;
      background: linear-gradient(135deg, rgba(6, 182, 212, 0.25) 0%, rgba(99, 102, 241, 0.2) 100%);
      border: 1px solid rgba(6, 182, 212, 0.4);
      display: flex;
      align-items: center;
      justify-content: center;
      color: #22d3ee;
      font-size: 1.35rem;
      box-shadow: 0 0 20px rgba(6, 182, 212, 0.2);
      flex-shrink: 0;
    }

    .title-text {
      .title-row {
        display: flex;
        align-items: center;
        gap: 0.6rem;
        flex-wrap: wrap;

        h1 {
          font-size: 1.45rem;
          font-weight: 800;
          color: #f8fafc;
          letter-spacing: -0.02em;
          margin: 0;
        }

        .version-tag {
          font-size: 0.7rem;
          font-weight: 700;
          padding: 0.15rem 0.55rem;
          border-radius: 9999px;
          background: rgba(6, 182, 212, 0.15);
          border: 1px solid rgba(6, 182, 212, 0.35);
          color: #67e8f9;
        }
      }

      .subtitle {
        color: #94a3b8;
        font-size: 0.85rem;
        margin: 0.2rem 0 0;
      }
    }
  }

  .header-actions {
    display: flex;
    gap: 0.65rem;
    align-items: center;
    flex-wrap: wrap;
  }
}

.btn-config {
  height: 38px;
  background: rgba(15, 23, 42, 0.6);
  color: #cbd5e1;
  border: 1px solid rgba(255, 255, 255, 0.12);
  padding: 0 1rem;
  border-radius: 0.5rem;
  font-weight: 600;
  font-size: 0.825rem;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 0.45rem;
  transition: all 0.15s ease;

  &:hover {
    background: rgba(255, 255, 255, 0.08);
    color: #fff;
    border-color: rgba(255, 255, 255, 0.2);
    transform: translate3d(0, -1px, 0);
  }
}

.sync-controls {
  display: flex;
  gap: 0.5rem;
  align-items: center;

  .bank-select {
    height: 38px;
    background: #0b1120;
    border: 1px solid rgba(255, 255, 255, 0.12);
    color: #f8fafc;
    padding: 0 0.85rem;
    border-radius: 0.5rem;
    font-size: 0.825rem;
    font-weight: 600;
    cursor: pointer;
    outline: none;
    transition: border-color 0.15s ease;

    &:focus {
      border-color: #38bdf8;
    }
  }

  .btn-sync {
    height: 38px;
    background: linear-gradient(135deg, #06b6d4 0%, #0284c7 100%);
    color: #fff;
    border: 1px solid rgba(6, 182, 212, 0.4);
    padding: 0 1.15rem;
    border-radius: 0.5rem;
    font-weight: 600;
    font-size: 0.825rem;
    cursor: pointer;
    display: inline-flex;
    align-items: center;
    gap: 0.45rem;
    transition: all 0.15s ease;
    box-shadow: 0 2px 10px rgba(6, 182, 212, 0.25);

    &:hover:not(:disabled) {
      filter: brightness(1.1);
      transform: translate3d(0, -1px, 0);
      box-shadow: 0 4px 16px rgba(6, 182, 212, 0.4);
    }

    &:disabled {
      opacity: 0.6;
      cursor: not-allowed;
      transform: none;
    }
  }
}

/* ============================================================
   CUSTOM BANK BAR
   ============================================================ */
.custom-bank-bar {
  padding: 1.1rem 1.25rem;
  background: rgba(11, 17, 32, 0.75);
  backdrop-filter: blur(12px);
  border: 1px dashed rgba(6, 182, 212, 0.4);
  border-radius: 0.75rem;

  .custom-bank-inputs {
    display: flex;
    gap: 0.85rem;
    margin-bottom: 0.4rem;

    input {
      flex: 1;
      height: 38px;
      background: #070b14;
      border: 1px solid rgba(255, 255, 255, 0.12);
      border-radius: 0.5rem;
      padding: 0 0.85rem;
      color: #f8fafc;
      font-size: 0.85rem;
      &:focus { outline: none; border-color: #38bdf8; }
    }
  }

  .field-hint {
    font-size: 0.75rem;
    color: #64748b;
  }
}

/* ============================================================
   CONFIG PANEL
   ============================================================ */
.config-panel {
  background: rgba(11, 17, 32, 0.9);
  backdrop-filter: blur(16px);
  border: 1px solid rgba(99, 102, 241, 0.35);
  border-radius: 1rem;
  padding: 1.5rem;
  box-shadow: 0 20px 40px rgba(0, 0, 0, 0.4);

  .config-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 0.5rem;

    h3 {
      margin: 0;
      color: #f8fafc;
      font-size: 1.15rem;
      font-weight: 700;
      display: flex;
      align-items: center;
      gap: 0.5rem;
      i { color: #818cf8; }
    }

    .close-config-btn {
      background: none;
      border: none;
      color: #94a3b8;
      cursor: pointer;
      font-size: 1.1rem;
      padding: 0.25rem;
      &:hover { color: #fff; }
    }
  }

  .config-desc {
    color: #94a3b8;
    font-size: 0.85rem;
    margin-bottom: 1.25rem;
  }

  .config-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
    gap: 1.25rem;
    margin-bottom: 1.25rem;
  }

  .form-group {
    display: flex;
    flex-direction: column;
    gap: 0.4rem;

    label {
      font-size: 0.825rem;
      font-weight: 600;
      color: #cbd5e1;
    }

    input {
      height: 38px;
      background: #070b14;
      border: 1px solid rgba(255, 255, 255, 0.12);
      border-radius: 0.5rem;
      padding: 0 0.85rem;
      color: #f8fafc;
      font-size: 0.85rem;

      &:focus {
        outline: none;
        border-color: #6366f1;
      }
    }

    .field-hint {
      font-size: 0.725rem;
      color: #64748b;
      line-height: 1.3;

      code {
        background: rgba(0, 0, 0, 0.4);
        padding: 0.1rem 0.3rem;
        border-radius: 0.25rem;
        color: #818cf8;
      }
    }
  }

  .config-actions {
    display: flex;
    align-items: center;
    gap: 1rem;

    .btn-save-config {
      height: 38px;
      background: linear-gradient(135deg, #10b981 0%, #059669 100%);
      color: white;
      border: 1px solid rgba(16, 185, 129, 0.4);
      padding: 0 1.25rem;
      border-radius: 0.5rem;
      font-weight: 600;
      font-size: 0.825rem;
      cursor: pointer;
      display: inline-flex;
      align-items: center;
      gap: 0.45rem;
      transition: all 0.15s ease;
      box-shadow: 0 2px 10px rgba(16, 185, 129, 0.25);

      &:hover:not(:disabled) {
        filter: brightness(1.1);
        transform: translate3d(0, -1px, 0);
        box-shadow: 0 4px 16px rgba(16, 185, 129, 0.4);
      }

      &:disabled { opacity: 0.6; cursor: not-allowed; }
    }

    .config-msg {
      font-size: 0.85rem;
      font-weight: 600;
      color: #34d399;
    }
  }
}

/* ============================================================
   SYNC BANNER
   ============================================================ */
.sync-banner {
  background: rgba(245, 158, 11, 0.12);
  color: #fbbf24;
  padding: 0.85rem 1.25rem;
  border-radius: 0.6rem;
  font-weight: 500;
  font-size: 0.85rem;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.75rem;
  border: 1px solid rgba(245, 158, 11, 0.3);

  .banner-pulse {
    width: 8px;
    height: 8px;
    border-radius: 50%;
    background: #f59e0b;
    box-shadow: 0 0 10px #f59e0b;
    animation: neonPulse 1.5s infinite;
  }
}

/* ============================================================
   SUMMARY CARDS (BENTO KPI TELEMETRY)
   ============================================================ */
.summary-cards {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 1.25rem;

  .summary-card {
    background: rgba(11, 17, 32, 0.75);
    backdrop-filter: blur(16px);
    border: 1px solid rgba(255, 255, 255, 0.08);
    border-radius: 1rem;
    padding: 1.35rem;
    display: flex;
    align-items: center;
    gap: 1.25rem;
    transition: transform 0.15s ease, border-color 0.15s ease;

    &:hover {
      transform: translate3d(0, -2px, 0);
    }

    .card-icon {
      width: 50px;
      height: 50px;
      border-radius: 0.75rem;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 1.5rem;
      flex-shrink: 0;
    }

    .card-body {
      flex: 1;
      min-width: 0;

      .card-label {
        color: #94a3b8;
        font-size: 0.75rem;
        font-weight: 700;
        text-transform: uppercase;
        letter-spacing: 0.04em;
      }

      .card-label-row {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 0.5rem;

        .balance-pill {
          font-size: 0.675rem;
          font-weight: 700;
          padding: 0.1rem 0.45rem;
          border-radius: 9999px;

          &.positive {
            background: rgba(16, 185, 129, 0.15);
            border: 1px solid rgba(16, 185, 129, 0.3);
            color: #34d399;
          }

          &.negative {
            background: rgba(239, 68, 68, 0.15);
            border: 1px solid rgba(239, 68, 68, 0.3);
            color: #fca5a5;
          }
        }
      }

      .card-value {
        margin: 0.35rem 0 0 0;
        font-size: 1.45rem;
        font-weight: 800;
        color: #f8fafc;
        letter-spacing: -0.02em;
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;

        &.positive { color: #34d399; text-shadow: 0 0 16px rgba(52, 211, 153, 0.25); }
        &.negative { color: #f87171; text-shadow: 0 0 16px rgba(248, 113, 113, 0.25); }
      }
    }

    &.income {
      border-left: 3px solid #10b981;
      .card-icon {
        background: rgba(16, 185, 129, 0.15);
        border: 1px solid rgba(16, 185, 129, 0.3);
        color: #34d399;
      }
      &:hover { border-color: rgba(16, 185, 129, 0.4); }
    }

    &.expense {
      border-left: 3px solid #ef4444;
      .card-icon {
        background: rgba(239, 68, 68, 0.15);
        border: 1px solid rgba(239, 68, 68, 0.3);
        color: #fca5a5;
      }
      &:hover { border-color: rgba(239, 68, 68, 0.4); }
    }

    &.balance {
      border-left: 3px solid #6366f1;
      .card-icon {
        background: rgba(99, 102, 241, 0.15);
        border: 1px solid rgba(99, 102, 241, 0.3);
        color: #818cf8;
      }
      &:hover { border-color: rgba(99, 102, 241, 0.4); }
    }
  }
}

/* ============================================================
   TRANSACTIONS TABLE
   ============================================================ */
.transaction-table {
  background: rgba(11, 17, 32, 0.85);
  backdrop-filter: blur(16px);
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 1rem;
  overflow: hidden;
  box-shadow: 0 16px 36px rgba(0, 0, 0, 0.35);

  .table-toolbar {
    padding: 0.9rem 1.25rem;
    border-bottom: 1px solid rgba(255, 255, 255, 0.08);
    background: rgba(15, 23, 42, 0.5);

    .table-title {
      font-size: 0.85rem;
      font-weight: 700;
      color: #cbd5e1;
      display: flex;
      align-items: center;
      gap: 0.5rem;
      i { color: #38bdf8; }
    }
  }

  .table-responsive {
    overflow-x: auto;
    width: 100%;

    &::-webkit-scrollbar {
      height: 6px;
    }
    &::-webkit-scrollbar-track {
      background: rgba(15, 23, 42, 0.4);
    }
    &::-webkit-scrollbar-thumb {
      background: rgba(255, 255, 255, 0.15);
      border-radius: 3px;
      &:hover { background: rgba(255, 255, 255, 0.25); }
    }
  }

  table {
    width: 100%;
    min-width: 1100px;
    border-collapse: collapse;
    text-align: left;
    font-size: 0.825rem;
  }

  th, td {
    padding: 0.85rem 1rem;
    border-bottom: 1px solid rgba(255, 255, 255, 0.05);
    white-space: nowrap;
  }

  th {
    background: rgba(15, 23, 42, 0.6);
    color: #94a3b8;
    font-weight: 700;
    font-size: 0.75rem;
    text-transform: uppercase;
    letter-spacing: 0.04em;
  }

  tbody tr {
    transition: background 0.12s ease;
    &:hover {
      background: rgba(255, 255, 255, 0.03);
    }
  }
}

.code-col code {
  background: rgba(0, 0, 0, 0.4);
  padding: 0.2rem 0.45rem;
  border-radius: 0.3rem;
  color: #60a5fa;
  font-family: monospace;
  font-size: 0.775rem;
  border: 1px solid rgba(96, 165, 250, 0.2);
}

.time-col {
  color: #cbd5e1;
  font-size: 0.8rem;
}

.bank-tag {
  font-weight: 700;
  color: #f1f5f9;
}

.fee-col {
  color: #94a3b8;
  font-size: 0.775rem;
}

.account-col code {
  color: #94a3b8;
  font-family: monospace;
  font-size: 0.775rem;
}

.beneficiary-col {
  color: #f8fafc;
}

.type-tag {
  font-size: 0.725rem;
  padding: 0.2rem 0.55rem;
  border-radius: 0.35rem;
  font-weight: 700;
  display: inline-flex;
  align-items: center;
  gap: 0.25rem;

  &.credit {
    background: rgba(16, 185, 129, 0.15);
    border: 1px solid rgba(16, 185, 129, 0.3);
    color: #34d399;
  }
  &.debit {
    background: rgba(239, 68, 68, 0.15);
    border: 1px solid rgba(239, 68, 68, 0.3);
    color: #fca5a5;
  }
}

.amount {
  font-weight: 800;
  letter-spacing: -0.01em;

  &.credit { color: #34d399; }
  &.debit { color: #f87171; }
}

.category-chip {
  background: rgba(99, 102, 241, 0.15);
  border: 1px solid rgba(99, 102, 241, 0.3);
  color: #c7d2fe;
  padding: 0.15rem 0.5rem;
  border-radius: 9999px;
  font-size: 0.725rem;
  font-weight: 600;
}

.desc {
  color: #94a3b8;
  max-width: 260px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

/* ============================================================
   RESPONSIVENESS
   ============================================================ */
@media (max-width: 1024px) {
  .summary-cards {
    grid-template-columns: repeat(2, 1fr);
    .summary-card.balance {
      grid-column: span 2;
    }
  }
}

@media (max-width: 768px) {
  .page-header {
    flex-direction: column;
    align-items: flex-start;

    .header-actions {
      width: 100%;
      flex-direction: column;
      align-items: stretch;

      .btn-config, .sync-controls {
        width: 100%;
      }

      .sync-controls {
        flex-direction: column;

        .bank-select, .btn-sync {
          width: 100%;
          justify-content: center;
        }
      }
    }
  }

  .summary-cards {
    grid-template-columns: 1fr;
    .summary-card.balance {
      grid-column: span 1;
    }
  }

  .custom-bank-bar {
    .custom-bank-inputs {
      flex-direction: column;
    }
  }
}
</style>
