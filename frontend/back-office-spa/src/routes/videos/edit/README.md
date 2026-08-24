# Videos Edit Route

## Scope

This route manages root match metadata and immediate video reference additions.

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
- Video reference additions are submitted independently from the metadata form.

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
- Add Video Reference sends a minimal request immediately and adds the server response to the table only after success.

### Action

- Reads multipart form data for the metadata form, or the add-reference intent.
- Parses `categories` JSON string into array.
- Sends metadata with `PUT` to `VIDEOS_DETAIL`, or `POST` to `VIDEOS_VIDEO_REFS` with `youtubeId` and category IDs.

## Data Shape Notes

Category payloads can arrive as either:
- `{ id, title }`
- `{ categoryId, title }`

The component normalizes to `id` internally to keep selection and modal behavior consistent.

## Integration Notes For Future Work

1. If a dedicated update endpoint for existing video refs is introduced, wire the modal to it without coupling it to the metadata form.
2. Consider extracting reusable `VideoRefEditor` component if create/edit/detail flows converge further.

## Minimal Manual Test Checklist

1. Open edit route with existing refs and categories.
2. Add a new ref with selected categories.
3. Verify the add request completes before the new row appears.
4. Reopen detail/edit and verify new ref persisted.
5. Edit existing ref categories in modal and verify the existing local behavior.
6. Verify duplicate id cannot be added from add section.
