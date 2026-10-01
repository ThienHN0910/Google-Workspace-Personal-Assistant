<template>
  <section class="cleanup-reviews-container" aria-labelledby="cleanup-review-title">
    <!-- Top Action Bar -->
    <div class="review-top-bar">
      <div class="title-group">
        <div class="title-icon-badge">
          <i class="pi pi-shield-check"></i>
        </div>
        <div class="title-text">
          <div class="title-row">
            <h3 id="cleanup-review-title">Chờ duyệt dọn dẹp</h3>
            <span v-if="totalCount > 0" class="counter-badge">{{ totalCount }}</span>
          </div>
          <p class="subtitle">Nhập lý do để lưu cùng email. Lý do này sẽ được gửi kèm những yêu cầu AI phù hợp về sau.</p>
        </div>
      </div>
      <button type="button" class="btn-refresh" @click="load" :disabled="loading">
        <i class="pi pi-refresh" :class="{ 'pi-spin': loading }"></i>
        <span>Làm mới</span>
      </button>
    </div>

    <!-- Error Alert -->
    <div v-if="error" role="alert" class="review-error-banner">
      <i class="pi pi-exclamation-triangle"></i>
      <span>{{ error }}</span>
    </div>

    <!-- Loading State -->
    <div v-if="loading && !reviews.length" class="loading-state">
      <i class="pi pi-spin pi-spinner" style="font-size: 2rem; color: #38bdf8;"></i>
      <p>Đang tải email chờ duyệt...</p>
    </div>

    <!-- Empty State -->
    <div v-else-if="!reviews.length" class="empty-state">
      <i class="pi pi-check-circle" style="color: #10b981; font-size: 2.5rem;"></i>
      <p>Tuyệt vời! Không có email nào đang chờ duyệt dọn dẹp.</p>
    </div>

    <!-- Reviews Grid / Cards -->
    <div v-else class="reviews-list">
      <div v-for="review in reviews" :key="review.id" class="review-card">
        <!-- Card Header -->
        <div class="card-header">
          <div class="sender-info">
            <i class="pi pi-envelope sender-icon"></i>
            <span class="sender-text">{{ review.sender }}</span>
          </div>
          <span class="status-badge" :class="getStatusBadgeClass(review.status)">
            {{ getStatusText(review.status) }}
          </span>
        </div>

        <!-- Subject & Snippet -->
        <div class="card-subject">{{ review.subject || '(Không có tiêu đề)' }}</div>
        <div v-if="review.snippet" class="card-snippet">"{{ review.snippet }}"</div>

        <!-- AI Guidance / Callouts -->
        <div v-if="review.aiReason" class="ai-callout">
          <i class="pi pi-sparkles ai-icon"></i>
          <div>
            <strong>AI đề xuất:</strong> {{ review.aiReason }}
          </div>
        </div>

        <div v-if="review.proposedRuleId" class="proposal-callout">
          <i class="pi pi-sliders-h"></i>
          <span>Có bản nháp quy tắc regex <code>{{ review.proposedRuleId }}</code> đang tắt, chờ duyệt riêng trong tab Quy tắc.</span>
        </div>

        <div v-if="review.status === 1" class="warn-callout">
          <i class="pi pi-clock"></i>
          <span>Thao tác Xóa đang được đối soát. Bạn có thể thử lại sau một phút.</span>
        </div>

        <div v-if="review.status === 3" class="info-callout">
          <i class="pi pi-info-circle"></i>
          <span>Email đã được chọn Giữ. Hãy lưu lại lý do nếu lần trước bị gián đoạn.</span>
        </div>

        <!-- Preset Reason Suggestions -->
        <div class="presets-section">
          <span class="presets-label"><i class="pi pi-tags"></i> Gợi ý nhanh:</span>
          <div class="presets-chips">
            <button
              v-for="preset in presetReasons"
              :key="preset"
              type="button"
              class="preset-chip"
              @click="applyPreset(review.id, preset)"
            >
              + {{ preset }}
            </button>
          </div>
        </div>

        <!-- User Reason Textarea -->
        <div class="reason-input-group">
          <label :for="`reason-${review.id}`" class="reason-label">
            Lý do của bạn <span class="required-star">*</span>
          </label>
          <textarea
            :id="`reason-${review.id}`"
            v-model="reasons[review.id]"
            rows="2"
            class="reason-textarea"
            placeholder="Ví dụ: Tôi luôn kiểm tra lại bản triển khai trên production"
            maxlength="1000"
          ></textarea>
        </div>

        <!-- Actions Footer -->
        <div class="review-actions">
          <button
            v-if="review.status !== 3"
            type="button"
            class="btn-action btn-trash"
            :disabled="busyId === review.id || !reasons[review.id]?.trim()"
            @click="resolve(review, 0)"
          >
            <i class="pi" :class="busyId === review.id ? 'pi-spin pi-spinner' : 'pi-trash'"></i>
            <span>Chuyển vào Thùng rác</span>
          </button>

          <button
            v-if="review.status !== 1"
            type="button"
            class="btn-action btn-keep"
            :disabled="busyId === review.id || !reasons[review.id]?.trim()"
            @click="resolve(review, 1)"
          >
            <i class="pi" :class="busyId === review.id ? 'pi-spin pi-spinner' : 'pi-shield'"></i>
            <span>Giữ trong Inbox</span>
          </button>
        </div>
      </div>
    </div>

    <!-- Pagination Controls -->
    <div class="review-pagination" v-if="totalCount > pageSize">
      <button
        type="button"
        class="btn-page"
        :disabled="page <= 1 || loading"
        @click="goTo(page - 1)"
      >
        <i class="pi pi-chevron-left"></i> Trước
      </button>
      <span class="page-indicator">Trang {{ page }} / {{ Math.ceil(totalCount / pageSize) }}</span>
      <button
        type="button"
        class="btn-page"
        :disabled="page * pageSize >= totalCount || loading"
        @click="goTo(page + 1)"
      >
        Sau <i class="pi pi-chevron-right"></i>
      </button>
    </div>
  </section>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue';
