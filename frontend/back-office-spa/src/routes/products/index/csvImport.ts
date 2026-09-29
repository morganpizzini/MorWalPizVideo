import type { BulkCreateProductRowDTO } from '@morwalpizvideo/models';

const MAX_IMPORT_ROWS = 100;

export function parseCsv(text: string): string[][] {
  const rows: string[][] = [];
  let row: string[] = [];
  let value = '';
  let quoted = false;
  for (let index = 0; index < text.length; index += 1) {
    const character = text[index];
    if (character === '"') {
      if (quoted && text[index + 1] === '"') {
        value += '"';
        index += 1;
      } else quoted = !quoted;
    } else if (character === ',' && !quoted) {
      row.push(value.trim());
      value = '';
    } else if ((character === '\n' || character === '\r') && !quoted) {
      if (character === '\r' && text[index + 1] === '\n') index += 1;
      row.push(value.trim());
      if (row.some(cell => cell.length > 0)) rows.push(row);
      row = [];
      value = '';
    } else value += character;
  }
  if (value.length > 0 || row.length > 0) {
    row.push(value.trim());
    rows.push(row);
  }
  return rows;
}

function splitList(value: string | undefined): string[] {
  return (value ?? '')
    .split(';')
    .map(item => item.trim())
    .filter(Boolean);
}

export function parseProductImport(text: string): BulkCreateProductRowDTO[] {
  const rows = parseCsv(text);
  if (rows.length < 2) throw new Error('The CSV must contain a header and at least one data row.');
  const headers = rows[0].map(header => header.trim().toLowerCase());
  const missing = ['title', 'description', 'url'].filter(header => !headers.includes(header));
  if (missing.length > 0) throw new Error(`Missing required column(s): ${missing.join(', ')}.`);
  const indexOf = (name: string) => headers.indexOf(name);
  if (rows.length - 1 > MAX_IMPORT_ROWS)
    throw new Error(
      `The CSV contains ${rows.length - 1} data rows; the maximum is ${MAX_IMPORT_ROWS}.`
    );
  return rows.slice(1).map((cells, offset) => ({
    rowNumber: offset + 2,
    inputKey: cells[indexOf('inputkey')]?.trim() || undefined,
    title: cells[indexOf('title')] ?? '',
    description: cells[indexOf('description')] ?? '',
    url: cells[indexOf('url')] ?? '',
    categoryIds: splitList(cells[indexOf('categoryids')]),
    categoryNames: splitList(cells[indexOf('categorynames')]),
  }));
}
