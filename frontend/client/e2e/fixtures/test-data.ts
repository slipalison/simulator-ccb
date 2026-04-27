/**
 * Test data generators for E2E tests.
 * Each run generates unique data (timestamp-based) to avoid CNPJ/email collisions.
 * CNPJ and CPF are generated with valid modulo-11 check digits.
 */

let counter = 0;

function uniqueId(): string {
  counter++;
  return `${Date.now()}-${counter}`;
}

/**
 * Generate unique suffix combining timestamp + counter
 */
export function generateUniqueSuffix(): string {
  return uniqueId();
}

/**
 * Generate complete company (PJ) test data with valid CNPJ.
 * Password meets Keycloak policy: min 8 chars, 1 upper, 1 lower, 1 digit, 1 special.
 */
export function generateCompanyData() {
  const id = uniqueId();
  return {
    razaoSocial: `Empresa E2E ${id}`,
    cnpj: generateValidCnpj(),
    email: `e2e-pj-${id}@test.com`,
    phone: '11999990000',
    password: 'E2e@Test2026',
  };
}

/**
 * Generate employee (PF) test data with valid CPF.
 */
export function generateEmployeeData() {
  const id = uniqueId();
  return {
    nome: `Funcionário E2E ${id}`,
    cpf: generateValidCpf(),
    email: `e2e-emp-${id}@test.com`,
    phone: '11988880000',
  };
}

/**
 * Generate a valid 14-digit CNPJ with correct modulo-11 check digits.
 * Format: 14 digits (XX.XXX.XXX/XXXX-XX unmasked)
 */
export function generateValidCnpj(): string {
  const base = Array.from({ length: 12 }, () => Math.floor(Math.random() * 10));
  const d1 = calcCnpjDigit(base, [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]);
  const d2 = calcCnpjDigit([...base, d1], [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]);
  return [...base, d1, d2].join('');
}

/**
 * Calculate a CNPJ check digit using modulo-11.
 */
function calcCnpjDigit(nums: number[], weights: number[]): number {
  const sum = nums.reduce((acc, n, i) => acc + n * weights[i], 0);
  const remainder = sum % 11;
  return remainder < 2 ? 0 : 11 - remainder;
}

/**
 * Generate a valid 11-digit CPF with correct modulo-11 check digits.
 * Format: 11 digits (XXX.XXX.XXX-XX unmasked)
 */
export function generateValidCpf(): string {
  const base = Array.from({ length: 9 }, () => Math.floor(Math.random() * 10));
  const d1 = calcCpfDigit(base);
  const d2 = calcCpfDigit([...base, d1]);
  return [...base, d1, d2].join('');
}

/**
 * Calculate a CPF check digit using modulo-11.
 */
function calcCpfDigit(nums: number[]): number {
  const sum = nums.reduce((acc, n, i) => acc + n * (nums.length + 1 - i), 0);
  const remainder = (sum * 10) % 11;
  return remainder === 10 ? 0 : remainder;
}