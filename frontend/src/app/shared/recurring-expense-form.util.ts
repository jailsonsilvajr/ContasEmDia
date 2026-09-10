export function parseValor(raw: string): number | null {
  const trimmed = raw.trim();
  if (!trimmed) return null;
  const normalized = trimmed.includes(',') ? trimmed.replace(/\./g, '').replace(',', '.') : trimmed;
  if (!/^\d+(\.\d+)?$/.test(normalized)) return null;
  const num = Number(normalized);
  return Number.isFinite(num) ? num : null;
}

export function parseDia(raw: string): number | null {
  const trimmed = raw.trim();
  if (!/^\d+$/.test(trimmed)) return null;
  return Number(trimmed);
}

export function getNomeError(nome: string): string | null {
  const trimmed = nome.trim();
  if (!trimmed) return 'Nome é obrigatório.';
  if (trimmed.length > 100) return 'Nome deve ter no máximo 100 caracteres.';
  return null;
}

export function getValorError(valor: string): string | null {
  const raw = valor.trim();
  const decimals = raw.includes(',') ? raw.split(',')[1] : raw.split('.')[1];
  const num = parseValor(raw);
  if (num === null) return 'Valor previsto mensal é obrigatório e deve ser um número válido.';
  if (num <= 0) return 'Valor previsto mensal deve ser maior que zero.';
  if (decimals && decimals.length > 2) return 'Valor previsto mensal deve ter no máximo duas casas decimais.';
  return null;
}

export function getDiaError(dia: string): string | null {
  const parsed = parseDia(dia);
  if (parsed === null || parsed < 1 || parsed > 31) return 'Dia de vencimento deve ser um número entre 1 e 31.';
  return null;
}

export function getDataInicioError(dataInicio: string): string | null {
  if (!dataInicio.trim()) return 'Data de início é obrigatória.';
  return null;
}
