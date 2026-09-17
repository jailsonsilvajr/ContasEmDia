import { describe, it, expect } from 'vitest';

import {
  parseValor,
  parseDia,
  getNomeError,
  getValorError,
  getDiaError,
  getDataInicioError,
  addYearsIso,
  getDataFimError,
} from './recurring-expense-form.util';

describe('parseValor', () => {
  it('returns null for an empty string', () => {
    expect(parseValor('')).toBeNull();
  });

  it('parses a comma-separated value with thousands dots', () => {
    expect(parseValor('1.500,00')).toBe(1500);
  });

  it('parses a plain integer string', () => {
    expect(parseValor('42')).toBe(42);
  });

  it('returns null for a non-numeric string', () => {
    expect(parseValor('abc')).toBeNull();
  });
});

describe('parseDia', () => {
  it('returns null for an empty string', () => {
    expect(parseDia('')).toBeNull();
  });

  it('parses a numeric string', () => {
    expect(parseDia('15')).toBe(15);
  });

  it('returns null for a non-numeric string', () => {
    expect(parseDia('abc')).toBeNull();
  });
});

describe('getNomeError', () => {
  it('requires a non-blank name', () => {
    expect(getNomeError('')).toBe('Nome é obrigatório.');
    expect(getNomeError('   ')).toBe('Nome é obrigatório.');
  });

  it('rejects a name longer than 100 characters', () => {
    expect(getNomeError('a'.repeat(101))).toBe('Nome deve ter no máximo 100 caracteres.');
  });

  it('accepts a valid name', () => {
    expect(getNomeError('Aluguel')).toBeNull();
  });
});

describe('getValorError', () => {
  it('requires a valid numeric value', () => {
    expect(getValorError('')).toBe('Valor previsto mensal é obrigatório e deve ser um número válido.');
    expect(getValorError('abc')).toBe('Valor previsto mensal é obrigatório e deve ser um número válido.');
  });

  it('rejects a value of zero or less', () => {
    expect(getValorError('0')).toBe('Valor previsto mensal deve ser maior que zero.');
  });

  it('rejects more than two decimal places', () => {
    expect(getValorError('10,123')).toBe('Valor previsto mensal deve ter no máximo duas casas decimais.');
  });

  it('accepts a valid amount', () => {
    expect(getValorError('1.500,00')).toBeNull();
  });
});

describe('getDiaError', () => {
  it('rejects a day outside 1-31', () => {
    expect(getDiaError('0')).toBe('Dia de vencimento deve ser um número entre 1 e 31.');
    expect(getDiaError('32')).toBe('Dia de vencimento deve ser um número entre 1 e 31.');
    expect(getDiaError('')).toBe('Dia de vencimento deve ser um número entre 1 e 31.');
  });

  it('accepts a day within 1-31', () => {
    expect(getDiaError('10')).toBeNull();
  });
});

describe('getDataInicioError', () => {
  it('requires a non-blank start date', () => {
    expect(getDataInicioError('')).toBe('Data de início é obrigatória.');
  });

  it('accepts a filled start date', () => {
    expect(getDataInicioError('2026-09-10')).toBeNull();
  });
});

describe('addYearsIso', () => {
  it('adds the given number of years to an ISO date', () => {
    expect(addYearsIso('2026-09-01', 1)).toBe('2027-09-01');
  });

  it('rolls Feb 29 on a leap year forward to Mar 1 on a non-leap year', () => {
    expect(addYearsIso('2028-02-29', 1)).toBe('2029-03-01');
  });
});

describe('getDataFimError', () => {
  it('requires a non-blank end date', () => {
    expect(getDataFimError('2026-09-01', '')).toBe('Data de fim é obrigatória.');
  });

  it('rejects an end date on or before the start date', () => {
    expect(getDataFimError('2026-09-10', '2026-09-10')).toBe(
      'A data de fim deve ser posterior à data de início.',
    );
    expect(getDataFimError('2026-09-10', '2026-09-05')).toBe(
      'A data de fim deve ser posterior à data de início.',
    );
  });

  it('rejects an end date beyond 1 year from the start date', () => {
    expect(getDataFimError('2026-09-01', '2027-09-02')).toBe(
      'A vigência não pode ultrapassar 1 ano a partir da data de início.',
    );
  });

  it('accepts a valid end date within the 1-year vigência', () => {
    expect(getDataFimError('2026-09-01', '2026-12-01')).toBeNull();
    expect(getDataFimError('2026-09-01', '2027-09-01')).toBeNull();
  });
});
