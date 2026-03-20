# API Update Implementation Guide

This guide provides detailed instructions for updating all remaining files to use apiService and endpoints.

## Progress Summary

### ✅ Completed (4 files)
- `src/routes/compilations/form/loader.ts`
- `src/routes/compilations/detail/loader.ts`
- `src/routes/calendarEvents/index/loader.ts`
- `src/routes/calendarEvents/index/action.ts`

### ⏳ Remaining (46 files)

## Update Pattern Reference

### For GET requests (loaders):
```typescript
import { get } from '@services/apiService';
import endpoints, { ComposeUrl } from '@services/endpoints';

// Simple GET
return get(endpoints.RESOURCE);

// GET with ID
return get(ComposeUrl(endpoints.RESOURCE_DETAIL, { resourceId: id }));
```

### For POST (create actions):
```typescript
import { post } from '@services/apiService';
import endpoints from '@services/endpoints';

return post(endpoints.RESOURCE, data);
```

### For PUT (update actions):
```typescript
import { put } from '@services/apiService';
import endpoints, { ComposeUrl } from '@services/endpoints';

return put(ComposeUrl(endpoints.RESOURCE_DETAIL, { resourceId: id }), data);
```

### For DELETE actions:
```typescript
import { Delete } from '@services/apiService';
import endpoints, { ComposeUrl } from '@services/endpoints';

return Delete(ComposeUrl(endpoints.RESOURCE_DETAIL, { resourceId: id }));
```

## Files by Category

### Calendar Events (5 files remaining)
1. **calendarEvents/create/action.ts** - POST
   - Use: `post(endpoints.CALENDAREVENTS, data)`

2. **calendarEvents/edit/loader.ts** - GET
   - Use: `get(ComposeUrl(endpoints.CALENDAREVENTS_DETAIL, { title }))`

3. **calendarEvents/edit/action.ts** - PUT
   - Use: `put(endpoints.CALENDAREVENTS, data)`
   - Note: Calendar events use title as identifier

4. **calendarEvents/detail/loader.ts** - GET
   - Use: `get(ComposeUrl(endpoints.CALENDAREVENTS_DETAIL, { title }))`

5. **calendarEvents/detail/action.ts** - DELETE
   - Use: `Delete(ComposeUrl(endpoints.CALENDAREVENTS_DETAIL, { title }))`

### Custom Forms (4 files)
1. **customForms/index/loader.ts** - GET
   - Use: `get(endpoints.CUSTOMFORMS)`

2. **customForms/index/action.ts** - DELETE
   - Use: `Delete(ComposeUrl(endpoints.CUSTOMFORMS_DETAIL, { customFormId }))`

3. **customForms/form/loader.ts** - GET (for edit mode)
   - Use: `get(ComposeUrl(endpoints.CUSTOMFORMS_DETAIL, { customFormId }))`

4. **customForms/detail/loader.ts** - GET
   - Use: `get(ComposeUrl(endpoints.CUSTOMFORMS_DETAIL, { customFormId }))`

### Categories (3 files)
1. **categories/index/action.ts** - DELETE
   - Use: `Delete(ComposeUrl(endpoints.CATEGORIES_DETAIL, { categoryId }))`

2. **categories/create/action.ts** - POST
   - Use: `post(endpoints.CATEGORIES, data)`

3. **categories/edit/action.ts** - PUT
   - Use: `put(ComposeUrl(endpoints.CATEGORIES_DETAIL, { categoryId }), data)`

### Channels (5 files)
1. **channels/index/loader.ts** - GET
   - Use: `get(endpoints.CHANNELS)`

2. **channels/index/action.ts** - DELETE
   - Use: `Delete(ComposeUrl(endpoints.CHANNELS_DETAIL, { channelId }))`

3. **channels/create/action.ts** - POST
   - Use: `post(endpoints.CHANNELS, data)`

4. **channels/edit/action.ts** - PUT
   - Use: `put(ComposeUrl(endpoints.CHANNELS_DETAIL, { channelId }), data)`

5. **channels/detail/loader.ts** - GET
   - Use: `get(ComposeUrl(endpoints.CHANNELS_DETAIL, { channelId }))`

### Configurations (5 files)
1. **morwalpizconfigurations/index/loader.ts** - GET
   - Use: `get(endpoints.CONFIGURATIONS)`

2. **morwalpizconfigurations/index/action.ts** - DELETE
   - Use: `Delete(ComposeUrl(endpoints.CONFIGURATIONS_DETAIL, { configurationId }))`

3. **morwalpizconfigurations/create/action.ts** - POST
   - Use: `post(endpoints.CONFIGURATIONS, data)`

4. **morwalpizconfigurations/edit/action.ts** - PUT
   - Use: `put(ComposeUrl(endpoints.CONFIGURATIONS_DETAIL, { configurationId }), data)`

5. **morwalpizconfigurations/detail/loader.ts** - GET
   - Use: `get(ComposeUrl(endpoints.CONFIGURATIONS_DETAIL, { configurationId }))`

