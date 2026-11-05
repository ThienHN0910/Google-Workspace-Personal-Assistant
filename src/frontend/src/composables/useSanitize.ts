import DOMPurify from 'dompurify';

export function sanitize(html: string | null | undefined): string {
  if (!html) return '';

  if (typeof DOMPurify.sanitize === 'function') {
    return DOMPurify.sanitize(html, {
      USE_PROFILES: { html: true }
    });
  }

  if (typeof window !== 'undefined' && typeof (DOMPurify as any) === 'function') {
    return (DOMPurify as any)(window).sanitize(html, {
      USE_PROFILES: { html: true }
    });
  }

  // Fallback for non-browser / headless test environments without DOM
  return html
    .replace(/<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>/gi, '')
    .replace(/\s+on\w+\s*=\s*(["'])?[^"'>]*\1/gi, '');
}

export function useSanitize() {
  return { sanitize };
}
