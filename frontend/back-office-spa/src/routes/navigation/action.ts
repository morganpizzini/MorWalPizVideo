import { data } from 'react-router';
import { saveNavigation } from '@morwalpizvideo/services';
import { NavigationItemType } from '@morwalpizvideo/models';
import type { NavigationMenuItemDTO, SaveNavigationDTO } from '@morwalpizvideo/models';
import { getPagesApiError } from '../pages/response';

export function validateNavigation(navigation: Partial<SaveNavigationDTO>): string[] {
  const errors: string[] = [];
  const footerColumnCount = navigation.footerColumnCount ?? 0;
  if (!Number.isInteger(footerColumnCount) || footerColumnCount < 1 || footerColumnCount > 8) {
    errors.push('Footer columns must be between 1 and 8.');
  }
  validateItems(navigation.headerItems, false, footerColumnCount, errors);
  validateItems(navigation.footerItems, true, footerColumnCount, errors);
  return errors;
}

function validateItems(items: NavigationMenuItemDTO[] | undefined, footer: boolean, footerColumnCount: number, errors: string[]): void {
  for (const item of items ?? []) {
    if (!item.displayText?.trim() || item.displayText.trim().length > 120) errors.push('Menu item display text is required and must be at most 120 characters.');
    if (item.type === NavigationItemType.Page && !item.pageId?.trim()) errors.push('Menu page links must reference a page.');
    if (item.type === NavigationItemType.Internal && !isSafeInternalUrl(item.targetUrl)) errors.push('Internal menu links must start with a single slash.');
    if (item.type === NavigationItemType.External && !isSafeExternalUrl(item.targetUrl)) errors.push('External menu links must use http or https without credentials.');
    if (footer && (!Number.isInteger(item.column) || item.column < 0 || item.column >= footerColumnCount)) errors.push('Footer item column is outside the configured column count.');
  }
}

function isSafeInternalUrl(value: string): boolean {
  return value.startsWith('/') && !value.startsWith('//') && !value.includes('\\');
}

function isSafeExternalUrl(value: string): boolean {
  try {
    const url = new URL(value);
    return (url.protocol === 'http:' || url.protocol === 'https:') && !url.username && !url.password;
  } catch {
    return false;
  }
}

export default async function action({ request }: { request: Request }) {
  try {
    const form = await request.formData();
    const navigation = JSON.parse(String(form.get('navigation') ?? '{}')) as SaveNavigationDTO;
    const validationErrors = validateNavigation(navigation);
    if (validationErrors.length > 0) return data({ success: false, errors: validationErrors }, { status: 400 });
    const response = await saveNavigation(navigation);
    const error = getPagesApiError(response);
    if (error) return data({ success: false, errors: error.errors }, { status: error.status ?? 500 });
    return { success: true };
  } catch {
    return data({ success: false, errors: ['Unable to save navigation'] }, { status: 400 });
  }
}