import api from '@/services/api.service';
import { showToast } from '@/services/notification.service';

interface CleanupReview {
  id: string;
  emailId: string;
  sender: string;
  subject?: string;
  snippet?: string;
  aiReason?: string;
  proposedRuleId?: string;
  status: number;
  resolvedReason?: string;
}

const emit = defineEmits<{
  (e: 'count-change', count: number): void;
  (e: 'resolved', reviewId: string): void;
}>();

const presetReasons = [
  'Quảng cáo / Khuyến mãi',
  'Bản tin không đọc',
  'Thông báo tự động vô ích',
  'Báo cáo / Cảnh báo định kỳ'
];

const reviews = ref<CleanupReview[]>([]);
const reasons = ref<Record<string, string>>({});
const totalCount = ref(0);
const page = ref(1);
const pageSize = 20;
const loading = ref(false);
const busyId = ref('');
const error = ref('');

const applyPreset = (reviewId: string, text: string) => {
  const current = reasons.value[reviewId]?.trim() || '';
  if (!current) {
    reasons.value[reviewId] = text;
  } else if (!current.includes(text)) {
    reasons.value[reviewId] = `${current}, ${text}`;
  }
};

const getStatusBadgeClass = (status: number) => {
  switch (status) {
    case 1: return 'badge-processing';
    case 3: return 'badge-kept';
    default: return 'badge-pending';
  }
};

const getStatusText = (status: number) => {
  switch (status) {
    case 1: return 'Đang xử lý';
    case 3: return 'Đã chọn Giữ';
    default: return 'Chờ quyết định';
  }
};

