<template>
  <div class="drive-guard-page">
    <header class="page-header">
      <h1>🛡 Drive Guard & Audit (UC05 & UC06)</h1>
      <p>Giám sát biến động thư mục Google Drive & Cảnh báo an ninh</p>
    </header>

    <LoadingSpinner v-if="loading" text="Đang tải dữ liệu cấu hình..." />

    <div v-else class="sections">
      <!-- Section 1: Monitored Folders & Config (UC05) -->
      <div class="card-section">
        <div class="section-header">
          <h2>📂 Cấu hình Thư mục & Hệ thống</h2>
          <div class="config-interval">
            <label>Chu kỳ quét (phút):</label>
            <input type="number" v-model="intervalMinutes" min="1" max="60" />
            <button class="btn-small" @click="updateInterval" :disabled="updatingInterval">
              {{ updatingInterval ? 'Đang lưu...' : 'Áp dụng' }}
            </button>
          </div>
        </div>
        
        <form @submit.prevent="addFolder" class="add-folder-form">
          <input v-model="newFolder.folderName" placeholder="Tên gợi nhớ (VD: Tài liệu mật)" required />
          <input v-model="newFolder.googleFolderId" placeholder="Google Folder ID" required />
          <button type="submit" class="btn-submit" :disabled="addingFolder">
            <i class="pi pi-plus"></i> {{ addingFolder ? 'Đang thêm...' : 'Thêm thư mục' }}
          </button>
        </form>
        <div v-if="folders.length === 0" class="empty mt-2">Chưa có thư mục nào đang được theo dõi.</div>
        <div v-else class="folders-list mt-2">
          <div v-for="f in folders" :key="f.id" class="folder-card">
            <div>
              <span class="folder-name">{{ f.folderName }}</span>
              <span class="folder-id">ID: {{ f.googleFolderId }}</span>
            </div>
            <div class="folder-actions">
              <span class="status-badge active"><i class="pi pi-eye"></i> Đang theo dõi</span>
              <button class="btn-delete-folder" @click="handleDeleteFolder(f.id)" title="Xóa thư mục khỏi theo dõi">
                <i class="pi pi-trash"></i>
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Section 2: Security Alerts (UC06) -->
      <div class="card-section">
        <h2>⚠️ Cảnh báo an ninh file nguy hiểm (UC06)</h2>
        <div v-if="alerts.length === 0" class="empty">Không có cảnh báo an ninh nào!</div>
        <div v-else class="alerts-list">
          <div v-for="a in alerts" :key="a.id" class="alert-card">
            <div class="alert-header">
              <span class="file-name">{{ a.fileName }}</span>
              <span class="severity-badge" :class="a.severity.toLowerCase()">{{ a.severity }}</span>
            </div>
            <p class="reason">{{ a.reason }}</p>
            <div class="alert-actions">
              <button class="quarantine-btn" @click="handleQuarantine(a.fileId)">
                <i class="pi pi-shield"></i> Cách ly vào "G-Ops Quarantine"
              </button>
              <button class="restore-btn" @click="handleRestore(a.fileId)" title="Khôi phục file">
                <i class="pi pi-replay"></i> Khôi phục
              </button>
              <button class="resolve-btn" @click="handleResolveAlert(a.id)">
                <i class="pi pi-check"></i> Đã xử lý
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Section 2: Drive Audit Logs (UC05) -->
      <div class="card-section">
        <h2>📋 Nhật ký biến động Drive (UC05)</h2>
        <div v-if="logs.length === 0" class="empty">Chưa có nhật ký hoạt động nào.</div>
        <div v-else class="logs-list">
          <div v-for="l in logs" :key="l.id" class="log-item">
            <span class="timestamp">{{ formatDate(l.actionTimestamp) }}</span>
            <span class="action">{{ l.actionType }}</span>
            <span class="user">{{ l.modifiedBy }}</span>
            <span class="file">{{ l.fileName }}</span>
          </div>
        </div>
        <InfiniteScrollObserver :loading="loadingLogs" :has-more="hasMoreLogs" @load-more="loadMoreLogs" />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, defineAsyncComponent } from 'vue';
