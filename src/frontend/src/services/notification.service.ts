import * as signalR from '@microsoft/signalr';

export interface ToastPayload {
  severity: 'success' | 'info' | 'warn' | 'error';
  summary: string;
  detail: string;
  life?: number;
}

type ToastTrigger = (toast: ToastPayload) => void;

let toastTrigger: ToastTrigger | null = null;
let connection: signalR.HubConnection | null = null;

export const registerToastTrigger = (trigger: ToastTrigger) => {
  toastTrigger = trigger;
};

export const showToast = (toast: ToastPayload) => {
  if (toastTrigger) {
    toastTrigger(toast);
  }
};

export const initSignalR = async () => {
  if (connection) return;

  connection = new signalR.HubConnectionBuilder()
    .withUrl('/hubs/notifications', {
      accessTokenFactory: () => localStorage.getItem('gopshub_token') || '',
    })
    .withAutomaticReconnect()
    .build();

  connection.on('ReceiveNotification', (data: { title: string; message: string; type: string }) => {
    const severity =
      data.type === 'critical' ? 'error' :
      data.type === 'warning' ? 'warn' :
      data.type === 'success' ? 'success' : 'info';

    showToast({
      severity,
      summary: data.title,
      detail: data.message,
      life: data.type === 'critical' ? undefined : 4000,
    });
  });

  try {
    await connection.start();
    console.log('[SignalR] Connected to NotificationHub');
  } catch (err) {
    console.warn('[SignalR] Initial connection failed (will retry automatically):', err);
  }
};
