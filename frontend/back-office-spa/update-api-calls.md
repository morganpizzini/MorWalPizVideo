# API Call Updates - Remaining Files

This document tracks the remaining files that need to be updated to use apiService and endpoints instead of hardcoded fetch calls.

## Completed Files
- ✅ `src/services/endpoints.ts` - Added all missing endpoint constants
- ✅ `src/routes/compilations/form/action.ts` - Updated to use post/put with endpoints
- ✅ `src/routes/compilations/index/loader.ts` - Updated to use get with endpoints
- ✅ `src/routes/compilations/index/action.ts` - Updated to use Delete with endpoints

## Remaining Files to Update

### Compilations
- `src/routes/compilations/form/loader.ts`
- `src/routes/compilations/detail/loader.ts`

### Calendar Events
- `src/routes/calendarEvents/index/loader.ts`
- `src/routes/calendarEvents/index/action.ts`
- `src/routes/calendarEvents/create/action.ts`
- `src/routes/calendarEvents/edit/loader.ts`
- `src/routes/calendarEvents/edit/action.ts`
- `src/routes/calendarEvents/detail/loader.ts`
- `src/routes/calendarEvents/detail/action.ts`

### Custom Forms
- `src/routes/customForms/index/loader.ts`
- `src/routes/customForms/index/action.ts`
- `src/routes/customForms/form/loader.ts`
- `src/routes/customForms/form/action.ts` - Already reviewed
- `src/routes/customForms/detail/loader.ts`

### Categories
- `src/routes/categories/index/action.ts`
- `src/routes/categories/create/action.ts`
- `src/routes/categories/edit/action.ts`

### Channels
- `src/routes/channels/index/loader.ts`
- `src/routes/channels/index/action.ts`
- `src/routes/channels/create/action.ts`
- `src/routes/channels/edit/action.ts`
- `src/routes/channels/detail/loader.ts`

### Configurations
- `src/routes/morwalpizconfigurations/index/loader.ts`
- `src/routes/morwalpizconfigurations/index/action.ts`
- `src/routes/morwalpizconfigurations/create/action.ts`
- `src/routes/morwalpizconfigurations/edit/action.ts`
- `src/routes/morwalpizconfigurations/detail/loader.ts`

### Query Links
- `src/routes/queryLinks/index/loader.ts`
- `src/routes/queryLinks/index/action.ts`
- `src/routes/queryLinks/create/action.ts`

### Short Links
- `src/routes/shortLinks/index/action.ts`
- `src/routes/shortLinks/create/action.ts`
- `src/routes/shortLinks/edit/action.ts`
- `src/routes/shortLinks/detail/loader.ts`

### Videos
- `src/routes/videos/detail/loader.ts`
- `src/routes/videos/edit/action.ts`
- `src/routes/videos/import/loader.ts`
- `src/routes/videos/import/action.ts`
- `src/routes/videos/create-root/loader.ts`
- `src/routes/videos/create-root/action.ts`
- `src/routes/videos/create-sub-video/loader.ts`
- `src/routes/videos/create-sub-video/action.ts`
- `src/routes/videos/convert-to-root/loader.ts`
- `src/routes/videos/convert-to-root/action.ts`
- `src/routes/videos/swap-thumbnail/loader.ts`
- `src/routes/videos/swap-thumbnail/action.ts`
- `src/routes/videos/translate/action.ts`

### Images
- `src/routes/images/upload/loader.ts`
- `src/routes/images/upload/action.ts`
- `src/routes/images/upload-multiple/loader.ts`
- `src/routes/images/upload-multiple/action.ts`

## Pattern to Follow

### For GET requests (loaders):
```typescript
// OLD
const response = await fetch('/api/resource');
return response.json();

// NEW
import { get } from '@services/apiService';
import endpoints from '@services/endpoints';
return get(endpoints.RESOURCE);
```

### For GET with ID (detail loaders):
```typescript
// OLD
const response = await fetch(`/api/resource/${id}`);
return response.json();

// NEW
import { get } from '@services/apiService';
import endpoints, { ComposeUrl } from '@services/endpoints';
return get(ComposeUrl(endpoints.RESOURCE_DETAIL, { resourceId: id }));
```

### For POST (create actions):
```typescript
// OLD
return fetch('/api/resource', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify(data)
});

// NEW
import { post } from '@services/apiService';
import endpoints from '@services/endpoints';
return post(endpoints.RESOURCE, data);
```

### For PUT (update actions):
```typescript
// OLD
return fetch(`/api/resource/${id}`, {
  method: 'PUT',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify(data)
});

// NEW
import { put } from '@services/apiService';
import endpoints, { ComposeUrl } from '@services/endpoints';
return put(ComposeUrl(endpoints.RESOURCE_DETAIL, { resourceId: id }), data);
```

### For DELETE (delete actions):
```typescript
// OLD
return fetch(`/api/resource/${id}`, { method: 'DELETE' });

// NEW
import { Delete } from '@services/apiService';
import endpoints, { ComposeUrl } from '@services/endpoints';
return Delete(ComposeUrl(endpoints.RESOURCE_DETAIL, { resourceId: id }));