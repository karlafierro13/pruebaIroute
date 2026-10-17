/**
 * Replica (solo para la previsualización) las reglas de dbo.fn_commerce_motivo,
 * para que el usuario anticipe qué registros irán a cuarentena.
 * La validación oficial siempre la realiza la base de datos.
 */
export function getCommerceIssues(nomcomred: string | null | undefined, numdoc: string | null | undefined): string[] {
  const issues: string[] = [];
  const doc = numdoc?.trim() ?? '';

  if (!nomcomred?.trim()) issues.push('El nombre del comercio (nomcomred) se encuentra vacío');

  if (!doc) {
    issues.push('El número de documento (numdoc) se encuentra vacío');
  } else {
    if (/\p{L}/u.test(doc)) issues.push('El número de documento (numdoc) contiene letras');
    if (/[^\p{L}0-9]/u.test(doc)) issues.push('El número de documento (numdoc) contiene caracteres especiales');
  }

  return issues;
}

/**
 * Fecha del nombre commerce_DDMMYYYY.csv en formato yyyy-MM-dd,
 * o null si el nombre no es válido.
 */
export function getCommerceFileDate(fileName: string): string | null {
  const match = /^commerce_(\d{2})(\d{2})(\d{4})\.csv$/i.exec(fileName);
  if (!match) return null;

  const iso = `${match[3]}-${match[2]}-${match[1]}`;
  return isRealDate(iso) ? iso : null;
}

// Formatos de pc_processdate aceptados por el API -> conversión a yyyy-MM-dd.
const DATE_FORMATS: { pattern: RegExp; toIso: (m: RegExpExecArray) => string }[] = [
  { pattern: /^(\d{4})-(\d{2})-(\d{2})$/, toIso: (m) => `${m[1]}-${m[2]}-${m[3]}` }, // yyyy-MM-dd
  { pattern: /^(\d{2})\/(\d{2})\/(\d{4})$/, toIso: (m) => `${m[3]}-${m[2]}-${m[1]}` }, // dd/MM/yyyy
  { pattern: /^(\d{2})(\d{2})(\d{4})$/, toIso: (m) => `${m[3]}-${m[2]}-${m[1]}` }, // ddMMyyyy
  { pattern: /^((?:19|20)\d{2})(\d{2})(\d{2})$/, toIso: (m) => `${m[1]}-${m[2]}-${m[3]}` }, // yyyyMMdd
];

export function normalizeProcessDate(value: string): string | null {
  const text = value.trim();
  for (const { pattern, toIso } of DATE_FORMATS) {
    const match = pattern.exec(text);
    if (match && isRealDate(toIso(match))) return toIso(match);
  }
  return null;
}

function isRealDate(iso: string): boolean {
  const [yyyy, mm, dd] = iso.split('-').map(Number);
  const date = new Date(yyyy, mm - 1, dd);
  return date.getFullYear() === yyyy && date.getMonth() === mm - 1 && date.getDate() === dd;
}
