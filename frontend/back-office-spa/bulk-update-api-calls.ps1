# Bulk API Service Migration Script
# This script updates all remaining route files to use apiService methods instead of raw fetch calls

Write-Host "Starting bulk API service migration..." -ForegroundColor Green

# Categories - Already done (3/3 complete)

# Channels (5 files)
Write-Host "`nUpdating Channels..." -ForegroundColor Yellow

# channels/index/loader.ts
$file = "src/routes/channels/index/loader.ts"
$content = @"
import { get } from '@services/apiService';
import endpoints from '@services/endpoints';

export default async function loader() {
  return get(endpoints.CHANNELS);
}
"@
Set-Content -Path $file -Value $content
Write-Host "  ✓ $file" -ForegroundColor Green

# channels/index/action.ts
$file = "src/routes/channels/index/action.ts"
$content = @"
import { data } from 'react-router';
import { Delete } from '@services/apiService';
import endpoints, { ComposeUrl } from '@services/endpoints';

export default async function action({ request }: { request: Request }) {
  const values = Object.fromEntries(await request.formData());

  try {
    await Delete(ComposeUrl(endpoints.CHANNELS_DETAIL, { channelId: values.id as string }));
    return data({ success: true }, { status: 200 });
  } catch (error) {
    return data({ success: false }, { status: 500 });
  }
}
"@
Set-Content -Path $file -Value $content
Write-Host "  ✓ $file" -ForegroundColor Green

# channels/create/action.ts
$file = "src/routes/channels/create/action.ts"
$content = @"
import { data } from 'react-router';
import { post } from '@services/apiService';
import endpoints from '@services/endpoints';
import { CreateChannelDTO } from '@/models';

export default async function action({ request }: { request: Request }) {
  const values = Object.fromEntries(await request.formData()) as CreateChannelDTO;
  const errors: Record<string, string | string[]> = {};

  // Validate fields
  if (!values.title || values.title.trim().length === 0) {
    errors['title'] = 'Title cannot be empty';
  }

  if (!values.url || values.url.trim().length === 0) {
    errors['url'] = 'URL cannot be empty';
  }

  // Check for errors
  if (Object.keys(errors).length > 0) {
    return data({ success: false, errors }, { status: 400 });
  }

  try {
    await post(endpoints.CHANNELS, values);
    return data({ success: true }, { status: 201 });
  } catch (error) {
    errors['generics'] = ['API error found'];
    return data({ success: false, errors }, { status: 500 });
  }
}
"@
Set-Content -Path $file -Value $content
Write-Host "  ✓ $file" -ForegroundColor Green

# channels/edit/action.ts
$file = "src/routes/channels/edit/action.ts"
$content = @"
import { ActionFunctionArgs, data } from 'react-router';
import { put } from '@services/apiService';
import endpoints, { ComposeUrl } from '@services/endpoints';
import { UpdateChannelDTO } from '@/models';

export default async function action({ request, params }: ActionFunctionArgs) {
  const values = Object.fromEntries(await request.formData()) as UpdateChannelDTO;
  const errors: Record<string, string | string[]> = {};

  // Validate fields
  if (!values.title || values.title.trim().length === 0) {
    errors['title'] = 'Title cannot be empty';
  }

  if (!values.url || values.url.trim().length === 0) {
    errors['url'] = 'URL cannot be empty';
  }

  // Check for errors
  if (Object.keys(errors).length > 0) {
    return data({ success: false, errors }, { status: 400 });
  }

  try {
    await put(ComposeUrl(endpoints.CHANNELS_DETAIL, { channelId: params.id! }), values);
    return data({ success: true }, { status: 200 });
  } catch (error) {
    errors['generics'] = ['API error found'];
    return data({ success: false, errors }, { status: 500 });
  }
}
"@
Set-Content -Path $file -Value $content
Write-Host "  ✓ $file" -ForegroundColor Green

# channels/detail/loader.ts
$file = "src/routes/channels/detail/loader.ts"
$content = @"
import { get } from '@services/apiService';
import endpoints, { ComposeUrl } from '@services/endpoints';

export default async function loader({ params }: { params: { id: string } }) {
  return get(ComposeUrl(endpoints.CHANNELS_DETAIL, { channelId: params.id }));
}
"@
Set-Content -Path $file -Value $content
Write-Host "  ✓ $file" -ForegroundColor Green

Write-Host "`nChannels migration complete!" -ForegroundColor Green
Write-Host "`nTo continue with other modules, run the respective sections or complete manually." -ForegroundColor Cyan
Write-Host "Remaining modules: CalendarEvents, CustomForms, Configurations, QueryLinks, ShortLinks, Videos, Images" -ForegroundColor Cyan