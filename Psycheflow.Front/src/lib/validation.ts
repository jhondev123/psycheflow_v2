/** Form validation + Brazilian masks (CPF / phone). */

export function isValidEmail(value: string): boolean {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value.trim());
}

/** Validates a Brazilian CPF including its two check digits. */
export function isValidCPF(value: string): boolean {
  const cpf = value.replace(/\D/g, "");
  if (cpf.length !== 11) return false;
  if (/^(\d)\1{10}$/.test(cpf)) return false;

  const digits = cpf.split("").map(Number);
  let sum = 0;
  for (let i = 0; i < 9; i++) sum += digits[i] * (10 - i);
  let check = sum % 11;
  check = check < 2 ? 0 : 11 - check;
  if (digits[9] !== check) return false;

  sum = 0;
  for (let i = 0; i < 10; i++) sum += digits[i] * (11 - i);
  check = sum % 11;
  check = check < 2 ? 0 : 11 - check;
  return digits[10] === check;
}

export function isValidPhone(value: string): boolean {
  return /^\(?\d{2}\)?\s?\d{4,5}-?\d{4}$/.test(value.trim());
}

export function maskCPF(value: string): string {
  const d = value.replace(/\D/g, "").slice(0, 11);
  return d
    .replace(/(\d{3})(\d)/, "$1.$2")
    .replace(/(\d{3})(\d)/, "$1.$2")
    .replace(/(\d{3})(\d{1,2})$/, "$1-$2");
}

export function maskPhone(value: string): string {
  const d = value.replace(/\D/g, "").slice(0, 11);
  if (d.length <= 10) {
    return d.replace(/(\d{2})(\d)/, "($1) $2").replace(/(\d{4})(\d)/, "$1-$2");
  }
  return d.replace(/(\d{2})(\d)/, "($1) $2").replace(/(\d{5})(\d)/, "$1-$2");
}

/** Password rule matching the API Identity defaults (min 6, upper, lower, digit, symbol). */
export function passwordIssues(pw: string): string[] {
  const issues: string[] = [];
  if (pw.length < 6) issues.push("ao menos 6 caracteres");
  if (!/[A-Z]/.test(pw)) issues.push("uma letra maiúscula");
  if (!/[a-z]/.test(pw)) issues.push("uma letra minúscula");
  if (!/\d/.test(pw)) issues.push("um número");
  if (!/[^A-Za-z0-9]/.test(pw)) issues.push("um símbolo");
  return issues;
}
