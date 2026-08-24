/**
 * Client-side mirror of MorWalPizVideo.Models.Constraints.ContentTagRules.
 * The server remains authoritative; these values drive input affordances only.
 */
export const MAX_TAGS_PER_CONTENT = 20;
export const MAX_TAG_LENGTH = 32;

export const equalsIgnoreCase = (left: string, right: string): boolean =>
  left.localeCompare(right, undefined, { sensitivity: 'accent' }) === 0;

export const containsIgnoreCase = (haystack: string, needle: string): boolean =>
  haystack.toLocaleLowerCase().includes(needle.toLocaleLowerCase());

/**
 * Normalizes a caller-supplied tag list the same way the server does:
 * trim, drop empty values, de-duplicate case-insensitively keeping the first display casing.
 */
export function normalizeTags(tags: string[]): string[] {
  const accepted: string[] = [];
  tags.forEach(tag => {
    const trimmed = tag.trim();
    if (trimmed.length === 0) return;
    if (accepted.some(existing => equalsIgnoreCase(existing, trimmed))) return;
    accepted.push(trimmed);
  });
  return accepted;
}
