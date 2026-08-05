<template>
  <div ref="observerRef" class="infinite-scroll-observer">
    <div v-if="loading" class="loading-state">
      <i class="pi pi-spin pi-spinner"></i> Đang tải thêm...
    </div>
    <div v-else-if="!hasMore" class="end-state">
      Hết dữ liệu
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue';

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

onMounted(() => {
  observer = new IntersectionObserver(
    (entries) => {
      const target = entries[0];
      if (target.isIntersecting && !props.loading && props.hasMore) {
        emit('loadMore');
      }
    },
    {
      root: null,
      rootMargin: '100px', // Load before it comes into view
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
</script>

<style scoped>
.infinite-scroll-observer {
  padding: 1rem;
  text-align: center;
  color: #94a3b8;
  font-size: 0.85rem;
}

.loading-state {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
}

.end-state {
  opacity: 0.5;
}
</style>
