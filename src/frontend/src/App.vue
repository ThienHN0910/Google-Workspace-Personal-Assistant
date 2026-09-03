<template>
  <Toast position="top-right" />
  <router-view />
</template>

<script setup lang="ts">
import { onMounted } from 'vue';
import Toast from 'primevue/toast';
import { useToast } from 'primevue/usetoast';
import { initSignalR, registerToastTrigger } from '@/services/notification.service';

const toast = useToast();

onMounted(() => {
  registerToastTrigger((payload) => {
    toast.add({
      severity: payload.severity,
      summary: payload.summary,
      detail: payload.detail,
      life: payload.life,
    });
  });

  initSignalR();
});
</script>
