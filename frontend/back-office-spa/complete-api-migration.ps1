# Complete API Service Migration Script
# Updates ALL remaining route files to use apiService methods

Write-Host "Starting complete API service migration..." -ForegroundColor Green
Write-Host "Working directory: $(Get-Location)" -ForegroundColor Cyan

$totalFiles = 0
$successFiles = 0
$errorFiles = 0

# Helper function to safely write file
function Safe-WriteFile {
    param($path, $content)
    try {
        Set-Content -Path $path -Value $content -ErrorAction Stop
        $script:successFiles++
        $script:totalFiles++
        Write-Host "  ✓ $path" -ForegroundColor Green
        return $true
    } catch {
        $script:errorFiles++
        $script:totalFiles++
        Write-Host "  ✗ $path - Error: $_" -ForegroundColor Red
        return $false
    }
}

# CalendarEvents (5 files) - Note: uses 'title' as identifier
Write-Host "`n[1/8] Updating Calendar Events..." -ForegroundColor Yellow

Safe-WriteFile "src/routes/calendarEvents/create/action.ts" @"
import { data } from 'react-router';
import { post } from '@services/apiService';
import endpoints from '@services/endpoints';

export default async function action({ request }: { request: Request }) {
  const values = Object.fromEntries(await request.formData());

  try {
    await post(endpoints.CALENDAREVENTS, values);
    return data({ success: true }, { status: 201 });
  } catch (error) {
    return data({ success: false, errors: { generics: ['API error found'] } }, { status: 500 });
  }
}
"@

Safe-WriteFile "src/routes/calendarEvents/edit/loader.ts" @"
import { get } from '@services/apiService';
import endpoints, { ComposeUrl } from '@services/endpoints';

export default async function loader({ params }: { params: { title: string } }) {
  const title = decodeURIComponent(params.title);
  try {
    const response = await get(ComposeUrl(endpoints.CALENDAREVENTS_DETAIL, { title: encodeURIComponent(title) }));
    return response;
  } catch (error) {
    throw new Response('Calendar event not found', { status: 404 });
  }
}
"@

Safe-WriteFile "src/routes/calendarEvents/edit/action.ts" @"
import { data } from 'react-router';
import { put } from '@services/apiService';
import endpoints from '@services/endpoints';

export default async function action({ request }: { request: Request }) {
  const values = Object.fromEntries(await request.formData());

  try {
    const response = await put(endpoints.CALENDAREVENTS, values);
    
    if (response?.errors) {
      return data({ success: false, errors: response.errors }, { status: 400 });
    }
    
    return data({ success: true }, { status: 200 });
  } catch (error) {
    return data({ success: false, errors: { generics: ['API error found'] } }, { status: 500 });
  }
}
"@

Safe-WriteFile "src/routes/calendarEvents/detail/loader.ts" @"
import { get } from '@services/apiService';
import endpoints, { ComposeUrl } from '@services/endpoints';

export default async function loader({ params }: { params: { title: string } }) {
  const title = decodeURIComponent(params.title);
  try {
    const response = await get(ComposeUrl(endpoints.CALENDAREVENTS_DETAIL, { title: encodeURIComponent(title) }));
    return response;
  } catch (error) {
    throw new Response('Calendar event not found', { status: 404 });
  }
}
"@

Safe-WriteFile "src/routes/calendarEvents/detail/action.ts" @"
import { data } from 'react-router';
import { Delete } from '@services/apiService';
import endpoints, { ComposeUrl } from '@services/endpoints';

export default async function action({ params }: { params: { title: string } }) {
  const title = decodeURIComponent(params.title);
  
  try {
    const response = await Delete(ComposeUrl(endpoints.CALENDAREVENTS_DETAIL, { title: encodeURIComponent(title) }));
    
    if (response?.errors) {
      return data({ success: false, errors: response.errors }, { status: 400 });
    }
    
    return data({ success: true }, { status: 200 });
  } catch (error) {
    return data({ success: false, errors: { generics: ['API error found'] } }, { status: 500 });
  }
}
"@

# CustomForms (4 files)
Write-Host "`n[2/8] Updating Custom Forms..." -ForegroundColor Yellow

Safe-WriteFile "src/routes/customForms/index/loader.ts" @"
import { get } from '@services/apiService';
import endpoints from '@services/endpoints';

export default async function loader() {
  try {
    const response = await get(endpoints.CUSTOMFORMS);
    return response;
  } catch (error) {
    return [];
  }
}
"@