import api from '@/services/api.service';
import { showToast } from '@/services/notification.service';

const LoadingSpinner = defineAsyncComponent(() => import('@/components/common/LoadingSpinner.vue'));
const InfiniteScrollObserver = defineAsyncComponent(() => import('@/components/common/InfiniteScrollObserver.vue'));

const alerts = ref<any[]>([]);
const logs = ref<any[]>([]);
const folders = ref<any[]>([]);
const addingFolder = ref(false);
const newFolder = ref({ folderName: '', googleFolderId: '' });

const intervalMinutes = ref(5);
const updatingInterval = ref(false);
const loading = ref(true);

const fetchData = async () => {
  loading.value = true;
  try {
    const resInterval: any = await api.get('/driveguard/interval');
    if (resInterval.success) {
      intervalMinutes.value = resInterval.data;
    }

    const resFolders: any = await api.get('/driveguard/folders');
    if (resFolders.success && resFolders.data) {
      folders.value = resFolders.data;
    }

    const resAlerts: any = await api.get('/driveguard/alerts');
    if (resAlerts.success && resAlerts.data) {
      alerts.value = resAlerts.data.items;
    }

    await fetchLogs(1);
  } catch (e) {
    console.error('Failed to load drive guard data', e);
  } finally {
    loading.value = false;
  }
};

const pageLogs = ref(1);
const hasMoreLogs = ref(true);
const loadingLogs = ref(false);

const fetchLogs = async (page = 1) => {
  loadingLogs.value = true;
  try {
    const resLogs: any = await api.get(`/driveguard/audit-logs?page=${page}&pageSize=20`);
    if (resLogs.success && resLogs.data) {
      if (page === 1) {
        logs.value = resLogs.data.items;
      } else {
        logs.value = [...logs.value, ...resLogs.data.items];
      }
      hasMoreLogs.value = page < resLogs.data.totalPages;
      pageLogs.value = page;
    }
  } catch (e) {
    console.error('Failed to load drive audit logs:', e);
  } finally {
    loadingLogs.value = false;
  }
};

const loadMoreLogs = () => {
  if (!loadingLogs.value && hasMoreLogs.value) {
    fetchLogs(pageLogs.value + 1);
  }
};

const addFolder = async () => {
  addingFolder.value = true;
  try {
    const res: any = await api.post('/driveguard/folders', newFolder.value);
    if (res.success) {
      newFolder.value = { folderName: '', googleFolderId: '' };
      showToast({
        severity: 'success',
        summary: 'Thành công',
        detail: 'Đã thêm thư mục vào danh sách theo dõi an ninh.',
      });
      fetchData();
    }
  } catch (e) {
    showToast({
      severity: 'error',
      summary: 'Lỗi',
      detail: 'Không thể thêm thư mục. Vui lòng kiểm tra lại Google Folder ID.',
    });
  } finally {
    addingFolder.value = false;
  }
};

const handleDeleteFolder = async (folderId: string) => {
  if (!confirm('Bạn có chắc chắn muốn hủy theo dõi thư mục này?')) return;

  try {
    const res: any = await api.delete(`/driveguard/folders/${folderId}`);
    if (res.success) {
      showToast({
        severity: 'info',
        summary: 'Đã xóa',
        detail: 'Đã dừng theo dõi thư mục này.',
      });
      folders.value = folders.value.filter(f => f.id !== folderId);
    }
  } catch (e) {
    showToast({
      severity: 'error',
      summary: 'Lỗi',
      detail: 'Không thể xóa thư mục theo dõi.',
    });
  }
};

const updateInterval = async () => {
  updatingInterval.value = true;
  try {
    const res: any = await api.post('/driveguard/interval', { minutes: intervalMinutes.value });
    if (res.success) {
      showToast({
        severity: 'success',
        summary: 'Đã cập nhật',
        detail: `Hệ thống sẽ quét Google Drive định kỳ mỗi ${intervalMinutes.value} phút.`,
      });
    }
  } catch (e) {
    showToast({
      severity: 'error',
      summary: 'Lỗi',
      detail: 'Không thể cập nhật chu kỳ quét.',
    });
  } finally {
    updatingInterval.value = false;
  }
};

