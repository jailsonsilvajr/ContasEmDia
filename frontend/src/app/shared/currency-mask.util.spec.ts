import { describe, it, expect } from 'vitest';

import { maskCurrencyDigits } from './currency-mask.util';

describe('maskCurrencyDigits', () => {
  it('returns an empty string for empty input', () => {
    expect(maskCurrencyDigits('')).toBe('');
  });

  it('returns an empty string when there are no digits at all', () => {
    expect(maskCurrencyDigits('abc')).toBe('');
  });

  it('ignores non-digit characters typed alongside digits', () => {
    expect(maskCurrencyDigits('R$12,50abc')).toBe('12,50');
  });

  it('treats the last two digits as cents', () => {
    expect(maskCurrencyDigits('1')).toBe('0,01');
    expect(maskCurrencyDigits('150')).toBe('1,50');
  });

  it('groups thousands with a dot and uses a comma as the decimal separator', () => {
    expect(maskCurrencyDigits('150050')).toBe('1.500,50');
  });

  it('groups multiple thousands correctly', () => {
    expect(maskCurrencyDigits('150000000')).toBe('1.500.000,00');
  });

  it('strips leading zeros from the digits typed', () => {
    expect(maskCurrencyDigits('000150')).toBe('1,50');
  });
});