Safe-WriteFile "src/routes/customForms/index/action.ts" @"
import { data } from 'react-router';
import { Delete } from '@services/apiService';
import endpoints, { ComposeUrl } from '@services/endpoints';

export default async function action({ request }: { request:  Request }) {
  const formData = await request.formData();
  const id = formData.get('id') as string;
  
  try {
    const response = await Delete(ComposeUrl(endpoints.CUSTOMFORMS_DETAIL, { customFormId: encodeURIComponent(id) }));
    
    if (response?.errors) {
      return data({ success: false, errors: response.errors }, { status: 400 });
    }
    
    return data({ success: true }, { status: 200 });
  } catch (error) {
    return data({ success: false, errors: { generics: ['API error found'] } }, { status: 500 });
  }
}
"@

Safe-WriteFile "src/routes/customForms/form/loader.ts" @"
import { get } from '@services/apiService';
import endpoints, { ComposeUrl } from '@services/endpoints';

export default async function loader({ params }: { params: { id?: string } }) {
  if (params.id) {
    try {
      const response = await get(ComposeUrl(endpoints.CUSTOMFORMS_DETAIL, { customFormId: encodeURIComponent(params.id) }));
      return response;
    } catch (error) {
      throw new Response('Custom form not found', { status: 404 });
    }
  }
  return null;
}
"@

Safe-WriteFile "src/routes/customForms/detail/loader.ts" @"
import { get } from '@services/apiService';
import endpoints, { ComposeUrl } from '@services/endpoints';

export default async function loader({ params }: { params: { id: string } }) {
  try {
    const response = await get(ComposeUrl(endpoints.CUSTOMFORMS_DETAIL, { customFormId: encodeURIComponent(params.id) }));
    return response;
  } catch (error) {
    throw new Response('Custom form not found', { status: 404 });
  }
}
"@

# Configurations (5 files)
Write-Host "`n[3/8] Updating Configurations..." -ForegroundColor Yellow

Safe-WriteFile "src/routes/morwalpizconfigurations/index/loader.ts" @"
import { get } from '@services/apiService';
import endpoints from '@services/endpoints';

export default async function loader() {
  return get(endpoints.CONFIGURATIONS);
}
"@

Safe-WriteFile "src/routes/morwalpizconfigurations/index/action.ts" @"
import { data } from 'react-router';
import { Delete } from '@services/apiService';
import endpoints, { ComposeUrl } from '@services/endpoints';

export default async function action({ request }: { request: Request }) {
  const values = Object.fromEntries(await request.formData());

  try {
    await Delete(ComposeUrl(endpoints.CONFIGURATIONS_DETAIL, { configurationId: values.id as string }));
    return data({ success: true }, { status: 200 });
  } catch (error) {
    return data({ success: false }, { status: 500 });
  }
}
"@

Safe-WriteFile "src/routes/morwalpizconfigurations/create/action.ts" @"
import { data } from 'react-router';
import { post } from '@services/apiService';
import endpoints from '@services/endpoints';

export default async function action({ request }: { request: Request }) {
  const values = Object.fromEntries(await request.formData());
  const errors: Record<string, string | string[]> = {};

  if (Object.keys(errors).length > 0) {
    return data({ success: false, errors }, { status: 400 });
  }

  try {
    await post(endpoints.CONFIGURATIONS, values);
    return data({ success: true }, { status: 201 });
  } catch (error) {
    errors['generics'] = ['API error found'];
    return data({ success: false, errors }, { status: 500 });
  }
}
"@

Safe-WriteFile "src/routes/morwalpizconfigurations/edit/action.ts" @"
import { ActionFunctionArgs, data } from 'react-router';
import { put } from '@services/apiService';
import endpoints, { ComposeUrl } from '@services/endpoints';

export default async function action({ request, params }: ActionFunctionArgs) {
  const values = Object.fromEntries(await request.formData());
  const errors: Record<string, string | string[]> = {};

  if (Object.keys(errors).length > 0) {
    return data({ success: false, errors }, { status: 400 });
  }

  try {
    await put(ComposeUrl(endpoints.CONFIGURATIONS_DETAIL, { configurationId: params.id! }), values);
    return data({ success: true }, { status: 200 });
  } catch (error) {
    errors['generics'] = ['API error found'];
    return data({ success: false, errors }, { status: 500 });
  }
}
"@

Safe-WriteFile "src/routes/morwalpizconfigurations/detail/loader.ts" @"
import { get } from '@services/apiService';
import endpoints, { ComposeUrl } from '@services/endpoints';

export default async function loader({ params }: { params: { id: string } }) {
  return get(ComposeUrl(endpoints.CONFIGURATIONS_DETAIL, { configurationId: params.id }));
}
"@

