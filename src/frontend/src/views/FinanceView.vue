<template>
  <div class="finance-page">
    <header class="page-header">
      <div class="header-left">
        <h1>💳 Telemetry Tài chính (UC04)</h1>
        <p>Báo cáo biến động số dư & Tự động đồng bộ Google Sheets</p>
      </div>
      <button class="btn-sync" @click="syncVPBank" :disabled="syncState.isSyncing">
        <span v-if="!syncState.isSyncing">🔄 Đồng bộ VPBank</span>
        <span v-else>⏳ Đang nhờ AI phân tích...</span>
      </button>
    </header>

    <div v-if="syncState.isSyncing" class="sync-banner">
      Hệ thống đang nén tất cả email chưa đọc và gửi cho AI xử lý trong 1 lần. Vui lòng đợi vài giây...
    </div>

    <div class="summary-cards">
      <div class="card income">
        <i class="pi pi-arrow-up-right"></i>
        <div>
          <span>Tổng Thu</span>
          <h3>{{ formatCurrency(totalIncome) }}</h3>
        </div>
      </div>
      <div class="card expense">
        <i class="pi pi-arrow-down-right"></i>
        <div>
          <span>Tổng Chi</span>
          <h3>{{ formatCurrency(totalExpense) }}</h3>
        </div>
      </div>
      <div class="card balance">
        <i class="pi pi-wallet"></i>
        <div>
          <span>Số dư</span>
          <h3>{{ formatCurrency(totalIncome - totalExpense) }}</h3>
        </div>
      </div>
    </div>

    <LoadingSpinner v-if="loading" text="Đang tải giao dịch..." />

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
      <InfiniteScrollObserver :loading="loading" :has-more="hasMore" @load-more="loadMore" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed, defineAsyncComponent } from 'vue';
import api from '@/services/api.service';
import LoadingSpinner from '@/components/common/LoadingSpinner.vue';

const InfiniteScrollObserver = defineAsyncComponent(() => import('@/components/common/InfiniteScrollObserver.vue'));

const transactions = ref<any[]>([]);
const loading = ref(true);

const syncState = ref({
  isSyncing: false
});

const syncVPBank = async () => {
  if (syncState.value.isSyncing) return;
  
  syncState.value.isSyncing = true;
  
  try {
    const res: any = await api.post('/finance/transactions/sync-batch', {
      Domain: 'vpb.com.vn',
      BankName: 'VPBank',
      SpreadsheetId: ''
    });
    
    if (res.success) {
      alert(`Đồng bộ hoàn tất! (Xử lý ${res.data} giao dịch mới)`);
      await fetchTransactions(1);
    } else {
      alert(res.message || "Lỗi khi đồng bộ.");
    }
  } catch (e) {
    console.error("Lỗi khi đồng bộ batch:", e);
    alert("Có lỗi xảy ra trong quá trình đồng bộ (Có thể do lỗi mạng hoặc quota).");
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

const totalIncome = computed(() => {
  return transactions.value.filter(t => t.transactionType === 0).reduce((sum, t) => sum + t.amount, 0);
});

const totalExpense = computed(() => {
  return transactions.value.filter(t => t.transactionType === 1).reduce((sum, t) => sum + t.amount, 0);
});

onMounted(() => fetchTransactions(1));
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
