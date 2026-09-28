/** Validações de formulário e máscaras brasileiras (espelham as regras da API). */

export function isValidEmail(value: string): boolean {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value.trim());
}

/** CPF com os dois dígitos verificadores. */
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

/** Telefone com DDD: 10 ou 11 dígitos. */
export function isValidPhone(value: string): boolean {
  const d = value.replace(/\D/g, "");
  return (d.length === 10 || d.length === 11) && d[0] !== "0";
}

/** CRP no formato "06/12345" (região / número). */
export function isValidLicense(value: string): boolean {
  return /^\d{2}\/\d{4,6}$/.test(value.trim());
}

export function isValidZipCode(value: string): boolean {
  return value.replace(/\D/g, "").length === 8;
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

export function maskZipCode(value: string): string {
  return value.replace(/\D/g, "").slice(0, 8).replace(/(\d{5})(\d)/, "$1-$2");
}

export function maskLicense(value: string): string {
  const d = value.replace(/\D/g, "").slice(0, 8);
  return d.length > 2 ? `${d.slice(0, 2)}/${d.slice(2)}` : d;
}

/** Mesmas regras de senha do ASP.NET Identity configurado na API. */
export function passwordIssues(pw: string): string[] {
  const issues: string[] = [];
  if (pw.length < 6) issues.push("ao menos 6 caracteres");
  if (!/[A-Z]/.test(pw)) issues.push("uma letra maiúscula");
  if (!/[a-z]/.test(pw)) issues.push("uma letra minúscula");
  if (!/\d/.test(pw)) issues.push("um número");
  if (!/[^A-Za-z0-9]/.test(pw)) issues.push("um símbolo");
  return issues;
}

export const PASSWORD_RULES: Array<{ key: string; label: string }> = [
  { key: "ao menos 6 caracteres", label: "6+ caracteres" },
  { key: "uma letra maiúscula", label: "Maiúscula" },
  { key: "uma letra minúscula", label: "Minúscula" },
  { key: "um número", label: "Número" },
  { key: "um símbolo", label: "Símbolo" },
];
