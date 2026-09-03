<template>
  <div class="finance-page">
    <header class="page-header">
      <div class="header-left">
        <h1>💳 Telemetry Tài chính (UC04)</h1>
        <p>Báo cáo biến động số dư & Tự động đồng bộ Google Sheets</p>
      </div>
      <div class="header-actions">
        <button class="btn-config" @click="showConfigPanel = !showConfigPanel">
          ⚙️ Cấu hình Drive & Sheets
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
    <div v-if="selectedBank === 'CUSTOM'" class="custom-bank-bar card">
      <div class="custom-bank-inputs">
        <input v-model="customDomain" placeholder="Domain gửi thư (vd: custombank.com.vn)..." />
        <input v-model="customBankName" placeholder="Tên ngân hàng (vd: MyBank)..." />
      </div>
      <span class="field-hint">Hệ thống sẽ lọc các thư chưa đọc từ domain này và nhờ AI phân tích giao dịch.</span>
    </div>

    <!-- Config Panel -->
    <div v-if="showConfigPanel" class="config-panel card">
      <h3>⚙️ Cấu hình Xuất File Google Drive & Sheets</h3>
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
          <span v-if="!savingConfig">💾 Lưu Cấu Hình</span>
          <span v-else>⏳ Đang lưu...</span>
        </button>
        <span v-if="configStatusMsg" class="config-msg">{{ configStatusMsg }}</span>
      </div>
    </div>

    <div v-if="syncState.isSyncing" class="sync-banner">
      Hệ thống đang nén tất cả email chưa đọc và gửi cho AI xử lý trong 1 lần. Vui lòng đợi vài giây...
    </div>

    <div class="summary-cards">
      <div class="card income">
        <i class="pi pi-arrow-up-right"></i>
        <div>
          <span>Tổng Thu (Tháng này)</span>
          <h3>{{ formatCurrency(monthlySummary.totalIncome) }}</h3>
        </div>
      </div>
      <div class="card expense">
        <i class="pi pi-arrow-down-right"></i>
        <div>
          <span>Tổng Chi (Tháng này)</span>
          <h3>{{ formatCurrency(monthlySummary.totalExpense) }}</h3>
        </div>
      </div>
      <div class="card balance">
        <i class="pi pi-wallet"></i>
        <div>
          <span>Số dư ròng (Net)</span>
          <h3 :class="{ positive: monthlySummary.netBalance >= 0, negative: monthlySummary.netBalance < 0 }">
            {{ formatCurrency(monthlySummary.netBalance) }}
          </h3>
        </div>
      </div>
    </div>

    <LoadingSpinner v-if="loading && transactions.length === 0" text="Đang tải giao dịch..." />

    <div v-else class="transaction-table">
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
              <td><strong>{{ t.bankName }}</strong></td>
              <td>
                <span class="type-tag" :class="t.transactionType === 0 ? 'credit' : 'debit'">
                  {{ t.transactionType === 0 ? '+ Nhận' : '- Chi' }}
                </span>
              </td>
              <td class="amount" :class="t.transactionType === 0 ? 'credit' : 'debit'">
                {{ formatCurrency(t.amount) }}
              </td>
              <td class="fee-col">{{ t.feeAmount ? formatCurrency(t.feeAmount) : '0 ₫' }}</td>
              <td class="account-col">{{ t.sourceAccount || '—' }}</td>
              <td class="account-col">{{ t.targetAccount || '—' }}</td>
              <td class="beneficiary-col"><strong>{{ t.beneficiaryName || '—' }}</strong></td>
              <td><span class="category-chip">{{ t.category }}</span></td>
              <td class="desc">{{ t.description }}</td>
            </tr>
          </tbody>
        </table>
      </div>
      <InfiniteScrollObserver :loading="loading" :has-more="hasMore" @load-more="loadMore" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed, defineAsyncComponent } from 'vue';
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
.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 2rem;
}

.header-actions {
  display: flex;
  gap: 0.75rem;
  align-items: center;
  flex-wrap: wrap;
}

.sync-controls {
  display: flex;
  gap: 0.5rem;
  align-items: center;

  .bank-select {
    background: #0f172a;
    border: 1px solid rgba(255, 255, 255, 0.15);
    color: #f8fafc;
    padding: 0.7rem 0.85rem;
    border-radius: 0.5rem;
    font-size: 0.875rem;
    font-weight: 500;
    cursor: pointer;
    outline: none;

    &:focus {
      border-color: #6366f1;
    }
  }
}

