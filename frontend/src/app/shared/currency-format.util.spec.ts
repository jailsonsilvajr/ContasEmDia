import { describe, it, expect } from 'vitest';

import { formatEUR } from './currency-format.util';

describe('formatEUR', () => {
  it('formats zero as "0,00 €"', () => {
    expect(formatEUR(0)).toBe('0,00 €');
  });

  it('uses a comma as the decimal separator and a trailing "€" sign', () => {
    expect(formatEUR(12.5)).toBe('12,50 €');
  });

  it('groups thousands with a dot', () => {
    expect(formatEUR(1234.5)).toBe('1.234,50 €');
  });

  it('groups multiple thousands correctly', () => {
    expect(formatEUR(1500000)).toBe('1.500.000,00 €');
  });

  it('prefixes negative values with a minus sign before the digits', () => {
    expect(formatEUR(-100)).toBe('-100,00 €');
  });

  it('treats null/NaN as zero', () => {
    expect(formatEUR(Number.NaN)).toBe('0,00 €');
  });
});
