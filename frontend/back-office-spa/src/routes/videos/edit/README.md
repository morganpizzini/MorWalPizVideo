# Videos Edit Route

## Scope

This route manages root match metadata and in-memory video reference editing before final save.

Path area:
- `src/routes/videos/edit/Component.tsx`
- `src/routes/videos/edit/loader.ts`
- `src/routes/videos/edit/action.ts`

## Current Behavior

### Loader

- Fetches match detail using `VIDEOS_DETAIL` endpoint and route param `id`.
- Fetches categories using `CATEGORIES` endpoint.
- Returns `{ match, categories }`.

### Main Edit Form

- Fields: title, description, url, root categories, match type, thumbnail video id.
- Root categories are serialized as JSON array of category ids in hidden field `categories`.
- Video references are serialized as JSON array in hidden field `videoRefs`.

### Video References Management

- Existing refs are shown in a table with categories and actions:
  - Edit categories (modal).
  - Set thumbnail id.
- New ref can be added with:
  - YouTube id input.
  - Multi-category checkbox selection.
- Add button is disabled when:
  - YouTube id is empty.
  - No categories selected.
  - Ref id already exists.
- Added refs are not sent immediately; they are included when submitting the main form.

### Action

- Reads multipart form data.
- Parses `categories` JSON string into array.
- Parses `videoRefs` JSON string into array.
- Sends full payload with `PUT` to `VIDEOS_DETAIL` endpoint.

## Data Shape Notes

Category payloads can arrive as either:
- `{ id, title }`
- `{ categoryId, title }`

The component normalizes to `id` internally to keep selection and modal behavior consistent.

## Integration Notes For Future Work

1. If backend introduces dedicated add/remove endpoints for video refs, replace hidden field strategy with explicit mutation calls.
2. If server-side validation for `videoRefs` is added, map response errors to UI near add/edit sections.
3. Consider extracting reusable `VideoRefEditor` component if create/edit/detail flows converge further.

## Minimal Manual Test Checklist

1. Open edit route with existing refs and categories.
2. Add a new ref with selected categories.
3. Save main form.
4. Reopen detail/edit and verify new ref persisted.
5. Edit existing ref categories in modal and save main form.
6. Verify duplicate id cannot be added from add section.
