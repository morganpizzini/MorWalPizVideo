export function normalizePublicBaseUrl(value: string): string | null {
  const trimmed = value.trim();
  if (!trimmed) return '';
  if (!/^https?:\/\/[^/?#\\\s]+\/?$/i.test(trimmed) || trimmed.includes('@')) return null;
  try {
    const url = new URL(trimmed);
    if (
      !url.hostname ||
      url.username ||
      url.password ||
      url.pathname !== '/' ||
      url.search ||
      url.hash
    )
      return null;
    return url.origin;
  } catch {
    return null;
  }
}
