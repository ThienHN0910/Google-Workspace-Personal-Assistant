<template>
  <div class="drive-guard-page">
    <header class="page-header">
      <h1>🛡 Drive Guard & Audit (UC05 & UC06)</h1>
      <p>Giám sát biến động thư mục Google Drive & Cảnh báo an ninh</p>
    </header>

    <div class="sections">
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
            <span class="status-badge active"><i class="pi pi-eye"></i> Đang theo dõi</span>
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
                <i class="pi pi-shield"></i> Cách ly vào Quarantine Folder
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
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import api from '@/services/api.service';

const alerts = ref<any[]>([]);
const logs = ref<any[]>([]);
const folders = ref<any[]>([]);
const addingFolder = ref(false);
const newFolder = ref({ folderName: '', googleFolderId: '' });

const intervalMinutes = ref(5);
const updatingInterval = ref(false);

const fetchData = async () => {
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

    const resLogs: any = await api.get('/driveguard/audit-logs');
    if (resLogs.success && resLogs.data) {
      logs.value = resLogs.data.items;
    }
  } catch (e) {
    console.error('Failed to load drive guard data:', e);
  }
};

const addFolder = async () => {
  addingFolder.value = true;
  try {
    const res: any = await api.post('/driveguard/folders', newFolder.value);
    if (res.success) {
      newFolder.value = { folderName: '', googleFolderId: '' };
      fetchData();
      alert('Đã thêm thư mục theo dõi!');
    }
  } catch (e) {
    alert('Lỗi thêm thư mục. Vui lòng kiểm tra lại.');
  } finally {
    addingFolder.value = false;
  }
};

const updateInterval = async () => {
  updatingInterval.value = true;
  try {
    const res: any = await api.post('/driveguard/interval', { minutes: intervalMinutes.value });
    if (res.success) {
      alert('Đã cập nhật chu kỳ quét thành công! Hệ thống sẽ quét theo lịch mới.');
    }
  } catch (e) {
    alert('Lỗi cập nhật cấu hình.');
  } finally {
    updatingInterval.value = false;
  }
};

const handleQuarantine = async (fileId: string) => {
  try {
    const res: any = await api.post('/driveguard/quarantine', {
      fileId,
      quarantineFolderId: 'QUARANTINE_DEFAULT_FOLDER_ID'
    });
    if (res.success) {
      alert('Đã cách ly file!');
      fetchData();
    }
  } catch (e) {
    alert('Lỗi cách ly file');
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
