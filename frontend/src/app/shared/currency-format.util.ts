export function formatEUR(value: number): string {
  const safeValue = value == null || Number.isNaN(value) ? 0 : value;
  const parts = Math.abs(safeValue).toFixed(2).split('.');
  const intPart = parts[0].replace(/\B(?=(\d{3})+(?!\d))/g, '.');

  return `${safeValue < 0 ? '-' : ''}${intPart},${parts[1]} €`;
}
