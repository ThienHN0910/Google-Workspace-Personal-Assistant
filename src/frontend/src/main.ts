import { createApp } from 'vue';
import { createPinia } from 'pinia';
import PrimeVue from 'primevue/config';
import Aura from '@primevue/themes/aura';

import App from './App.vue';
import router from './router';

import 'primeicons/primeicons.css';
import 'quill/dist/quill.snow.css'; // Required for PrimeVue Editor
import '@/assets/styles/main.scss';

import ToastService from 'primevue/toastservice';

const app = createApp(App);

app.use(createPinia());
app.use(router);
app.use(PrimeVue, {
  theme: {
    preset: Aura,
    options: {
      darkModeSelector: 'system',
    },
  },
});
app.use(ToastService);

app.mount('#app');
