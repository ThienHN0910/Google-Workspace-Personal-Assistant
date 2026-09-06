<template>
  <div class="action-logs-container">
    <!-- Filters & Action Bar -->
    <div class="logs-toolbar">
      <div class="filter-pills">
        <button 
          v-for="filter in actionFilters" 
          :key="filter.value" 
          class="pill-btn" 
          :class="{ active: currentActionFilter === filter.value }"
          @click="changeActionFilter(filter.value)"
        >
          <i :class="filter.icon"></i>
          <span>{{ filter.label }}</span>
          <span v-if="filter.value === 'PendingApproval' && pendingCount > 0" class="counter-badge">
            {{ pendingCount }}
          </span>
        </button>
      </div>

      <div class="toolbar-actions">
        <div class="search-input-wrap">
          <i class="pi pi-search"></i>
          <input 
            v-model="searchQuery" 
            placeholder="Tìm theo tiêu đề, người gửi, lý do..." 
            @keyup.enter="fetchLogs(1)"
          />
          <button v-if="searchQuery" class="clear-btn" @click="clearSearch">
            <i class="pi pi-times"></i>
          </button>
        </div>

        <button 
          v-if="selectedIds.length > 0" 
          class="btn-danger-outline" 
          @click="handleDeleteSelected"
          :disabled="deleting"
        >
          <i class="pi pi-trash"></i> Xóa đã chọn ({{ selectedIds.length }})
        </button>

        <button class="btn-secondary" @click="showPruneModal = true">
          <i class="pi pi-filter-slash"></i> Dọn bớt log
        </button>

        <button class="btn-icon" @click="fetchLogs(currentPage)" title="Làm mới">
          <i class="pi pi-refresh" :class="{ 'pi-spin': loading }"></i>
        </button>
      </div>
    </div>

    <!-- Loading State -->
    <LoadingSpinner v-if="loading && logs.length === 0" text="Đang tải nhật ký kiểm toán..." />

    <!-- Empty State -->
    <div v-else-if="logs.length === 0" class="empty-state">
      <i class="pi pi-inbox"></i>
      <p>Không có nhật ký hành động nào phù hợp.</p>
    </div>

    <!-- Logs Table -->
    <div v-else class="table-responsive">
      <table class="action-logs-table">
        <thead>
          <tr>
            <th class="col-checkbox">
              <input 
                type="checkbox" 
                :checked="isAllSelected" 
                @change="toggleSelectAll"
                title="Chọn tất cả trên trang này"
              />
            </th>
            <th class="col-time">Thời gian</th>
            <th class="col-subject">Tiêu đề email</th>
            <th class="col-sender">Người gửi</th>
            <th class="col-action">Hành động</th>
            <th class="col-reason">Lý do / Quy tắc</th>
            <th class="col-ops">Thao tác</th>
          </tr>
        </thead>
        <tbody>
          <tr 
            v-for="item in logs" 
            :key="item.id" 
            :class="{ 'row-selected': selectedIds.includes(item.id), 'row-pending': item.action === 'PendingApproval' }"
          >
            <td class="col-checkbox">
              <input 
                type="checkbox" 
                :value="item.id" 
                v-model="selectedIds" 
              />
            </td>
            <td class="col-time">
              <span class="time-text">{{ formatDate(item.executedAt) }}</span>
            </td>
            <td class="col-subject">
              <div class="subject-wrap" :title="item.subject || '(Không có tiêu đề)'">
                <span class="subject-text">{{ item.subject || '(Không có tiêu đề)' }}</span>
                <span class="source-tag">{{ item.sourceJob }}</span>
              </div>
            </td>
            <td class="col-sender">
              <span class="sender-text" :title="item.sender">{{ item.sender }}</span>
            </td>
            <td class="col-action">
              <span class="action-badge" :class="getActionBadgeClass(item.action)">
                <i :class="getActionIcon(item.action)"></i>
                {{ getActionLabel(item.action) }}
              </span>
            </td>
            <td class="col-reason">
              <div class="reason-cell" :title="item.reason">
                {{ item.reason }}
              </div>
            </td>
            <td class="col-ops">
              <div class="row-actions">
                <!-- If Pending Approval, show Approve (Trash/Archive) and Dismiss -->
                <template v-if="item.action === 'PendingApproval'">
                  <button 
                    class="btn-action-small btn-trash-action" 
                    @click="handleApprove(item, 'Trash')" 
                    title="Duyệt chuyển Thùng rác"
                  >
                    <i class="pi pi-trash"></i> Duyệt xóa
                  </button>
                  <button 
                    class="btn-action-small btn-archive-action" 
                    @click="handleApprove(item, 'Archive')" 
                    title="Duyệt Lưu trữ"
                  >
                    <i class="pi pi-box"></i> Lưu trữ
                  </button>
                  <button 
                    class="btn-action-small btn-dismiss-action" 
                    @click="handleDismiss(item)" 
                    title="Bỏ qua không xử lý"
                  >
                    <i class="pi pi-times"></i>
                  </button>
                </template>

                <!-- Delete this single log entry from database -->
                <button 
                  class="btn-icon-subtle text-red" 
                  @click="handleDeleteSingle(item.id)" 
                  title="Xóa bản ghi nhật ký này"
                >
                  <i class="pi pi-trash"></i>
                </button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Pagination Controls -->
    <div v-if="totalPages > 1 || totalCount > 0" class="pagination-bar">
      <div class="pagination-info">
        Hiển thị <strong>{{ logs.length }}</strong> / <strong>{{ totalCount }}</strong> bản ghi (Trang {{ currentPage }} / {{ totalPages }})
      </div>
      <div class="pagination-buttons">
        <button 
          class="btn-page" 
          :disabled="currentPage <= 1 || loading" 
          @click="fetchLogs(currentPage - 1)"
        >
          <i class="pi pi-chevron-left"></i> Trước
        </button>
        <span class="page-current">{{ currentPage }}</span>
        <button 
          class="btn-page" 
          :disabled="currentPage >= totalPages || loading" 
          @click="fetchLogs(currentPage + 1)"
        >
          Sau <i class="pi pi-chevron-right"></i>
        </button>
      </div>
    </div>

    <!-- Modal Prune Logs -->
    <div v-if="showPruneModal" class="modal-overlay" @click.self="showPruneModal = false">
      <div class="modal-content">
        <h3><i class="pi pi-filter-slash"></i> Dọn bớt nhật ký kiểm toán</h3>
        <p class="modal-desc">
          Xóa bớt các bản ghi nhật ký trong cơ sở dữ liệu để giải phóng dung lượng và tinh gọn lịch sử. Thao tác này chỉ xóa lịch sử log, không ảnh hưởng đến email trên Gmail.
        </p>

        <div class="prune-options">
          <label class="radio-option">
            <input type="radio" v-model="pruneOption" value="7days" />
            <span>Xóa các log cũ hơn <strong>7 ngày</strong></span>
          </label>
          <label class="radio-option">
            <input type="radio" v-model="pruneOption" value="30days" />
            <span>Xóa các log cũ hơn <strong>30 ngày</strong></span>
          </label>
          <label class="radio-option text-danger">
            <input type="radio" v-model="pruneOption" value="all" />
            <span>Xóa <strong>toàn bộ nhật ký</strong> (không khôi phục được)</span>
          </label>
        </div>

        <div class="modal-actions">
          <button class="btn-cancel" @click="showPruneModal = false">Hủy</button>
          <button class="btn-danger" @click="executePrune" :disabled="deleting">
            <i class="pi" :class="deleting ? 'pi-spin pi-spinner' : 'pi-trash'"></i>
            {{ deleting ? 'Đang xóa...' : 'Xác nhận xóa' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import api from '@/services/api.service';
import LoadingSpinner from '@/components/common/LoadingSpinner.vue';
import { showToast } from '@/services/notification.service';

interface EmailActionLog {
  id: string;
  emailId: string;
  subject?: string;
  sender?: string;
  action: string;
  sourceJob: string;
  reason: string;
  executedAt: string;
}

const logs = ref<EmailActionLog[]>([]);
const totalCount = ref(0);
const currentPage = ref(1);
const pageSize = ref(20);
const loading = ref(false);
const deleting = ref(false);
const searchQuery = ref('');
const currentActionFilter = ref('');
const selectedIds = ref<string[]>([]);
const pendingCount = ref(0);

const showPruneModal = ref(false);
const pruneOption = ref('30days');

const actionFilters = [
  { label: 'Tất cả', value: '', icon: 'pi pi-list' },
  { label: 'Chờ duyệt', value: 'PendingApproval', icon: 'pi pi-clock' },
  { label: 'Đã xóa', value: 'Trashed', icon: 'pi pi-trash' },
  { label: 'Đã lưu trữ', value: 'Archived', icon: 'pi pi-box' },
  { label: 'Đã đọc', value: 'MarkedRead', icon: 'pi pi-check' },
  { label: 'Bỏ qua', value: 'Dismissed', icon: 'pi pi-times' },
];

const totalPages = computed(() => Math.ceil(totalCount.value / pageSize.value) || 1);

const isAllSelected = computed(() => {
  return logs.value.length > 0 && logs.value.every(l => selectedIds.value.includes(l.id));
});

const toggleSelectAll = () => {
  if (isAllSelected.value) {
    selectedIds.value = [];
  } else {
    selectedIds.value = logs.value.map(l => l.id);
  }
};

const formatDate = (iso: string) => {
  if (!iso) return '';
  const d = new Date(iso);
  return d.toLocaleString('vi-VN', {
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  });
};

const getActionBadgeClass = (action: string) => {
  switch (action) {
    case 'PendingApproval': return 'badge-pending';
    case 'Trashed': return 'badge-trashed';
    case 'Archived': return 'badge-archived';
    case 'MarkedRead': return 'badge-read';
    case 'Dismissed': return 'badge-dismissed';
    default: return 'badge-default';
  }
};

const getActionIcon = (action: string) => {
  switch (action) {
    case 'PendingApproval': return 'pi pi-exclamation-triangle';
    case 'Trashed': return 'pi pi-trash';
    case 'Archived': return 'pi pi-box';
    case 'MarkedRead': return 'pi pi-check';
    case 'Dismissed': return 'pi pi-times';
    default: return 'pi pi-info-circle';
  }
};

const getActionLabel = (action: string) => {
  switch (action) {
    case 'PendingApproval': return 'Chờ duyệt';
    case 'Trashed': return 'Đã xóa';
    case 'Archived': return 'Lưu trữ';
    case 'MarkedRead': return 'Đã đọc';
    case 'Dismissed': return 'Bỏ qua';
    default: return action;
  }
};

const fetchLogs = async (page = 1) => {
  loading.value = true;
  try {
    const params: any = {
      page,
      pageSize: pageSize.value,
    };
    if (currentActionFilter.value) {
      params.action = currentActionFilter.value;
    }
    if (searchQuery.value.trim()) {
      params.search = searchQuery.value.trim();
    }

    const res: any = await api.get('/emailops/action-logs', { params });
    if (res.data) {
      logs.value = res.data.items || [];
      totalCount.value = res.data.totalCount || 0;
      currentPage.value = res.data.page || page;
      selectedIds.value = [];
    }

    // Refresh pending count
    fetchPendingCount();
  } catch (err: any) {
    showToast({
      severity: 'error',
      summary: 'Lỗi',
      detail: err.message || 'Không thể tải nhật ký hành động email',
    });
  } finally {
    loading.value = false;
  }
};

const fetchPendingCount = async () => {
  try {
    const res: any = await api.get('/emailops/action-logs', {
      params: { page: 1, pageSize: 1, action: 'PendingApproval' }
    });
    if (res.data) {
      pendingCount.value = res.data.totalCount || 0;
    }
  } catch {
    // Ignore
  }
};

const changeActionFilter = (filterVal: string) => {
  currentActionFilter.value = filterVal;
  fetchLogs(1);
};

const clearSearch = () => {
  searchQuery.value = '';
  fetchLogs(1);
};

const handleApprove = async (item: EmailActionLog, targetAction: string) => {
  try {
    const actionText = targetAction === 'Archive' ? 'Lưu trữ' : 'Xóa tạm';
    const res: any = await api.post(`/emailops/action-logs/${item.id}/approve`, {
      action: targetAction,
    });
    showToast({
      severity: 'success',
      summary: 'Duyệt thành công',
      detail: `Đã thực thi ${actionText} email trên Gmail: "${item.subject || item.emailId}"`,
    });
    fetchLogs(currentPage.value);
  } catch (err: any) {
    showToast({
      severity: 'error',
      summary: 'Lỗi phê duyệt',
      detail: err.message || 'Thao tác thất bại',
    });
  }
};

const handleDismiss = async (item: EmailActionLog) => {
  try {
    await api.post(`/emailops/action-logs/${item.id}/reject`, {});
    showToast({
      severity: 'info',
      summary: 'Đã bỏ qua',
      detail: `Đã chuyển email "${item.subject || item.emailId}" sang trạng thái Bỏ qua.`,
    });
    fetchLogs(currentPage.value);
  } catch (err: any) {
    showToast({
      severity: 'error',
      summary: 'Lỗi',
      detail: err.message || 'Thao tác thất bại',
    });
  }
};

const handleDeleteSingle = async (id: string) => {
  if (!confirm('Bạn có chắc muốn xóa bản ghi nhật ký này khỏi hệ thống?')) return;
  try {
    await api.delete(`/emailops/action-logs/${id}`);
    showToast({
      severity: 'success',
      summary: 'Thành công',
      detail: 'Đã xóa bản ghi nhật ký.',
    });
    fetchLogs(currentPage.value);
  } catch (err: any) {
    showToast({
      severity: 'error',
      summary: 'Lỗi xóa log',
      detail: err.message || 'Không thể xóa bản ghi',
    });
  }
};

const handleDeleteSelected = async () => {
  if (selectedIds.value.length === 0) return;
  if (!confirm(`Bạn có chắc muốn xóa ${selectedIds.value.length} bản ghi nhật ký đã chọn?`)) return;

  deleting.value = true;
  try {
    await api.post('/emailops/action-logs/delete-batch', {
      ids: selectedIds.value,
      deleteAll: false,
    });
    showToast({
      severity: 'success',
      summary: 'Thành công',
      detail: `Đã xóa ${selectedIds.value.length} bản ghi nhật ký.`,
    });
    selectedIds.value = [];
    fetchLogs(currentPage.value);
  } catch (err: any) {
    showToast({
      severity: 'error',
      summary: 'Lỗi xóa',
      detail: err.message || 'Không thể xóa các bản ghi đã chọn',
    });
  } finally {
    deleting.value = false;
  }
};

const executePrune = async () => {
  deleting.value = true;
  try {
    const payload: any = {};
    if (pruneOption.value === 'all') {
      payload.deleteAll = true;
    } else if (pruneOption.value === '7days') {
      payload.olderThanDays = 7;
    } else if (pruneOption.value === '30days') {
      payload.olderThanDays = 30;
    }

    await api.post('/emailops/action-logs/delete-batch', payload);
    showToast({
      severity: 'success',
      summary: 'Dọn log thành công',
      detail: 'Đã dọn dẹp các bản ghi nhật ký theo yêu cầu.',
    });
    showPruneModal.value = false;
    fetchLogs(1);
  } catch (err: any) {
    showToast({
      severity: 'error',
      summary: 'Lỗi dọn log',
      detail: err.message || 'Không thể dọn nhật ký',
    });
  } finally {
    deleting.value = false;
  }
};

onMounted(() => {
  fetchLogs(1);
});
</script>

<style scoped lang="scss">
.action-logs-container {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.logs-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 0.75rem;
  background: #1e293b;
  padding: 0.75rem 1rem;
  border-radius: 0.75rem;
  border: 1px solid rgba(255, 255, 255, 0.08);
}

.filter-pills {
  display: flex;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.pill-btn {
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid rgba(255, 255, 255, 0.1);
  color: #94a3b8;
  padding: 0.4rem 0.75rem;
  border-radius: 2rem;
  font-size: 0.825rem;
  font-weight: 500;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 0.4rem;
  transition: all 0.2s;

  &:hover {
    background: rgba(255, 255, 255, 0.1);
    color: #fff;
  }

  &.active {
    background: #4f46e5;
    border-color: #6366f1;
    color: #fff;
    box-shadow: 0 0 10px rgba(99, 102, 241, 0.3);
  }
}

.counter-badge {
  background: #ef4444;
  color: #fff;
  font-size: 0.7rem;
  font-weight: 700;
  padding: 0.1rem 0.45rem;
  border-radius: 1rem;
}

.toolbar-actions {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.search-input-wrap {
  display: flex;
  align-items: center;
  background: rgba(0, 0, 0, 0.2);
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 0.5rem;
  padding: 0.35rem 0.65rem;
  gap: 0.4rem;

  i {
    color: #64748b;
    font-size: 0.85rem;
  }

  input {
    background: transparent;
    border: none;
    color: #fff;
    font-size: 0.85rem;
    outline: none;
    width: 200px;

    &::placeholder {
      color: #64748b;
    }
  }

  .clear-btn {
    background: none;
    border: none;
    color: #94a3b8;
    cursor: pointer;
    padding: 0;
    font-size: 0.75rem;
  }
}

.btn-secondary {
  background: rgba(255, 255, 255, 0.08);
  border: 1px solid rgba(255, 255, 255, 0.15);
  color: #e2e8f0;
  padding: 0.45rem 0.85rem;
  border-radius: 0.5rem;
  font-size: 0.85rem;
  font-weight: 500;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 0.4rem;
  transition: background 0.2s;

  &:hover {
    background: rgba(255, 255, 255, 0.15);
  }
}

.btn-danger-outline {
  background: rgba(239, 68, 68, 0.15);
  border: 1px solid rgba(239, 68, 68, 0.3);
  color: #fca5a5;
  padding: 0.45rem 0.85rem;
  border-radius: 0.5rem;
  font-size: 0.85rem;
  font-weight: 600;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 0.4rem;

  &:hover {
    background: rgba(239, 68, 68, 0.25);
  }
}

.btn-icon {
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid rgba(255, 255, 255, 0.1);
  color: #94a3b8;
  padding: 0.45rem;
  border-radius: 0.5rem;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;

  &:hover {
    background: rgba(255, 255, 255, 0.1);
    color: #fff;
  }
}

.table-responsive {
  overflow-x: auto;
  background: #1e293b;
  border-radius: 0.75rem;
  border: 1px solid rgba(255, 255, 255, 0.08);
}

.action-logs-table {
  width: 100%;
  border-collapse: collapse;
  text-align: left;
  font-size: 0.875rem;

  th {
    background: rgba(0, 0, 0, 0.25);
    color: #94a3b8;
    padding: 0.75rem 1rem;
    font-weight: 600;
    border-bottom: 1px solid rgba(255, 255, 255, 0.08);
    white-space: nowrap;
  }

  td {
    padding: 0.75rem 1rem;
    border-bottom: 1px solid rgba(255, 255, 255, 0.05);
    color: #e2e8f0;
    vertical-align: middle;
  }

  tr:hover {
    background: rgba(255, 255, 255, 0.02);
  }

  tr.row-selected {
    background: rgba(99, 102, 241, 0.1);
  }

  tr.row-pending {
    background: rgba(245, 158, 11, 0.05);
    border-left: 3px solid #f59e0b;
  }
}

.col-checkbox {
  width: 40px;
  text-align: center;
}

.col-time {
  width: 110px;
  white-space: nowrap;
}

.time-text {
  font-size: 0.8rem;
  color: #94a3b8;
}

.col-subject {
  min-width: 200px;
  max-width: 320px;
}

.subject-wrap {
  display: flex;
  flex-direction: column;
  gap: 0.2rem;
}

.subject-text {
  font-weight: 600;
  color: #f8fafc;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.source-tag {
  font-size: 0.7rem;
  color: #64748b;
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.col-sender {
  min-width: 150px;
  max-width: 220px;
}

.sender-text {
  color: #cbd5e1;
  font-size: 0.825rem;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  display: block;
}

.col-action {
  width: 130px;
}

.action-badge {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  padding: 0.25rem 0.6rem;
  border-radius: 1rem;
  font-size: 0.75rem;
  font-weight: 600;
  white-space: nowrap;

  &.badge-pending {
    background: rgba(245, 158, 11, 0.2);
    color: #fcd34d;
    border: 1px solid rgba(245, 158, 11, 0.4);
  }

  &.badge-trashed {
    background: rgba(239, 68, 68, 0.2);
    color: #fca5a5;
  }

  &.badge-archived {
    background: rgba(59, 130, 246, 0.2);
    color: #93c5fd;
  }

  &.badge-read {
    background: rgba(16, 185, 129, 0.2);
    color: #6ee7b7;
  }

  &.badge-dismissed {
    background: rgba(148, 163, 184, 0.2);
    color: #cbd5e1;
  }
}

.col-reason {
  min-width: 200px;
  max-width: 320px;
}

.reason-cell {
  font-size: 0.8rem;
  color: #94a3b8;
  line-height: 1.4;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.col-ops {
  width: 160px;
  text-align: right;
}

.row-actions {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 0.35rem;
}

.btn-action-small {
  border: none;
  border-radius: 0.35rem;
  padding: 0.25rem 0.5rem;
  font-size: 0.75rem;
  font-weight: 600;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 0.25rem;

  &.btn-trash-action {
    background: #ef4444;
    color: #fff;
    &:hover { background: #dc2626; }
  }

  &.btn-archive-action {
    background: #3b82f6;
    color: #fff;
    &:hover { background: #2563eb; }
  }

  &.btn-dismiss-action {
    background: rgba(255, 255, 255, 0.1);
    color: #94a3b8;
    &:hover { background: rgba(255, 255, 255, 0.2); color: #fff; }
  }
}

.btn-icon-subtle {
  background: none;
  border: none;
  color: #64748b;
  cursor: pointer;
  padding: 0.35rem;
  border-radius: 0.25rem;
  font-size: 0.85rem;

  &:hover {
    color: #ef4444;
    background: rgba(239, 68, 68, 0.1);
  }
}

.pagination-bar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 0.5rem;
  padding: 0.75rem 0.5rem;
}

.pagination-info {
  font-size: 0.85rem;
  color: #94a3b8;
}

.pagination-buttons {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.btn-page {
  background: #1e293b;
  border: 1px solid rgba(255, 255, 255, 0.1);
  color: #e2e8f0;
  padding: 0.35rem 0.75rem;
  border-radius: 0.375rem;
  font-size: 0.825rem;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 0.3rem;

  &:hover:not(:disabled) {
    background: rgba(255, 255, 255, 0.1);
  }

  &:disabled {
    opacity: 0.4;
    cursor: not-allowed;
  }
}

.page-current {
  color: #6366f1;
  font-weight: 700;
  font-size: 0.9rem;
  padding: 0 0.5rem;
}

.empty-state {
  text-align: center;
  padding: 3rem 1rem;
  background: #1e293b;
  border-radius: 0.75rem;
  border: 1px solid rgba(255, 255, 255, 0.08);

  i {
    font-size: 2.5rem;
    color: #64748b;
    margin-bottom: 0.75rem;
  }

  p {
    color: #94a3b8;
    margin: 0;
  }
}

/* Prune Modal */
.modal-overlay {
  position: fixed;
  top: 0; left: 0; right: 0; bottom: 0;
  background: rgba(0, 0, 0, 0.7);
  backdrop-filter: blur(4px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  padding: 1rem;
}

.modal-content {
  background: #1e293b;
  border: 1px solid rgba(255, 255, 255, 0.15);
  border-radius: 1rem;
  width: 100%;
  max-width: 480px;
  padding: 1.5rem;
  box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.5);

  h3 {
    margin-top: 0;
    margin-bottom: 0.75rem;
    font-size: 1.15rem;
    display: flex;
    align-items: center;
    gap: 0.5rem;
    color: #f8fafc;
  }

  .modal-desc {
    font-size: 0.85rem;
    color: #94a3b8;
    line-height: 1.5;
    margin-bottom: 1.25rem;
  }
}

.prune-options {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  margin-bottom: 1.5rem;
}

.radio-option {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.75rem 1rem;
  background: rgba(0, 0, 0, 0.2);
  border: 1px solid rgba(255, 255, 255, 0.05);
  border-radius: 0.5rem;
  cursor: pointer;
  font-size: 0.875rem;
  color: #e2e8f0;

  &:hover {
    background: rgba(255, 255, 255, 0.04);
  }

  &.text-danger {
    color: #fca5a5;
  }

  input[type="radio"] {
    accent-color: #6366f1;
  }
}

.modal-actions {
  display: flex;
  justify-content: flex-end;
  gap: 0.75rem;
}

.btn-cancel {
  background: rgba(255, 255, 255, 0.1);
  border: 1px solid rgba(255, 255, 255, 0.15);
  color: #e2e8f0;
  padding: 0.5rem 1rem;
  border-radius: 0.5rem;
  cursor: pointer;
  font-weight: 500;

  &:hover {
    background: rgba(255, 255, 255, 0.15);
  }
}

.btn-danger {
  background: #ef4444;
  border: none;
  color: #fff;
  padding: 0.5rem 1.25rem;
  border-radius: 0.5rem;
  cursor: pointer;
  font-weight: 600;
  display: flex;
  align-items: center;
  gap: 0.4rem;

  &:hover:not(:disabled) {
    background: #dc2626;
  }

  &:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }
}
</style>
