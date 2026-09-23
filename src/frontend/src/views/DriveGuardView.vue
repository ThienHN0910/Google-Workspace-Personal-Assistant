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
              <button class="btn-explore" @click="startExploring(f.googleFolderId, f.folderName)" title="Khám phá chi tiết cấu trúc thư mục">
                <i class="pi pi-compass"></i> Khám phá
              </button>
              <span class="status-badge active"><i class="pi pi-eye"></i> Đang theo dõi</span>
              <button class="btn-delete-folder" @click="handleDeleteFolder(f.id)" title="Xóa thư mục khỏi theo dõi">
                <i class="pi pi-trash"></i>
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Section 1.5: Deep Folder Explorer -->
      <div v-if="exploring" class="card-section explorer-section">
        <div class="section-header">
          <div class="explorer-header-left">
            <h2>
              <i class="pi pi-compass text-cyan"></i>
              Khám phá cấu trúc: <span class="highlight-folder">{{ currentFolder?.name }}</span>
            </h2>
            <div class="folder-counts">
              <span class="count-badge"><i class="pi pi-folder"></i> {{ folderStats.folders }} thư mục</span>
              <span class="count-badge"><i class="pi pi-file"></i> {{ folderStats.files }} tệp tin</span>
            </div>
          </div>
          <div class="explorer-header-actions">
            <div class="search-box">
              <i class="pi pi-search"></i>
              <input
                v-model="folderSearchQuery"
                type="text"
                placeholder="Lọc tên file, thư mục..."
                class="search-input"
              />
              <button v-if="folderSearchQuery" class="btn-clear-search" @click="folderSearchQuery = ''">
                <i class="pi pi-times"></i>
              </button>
            </div>
            <button
              class="btn-icon"
              @click="loadFolderContents(currentFolder!.id, currentFolder!.name)"
              :disabled="loadingFolderItems"
              title="Tải lại nội dung thư mục"
            >
              <i class="pi pi-sync" :class="{ 'pi-spin': loadingFolderItems }"></i>
            </button>
            <button class="btn-icon btn-close" @click="closeExplorer" title="Đóng khám phá">
              <i class="pi pi-times"></i>
            </button>
          </div>
        </div>

        <!-- Breadcrumbs Navigation -->
        <nav class="breadcrumbs-bar" aria-label="Thanh điều hướng thư mục">
          <span
            v-for="(crumb, idx) in breadcrumbs"
            :key="crumb.id"
            class="breadcrumb-item"
            :class="{ active: idx === breadcrumbs.length - 1 }"
          >
            <button
              v-if="idx < breadcrumbs.length - 1"
              class="breadcrumb-link"
              @click="navigateToBreadcrumb(idx)"
            >
              <i v-if="idx === 0" class="pi pi-home breadcrumb-icon"></i>
              <i v-else class="pi pi-folder breadcrumb-icon"></i>
              <span>{{ crumb.name }}</span>
            </button>
            <span v-else class="breadcrumb-current">
              <i v-if="idx === 0" class="pi pi-home breadcrumb-icon"></i>
              <i v-else class="pi pi-folder-open breadcrumb-icon"></i>
              <span>{{ crumb.name }}</span>
            </span>
            <i v-if="idx < breadcrumbs.length - 1" class="pi pi-chevron-right breadcrumb-separator"></i>
          </span>
        </nav>

        <!-- Folder Items Content -->
        <div v-if="loadingFolderItems" class="loading-wrap">
          <LoadingSpinner text="Đang duyệt nội dung thư mục..." />
        </div>
        <div v-else-if="filteredFolderItems.length === 0" class="empty">
          {{ folderSearchQuery ? 'Không tìm thấy file hoặc thư mục phù hợp với từ khóa.' : 'Thư mục này hiện đang trống.' }}
        </div>
        <div v-else class="explorer-table-container">
          <table class="explorer-table">
            <thead>
              <tr>
                <th class="col-name">Tên file / thư mục</th>
                <th class="col-type">Loại</th>
                <th class="col-size">Kích thước</th>
                <th class="col-modified">Cập nhật lần cuối</th>
                <th class="col-actions">Hành động</th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="item in filteredFolderItems"
                :key="item.id"
                class="explorer-row"
                :class="{ 'is-folder': isFolder(item.mimeType) }"
              >
                <td class="col-name">
                  <div
                    v-if="isFolder(item.mimeType)"
                    class="folder-link"
                    @click="drillDown(item)"
                    role="button"
                    tabindex="0"
                    title="Nhấn để mở thư mục con"
                  >
                    <i class="pi pi-folder-fill folder-icon"></i>
                    <span class="item-name font-bold">{{ item.name }}</span>
                    <span class="subfolder-indicator"><i class="pi pi-angle-right"></i></span>
                  </div>
                  <div v-else class="file-display">
                    <i :class="getFileIcon(item.mimeType)" class="file-icon"></i>
                    <span class="item-name">{{ item.name }}</span>
                  </div>
                </td>
                <td class="col-type">
                  <span class="type-badge" :class="getTypeBadgeClass(item.mimeType)">
                    {{ formatMimeType(item.mimeType) }}
                  </span>
                </td>
                <td class="col-size">
                  <span class="size-text">{{ formatBytes(item.size) }}</span>
                </td>
                <td class="col-modified">
                  <div class="modified-cell">
                    <span class="date">{{ item.modifiedTime ? formatDate(item.modifiedTime) : '-' }}</span>
                    <span v-if="item.lastModifyingUser" class="author">
                      <i class="pi pi-user text-xs"></i> {{ item.lastModifyingUser }}
                    </span>
                  </div>
                </td>
                <td class="col-actions">
                  <div class="row-actions">
                    <a
                      :href="getDriveLink(item)"
                      target="_blank"
                      rel="noopener noreferrer"
                      class="btn-row-action"
                      title="Mở trên Google Drive"
                    >
                      <i class="pi pi-external-link"></i> Mở
                    </a>
                    <button
                      v-if="!isFolder(item.mimeType)"
                      class="btn-row-action btn-quarantine-quick"
                      @click="handleQuarantine(item.id)"
                      title="Cách ly file nghi ngờ vào G-Ops Quarantine"
                    >
                      <i class="pi pi-shield"></i> Cách ly
                    </button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
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
import { ref, computed, onMounted, defineAsyncComponent } from 'vue';
import api from '@/services/api.service';
import { showToast } from '@/services/notification.service';

