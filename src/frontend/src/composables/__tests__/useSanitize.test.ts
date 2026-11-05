import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import { sanitize } from '../useSanitize.ts';

describe('useSanitize', () => {
  it('returns empty string for null or undefined input', () => {
    assert.equal(sanitize(null), '');
    assert.equal(sanitize(undefined), '');
    assert.equal(sanitize(''), '');
  });

  it('strips malicious script tags from html', () => {
    const malicious = '<div>Hello<script>alert("xss")</script> World</div>';
    const cleaned = sanitize(malicious);
    assert.equal(cleaned.includes('<script>'), false);
    assert.equal(cleaned.includes('alert'), false);
    assert.equal(cleaned.includes('Hello'), true);
  });

  it('strips onerror and onclick event handlers', () => {
    const malicious = '<img src="invalid.jpg" onerror="alert(1)" onclick="steal()" />';
    const cleaned = sanitize(malicious);
    assert.equal(cleaned.includes('onerror'), false);
    assert.equal(cleaned.includes('onclick'), false);
    assert.equal(cleaned.includes('<img'), true);
  });
});
