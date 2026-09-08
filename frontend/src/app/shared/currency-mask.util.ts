export function maskCurrencyDigits(raw: string): string {
  let digits = (raw ?? '').replace(/\D/g, '');
  if (!digits) return '';

  digits = digits.replace(/^0+/, '') || '0';
  while (digits.length < 3) digits = '0' + digits;

  const centsPart = digits.slice(-2);
  let intPart = digits.slice(0, -2);
  intPart = intPart.replace(/^0+(?=\d)/, '');
  intPart = intPart.replace(/\B(?=(\d{3})+(?!\d))/g, '.');

  return `${intPart},${centsPart}`;
}