const LoadingSpinner = defineAsyncComponent(() => import('@/components/common/LoadingSpinner.vue'));
const InfiniteScrollObserver = defineAsyncComponent(() => import('@/components/common/InfiniteScrollObserver.vue'));

const alerts = ref<any[]>([]);
const logs = ref<any[]>([]);
const folders = ref<any[]>([]);
const addingFolder = ref(false);
const newFolder = ref({ folderName: '', googleFolderId: '' });

// Folder Explorer state
const exploring = ref(false);
const currentFolder = ref<{ id: string; name: string } | null>(null);
const breadcrumbs = ref<Array<{ id: string; name: string }>>([]);
const folderItems = ref<any[]>([]);
const loadingFolderItems = ref(false);
const folderSearchQuery = ref('');

const isFolder = (mimeType: string) => mimeType === 'application/vnd.google-apps.folder';

const getFileIcon = (mimeType: string) => {
  if (isFolder(mimeType)) return 'pi pi-folder-fill text-amber';
  if (!mimeType) return 'pi pi-file text-slate';
  if (mimeType.includes('pdf')) return 'pi pi-file-pdf text-rose';
  if (mimeType.includes('spreadsheet') || mimeType.includes('excel') || mimeType.includes('csv')) return 'pi pi-file-excel text-emerald';
  if (mimeType.includes('document') || mimeType.includes('word')) return 'pi pi-file-word text-blue';
  if (mimeType.includes('presentation') || mimeType.includes('powerpoint')) return 'pi pi-desktop text-orange';
  if (mimeType.includes('image')) return 'pi pi-image text-purple';
  if (mimeType.includes('video')) return 'pi pi-video text-pink';
  if (mimeType.includes('zip') || mimeType.includes('compressed') || mimeType.includes('tar') || mimeType.includes('rar')) return 'pi pi-box text-amber';
  return 'pi pi-file text-slate';
};

const getTypeBadgeClass = (mimeType: string) => {
  if (isFolder(mimeType)) return 'badge-folder';
  if (!mimeType) return 'badge-file';
  if (mimeType.includes('pdf')) return 'badge-pdf';
  if (mimeType.includes('spreadsheet') || mimeType.includes('excel')) return 'badge-sheet';
  if (mimeType.includes('document') || mimeType.includes('word')) return 'badge-doc';
  if (mimeType.includes('image')) return 'badge-image';
  return 'badge-file';
};

