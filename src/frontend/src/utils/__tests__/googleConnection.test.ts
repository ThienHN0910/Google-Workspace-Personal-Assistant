import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import { isGoogleDisconnectedError, googleDisconnectedMessage, DEFAULT_GOOGLE_DISCONNECTED_MESSAGE } from '../googleConnection.ts';

describe('googleConnection', () => {
  it('treats HTTP 409 as Google disconnected', () => {
    assert.equal(isGoogleDisconnectedError({ status: 409, message: 'x' }), true);
    assert.equal(isGoogleDisconnectedError({ response: { status: 409 } }), true);
  });

  it('recognises the Tasks 400 "Vui lòng kết nối Google" message', () => {
    assert.equal(isGoogleDisconnectedError({ status: 400, message: 'Không tìm thấy Task List. Vui lòng kết nối Google.' }), true);
  });

  it('does not treat unrelated 400 validation errors as disconnected', () => {
    assert.equal(isGoogleDisconnectedError({ status: 400, message: 'Tiêu đề danh sách không được để trống.' }), false);
    assert.equal(isGoogleDisconnectedError({ status: 400, message: 'Google Calendar event title is required' }), false);
    assert.equal(isGoogleDisconnectedError(null), false);
  });

  it('falls back to a default message', () => {
    assert.equal(googleDisconnectedMessage({}), DEFAULT_GOOGLE_DISCONNECTED_MESSAGE);
    assert.equal(googleDisconnectedMessage({ message: 'Hết hạn' }), 'Hết hạn');
  });
});
