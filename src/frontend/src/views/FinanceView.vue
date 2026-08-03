<template>
  <div class="finance-page">
    <header class="page-header">
      <h1>💳 Telemetry Tài chính (UC04)</h1>
      <p>Báo cáo biến động số dư & Tự động đồng bộ Google Sheets</p>
    </header>

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
.page-header { margin-bottom: 2rem; }

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
