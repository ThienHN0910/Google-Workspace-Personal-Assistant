import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import { formatDate, formatDateTime, formatCurrency, formatTokens, formatBytes } from '../formatters.ts';

describe('formatters', () => {
  it('formatDate returns dash for empty date', () => {
    assert.equal(formatDate(null), '—');
    assert.equal(formatDate(undefined), '—');
    assert.equal(formatDateTime(null), '—');
  });

  it('formatCurrency formats zero and positive numbers in VND', () => {
    assert.equal(formatCurrency(0), '0\u00A0₫');
    assert.match(formatCurrency(100000), /100[.,\s]?000/);
  });

  it('formatTokens formats numbers with locale grouping', () => {
    assert.equal(formatTokens(0), '0');
    assert.equal(formatTokens(null), '0');
    assert.match(formatTokens(250000), /250[.,\s]?000/);
  });

  it('formatBytes formats file sizes correctly', () => {
    assert.equal(formatBytes(0), '0 B');
    assert.equal(formatBytes(null), '0 B');
    assert.equal(formatBytes(1024), '1 KB');
    assert.equal(formatBytes(1048576), '1 MB');
  });
});
