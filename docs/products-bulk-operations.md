# BackOffice Product Bulk Operations

## CSV import

The Products page accepts one CSV request with at most 100 data rows. The header is case-insensitive and must contain:

`Title,Description,Url,CategoryIds,CategoryNames`

`InputKey` is optional and is returned unchanged as the row identity. `CategoryIds` and `CategoryNames` are optional semicolon-delimited lists. The server resolves both lists against categories owned by the selected channel, unions the references, and deduplicates them by stable category ID. A missing or invalid ID/name rejects only that row. The client rejects more than 100 data rows before submission.

## API contracts

Both endpoints require the selected `X-Channel-Id`, `RequireChannelScope`, and the existing product permissions.

`POST /api/products/bulk` uses `ProductsCreate` or `ProductsManage`:

```json
{
  "items": [
    {
      "rowNumber": 2,
      "inputKey": "sku-1",
      "title": "Example",
      "description": "Description",
      "url": "https://example.com/example",
      "categoryIds": ["category-id"],
      "categoryNames": ["Reviews"]
    }
  ]
}
```

`POST /api/products/categories/bulk` uses `ProductsUpdate` or `ProductsManage`:

```json
{
  "items": [
    {
      "productId": "product-id",
      "inputKey": "selection-1",
      "categoryIds": ["category-id"]
    }
  ]
}
```

Each category-assignment item must contain at least one category ID. Existing product categories are retained and merged with the submitted IDs.

Both return the unchanged row or input identity and a partial result for every submitted item:

```json
{
  "results": [
    {
      "rowNumber": 2,
      "inputKey": "sku-1",
      "productId": "product-id",
      "success": true,
      "error": null
    }
  ]
}
```

Errors are human-readable and do not include CSV contents. Product creation validates required fields, absolute URLs, category IDs/names, channel ownership, and per-channel title uniqueness. Category assignment validates product and category ownership and adds categories without removing existing references. Product cache reset and public product-tag purge happen once when a request has at least one successful mutation.
