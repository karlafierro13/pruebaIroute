export interface CsvParseResult<T> {
  headers: string[];
  rows: T[];
  missingColumns: string[];
}

/**
 * Parser CSV liviano para la previsualización: detecta el delimitador (, o ;),
 * soporta campos entre comillas y comillas escapadas ("").
 */
export function parseCsv<T extends object>(content: string, requiredColumns: readonly string[]): CsvParseResult<T> {
  const lines = content
    .replace(/^\uFEFF/, '')
    .split(/\r?\n/)
    .filter((line) => line.trim().length > 0);

  if (lines.length === 0) return { headers: [], rows: [], missingColumns: [...requiredColumns] };

  const delimiter = countChar(lines[0], ';') > countChar(lines[0], ',') ? ';' : ',';
  const headers = splitLine(lines[0], delimiter).map((h) => h.trim().toLowerCase());
  const missingColumns = requiredColumns.filter((c) => !headers.includes(c));

  const rows = lines.slice(1).map((line) => {
    const fields = splitLine(line, delimiter);
    return Object.fromEntries(headers.map((h, i) => [h, (fields[i] ?? '').trim()])) as T;
  });

  return { headers, rows, missingColumns };
}

function splitLine(line: string, delimiter: string): string[] {
  const fields: string[] = [];
  let current = '';
  let inQuotes = false;

  for (let i = 0; i < line.length; i++) {
    const c = line[i];
    if (inQuotes) {
      if (c === '"' && line[i + 1] === '"') {
        current += '"';
        i++;
      } else if (c === '"') inQuotes = false;
      else current += c;
    } else if (c === '"') inQuotes = true;
    else if (c === delimiter) {
      fields.push(current);
      current = '';
    } else current += c;
  }

  fields.push(current);
  return fields;
}

function countChar(text: string, char: string): number {
  return text.split(char).length - 1;
}