# QueryLinks (3 files)
Write-Host "`n[4/8] Updating Query Links..." -ForegroundColor Yellow

Safe-WriteFile "src/routes/queryLinks/index/loader.ts" @"
import { get } from '@services/apiService';
import endpoints from '@services/endpoints';

export default async function loader() {
  return get(endpoints.QUERYLINKS);
}
"@

Safe-WriteFile "src/routes/queryLinks/index/action.ts" @"
import { data } from 'react-router';
import { Delete } from '@services/apiService';
import endpoints, { ComposeUrl } from '@services/endpoints';

export default async function action({ request }: { request: Request }) {
  const values = Object.fromEntries(await request.formData());

  try {
    await Delete(ComposeUrl(endpoints.QUERYLINKS_DETAIL, { querylinkId: values.id as string }));
    return data({ success: true }, { status: 200 });
  } catch (error) {
    return data({ success: false }, { status: 500 });
  }
}
"@

Safe-WriteFile "src/routes/queryLinks/create/action.ts" @"
import { data } from 'react-router';
import { post } from '@services/apiService';
import endpoints from '@services/endpoints';

export default async function action({ request }: { request: Request }) {
  const values = Object.fromEntries(await request.formData());
  const errors: Record<string, string | string[]> = {};

  if (!values.title || (values.title as string).trim().length === 0) {
    errors['title'] = 'Title cannot be empty';
  }

  if (!values.link || (values.link as string).trim().length === 0) {
    errors['link'] = 'Link cannot be empty';
  }

  if (Object.keys(errors).length > 0) {
    return data({ success: false, errors }, { status: 400 });
  }

  try {
    await post(endpoints.QUERYLINKS, values);
    return data({ success: true }, { status: 201 });
  } catch (error) {
    errors['generics'] = ['API error found'];
    return data({ success: false, errors }, { status: 500 });
  }
}
"@

# ShortLinks (4 files)
Write-Host "`n[5/8] Updating Short Links..." -ForegroundColor Yellow

Safe-WriteFile "src/routes/shortLinks/index/action.ts" @"
import { data } from 'react-router';
import { Delete } from '@services/apiService';
import endpoints, { ComposeUrl } from '@services/endpoints';

export default async function action({ request }: { request: Request }) {
  const values = Object.fromEntries(await request.formData());

  try {
    await Delete(ComposeUrl(endpoints.SHORTLINKS_DETAIL, { querylinkId: values.id as string }));
    return data({ success: true }, { status: 200 });
  } catch (error) {
    return data({ success: false }, { status: 500 });
  }
}
"@

Safe-WriteFile "src/routes/shortLinks/create/action.ts" @"
import { data } from 'react-router';
import { post } from '@services/apiService';
import endpoints from '@services/endpoints';

export default async function action({ request }: { request: Request }) {
  const values = Object.fromEntries(await request.formData());
  const errors: Record<string, string | string[]> = {};

  if (!values.title || (values.title as string).trim().length === 0) {
    errors['title'] = 'Title cannot be empty';
  }

  if (!values.longUrl || (values.longUrl as string).trim().length === 0) {
    errors['longUrl'] = 'URL cannot be empty';
  }

  if (Object.keys(errors).length > 0) {
    return data({ success: false, errors }, { status: 400 });
  }

  try {
    await post(endpoints.SHORTLINKS, values);
    return data({ success: true }, { status: 201 });
  } catch (error) {
    errors['generics'] = ['API error found'];
    return data({ success: false, errors }, { status: 500 });
  }
}
"@

Safe-WriteFile "src/routes/shortLinks/edit/action.ts" @"
import { ActionFunctionArgs, data } from 'react-router';
import { put } from '@services/apiService';
import endpoints, { ComposeUrl } from '@services/endpoints';

export default async function action({ request, params }: ActionFunctionArgs) {
  const values = Object.fromEntries(await request.formData());
  const errors: Record<string, string | string[]> = {};

  if (!values.title || (values.title as string).trim().length === 0) {
    errors['title'] = 'Title cannot be empty';
  }

  if (!values.longUrl || (values.longUrl as string).trim().length === 0) {
    errors['longUrl'] = 'URL cannot be empty';
  }

  if (Object.keys(errors).length > 0) {
    return data({ success: false, errors }, { status: 400 });
  }

  try {
    await put(ComposeUrl(endpoints.SHORTLINKS_DETAIL, { querylinkId: params.id! }), values);
    return data({ success: true }, { status: 200 });
  } catch (error) {
    errors['generics'] = ['API error found'];
    return data({ success: false, errors }, { status: 500 });
  }
}
"@