async function load() {
  loading.value = true;
  error.value = '';
  try {
    const res: any = await api.get('/emailops/cleanup/reviews', { params: { page: page.value, pageSize } });
    reviews.value = res.data?.items || [];
    totalCount.value = res.data?.totalCount || 0;
    emit('count-change', totalCount.value);
    for (const review of reviews.value) {
      if (!reasons.value[review.id] && review.resolvedReason) {
        reasons.value[review.id] = review.resolvedReason;
      }
    }
  } catch (err: any) {
    error.value = err.message || 'Không thể tải email chờ duyệt.';
  } finally {
    loading.value = false;
  }
}

async function goTo(nextPage: number) {
  page.value = nextPage;
  await load();
}

async function resolve(review: CleanupReview, decision: 0 | 1) {
  if ((review.status === 3 && decision !== 1) || (review.status === 1 && decision !== 0)) return;
  const reason = reasons.value[review.id]?.trim();
  if (!reason) return;
  busyId.value = review.id;
  error.value = '';
  try {
    await api.post(`/emailops/cleanup/reviews/${review.id}/resolve`, { decision, reason });
    delete reasons.value[review.id];
    showToast({
      severity: 'success',
      summary: decision === 0 ? 'Đã chuyển vào Thùng rác' : 'Đã giữ email',
      detail: 'Đã lưu lý do để dùng cho các lần phân loại sau.'
    });
    emit('resolved', review.id);
    await load();
  } catch (err: any) {
    error.value = err.message || 'Không thể xử lý email này.';
  } finally {
    busyId.value = '';
  }
}

onMounted(load);
</script>

<style scoped lang="scss">
.cleanup-reviews-container {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

/* ============================================================
   TOP BAR & HEADER
   ============================================================ */
.review-top-bar {
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
      background: linear-gradient(135deg, rgba(245, 158, 11, 0.25), rgba(217, 119, 6, 0.2));
      border: 1px solid rgba(245, 158, 11, 0.35);
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 1.25rem;
      color: #fbbf24;
      box-shadow: 0 0 16px rgba(245, 158, 11, 0.2);
    }

    .title-text {
      display: flex;
      flex-direction: column;
      gap: 0.2rem;

      .title-row {
        display: flex;
        align-items: center;
        gap: 0.65rem;

        h3 {
          font-size: 1.35rem;
          font-weight: 700;
          color: #f8fafc;
          margin: 0;
        }

        .counter-badge {
          background: rgba(245, 158, 11, 0.2);
          border: 1px solid rgba(245, 158, 11, 0.4);
          color: #fbbf24;
          font-size: 0.75rem;
          font-weight: 700;
          padding: 0.15rem 0.55rem;
          border-radius: 9999px;
        }
      }

      .subtitle {
        color: #94a3b8;
        font-size: 0.825rem;
        margin: 0;
      }
    }
  }

  .btn-refresh {
    background: rgba(30, 41, 59, 0.8);
    border: 1px solid rgba(148, 163, 184, 0.2);
    color: #cbd5e1;
    padding: 0.55rem 1rem;
    border-radius: 0.5rem;
    font-size: 0.825rem;
    font-weight: 600;
    cursor: pointer;
    display: inline-flex;
    align-items: center;
    gap: 0.45rem;
    transition: all 0.2s ease;

    &:hover:not(:disabled) {
      background: rgba(51, 65, 85, 0.8);
      color: #ffffff;
      border-color: rgba(148, 163, 184, 0.4);
    }

    &:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }
  }
}

/* ============================================================
   ALERTS & STATES
   ============================================================ */
.review-error-banner {
  display: flex;
  align-items: center;
  gap: 0.65rem;
  padding: 0.85rem 1rem;
  background: rgba(239, 68, 68, 0.12);
  border: 1px solid rgba(239, 68, 68, 0.3);
  border-radius: 0.5rem;
  color: #fca5a5;
  font-size: 0.85rem;
}

.loading-state, .empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 3.5rem 1rem;
  background: rgba(15, 23, 42, 0.7);
  border: 1px solid rgba(148, 163, 184, 0.12);
  border-radius: 0.75rem;
  gap: 0.85rem;
  color: #94a3b8;
  font-size: 0.95rem;
}

