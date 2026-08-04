<template>
  <div class="finance-page">
    <header class="page-header">
      <div class="header-left">
        <h1>💳 Telemetry Tài chính (UC04)</h1>
        <p>Báo cáo biến động số dư & Tự động đồng bộ Google Sheets</p>
      </div>
      <button class="btn-sync" @click="syncVPBank" :disabled="syncState.isSyncing">
        <span v-if="!syncState.isSyncing">🔄 Đồng bộ VPBank</span>
        <span v-else>⏳ Đang xử lý ({{ syncState.current }}/{{ syncState.total }})...</span>
      </button>
    </header>

    <div v-if="syncState.isSyncing" class="sync-banner">
      Hệ thống đang gọi AI để phân tích từng email... (Mỗi email chờ 6s để tránh lỗi giới hạn)
    </div>

    <div v-if="loading" class="loading">Đang tải giao dịch...</div>

    <div v-else class="transaction-table">
      <table>
        <thead>
          <tr>
            <th>Thời gian</th>
            <th>Ngân hàng</th>
            <th>Loại</th>
            <th>Số tiền</th>
            <th>Danh mục</th>
            <th>Nội dung</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="t in transactions" :key="t.id">
            <td>{{ formatDate(t.transactionDate) }}</td>
            <td><strong>{{ t.bankName }}</strong></td>
            <td>
              <span class="type-tag" :class="t.transactionType === 0 ? 'credit' : 'debit'">
                {{ t.transactionType === 0 ? '+ Nhận' : '- Chi' }}
              </span>
            </td>
            <td class="amount" :class="t.transactionType === 0 ? 'credit' : 'debit'">
              {{ formatCurrency(t.amount) }}
            </td>
            <td><span class="category-chip">{{ t.category }}</span></td>
            <td class="desc">{{ t.description }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import api from '@/services/api.service';

const transactions = ref<any[]>([]);
const loading = ref(true);

const syncState = ref({
  isSyncing: false,
  total: 0,
  current: 0
});

const syncVPBank = async () => {
  if (syncState.value.isSyncing) return;
  
  syncState.value.isSyncing = true;
  syncState.value.total = 0;
  syncState.value.current = 0;
  
  try {
    const res: any = await api.get('/finance/transactions/pending?domain=vpb.com.vn');
    if (!res.success || !res.data || res.data.length === 0) {
      alert("Không có email biến động số dư VPBank nào mới!");
      return;
    }
    
    const pendingEmails = res.data;
    syncState.value.total = pendingEmails.length;
    
    for (const email of pendingEmails) {
      syncState.value.current++;
      await api.post('/finance/transactions/parse', {
        GmailMessageId: email.id,
        BankName: 'VPBank',
        SpreadsheetId: '' // No auto sheet sync by default unless set
      });
    }
    
    alert("Đồng bộ hoàn tất!");
    await fetchTransactions();
  } catch (e) {
    console.error("Lỗi khi đồng bộ:", e);
    alert("Có lỗi xảy ra trong quá trình đồng bộ (Có thể do lỗi mạng hoặc quota).");
  } finally {
    syncState.value.isSyncing = false;
  }
};

const fetchTransactions = async () => {
  loading.value = true;
  try {
    const res: any = await api.get('/finance/transactions');
    if (res.success && res.data) {
      transactions.value = res.data.items;
    }
  } catch (e) {
    console.error('Failed to fetch transactions:', e);
  } finally {
    loading.value = false;
  }
};

const formatDate = (dateStr: string) => {
  return new Date(dateStr).toLocaleString('vi-VN');
};

const formatCurrency = (val: number) => {
  return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(val);
};

onMounted(fetchTransactions);
</script>

<style scoped lang="scss">
.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 2rem;
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

.transaction-table {
  background: #1e293b;
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 1rem;
  overflow: hidden;

  table {
    width: 100%;
    border-collapse: collapse;
    text-align: left;
    font-size: 0.9rem;
  }

  th, td {
    padding: 1rem 1.25rem;
    border-bottom: 1px solid rgba(255, 255, 255, 0.05);
  }

  th {
    background: rgba(15, 23, 42, 0.5);
    color: #94a3b8;
    font-weight: 700;
  }
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

.desc { color: #94a3b8; max-width: 300px; }
</style>