Safe-WriteFile "src/routes/shortLinks/detail/loader.ts" @"
import { get } from '@services/apiService';
import endpoints, { ComposeUrl } from '@services/endpoints';

export default async function loader({ params }: { params: { id: string } }) {
  return get(ComposeUrl(endpoints.SHORTLINKS_DETAIL, { querylinkId: params.id }));
}
"@

# Videos (12 files) - Complex operations
Write-Host "`n[6/8] Updating Videos..." -ForegroundColor Yellow

Safe-WriteFile "src/routes/videos/detail/loader.ts" @"
import { get } from '@services/apiService';
import endpoints, { ComposeUrl } from '@services/endpoints';

export default async function loader({ params }: { params: { id: string } }) {
  const [video, categories] = await Promise.all([
    get(ComposeUrl(endpoints.VIDEOS_DETAIL, { videoId: params.id })),
    get(endpoints.CATEGORIES)
  ]);

  return { video, categories };
}
"@

Safe-WriteFile "src/routes/videos/edit/action.ts" @"
import { ActionFunctionArgs, data } from 'react-router';
import { put } from '@services/apiService';
import endpoints, { ComposeUrl } from '@services/endpoints';

export default async function action({ request, params }: ActionFunctionArgs) {
  const values = Object.fromEntries(await request.formData());

  try {
    const response = await put(ComposeUrl(endpoints.VIDEOS_DETAIL, { videoId: params.id! }), values);
    
    if (response?.errors) {
      return data({ success: false, errors: response.errors }, { status: 400 });
    }
    
    return data({ success: true }, { status: 200 });
  } catch (error) {
    return data({ success: false, errors: { generics: ['API error found'] } }, { status: 500 });
  }
}
"@

Safe-WriteFile "src/routes/videos/import/loader.ts" @"
import { get } from '@services/apiService';
import endpoints from '@services/endpoints';

export default async function loader() {
  const categories = await get(endpoints.CATEGORIES);
  return { categories };
}
"@

Safe-WriteFile "src/routes/videos/import/action.ts" @"
import { data } from 'react-router';
import { post } from '@services/apiService';
import endpoints from '@services/endpoints';

export default async function action({ request }: { request: Request }) {
  const values = Object.fromEntries(await request.formData());

  try {
    await post(endpoints.VIDEOS_IMPORT, values);
    return data({ success: true }, { status: 201 });
  } catch (error) {
    return data({ success: false, errors: { generics: ['API error found'] } }, { status: 500 });
  }
}
"@

Safe-WriteFile "src/routes/videos/create-root/loader.ts" @"
import { get } from '@services/apiService';
import endpoints from '@services/endpoints';

export default async function loader() {
  const categories = await get(endpoints.CATEGORIES);
  return { categories };
}
"@

Safe-WriteFile "src/routes/videos/create-root/action.ts" @"
import { data } from 'react-router';
import { post } from '@services/apiService';
import endpoints from '@services/endpoints';

export default async function action({ request }: { request: Request }) {
  const values = Object.fromEntries(await request.formData());

  try {
    await post(endpoints.VIDEOS_ROOT_CREATION, values);
    return data({ success: true }, { status: 201 });
  } catch (error) {
    return data({ success: false, errors: { generics: ['API error found'] } }, { status: 500 });
  }
}
"@

Safe-WriteFile "src/routes/videos/create-sub-video/loader.ts" @"
import { get } from '@services/apiService';
import endpoints from '@services/endpoints';

export default async function loader() {
  const [categories, videos] = await Promise.all([
    get(endpoints.CATEGORIES),
    get(endpoints.VIDEOS)
  ]);

  return { categories, videos };
}
"@

Safe-WriteFile "src/routes/videos/create-sub-video/action.ts" @"
import { data } from 'react-router';
import { post } from '@services/apiService';
import endpoints from '@services/endpoints';

export default async function action({ request }: { request: Request }) {
  const values = Object.fromEntries(await request.formData());

  try {
    await post(endpoints.VIDEOS_IMPORT_SUB, values);
    return data({ success: true }, { status: 201 });
  } catch (error) {
    return data({ success: false, errors: { generics: ['API error found'] } }, { status: 500 });
  }
}
"@

Safe-WriteFile "src/routes/videos/convert-to-root/loader.ts" @"
import { get } from '@services/apiService';
import endpoints from '@services/endpoints';