### Query Links (3 files)
1. **queryLinks/index/loader.ts** - GET
   - Use: `get(endpoints.QUERYLINKS)`

2. **queryLinks/index/action.ts** - DELETE
   - Use: `Delete(ComposeUrl(endpoints.QUERYLINKS_DETAIL, { querylinkId }))`

3. **queryLinks/create/action.ts** - POST
   - Use: `post(endpoints.QUERYLINKS, data)`

### Short Links (4 files)
1. **shortLinks/index/action.ts** - DELETE
   - Use: `Delete(ComposeUrl(endpoints.SHORTLINKS_DETAIL, { querylinkId }))`
   - Note: endpoint uses querylinkId parameter name

2. **shortLinks/create/action.ts** - POST
   - Use: `post(endpoints.SHORTLINKS, data)`

3. **shortLinks/edit/action.ts** - PUT
   - Use: `put(ComposeUrl(endpoints.SHORTLINKS_DETAIL, { querylinkId }), data)`

4. **shortLinks/detail/loader.ts** - GET
   - Use: `get(ComposeUrl(endpoints.SHORTLINKS_DETAIL, { querylinkId }))`

### Videos (12 files)
1. **videos/detail/loader.ts** - GET
   - Use: `get(ComposeUrl(endpoints.VIDEOS_DETAIL, { videoId }))`

2. **videos/edit/action.ts** - PUT
   - Use: `put(ComposeUrl(endpoints.VIDEOS_DETAIL, { videoId }), data)`

3. **videos/import/loader.ts** - GET (if needed for categories)
   - Review file to determine if fetch calls exist

4. **videos/import/action.ts** - POST
   - Use: `post(endpoints.VIDEOS_IMPORT, data)`

5. **videos/create-root/loader.ts** - GET (if needed)
   - Review file to determine if fetch calls exist

6. **videos/create-root/action.ts** - POST
   - Use: `post(endpoints.VIDEOS_ROOT_CREATION, data)`

7. **videos/create-sub-video/loader.ts** - GET (if needed)
   - Review file

8. **videos/create-sub-video/action.ts** - POST
   - Use: `post(endpoints.VIDEOS_IMPORT_SUB, data)`

9. **videos/convert-to-root/loader.ts** - GET (if needed)
   - Review file

10. **videos/convert-to-root/action.ts** - POST
    - Use: `post(endpoints.VIDEOS_CONVERT_TO_ROOT, data)`

11. **videos/swap-thumbnail/loader.ts** - GET (if needed)
    - Review file

12. **videos/swap-thumbnail/action.ts** - POST
    - Use: `post(endpoints.VIDEOS_SWAP_THUMBNAIL, data)`

13. **videos/translate/action.ts** - POST
    - Use: `post(endpoints.VIDEOS_TRANSLATE, data)`

### Images (4 files)
1. **images/upload/loader.ts** - GET (if needed for categories)
   - Review file

2. **images/upload/action.ts** - POST with FormData
   - Use: `postFormData(endpoints.IMAGE_UPLOAD, formData)`
   - Note: Use postFormData for file uploads

3. **images/upload-multiple/loader.ts** - GET (if needed)
   - Review file

4. **images/upload-multiple/action.ts** - POST with FormData
   - Use: `postFormData(endpoints.IMAGE_UPLOAD_MULTIPLE, formData)`

## Step-by-Step Update Process

For each file:

1. **Read the current file** to understand the existing fetch pattern
2. **Identify the HTTP method** (GET, POST, PUT, DELETE)
3. **Determine the endpoint** from the URL pattern
4. **Add imports** at the top:
   ```typescript
   import { get/post/put/Delete } from '@services/apiService';
   import endpoints, { ComposeUrl } from '@services/endpoints';
   ```
5. **Replace the fetch call** with the appropriate apiService method
6. **Update error handling** if needed (apiService returns errors in result.errors)
7. **Test** the updated file if possible

## Common Patterns

### Error Handling Pattern
```typescript
try {
  const result = await Delete(ComposeUrl(endpoints.RESOURCE_DETAIL, { id }));
  
  if (result?.errors) {
    return {
      success: false,
      errors: result.errors
    };
  }
  
  return { success: true };
} catch (error) {
  return {
    success: false,
    errors: {
      generics: [(error as Error).message]
    }
  };
}
```

### Loader Pattern
```typescript
export default async function loader({ params }: { params: any }) {
  return get(ComposeUrl(endpoints.RESOURCE_DETAIL, { resourceId: params.id }));
}
```

## Verification

After updating all files:

1. Run the PowerShell script to verify no fetch calls remain:
   ```powershell
   pwsh -File update-remaining-files.ps1
   ```

2. Build the application:
   ```bash
   npm run build
   ```

3. Check for TypeScript errors:
   ```bash
   npm run type-check
   ```

## Notes

- Calendar events use `title` as identifier instead of `id`
- Short links endpoint uses `querylinkId` parameter name
- Image uploads require `postFormData` instead of `post`
- Some loader files may only need fetch calls for loading categories/dropdown data
- apiService automatically adds authentication headers
- apiService handles base URL configuration for different environments