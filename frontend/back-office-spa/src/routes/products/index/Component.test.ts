import { describe, expect, it } from 'vitest';
import { parseProductImport } from './Component';

describe('product CSV import parsing', () => {
  it('maps quoted fields and both category representations', () => {
    const rows = parseProductImport(
      'InputKey,Title,Description,Url,CategoryIds,CategoryNames\n' +
        'sku-1,"A, product","Long description","https://example.com/a","cat-1;cat-2","News;Reviews"\n'
    );

    expect(rows).toEqual([
      {
        rowNumber: 2,
        inputKey: 'sku-1',
        title: 'A, product',
        description: 'Long description',
        url: 'https://example.com/a',
        categoryIds: ['cat-1', 'cat-2'],
        categoryNames: ['News', 'Reviews'],
      },
    ]);
  });

  it('rejects more than 100 data rows before submission', () => {
    const csv = [
      'Title,Description,Url',
      ...Array.from(
        { length: 101 },
        (_, index) => `Product ${index},Description,https://example.com/${index}`
      ),
    ].join('\n');
    expect(() => parseProductImport(csv)).toThrow('maximum is 100');
  });
});