const formatBytes = (bytes?: number) => {
  if (bytes === undefined || bytes === null) return '-';
  if (bytes === 0) return '0 B';
  const k = 1024;
  const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
};

const formatMimeType = (mimeType: string) => {
  if (!mimeType) return 'Tệp tin';
  if (mimeType === 'application/vnd.google-apps.folder') return 'Thư mục';
  if (mimeType === 'application/vnd.google-apps.spreadsheet') return 'Google Sheet';
  if (mimeType === 'application/vnd.google-apps.document') return 'Google Doc';
  if (mimeType === 'application/vnd.google-apps.presentation') return 'Google Slides';
  if (mimeType === 'application/pdf') return 'PDF';
  if (mimeType.includes('image/')) return 'Hình ảnh';
  if (mimeType.includes('video/')) return 'Video';
  if (mimeType.includes('zip') || mimeType.includes('compressed')) return 'Tệp nén';
  const parts = mimeType.split('/');
  return parts.length > 1 ? parts[1].toUpperCase() : mimeType;
};

const getDriveLink = (item: any) => {
  if (isFolder(item.mimeType)) {
    return `https://drive.google.com/drive/folders/${item.id}`;
  }
  return `https://drive.google.com/file/d/${item.id}/view`;
};

const folderStats = computed(() => {
  const foldersCount = folderItems.value.filter(x => isFolder(x.mimeType)).length;
  const filesCount = folderItems.value.length - foldersCount;
  return { folders: foldersCount, files: filesCount };
});

const filteredFolderItems = computed(() => {
  let list = [...folderItems.value];
  if (folderSearchQuery.value.trim()) {
    const q = folderSearchQuery.value.trim().toLowerCase();
    list = list.filter(item =>
      (item.name && item.name.toLowerCase().includes(q)) ||
      (item.lastModifyingUser && item.lastModifyingUser.toLowerCase().includes(q))
    );
  }
  return list.sort((a, b) => {
    const aIsFolder = isFolder(a.mimeType);
    const bIsFolder = isFolder(b.mimeType);
    if (aIsFolder && !bIsFolder) return -1;
    if (!aIsFolder && bIsFolder) return 1;
    return (a.name || '').localeCompare(b.name || '');
  });
});

const startExploring = (folderId: string, folderName: string) => {
  exploring.value = true;
  breadcrumbs.value = [{ id: folderId, name: folderName }];
  currentFolder.value = { id: folderId, name: folderName };
  folderSearchQuery.value = '';
  loadFolderContents(folderId, folderName);
};

const drillDown = (item: any) => {
  if (!isFolder(item.mimeType)) return;
  breadcrumbs.value.push({ id: item.id, name: item.name });
  currentFolder.value = { id: item.id, name: item.name };
  folderSearchQuery.value = '';
  loadFolderContents(item.id, item.name);
};

const navigateToBreadcrumb = (index: number) => {
  if (index < 0 || index >= breadcrumbs.value.length) return;
  const target = breadcrumbs.value[index];
  breadcrumbs.value = breadcrumbs.value.slice(0, index + 1);
  currentFolder.value = target;
  folderSearchQuery.value = '';
  loadFolderContents(target.id, target.name);
};

const loadFolderContents = async (folderId: string, folderName: string) => {
  loadingFolderItems.value = true;
  folderItems.value = [];
  try {
    const res: any = await api.get(`/driveguard/folders/${folderId}/contents`);
    if (res.success && res.data) {
      folderItems.value = res.data;
    } else {
      folderItems.value = [];
    }
  } catch (e: any) {
    showToast({
      severity: 'error',
      summary: 'Lỗi khám phá',
      detail: e?.message || 'Không thể tải nội dung thư mục Google Drive.',
    });
  } finally {
    loadingFolderItems.value = false;
  }
};

