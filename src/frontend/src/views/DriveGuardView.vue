<template>
  <div class="drive-guard-page">
    <header class="page-header">
      <h1>🛡 Drive Guard & Audit (UC05 & UC06)</h1>
      <p>Giám sát biến động thư mục Google Drive & Cảnh báo an ninh</p>
    </header>

    <div class="sections">
      <!-- Section 1: Security Alerts (UC06) -->
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

const fetchData = async () => {
  try {
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

  h2 { font-size: 1.25rem; font-weight: 800; margin-bottom: 1rem; color: #f8fafc; }
}

.empty { color: #94a3b8; font-size: 0.9rem; }

.alert-card {
  background: rgba(245, 158, 11, 0.05);
  border: 1px solid rgba(245, 158, 11, 0.2);
  border-radius: 0.75rem;
  padding: 1rem;
  margin-bottom: 0.75rem;
}

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