const handleQuarantine = async (fileId: string) => {
  try {
    const res: any = await api.post('/driveguard/quarantine', {
      fileId,
      quarantineFolderId: '' // Automatically provision or find "G-Ops Quarantine" folder on real Google Drive
    });
    if (res.success) {
      showToast({
        severity: 'warn',
        summary: 'Đã cách ly file',
        detail: 'File nguy hiểm đã được chuyển vào thư mục Google Drive "G-Ops Quarantine".',
      });
      fetchData();
    }
  } catch (e) {
    showToast({
      severity: 'error',
      summary: 'Lỗi cách ly',
      detail: 'Không thể di chuyển file vào thư mục cách ly.',
    });
  }
};

const handleRestore = async (fileId: string) => {
  const targetFolder = prompt('Nhập ID thư mục Google Drive muốn khôi phục file về (hoặc để "root"):', 'root');
  if (targetFolder === null) return;

  try {
    const res: any = await api.post('/driveguard/quarantine/restore', {
      fileId,
      targetFolderId: targetFolder.trim() || 'root'
    });
    if (res.success) {
      showToast({
        severity: 'success',
        summary: 'Đã khôi phục',
        detail: 'File đã được khôi phục về thư mục đích an toàn.',
      });
      fetchData();
    }
  } catch (e) {
    showToast({
      severity: 'error',
      summary: 'Lỗi',
      detail: 'Không thể khôi phục file.',
    });
  }
};

const handleResolveAlert = async (alertId: string) => {
  try {
    const res: any = await api.post(`/driveguard/alerts/${alertId}/resolve`, {
      note: 'Đã xử lý bởi quản trị viên.'
    });
    if (res.success) {
      showToast({
        severity: 'success',
        summary: 'Đã xử lý',
        detail: 'Cảnh báo an ninh đã được đánh dấu là đã giải quyết.',
      });
      alerts.value = alerts.value.filter(a => a.id !== alertId);
    }
  } catch (e) {
    showToast({
      severity: 'error',
      summary: 'Lỗi',
      detail: 'Không thể cập nhật trạng thái cảnh báo.',
    });
  }
};

const formatDate = (dateStr: string) => {
  return new Date(dateStr).toLocaleString('vi-VN');
};

onMounted(fetchData);
</script>

<style scoped lang="scss">
.page-header { margin-bottom: 2rem; }
.sections { display: flex; flex-direction: column; gap: 2rem; }

.card-section {
  background: #1e293b;
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 1rem;
  padding: 1.5rem;

  .section-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 1rem;
    
    h2 { margin-bottom: 0; }
  }

  h2 { font-size: 1.25rem; font-weight: 800; margin-bottom: 1rem; color: #f8fafc; }
}

