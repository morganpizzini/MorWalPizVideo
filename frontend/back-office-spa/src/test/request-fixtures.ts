export function createFormRequest(
  url: string,
  values: Record<string, string | readonly string[]>,
  method = 'POST'
): Request {
  const body = new URLSearchParams();
  for (const [key, value] of Object.entries(values)) {
    for (const entry of Array.isArray(value) ? value : [value]) {
      body.append(key, entry);
    }
  }

  return new Request(url, {
    method,
    body,
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
  });
}
