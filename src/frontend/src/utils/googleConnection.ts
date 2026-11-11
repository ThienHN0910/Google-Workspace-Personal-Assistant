/**
 * Detects API errors meaning "the owner's Google account is not connected / token expired".
 * The backend answers 409 (GoogleAccountNotConnectedException); TasksController still answers
 * 400 "Không tìm thấy Task List. Vui lòng kết nối Google." when no list can be loaded.
 */
export function isGoogleDisconnectedError(error: unknown): boolean {
  if (!error || typeof error !== 'object') return false;
  const e = error as { status?: number; message?: string; response?: { status?: number; data?: { message?: string } } };

  const status = e.status ?? e.response?.status;
  if (status === 409) return true;

  const message = (e.message || e.response?.data?.message || '').toLowerCase();
  return message.includes('tài khoản google chưa được kết nối') || message.includes('vui lòng kết nối google');
}

export const DEFAULT_GOOGLE_DISCONNECTED_MESSAGE =
  'Tài khoản Google chưa được kết nối hoặc token đã hết hạn. Vui lòng đăng nhập lại.';

/** Message to show in GoogleReconnectNotice for an error accepted by isGoogleDisconnectedError. */
export function googleDisconnectedMessage(error: unknown): string {
  const e = error as { message?: string; response?: { data?: { message?: string } } } | null;
  return e?.message || e?.response?.data?.message || DEFAULT_GOOGLE_DISCONNECTED_MESSAGE;
}