.config-interval {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  background: rgba(255,255,255,0.05);
  padding: 0.5rem 1rem;
  border-radius: 0.5rem;

  label { font-size: 0.85rem; color: #cbd5e1; }
  input {
    width: 60px;
    background: #0f172a;
    border: 1px solid rgba(255,255,255,0.1);
    color: #fff;
    padding: 0.25rem 0.5rem;
    border-radius: 0.25rem;
    text-align: center;
  }
}

.btn-small {
  background: #10b981;
  color: #fff;
  border: none;
  padding: 0.35rem 0.75rem;
  border-radius: 0.25rem;
  font-size: 0.8rem;
  font-weight: 600;
  cursor: pointer;
  &:hover:not(:disabled) { background: #059669; }
  &:disabled { opacity: 0.5; cursor: not-allowed; }
}

.empty { color: #94a3b8; font-size: 0.9rem; }

.alert-card {
  background: rgba(245, 158, 11, 0.05);
  border: 1px solid rgba(245, 158, 11, 0.2);
  border-radius: 0.75rem;
  padding: 1rem;
  margin-bottom: 0.75rem;
}

.add-folder-form {
  display: flex;
  gap: 1rem;
  margin-bottom: 1rem;
  
  input {
    flex: 1;
    background: #0f172a;
    border: 1px solid rgba(255, 255, 255, 0.1);
    color: #fff;
    padding: 0.75rem 1rem;
    border-radius: 0.5rem;
    &:focus { outline: none; border-color: #6366f1; }
  }
}

.btn-submit {
  background: #6366f1;
  color: #fff;
  border: none;
  padding: 0.75rem 1.5rem;
  border-radius: 0.5rem;
  font-weight: 600;
  cursor: pointer;
  &:hover:not(:disabled) { background: #4f46e5; }
  &:disabled { opacity: 0.5; cursor: not-allowed; }
}

.folder-card {
  display: flex;
  justify-content: space-between;
  align-items: center;
  background: rgba(255, 255, 255, 0.03);
  border: 1px solid rgba(255, 255, 255, 0.1);
  padding: 1rem;
  border-radius: 0.5rem;
  margin-bottom: 0.5rem;

  .folder-name { font-weight: 700; color: #f8fafc; margin-right: 1rem; }
  .folder-id { font-size: 0.85rem; color: #94a3b8; font-family: monospace; }

  .folder-actions {
    display: flex;
    align-items: center;
    gap: 0.75rem;

    .btn-delete-folder {
      background: none;
      border: 1px solid rgba(239, 68, 68, 0.2);
      color: #f87171;
      padding: 0.35rem 0.6rem;
      border-radius: 0.35rem;
      cursor: pointer;
      &:hover { background: rgba(239, 68, 68, 0.15); }
    }
  }
}

.status-badge.active {
  background: rgba(16, 185, 129, 0.2);
  color: #34d399;
  font-size: 0.8rem;
  padding: 0.25rem 0.75rem;
  border-radius: 1rem;
  font-weight: 600;
}

.mt-2 { margin-top: 1.5rem; }

.alert-header { display: flex; justify-content: space-between; margin-bottom: 0.5rem; }
.file-name { font-weight: 700; color: #fbbf24; }

.severity-badge {
  font-size: 0.75rem;
  padding: 0.2rem 0.5rem;
  border-radius: 0.25rem;
  font-weight: 800;
  &.high { background: rgba(239, 68, 68, 0.2); color: #fca5a5; }
  &.medium { background: rgba(245, 158, 11, 0.2); color: #fbbf24; }
}

.reason { font-size: 0.85rem; color: #94a3b8; margin-bottom: 0.75rem; }

.alert-actions {
  display: flex;
  gap: 0.75rem;
  align-items: center;
}

.quarantine-btn {
  background: #ef4444;
  color: #fff;
  border: none;
  padding: 0.5rem 1rem;
  border-radius: 0.5rem;
  font-weight: 600;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 0.35rem;
  font-size: 0.85rem;
  &:hover { background: #dc2626; }
}

.restore-btn {
  background: rgba(99, 102, 241, 0.15);
  color: #818cf8;
  border: 1px solid rgba(99, 102, 241, 0.3);
  padding: 0.5rem 1rem;
  border-radius: 0.5rem;
  font-weight: 600;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 0.35rem;
  font-size: 0.85rem;
  &:hover { background: rgba(99, 102, 241, 0.25); }
}

.resolve-btn {
  background: rgba(16, 185, 129, 0.15);
  color: #34d399;
  border: 1px solid rgba(16, 185, 129, 0.3);
  padding: 0.5rem 1rem;
  border-radius: 0.5rem;
  font-weight: 600;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 0.35rem;
  font-size: 0.85rem;
  &:hover { background: rgba(16, 185, 129, 0.25); }
}

.log-item {
  display: flex;
  gap: 1rem;
  padding: 0.75rem 0;
  border-bottom: 1px solid rgba(255, 255, 255, 0.05);
  font-size: 0.85rem;
}

.timestamp { color: #64748b; width: 140px; }
.action { color: #818cf8; font-weight: 700; width: 100px; }
.user { color: #94a3b8; width: 150px; }
.file { color: #f8fafc; flex: 1; }
</style>