/* ============================================================
   REVIEWS LIST & CARDS
   ============================================================ */
.reviews-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.review-card {
  background: rgba(15, 23, 42, 0.75);
  border: 1px solid rgba(148, 163, 184, 0.14);
  border-top: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 0.85rem;
  padding: 1.25rem;
  backdrop-filter: blur(14px);
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  transition: all 0.2s ease;

  &:hover {
    border-color: rgba(148, 163, 184, 0.28);
    box-shadow: 0 4px 20px rgba(0, 0, 0, 0.3);
  }

  .card-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 0.5rem;
    flex-wrap: wrap;

    .sender-info {
      display: flex;
      align-items: center;
      gap: 0.45rem;
      font-size: 0.85rem;
      color: #38bdf8;
      font-weight: 600;

      .sender-icon {
        font-size: 0.9rem;
      }
    }

    .status-badge {
      font-size: 0.72rem;
      font-weight: 700;
      padding: 0.2rem 0.55rem;
      border-radius: 9999px;

      &.badge-pending {
        background: rgba(245, 158, 11, 0.15);
        border: 1px solid rgba(245, 158, 11, 0.35);
        color: #fbbf24;
      }

      &.badge-processing {
        background: rgba(59, 130, 246, 0.15);
        border: 1px solid rgba(59, 130, 246, 0.35);
        color: #93c5fd;
      }

      &.badge-kept {
        background: rgba(16, 185, 129, 0.15);
        border: 1px solid rgba(16, 185, 129, 0.35);
        color: #6ee7b7;
      }
    }
  }

  .card-subject {
    font-size: 1rem;
    font-weight: 700;
    color: #f8fafc;
    line-height: 1.4;
  }

  .card-snippet {
    font-size: 0.825rem;
    color: #94a3b8;
    line-height: 1.45;
    font-style: italic;
    background: rgba(2, 6, 23, 0.4);
    border-left: 2px solid rgba(148, 163, 184, 0.3);
    padding: 0.45rem 0.75rem;
    border-radius: 0 0.35rem 0.35rem 0;
  }
}

/* Callouts */
.ai-callout {
  display: flex;
  align-items: flex-start;
  gap: 0.6rem;
  background: rgba(139, 92, 246, 0.12);
  border: 1px solid rgba(139, 92, 246, 0.28);
  border-radius: 0.5rem;
  padding: 0.65rem 0.85rem;
  font-size: 0.825rem;
  color: #d8b4fe;

  .ai-icon {
    color: #c084fc;
    margin-top: 0.15rem;
  }
}

.proposal-callout, .warn-callout, .info-callout {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  border-radius: 0.5rem;
  padding: 0.55rem 0.85rem;
  font-size: 0.8rem;

  code {
    background: rgba(0, 0, 0, 0.3);
    padding: 0.1rem 0.35rem;
    border-radius: 4px;
    font-family: monospace;
    color: #38bdf8;
  }
}

.proposal-callout {
  background: rgba(6, 182, 212, 0.1);
  border: 1px solid rgba(6, 182, 212, 0.25);
  color: #67e8f9;
}

.warn-callout {
  background: rgba(245, 158, 11, 0.1);
  border: 1px solid rgba(245, 158, 11, 0.25);
  color: #fcd34d;
}

.info-callout {
  background: rgba(59, 130, 246, 0.1);
  border: 1px solid rgba(59, 130, 246, 0.25);
  color: #93c5fd;
}

