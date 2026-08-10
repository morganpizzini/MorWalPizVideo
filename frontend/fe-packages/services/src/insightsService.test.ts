import { describe, expect, it } from 'vitest';
import { requireSuccessfulResponse } from './insightsService';

describe('requireSuccessfulResponse', () => {
  it('accepts a completed response with an empty errors array', () => {
    const response = { status: 2, errors: [], createdNewsItemCount: 1 };
    expect(requireSuccessfulResponse(response)).toBe(response);
  });

  it('rejects a response containing errors', () => {
    expect(() => requireSuccessfulResponse({ errors: ['analysis failed'] })).toThrow('analysis failed');
  });
});