.custom-bank-bar {
  margin-bottom: 1.5rem;
  padding: 1rem 1.25rem;
  background: rgba(99, 102, 241, 0.08);
  border: 1px dashed rgba(99, 102, 241, 0.3);
  border-radius: 0.75rem;

  .custom-bank-inputs {
    display: flex;
    gap: 1rem;
    margin-bottom: 0.5rem;

    input {
      flex: 1;
      background: #0f172a;
      border: 1px solid rgba(255, 255, 255, 0.12);
      border-radius: 0.5rem;
      padding: 0.6rem 0.85rem;
      color: #f8fafc;
      font-size: 0.85rem;
      &:focus { outline: none; border-color: #6366f1; }
    }
  }
}

.positive { color: #34d399; }
.negative { color: #f87171; }

.btn-config {
  background: rgba(255, 255, 255, 0.08);
  color: #f8fafc;
  border: 1px solid rgba(255, 255, 255, 0.15);
  padding: 0.75rem 1.25rem;
  border-radius: 0.5rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    background: rgba(255, 255, 255, 0.15);
  }
}

.btn-sync {
  background: #6366f1;
  color: white;
  border: none;
  padding: 0.75rem 1.5rem;
  border-radius: 0.5rem;
  font-weight: 600;
  cursor: pointer;
  transition: background 0.2s;

  &:hover:not(:disabled) { background: #4f46e5; }
  &:disabled { opacity: 0.6; cursor: not-allowed; }
}

.config-panel {
  background: #1e293b;
  border: 1px solid rgba(99, 102, 241, 0.3);
  border-radius: 1rem;
  padding: 1.5rem;
  margin-bottom: 2rem;

  h3 {
    margin: 0 0 0.5rem 0;
    color: #f8fafc;
    font-size: 1.2rem;
  }

  .config-desc {
    color: #94a3b8;
    font-size: 0.9rem;
    margin-bottom: 1.25rem;
  }

  .config-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
    gap: 1.25rem;
    margin-bottom: 1.25rem;
  }

  .form-group {
    display: flex;
    flex-direction: column;
    gap: 0.4rem;

    label {
      font-size: 0.85rem;
      font-weight: 600;
      color: #cbd5e1;
    }

    input {
      background: #0f172a;
      border: 1px solid rgba(255, 255, 255, 0.15);
      border-radius: 0.5rem;
      padding: 0.65rem 0.85rem;
      color: white;
      font-size: 0.9rem;

      &:focus {
        outline: none;
        border-color: #6366f1;
      }
    }

    .field-hint {
      font-size: 0.75rem;
      color: #64748b;
      line-height: 1.3;

      code {
        background: rgba(0, 0, 0, 0.3);
        padding: 0.1rem 0.3rem;
        border-radius: 0.2rem;
        color: #818cf8;
      }
    }
  }

  .config-actions {
    display: flex;
    align-items: center;
    gap: 1rem;

    .btn-save-config {
      background: #10b981;
      color: white;
      border: none;
      padding: 0.65rem 1.25rem;
      border-radius: 0.5rem;
      font-weight: 600;
      cursor: pointer;
      transition: background 0.2s;

      &:hover {
        background: #059669;
      }
    }

    .config-msg {
      font-size: 0.9rem;
      font-weight: 600;
    }
  }
}

.sync-banner {
  background: rgba(245, 158, 11, 0.15);
  color: #fbbf24;
  padding: 1rem;
  border-radius: 0.5rem;
  margin-bottom: 1.5rem;
  font-weight: 500;
  text-align: center;
  border: 1px solid rgba(245, 158, 11, 0.3);
}

.summary-cards {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 1.5rem;
  margin-bottom: 2rem;
  
  .card {
    background: #1e293b;
    border: 1px solid rgba(255, 255, 255, 0.1);
    border-radius: 1rem;
    padding: 1.5rem;
    display: flex;
    align-items: center;
    gap: 1.25rem;

    i {
      font-size: 2.5rem;
      padding: 1rem;
      border-radius: 0.75rem;
    }
    
    span { color: #94a3b8; font-size: 0.9rem; font-weight: 600; text-transform: uppercase; }
    h3 { margin: 0.25rem 0 0 0; font-size: 1.5rem; color: #f8fafc; }

    &.income i { background: rgba(16, 185, 129, 0.1); color: #34d399; }
    &.expense i { background: rgba(239, 68, 68, 0.1); color: #fca5a5; }
    &.balance i { background: rgba(99, 102, 241, 0.1); color: #818cf8; }
  }
}

.transaction-table {
  background: #1e293b;
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 1rem;
  overflow: hidden;

  .table-responsive {
    overflow-x: auto;
    width: 100%;
  }

  table {
    width: 100%;
    min-width: 1100px;
    border-collapse: collapse;
    text-align: left;
    font-size: 0.85rem;
  }

  th, td {
    padding: 0.85rem 1rem;
    border-bottom: 1px solid rgba(255, 255, 255, 0.05);
    white-space: nowrap;
  }

  th {
    background: rgba(15, 23, 42, 0.5);
    color: #94a3b8;
    font-weight: 700;
  }
}

.code-col code {
  background: rgba(0, 0, 0, 0.3);
  padding: 0.2rem 0.4rem;
  border-radius: 0.25rem;
  color: #60a5fa;
  font-family: monospace;
  font-size: 0.8rem;
}

.fee-col {
  color: #94a3b8;
  font-size: 0.8rem;
}

.account-col {
  color: #cbd5e1;
  font-family: monospace;
  font-size: 0.8rem;
}

.beneficiary-col {
  color: #f8fafc;
}

.type-tag {
  font-size: 0.75rem;
  padding: 0.2rem 0.5rem;
  border-radius: 0.25rem;
  font-weight: 700;

  &.credit { background: rgba(16, 185, 129, 0.2); color: #34d399; }
  &.debit { background: rgba(239, 68, 68, 0.2); color: #fca5a5; }
}

.amount {
  font-weight: 700;
  &.credit { color: #34d399; }
  &.debit { color: #fca5a5; }
}

.category-chip {
  background: rgba(99, 102, 241, 0.15);
  color: #818cf8;
  padding: 0.2rem 0.5rem;
  border-radius: 0.25rem;
  font-size: 0.75rem;
}

.desc {
  color: #94a3b8;
  max-width: 250px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
</style>
