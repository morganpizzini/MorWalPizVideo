const accountAuthenticationPaths = ['/account/login', '/account/register'];
const localOrigin = 'https://morwalpiz.invalid';

export function getSafeReturnUrl(search: string): string | undefined {
  const candidate = new URLSearchParams(search).get('returnUrl');
  if (!candidate?.startsWith('/') || candidate.startsWith('//') || candidate.includes('\\')) {
    return undefined;
  }

  const destination = new URL(candidate, localOrigin);
  if (
    destination.origin !== localOrigin ||
    accountAuthenticationPaths.includes(destination.pathname)
  ) {
    return undefined;
  }

  return `${destination.pathname}${destination.search}${destination.hash}`;
}

export function getAccountLoginPath(returnUrl?: string): string {
  return addReturnUrl('/account/login', returnUrl);
}

export function getAccountRegisterPath(returnUrl?: string): string {
  return addReturnUrl('/account/register', returnUrl);
}

export function getAccountLoginLink(location: {
  pathname: string;
  search: string;
  hash: string;
}): string {
  const returnUrl = accountAuthenticationPaths.includes(location.pathname)
    ? getSafeReturnUrl(location.search)
    : `${location.pathname}${location.search}${location.hash}`;
  return getAccountLoginPath(returnUrl);
}

export function getPostLoginDestination(search: string): string {
  return getSafeReturnUrl(search) ?? '/account/profile';
}

function addReturnUrl(path: string, returnUrl?: string): string {
  return returnUrl
    ? `${path}?${new URLSearchParams({ returnUrl }).toString()}`
    : path;
}
