<template>
  <section class="cleanup-reviews" aria-labelledby="cleanup-review-title">
    <div class="review-heading">
      <div>
        <h3 id="cleanup-review-title">Chờ duyệt dọn dẹp <span>{{ totalCount }}</span></h3>
        <p>Nhập lý do để lưu cùng email. Lý do này sẽ được gửi kèm những yêu cầu AI phù hợp về sau.</p>
      </div>
      <button type="button" @click="load" :disabled="loading">Làm mới</button>
    </div>
    <p v-if="error" role="alert" class="review-error">{{ error }}</p>
    <p v-if="loading && !reviews.length">Đang tải email chờ duyệt...</p>
    <p v-else-if="!reviews.length">Không có email chờ duyệt.</p>
    <div v-for="review in reviews" :key="review.id" class="review-card">
      <div class="review-sender">{{ review.sender }}</div>
      <strong>{{ review.subject || '(Không có tiêu đề)' }}</strong>
      <p v-if="review.snippet" class="review-snippet">{{ review.snippet }}</p>
      <p v-if="review.aiReason" class="review-ai">AI gợi ý: {{ review.aiReason }}</p>
      <p v-if="review.proposedRuleId" class="review-ai">Có bản nháp regex {{ review.proposedRuleId }} đang tắt, chờ duyệt riêng.</p>
      <label :for="`reason-${review.id}`">Lý do của bạn</label>
      <textarea :id="`reason-${review.id}`" v-model="reasons[review.id]" rows="2"
        placeholder="Ví dụ: Tôi luôn kiểm tra lại bản triển khai trên production" maxlength="1000" />
      <div class="review-actions">
        <button type="button" class="trash" :disabled="busyId === review.id || !reasons[review.id]?.trim()"
          @click="resolve(review, 0)">Chuyển vào Thùng rác</button>
        <button type="button" :disabled="busyId === review.id || !reasons[review.id]?.trim()"
          @click="resolve(review, 1)">Giữ trong Inbox</button>
      </div>
    </div>
    <div class="review-pages" v-if="totalCount > pageSize">
      <button type="button" :disabled="page <= 1 || loading" @click="goTo(page - 1)">Trước</button>
      <span>Trang {{ page }} / {{ Math.ceil(totalCount / pageSize) }}</span>
      <button type="button" :disabled="page * pageSize >= totalCount || loading" @click="goTo(page + 1)">Sau</button>
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
}

const reviews = ref<CleanupReview[]>([]);
const reasons = ref<Record<string, string>>({});
const totalCount = ref(0);
const page = ref(1);
const pageSize = 20;
const loading = ref(false);
const busyId = ref('');
const error = ref('');

async function load() {
  loading.value = true;
  error.value = '';
  try {
    const res: any = await api.get('/emailops/cleanup/reviews', { params: { page: page.value, pageSize } });
    reviews.value = res.data?.items || [];
    totalCount.value = res.data?.totalCount || 0;
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
  const reason = reasons.value[review.id]?.trim();
  if (!reason) return;
  busyId.value = review.id;
  error.value = '';
  try {
    await api.post(`/emailops/cleanup/reviews/${review.id}/resolve`, { decision, reason });
    delete reasons.value[review.id];
    showToast({ severity: 'success', summary: decision === 0 ? 'Đã chuyển vào Thùng rác' : 'Đã giữ email',
      detail: 'Đã lưu lý do để dùng cho các lần phân loại sau.' });
    await load();
  } catch (err: any) {
    error.value = err.message || 'Không thể xử lý email này.';
  } finally {
    busyId.value = '';
  }
}

onMounted(load);
</script>

<style scoped>
.cleanup-reviews { margin-bottom: 1.5rem; color: #e2e8f0; }
.review-heading, .review-actions, .review-pages { display: flex; align-items: center; justify-content: space-between; gap: .75rem; flex-wrap: wrap; }
.review-heading h3 { margin: 0; }
.review-heading h3 span { color: #fbbf24; }
.review-heading p, .review-ai, .review-snippet { color: #94a3b8; font-size: .85rem; }
.review-card { padding: 1rem; margin: .75rem 0; border: 1px solid #475569; border-radius: .65rem; background: #1e293b; display: grid; gap: .55rem; }
.review-sender { color: #38bdf8; font-size: .8rem; }
.review-card p { margin: 0; }
.review-card label { font-size: .8rem; font-weight: 700; }
.review-card textarea { width: 100%; box-sizing: border-box; background: #0f172a; color: white; border: 1px solid #64748b; border-radius: .4rem; padding: .6rem; }
button { color: white; background: #334155; border: 1px solid #64748b; border-radius: .4rem; padding: .5rem .75rem; cursor: pointer; }
button.trash { background: #9f1239; border-color: #be123c; }
button:disabled { opacity: .45; cursor: not-allowed; }
.review-error { color: #fda4af; }
</style>
