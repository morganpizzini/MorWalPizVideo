import assert from 'node:assert/strict';
import test from 'node:test';
import { scanContent } from './secret-artifact-guard.mjs';

test('detects high-confidence token formats without exposing values', () => {
  const token = ['ghp_', '123456789012345678901234567890'].join('');
  const findings = scanContent('src/config.ts', `const token = "${token}";`);

  assert.deepEqual(findings, ['known token format', 'credential-like assignment']);
});

test('ignores documented placeholders and test fixtures', () => {
  assert.deepEqual(
    scanContent('docs/example.md', 'password: "change_me_for_local_development"'),
    []
  );
  assert.deepEqual(
    scanContent('src/example.test.ts', 'const token = "test-token-value-that-is-safe";'),
    []
  );
});

test('detects non-placeholder credential assignments in source', () => {
  const findings = scanContent('src/config.ts', 'const apiKey = "not-a-placeholder-secret-value";');

  assert.deepEqual(findings, ['credential-like assignment']);
});