/* Presets */
.presets-section {
  display: flex;
  align-items: center;
  gap: 0.55rem;
  flex-wrap: wrap;

  .presets-label {
    font-size: 0.775rem;
    color: #94a3b8;
    font-weight: 600;
    display: inline-flex;
    align-items: center;
    gap: 0.3rem;
  }

  .presets-chips {
    display: flex;
    gap: 0.4rem;
    flex-wrap: wrap;

    .preset-chip {
      background: rgba(30, 41, 59, 0.7);
      border: 1px solid rgba(148, 163, 184, 0.2);
      color: #94a3b8;
      font-size: 0.75rem;
      padding: 0.25rem 0.65rem;
      border-radius: 9999px;
      cursor: pointer;
      transition: all 0.15s ease;

      &:hover {
        background: rgba(139, 92, 246, 0.15);
        border-color: rgba(139, 92, 246, 0.4);
        color: #e2e8f0;
      }
    }
  }
}

/* Reason Input */
.reason-input-group {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;

  .reason-label {
    font-size: 0.8rem;
    font-weight: 600;
    color: #cbd5e1;

    .required-star {
      color: #f43f5e;
    }
  }

  .reason-textarea {
    width: 100%;
    box-sizing: border-box;
    background: rgba(15, 23, 42, 0.8);
    border: 1px solid rgba(148, 163, 184, 0.2);
    border-radius: 0.5rem;
    padding: 0.65rem 0.85rem;
    color: #f8fafc;
    font-size: 0.85rem;
    font-family: inherit;
    resize: vertical;
    transition: all 0.2s ease;

    &:focus {
      outline: none;
      border-color: rgba(139, 92, 246, 0.5);
      box-shadow: 0 0 12px rgba(139, 92, 246, 0.2);
    }

    &::placeholder {
      color: #64748b;
    }
  }
}

/* Actions Footer */
.review-actions {
  display: flex;
  justify-content: flex-end;
  gap: 0.75rem;
  flex-wrap: wrap;
  padding-top: 0.5rem;
  border-top: 1px solid rgba(148, 163, 184, 0.08);

  .btn-action {
    display: inline-flex;
    align-items: center;
    gap: 0.45rem;
    padding: 0.55rem 1.15rem;
    border-radius: 0.5rem;
    font-size: 0.825rem;
    font-weight: 600;
    cursor: pointer;
    transition: all 0.15s ease;

    &:disabled {
      opacity: 0.45;
      cursor: not-allowed;
    }

    &.btn-trash {
      background: linear-gradient(135deg, #e11d48 0%, #be123c 100%);
      border: 1px solid rgba(225, 29, 72, 0.4);
      color: #ffffff;
      box-shadow: 0 2px 8px rgba(225, 29, 72, 0.25);

      &:hover:not(:disabled) {
        filter: brightness(1.1);
        transform: translate3d(0, -1px, 0);
        box-shadow: 0 4px 14px rgba(225, 29, 72, 0.4);
      }
    }

    &.btn-keep {
      background: linear-gradient(135deg, #059669 0%, #047857 100%);
      border: 1px solid rgba(5, 150, 105, 0.4);
      color: #ffffff;
      box-shadow: 0 2px 8px rgba(5, 150, 105, 0.25);

      &:hover:not(:disabled) {
        filter: brightness(1.1);
        transform: translate3d(0, -1px, 0);
        box-shadow: 0 4px 14px rgba(5, 150, 105, 0.4);
      }
    }
  }
}

/* Pagination */
.review-pagination {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 1rem;
  padding: 1rem 0;

  .btn-page {
    background: rgba(30, 41, 59, 0.8);
    border: 1px solid rgba(148, 163, 184, 0.2);
    color: #cbd5e1;
    padding: 0.45rem 0.85rem;
    border-radius: 0.45rem;
    font-size: 0.8rem;
    font-weight: 600;
    cursor: pointer;
    display: inline-flex;
    align-items: center;
    gap: 0.35rem;

    &:hover:not(:disabled) {
      background: rgba(51, 65, 85, 0.8);
      color: #ffffff;
    }

    &:disabled {
      opacity: 0.4;
      cursor: not-allowed;
    }
  }

  .page-indicator {
    font-size: 0.825rem;
    color: #94a3b8;
  }
}
</style>