export default async function loader() {
  const [categories, videos] = await Promise.all([
    get(endpoints.CATEGORIES),
    get(endpoints.VIDEOS)
  ]);

  return { categories, videos };
}
"@

Safe-WriteFile "src/routes/videos/convert-to-root/action.ts" @"
import { data } from 'react-router';
import { post } from '@services/apiService';
import endpoints from '@services/endpoints';

export default async function action({ request }: { request: Request }) {
  const values = Object.fromEntries(await request.formData());

  try {
    await post(endpoints.VIDEOS_CONVERT_TO_ROOT, values);
    return data({ success: true }, { status: 200 });
  } catch (error) {
    return data({ success: false, errors: { generics: ['API error found'] } }, { status: 500 });
  }
}
"@

Safe-WriteFile "src/routes/videos/swap-thumbnail/loader.ts" @"
import { get } from '@services/apiService';
import endpoints from '@services/endpoints';

export default async function loader() {
  const videos = await get(endpoints.VIDEOS);
  return { videos };
}
"@

Safe-WriteFile "src/routes/videos/swap-thumbnail/action.ts" @"
import { data } from 'react-router';
import { post } from '@services/apiService';
import endpoints from '@services/endpoints';

export default async function action({ request }: { request: Request }) {
  const values = Object.fromEntries(await request.formData());

  try {
    await post(endpoints.VIDEOS_SWAP_THUMBNAIL, values);
    return data({ success: true }, { status: 200 });
  } catch (error) {
    return data({ success: false, errors: { generics: ['API error found'] } }, { status: 500 });
  }
}
"@

Safe-WriteFile "src/routes/videos/translate/action.ts" @"
import { data } from 'react-router';
import { post } from '@services/apiService';
import endpoints from '@services/endpoints';

export default async function action({ request }: { request: Request }) {
  const values = Object.fromEntries(await request.formData());

  try {
    await post(endpoints.VIDEOS_TRANSLATE, values);
    return data({ success: true }, { status: 200 });
  } catch (error) {
    return data({ success: false, errors: { generics: ['API error found'] } }, { status: 500 });
  }
}
"@

# Images (4 files) - uses postFormData
Write-Host "`n[7/8] Updating Images..." -ForegroundColor Yellow

Safe-WriteFile "src/routes/images/upload/loader.ts" @"
import { get } from '@services/apiService';
import endpoints from '@services/endpoints';

export default async function loader() {
  const matches = await get(endpoints.VIDEOS);
  return { matches };
}
"@

Safe-WriteFile "src/routes/images/upload/action.ts" @"
import { data } from 'react-router';
import { postFormData } from '@services/apiService';
import endpoints from '@services/endpoints';

export default async function action({ request }: { request: Request }) {
  const formData = await request.formData();

  try {
    await postFormData(endpoints.IMAGE_UPLOAD, formData);
    return data({ success: true }, { status: 201 });
  } catch (error) {
    return data({ success: false, errors: { generics: ['API error found'] } }, { status: 500 });
  }
}
"@

Safe-WriteFile "src/routes/images/upload-multiple/loader.ts" @"
import { get } from '@services/apiService';
import endpoints from '@services/endpoints';

export default async function loader() {
  try {
    const matches = await get(endpoints.VIDEOS);
    return { matches };
  } catch (error) {
    return { matches: [] };
  }
}
"@

Safe-WriteFile "src/routes/images/upload-multiple/action.ts" @"
import { data } from 'react-router';
import { postFormData } from '@services/apiService';
import endpoints from '@services/endpoints';

export default async function action({ request }: { request: Request }) {
  const formData = await request.formData();

  try {
    await postFormData(endpoints.IMAGE_UPLOAD_MULTIPLE, formData);
    return data({ success: true }, { status: 201 });
  } catch (error) {
    return data({ success: false, errors: { generics: ['API error found'] } }, { status: 500 });
  }
}
"@

# Summary
Write-Host "`n" -NoNewline
Write-Host "=" * 60 -ForegroundColor Cyan
Write-Host "Migration Complete!" -ForegroundColor Green
Write-Host "=" * 60 -ForegroundColor Cyan
Write-Host "Total files processed: $totalFiles" -ForegroundColor White
Write-Host "  ✓ Success: $successFiles" -ForegroundColor Green
Write-Host "  ✗ Errors: $errorFiles" -ForegroundColor Red
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Run: cd frontend/back-office-spa && npm run type-check" -ForegroundColor Cyan
Write-Host "  2. Run: pwsh -File update-remaining-files.ps1" -ForegroundColor Cyan
Write-Host "  3. Check for any remaining fetch calls" -ForegroundColor Cyan