const closeExplorer = () => {
  exploring.value = false;
  breadcrumbs.value = [];
  folderItems.value = [];
  currentFolder.value = null;
  folderSearchQuery.value = '';
};

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

  .btn-explore {
    height: 30px;
    background: rgba(6, 182, 212, 0.15);
    border: 1px solid rgba(6, 182, 212, 0.35);
    color: #22d3ee;
    padding: 0 0.75rem;
    border-radius: 0.4rem;
    font-size: 0.75rem;
    font-weight: 600;
    cursor: pointer;
    display: inline-flex;
    align-items: center;
    gap: 0.35rem;
    transition: all 0.15s ease;

    &:hover {
      background: #06b6d4;
      color: #0b0f19;
      transform: translate3d(0, -1px, 0);
      box-shadow: 0 0 12px rgba(6, 182, 212, 0.4);
    }
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
   EXPLORER SECTION (DEEP FOLDER INSPECTION)
   ============================================================ */
.explorer-section {
  border-color: rgba(6, 182, 212, 0.3);
  box-shadow: 0 0 24px rgba(6, 182, 212, 0.08);

  .explorer-header-left {
    display: flex;
    flex-direction: column;
    gap: 0.35rem;

    h2 {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      margin: 0;
      font-size: 1.05rem;

      .highlight-folder {
        color: #22d3ee;
        font-weight: 800;
      }
    }

    .folder-counts {
      display: flex;
      gap: 0.5rem;

      .count-badge {
        font-size: 0.725rem;
        background: rgba(148, 163, 184, 0.1);
        border: 1px solid rgba(148, 163, 184, 0.2);
        color: #94a3b8;
        padding: 0.15rem 0.5rem;
        border-radius: 9999px;
        display: inline-flex;
        align-items: center;
        gap: 0.35rem;
      }
    }
  }

  .explorer-header-actions {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    flex-wrap: wrap;

    .search-box {
      position: relative;
      display: flex;
      align-items: center;

      i.pi-search {
        position: absolute;
        left: 0.65rem;
        color: #64748b;
        font-size: 0.8rem;
      }

      .search-input {
        width: 220px;
        height: 32px;
        padding: 0 1.8rem 0 2rem;
        background: rgba(11, 17, 32, 0.8);
        border: 1px solid rgba(148, 163, 184, 0.2);
        border-radius: 0.4rem;
        color: #f8fafc;
        font-size: 0.775rem;
        outline: none;
        transition: all 0.15s ease;

        &:focus {
          border-color: rgba(6, 182, 212, 0.5);
          box-shadow: 0 0 10px rgba(6, 182, 212, 0.2);
        }
      }

      .btn-clear-search {
        position: absolute;
        right: 0.4rem;
        background: none;
        border: none;
        color: #64748b;
        cursor: pointer;
        font-size: 0.75rem;
        padding: 0.2rem;
        &:hover { color: #fff; }
      }
    }

    .btn-icon {
      width: 32px;
      height: 32px;
      background: rgba(148, 163, 184, 0.1);
      border: 1px solid rgba(148, 163, 184, 0.2);
      border-radius: 0.4rem;
      color: #94a3b8;
      cursor: pointer;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      transition: all 0.15s ease;

      &:hover:not(:disabled) {
        background: rgba(148, 163, 184, 0.2);
        color: #f8fafc;
      }

      &.btn-close:hover {
        background: rgba(244, 63, 94, 0.2);
        border-color: rgba(244, 63, 94, 0.4);
        color: #fb7185;
      }
    }
  }

  .breadcrumbs-bar {
    display: flex;
    align-items: center;
    flex-wrap: wrap;
    gap: 0.35rem;
    padding: 0.5rem 0.75rem;
    background: rgba(11, 17, 32, 0.7);
    border: 1px solid rgba(148, 163, 184, 0.12);
    border-radius: 0.5rem;
    margin: 0.75rem 0 1rem 0;

    .breadcrumb-item {
      display: inline-flex;
      align-items: center;
      gap: 0.35rem;
      font-size: 0.775rem;

      .breadcrumb-link {
        background: transparent;
        border: none;
        color: #38bdf8;
        cursor: pointer;
        padding: 0.2rem 0.4rem;
        border-radius: 0.25rem;
        display: inline-flex;
        align-items: center;
        gap: 0.35rem;
        font-weight: 500;
        transition: all 0.15s ease;

        &:hover {
          background: rgba(56, 189, 248, 0.15);
          color: #7dd3fc;
        }
      }

      .breadcrumb-current {
        color: #f1f5f9;
        font-weight: 700;
        padding: 0.2rem 0.4rem;
        display: inline-flex;
        align-items: center;
        gap: 0.35rem;
      }

      .breadcrumb-separator {
        color: #475569;
        font-size: 0.65rem;
      }

      .breadcrumb-icon {
        font-size: 0.75rem;
      }
    }
  }

  .explorer-table-container {
    overflow-x: auto;
    border-radius: 0.5rem;
    border: 1px solid rgba(148, 163, 184, 0.1);
  }

  .explorer-table {
    width: 100%;
    border-collapse: collapse;
    font-size: 0.825rem;

    thead tr {
      background: rgba(11, 17, 32, 0.85);
      border-bottom: 1px solid rgba(148, 163, 184, 0.15);

      th {
        padding: 0.75rem 0.85rem;
        text-align: left;
        font-weight: 600;
        color: #94a3b8;
        font-size: 0.75rem;
        text-transform: uppercase;
        letter-spacing: 0.04em;
        white-space: nowrap;
      }
    }

    tbody tr.explorer-row {
      border-bottom: 1px solid rgba(148, 163, 184, 0.08);
      transition: background 0.15s ease;

      &:hover {
        background: rgba(30, 41, 59, 0.4);
      }

      &.is-folder {
        background: rgba(245, 158, 11, 0.02);
        &:hover { background: rgba(245, 158, 11, 0.08); }
      }

      td {
        padding: 0.65rem 0.85rem;
        vertical-align: middle;
      }
    }

    .folder-link {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      cursor: pointer;
      color: #fbbf24;
      font-weight: 600;
      transition: color 0.15s ease;

      &:hover {
        color: #fef08a;
        .subfolder-indicator { transform: translateX(3px); }
      }

      .folder-icon {
        color: #f59e0b;
        font-size: 1rem;
      }

      .subfolder-indicator {
        font-size: 0.75rem;
        color: #f59e0b;
        transition: transform 0.15s ease;
      }
    }

    .file-display {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      color: #e2e8f0;

      .file-icon {
        font-size: 0.95rem;
      }
    }

    .type-badge {
      font-size: 0.7rem;
      font-weight: 600;
      padding: 0.15rem 0.5rem;
      border-radius: 9999px;
      white-space: nowrap;

      &.badge-folder { background: rgba(245, 158, 11, 0.15); color: #fbbf24; border: 1px solid rgba(245, 158, 11, 0.3); }
      &.badge-sheet { background: rgba(16, 185, 129, 0.15); color: #34d399; border: 1px solid rgba(16, 185, 129, 0.3); }
      &.badge-doc { background: rgba(59, 130, 246, 0.15); color: #60a5fa; border: 1px solid rgba(59, 130, 246, 0.3); }
      &.badge-pdf { background: rgba(244, 63, 94, 0.15); color: #fb7185; border: 1px solid rgba(244, 63, 94, 0.3); }
      &.badge-image { background: rgba(168, 85, 247, 0.15); color: #c084fc; border: 1px solid rgba(168, 85, 247, 0.3); }
      &.badge-file { background: rgba(148, 163, 184, 0.12); color: #94a3b8; border: 1px solid rgba(148, 163, 184, 0.2); }
    }

    .size-text {
      color: #94a3b8;
      font-family: var(--font-mono, monospace);
      font-size: 0.775rem;
      white-space: nowrap;
    }

    .modified-cell {
      display: flex;
      flex-direction: column;
      gap: 0.15rem;

      .date {
        color: #cbd5e1;
        font-size: 0.75rem;
        font-variant-numeric: tabular-nums;
        white-space: nowrap;
      }
      .author {
        color: #64748b;
        font-size: 0.7rem;
        display: inline-flex;
        align-items: center;
        gap: 0.25rem;
        white-space: nowrap;
      }
    }

    .row-actions {
      display: flex;
      align-items: center;
      gap: 0.4rem;
      white-space: nowrap;

      .btn-row-action {
        height: 26px;
        padding: 0 0.55rem;
        border-radius: 0.35rem;
        font-size: 0.725rem;
        font-weight: 600;
        cursor: pointer;
        display: inline-flex;
        align-items: center;
        gap: 0.3rem;
        text-decoration: none;
        background: rgba(30, 41, 59, 0.6);
        border: 1px solid rgba(148, 163, 184, 0.2);
        color: #cbd5e1;
        transition: all 0.15s ease;

        &:hover {
          background: rgba(59, 130, 246, 0.2);
          border-color: rgba(59, 130, 246, 0.4);
          color: #93c5fd;
        }

        &.btn-quarantine-quick {
          background: rgba(244, 63, 94, 0.1);
          border-color: rgba(244, 63, 94, 0.25);
          color: #fb7185;

          &:hover {
            background: #f43f5e;
            color: #fff;
          }
        }
      }
    }
  }

  .loading-wrap {
    padding: 2rem 0;
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
