<template>
  <div class="drive-guard-page">
    <header class="page-header">
      <div class="title-group">
        <div class="title-icon-badge">
          <i class="pi pi-shield"></i>
        </div>
        <div class="title-text">
          <div class="title-row">
            <h1>Drive Guard & Security Audit</h1>
            <span class="version-tag">UC05 • UC06</span>
          </div>
          <p class="subtitle">Giám sát biến động thư mục Google Drive, cách ly rủi ro & cảnh báo an ninh</p>
        </div>
      </div>
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
            <button class="btn-scan-now" @click="handleScanNow" :disabled="scanningNow">
              <i class="pi" :class="scanningNow ? 'pi-spin pi-spinner' : 'pi-sync'"></i>
              {{ scanningNow ? 'Đang quét...' : 'Quét ngay' }}
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

const scanningNow = ref(false);
const handleScanNow = async () => {
  scanningNow.value = true;
  try {
    const res: any = await api.post('/driveguard/audit/run', {});
    if (res.success) {
      showToast({
        severity: 'success',
        summary: 'Hoàn thành quét',
        detail: res.message || 'Đã kiểm tra xong biến động thư mục Google Drive.',
      });
      fetchData();
    }
  } catch (e: any) {
    showToast({
      severity: 'error',
      summary: 'Lỗi',
      detail: e.message || 'Không thể thực hiện quét ngay.',
    });
  } finally {
    scanningNow.value = false;
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
.drive-guard-page {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

/* ============================================================
   CYBER-COCKPIT HEADER
   ============================================================ */
.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 1rem;
  padding: 0.25rem 0.1rem;

  .title-group {
    display: flex;
    align-items: center;
    gap: 0.85rem;

    .title-icon-badge {
      width: 42px;
      height: 42px;
      border-radius: 0.75rem;
      background: linear-gradient(135deg, rgba(244, 63, 94, 0.25), rgba(6, 182, 212, 0.2));
      border: 1px solid rgba(244, 63, 94, 0.35);
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 1.25rem;
      color: #fb7185;
      box-shadow: 0 0 16px rgba(244, 63, 94, 0.2);
    }

    .title-text {
      display: flex;
      flex-direction: column;
      gap: 0.2rem;

      .title-row {
        display: flex;
        align-items: center;
        gap: 0.65rem;
        flex-wrap: wrap;

        h1 {
          font-size: 1.45rem;
          font-weight: 800;
          letter-spacing: -0.02em;
          margin: 0;
          background: linear-gradient(135deg, #ffffff 0%, #cbd5e1 100%);
          -webkit-background-clip: text;
          -webkit-text-fill-color: transparent;
        }

        .version-tag {
          font-size: 0.675rem;
          font-family: var(--font-mono, monospace);
          font-weight: 700;
          padding: 0.15rem 0.5rem;
          border-radius: 9999px;
          background: rgba(244, 63, 94, 0.12);
          border: 1px solid rgba(244, 63, 94, 0.3);
          color: #fb7185;
          letter-spacing: 0.04em;
        }
      }

      .subtitle {
        color: #94a3b8;
        font-size: 0.825rem;
        margin: 0;
      }
    }
  }
}

.sections {
  display: flex;
  flex-direction: column;
  gap: 1.15rem;
}

/* ============================================================
   CARD SECTION
   ============================================================ */
.card-section {
  background: rgba(15, 23, 42, 0.7);
  border: 1px solid rgba(148, 163, 184, 0.12);
  border-top: 1px solid rgba(255, 255, 255, 0.12);
  border-radius: 0.85rem;
  padding: 1.25rem;
  backdrop-filter: blur(14px);
  -webkit-backdrop-filter: blur(14px);
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.3);

  .section-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    flex-wrap: wrap;
    gap: 0.75rem;
    margin-bottom: 1rem;
    
    h2 { margin-bottom: 0; }
  }

  h2 {
    font-size: 1rem;
    font-weight: 700;
    margin-bottom: 1rem;
    color: #f8fafc;
  }
}

/* ============================================================
   INTERVAL CONFIG & SCAN
   ============================================================ */
.config-interval {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  background: rgba(11, 17, 32, 0.6);
  border: 1px solid rgba(148, 163, 184, 0.12);
  padding: 0.35rem 0.65rem;
  border-radius: 0.5rem;
  flex-wrap: wrap;

  label {
    font-size: 0.775rem;
    color: #94a3b8;
    font-weight: 500;
  }

  input {
    width: 52px;
    height: 30px;
    background: rgba(15, 23, 42, 0.8);
    border: 1px solid rgba(148, 163, 184, 0.2);
    color: #fff;
    padding: 0 0.35rem;
    border-radius: 0.35rem;
    text-align: center;
    font-size: 0.825rem;
    font-family: var(--font-mono, monospace);
    outline: none;

    &:focus {
      border-color: rgba(6, 182, 212, 0.45);
    }
  }
}

.btn-small {
  height: 30px;
  background: linear-gradient(135deg, #10b981 0%, #059669 100%);
  color: #fff;
  border: 1px solid rgba(52, 211, 153, 0.3);
  padding: 0 0.65rem;
  border-radius: 0.35rem;
  font-size: 0.75rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.15s ease;

  &:hover:not(:disabled) {
    filter: brightness(1.1);
    transform: translate3d(0, -1px, 0);
  }
  &:disabled { opacity: 0.5; cursor: not-allowed; }
}

.btn-scan-now {
  height: 30px;
  background: rgba(99, 102, 241, 0.15);
  border: 1px solid rgba(99, 102, 241, 0.35);
  color: #a5b4fc;
  padding: 0 0.75rem;
  border-radius: 0.35rem;
  font-size: 0.75rem;
  font-weight: 600;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  transition: all 0.15s ease;

  &:hover:not(:disabled) {
    background: #6366f1;
    color: #fff;
    transform: translate3d(0, -1px, 0);
  }
  &:disabled { opacity: 0.5; cursor: not-allowed; }
}

/* ============================================================
   ADD FOLDER FORM
   ============================================================ */
.add-folder-form {
  display: flex;
  gap: 0.65rem;
  flex-wrap: wrap;

  input {
    flex: 1;
    min-width: 220px;
    height: 38px;
    background: rgba(11, 17, 32, 0.7);
    border: 1px solid rgba(148, 163, 184, 0.16);
    border-radius: 0.5rem;
    padding: 0 0.85rem;
    color: #f8fafc;
    font-size: 0.825rem;
    outline: none;
    transition: all 0.15s ease;

    &::placeholder { color: #64748b; }

    &:focus {
      border-color: rgba(6, 182, 212, 0.45);
      box-shadow: 0 0 10px rgba(6, 182, 212, 0.15);
    }
  }

  .btn-submit {
    height: 38px;
    padding: 0 1.15rem;
    background: linear-gradient(135deg, #6366f1 0%, #4f46e5 100%);
    border: 1px solid rgba(99, 102, 241, 0.4);
    color: #fff;
    border-radius: 0.5rem;
    font-weight: 600;
    font-size: 0.825rem;
    cursor: pointer;
    display: inline-flex;
    align-items: center;
    gap: 0.45rem;
    transition: all 0.15s ease;

    &:hover:not(:disabled) {
      filter: brightness(1.1);
      transform: translate3d(0, -1.5px, 0);
    }
    &:disabled { opacity: 0.5; cursor: not-allowed; }
  }
}

.folders-list {
  display: flex;
  flex-direction: column;
  gap: 0.55rem;
}

.folder-card {
  display: flex;
  justify-content: space-between;
  align-items: center;
  background: rgba(11, 17, 32, 0.5);
  border: 1px solid rgba(148, 163, 184, 0.1);
  border-radius: 0.65rem;
  padding: 0.75rem 0.95rem;
  transition: all 0.15s ease;

  &:hover {
    border-color: rgba(99, 102, 241, 0.3);
    transform: translate3d(0, -1px, 0);
  }

  .folder-name {
    font-weight: 700;
    color: #f1f5f9;
    font-size: 0.85rem;
    display: block;
    margin-bottom: 0.15rem;
  }

  .folder-id {
    font-size: 0.725rem;
    color: #64748b;
    font-family: var(--font-mono, monospace);
  }

  .folder-actions {
    display: flex;
    align-items: center;
    gap: 0.65rem;
  }

  .status-badge {
    display: inline-flex;
    align-items: center;
    gap: 0.35rem;
    font-size: 0.725rem;
    font-weight: 600;
    padding: 0.2rem 0.55rem;
    border-radius: 9999px;

    &.active {
      background: rgba(16, 185, 129, 0.12);
      border: 1px solid rgba(16, 185, 129, 0.3);
      color: #34d399;
    }
  }

  .btn-delete-folder {
    width: 32px;
    height: 32px;
    background: rgba(244, 63, 94, 0.1);
    border: 1px solid rgba(244, 63, 94, 0.2);
    border-radius: 0.4rem;
    color: #fb7185;
    cursor: pointer;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 0.85rem;
    transition: all 0.15s ease;

    &:hover {
      background: #f43f5e;
      color: #fff;
      transform: translate3d(0, -1px, 0);
    }
  }
}

/* ============================================================
   SECURITY ALERTS
   ============================================================ */
.alerts-list {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.alert-card {
  background: linear-gradient(145deg, rgba(15, 23, 42, 0.85) 0%, rgba(244, 63, 94, 0.08) 100%);
  border: 1px solid rgba(244, 63, 94, 0.3);
  border-radius: 0.75rem;
  padding: 1rem 1.15rem;

  .alert-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 0.35rem;

    .file-name {
      font-weight: 700;
      color: #f8fafc;
      font-size: 0.9rem;
    }

    .severity-badge {
      font-size: 0.7rem;
      font-weight: 700;
      padding: 0.15rem 0.5rem;
      border-radius: 9999px;
      letter-spacing: 0.03em;
      text-transform: uppercase;

      &.critical, &.high {
        background: rgba(244, 63, 94, 0.2);
        border: 1px solid rgba(244, 63, 94, 0.4);
        color: #fb7185;
      }
      &.medium, &.warning {
        background: rgba(245, 158, 11, 0.2);
        border: 1px solid rgba(245, 158, 11, 0.4);
        color: #fbbf24;
      }
      &.low, &.info {
        background: rgba(6, 182, 212, 0.2);
        border: 1px solid rgba(6, 182, 212, 0.4);
        color: #22d3ee;
      }
    }
  }

  .reason {
    color: #cbd5e1;
    font-size: 0.8rem;
    line-height: 1.4;
    margin: 0 0 0.85rem 0;
  }

  .alert-actions {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    flex-wrap: wrap;

    button {
      height: 32px;
      padding: 0 0.75rem;
      border-radius: 0.4rem;
      font-size: 0.775rem;
      font-weight: 600;
      cursor: pointer;
      display: inline-flex;
      align-items: center;
      gap: 0.35rem;
      transition: all 0.15s ease;
      border: 1px solid transparent;
    }

    .quarantine-btn {
      background: linear-gradient(135deg, #f43f5e 0%, #e11d48 100%);
      border-color: rgba(244, 63, 94, 0.4);
      color: #fff;
      &:hover { filter: brightness(1.1); transform: translate3d(0, -1px, 0); }
    }

    .restore-btn {
      background: rgba(30, 41, 59, 0.6);
      border-color: rgba(148, 163, 184, 0.2);
      color: #cbd5e1;
      &:hover { background: rgba(51, 65, 85, 0.8); color: #fff; }
    }

    .resolve-btn {
      background: rgba(16, 185, 129, 0.12);
      border-color: rgba(16, 185, 129, 0.3);
      color: #34d399;
      &:hover { background: #10b981; color: #fff; transform: translate3d(0, -1px, 0); }
    }
  }
}

/* ============================================================
   DRIVE AUDIT LOGS
   ============================================================ */
.logs-list {
  display: flex;
  flex-direction: column;
}

.log-item {
  display: flex;
  gap: 1rem;
  padding: 0.65rem 0.25rem;
  border-bottom: 1px solid rgba(148, 163, 184, 0.08);
  font-size: 0.8rem;
  align-items: center;

  .timestamp {
    color: #64748b;
    width: 140px;
    font-size: 0.725rem;
    font-variant-numeric: tabular-nums;
  }
  .action {
    color: #38bdf8;
    font-weight: 700;
    width: 100px;
    font-size: 0.75rem;
  }
  .user {
    color: #94a3b8;
    width: 150px;
    white-space: nowrap;
    text-overflow: ellipsis;
    overflow: hidden;
  }
  .file {
    color: #f8fafc;
    flex: 1;
    white-space: nowrap;
    text-overflow: ellipsis;
    overflow: hidden;
  }
}

.empty {
  color: #64748b;
  font-size: 0.825rem;
  text-align: center;
  padding: 1.5rem 0;
}

.mt-2 { margin-top: 0.75rem; }

/* ============================================================
   RESPONSIVE BREAKPOINTS
   ============================================================ */
@media (max-width: 640px) {
  .card-section .section-header {
    flex-direction: column;
    align-items: stretch;

    .config-interval {
      width: 100%;
      justify-content: space-between;
    }
  }

  .add-folder-form {
    flex-direction: column;

    input, .btn-submit {
      width: 100%;
    }
  }

  .log-item {
    flex-direction: column;
    align-items: flex-start;
    gap: 0.25rem;

    .timestamp, .action, .user, .file {
      width: 100%;
    }
  }
}
</style>
