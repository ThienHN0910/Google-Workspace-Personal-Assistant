<template>
  <div ref="observerRef" class="infinite-scroll-observer">
    <div v-if="loading" class="loading-state">
      <i class="pi pi-spin pi-spinner"></i> Đang tải thêm dữ liệu...
    </div>
    <div v-else-if="!hasMore" class="end-state">
      — Hết dữ liệu —
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, watch } from 'vue';

const props = defineProps({
  loading: {
    type: Boolean,
    default: false
  },
  hasMore: {
    type: Boolean,
    default: true
  }
});

const emit = defineEmits(['loadMore']);
const observerRef = ref<HTMLElement | null>(null);
let observer: IntersectionObserver | null = null;
let isEmitting = false;

const checkAndEmit = (isIntersecting: boolean) => {
  if (isIntersecting && !props.loading && props.hasMore && !isEmitting) {
    isEmitting = true;
    emit('loadMore');
    setTimeout(() => {
      isEmitting = false;
    }, 400);
  }
};

onMounted(() => {
  observer = new IntersectionObserver(
    (entries) => {
      const target = entries[0];
      checkAndEmit(target.isIntersecting);
    },
    {
      root: null,
      rootMargin: '150px',
      threshold: 0
    }
  );

  if (observerRef.value) {
    observer.observe(observerRef.value);
  }
});

onUnmounted(() => {
  if (observer && observerRef.value) {
    observer.unobserve(observerRef.value);
  }
});

watch(() => props.loading, (newVal) => {
  if (!newVal) {
    isEmitting = false;
  }
});
</script>

<style scoped>
.infinite-scroll-observer {
  padding: 1.25rem 1rem;
  text-align: center;
  color: #94a3b8;
  font-size: 0.85rem;
  min-height: 50px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.loading-state {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  color: #818cf8;
  font-weight: 500;
}

.end-state {
  opacity: 0.5;
  font-style: italic;
}
</style>
