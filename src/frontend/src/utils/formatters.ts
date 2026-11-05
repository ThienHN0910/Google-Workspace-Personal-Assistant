const VN_LOCALE = 'vi-VN';

export const formatDate = (date: string | Date | null | undefined): string => {
  if (!date) return '—';
  const d = new Date(date);
  return isNaN(d.getTime()) ? '—' : d.toLocaleDateString(VN_LOCALE);
};

export const formatDateTime = (date: string | Date | null | undefined): string => {
  if (!date) return '—';
  const d = new Date(date);
  return isNaN(d.getTime()) ? '—' : d.toLocaleString(VN_LOCALE);
};

export const formatCurrency = (amount: number | null | undefined): string => {
  if (amount == null || isNaN(amount)) return '0 ₫';
  return new Intl.NumberFormat(VN_LOCALE, { style: 'currency', currency: 'VND' }).format(amount);
};

export const formatTokens = (n: number | null | undefined): string => {
  if (n == null || isNaN(n)) return '0';
  return new Intl.NumberFormat(VN_LOCALE).format(n);
};

export const formatBytes = (bytes: number | null | undefined, decimals = 2): string => {
  if (!bytes || bytes === 0) return '0 B';
  const k = 1024;
  const dm = decimals < 0 ? 0 : decimals;
  const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return `${parseFloat((bytes / Math.pow(k, i)).toFixed(dm))} ${sizes[i]}`;
};
