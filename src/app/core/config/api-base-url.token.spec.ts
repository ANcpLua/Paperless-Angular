import { API_BASE_URL } from './api-base-url.token';

describe('API_BASE_URL', () => {
  it('is a named injection token', () => {
    expect(API_BASE_URL.toString()).toContain('API_BASE_URL');
  });
});
