import '@testing-library/jest-dom';
import { URLSearchParams as NodeURLSearchParams } from 'node:url';

const localStorageValues = new Map<string, string>();
Object.defineProperty(window, 'localStorage', {
  configurable: true,
  value: {
    getItem: (key: string) => localStorageValues.get(key) ?? null,
    setItem: (key: string, value: string) => localStorageValues.set(key, String(value)),
    removeItem: (key: string) => localStorageValues.delete(key),
    clear: () => localStorageValues.clear(),
    key: (index: number) => Array.from(localStorageValues.keys())[index] ?? null,
    get length() {
      return localStorageValues.size;
    },
  } satisfies Storage,
});

Object.defineProperty(globalThis, 'URLSearchParams', {
  configurable: true,
  value: NodeURLSearchParams,
});
Object.defineProperty(window, 'URLSearchParams', {
  configurable: true,
  value: NodeURLSearchParams,
});

Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: (query: string): MediaQueryList => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: () => undefined,
    removeListener: () => undefined,
    addEventListener: () => undefined,
    removeEventListener: () => undefined,
    dispatchEvent: () => false,
  }),
});

Object.defineProperty(window, 'scrollTo', {
  configurable: true,
  value: () => undefined,
});
