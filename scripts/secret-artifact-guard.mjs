import { execFileSync } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';
import process from 'node:process';
import { pathToFileURL } from 'node:url';

const repositoryRoot = execFileSync('git', ['rev-parse', '--show-toplevel'], {
  encoding: 'utf8',
}).trim();

const generatedDirectoryNames = new Set([
  '.git',
  'bin',
  'obj',
  'node_modules',
  'dist',
  'dist-ssr',
  'coverage',
  'test-results',
]);

const secretAssignmentPattern =
  /\b(?:password|passwd|secret|token|api[_-]?key|connectionstring|client[_-]?secret)\b\s*[:=]\s*["']([^"'`${}\r\n]{12,})["']/gi;
const knownTokenPatterns = [
  /\bAKIA[0-9A-Z]{16}\b/,
  /\bgh[pousr]_[A-Za-z0-9_]{20,}\b/,
  /\bgithub_pat_[A-Za-z0-9_]{20,}\b/,
  /\bAIza[0-9A-Za-z_-]{30,}\b/,
  /\bxox[baprs]-[0-9A-Za-z-]{20,}\b/,
  /\bnpm_[A-Za-z0-9]{30,}\b/,
];

function hasGeneratedDirectory(filePath) {
  return filePath.split('/').some(directory => generatedDirectoryNames.has(directory));
}

function isCredentialArtifactPath(filePath) {
  const normalizedPath = filePath.replaceAll('\\', '/').toLowerCase();
  const fileName = path.posix.basename(normalizedPath);

  return (
    fileName === 'credentials.json' ||
    fileName === 'secrets.json' ||
    fileName.startsWith('service-account') ||
    /\.(?:p12|pfx|key|pem)$/.test(fileName)
  );
}

function isDocumentationOrTestPath(filePath) {
  const normalizedPath = filePath.replaceAll('\\', '/').toLowerCase();
  return (
    normalizedPath.startsWith('.github/') ||
    normalizedPath.startsWith('docs/') ||
    normalizedPath.includes('/docs/') ||
    normalizedPath.includes('/test/') ||
    normalizedPath.includes('/tests/') ||
    /(?:^|\/)[^/]*(?:test|tests|spec|specs)(?:\/|$)/.test(normalizedPath) ||
    normalizedPath.includes('/migrations/') ||
    normalizedPath.includes('.test.') ||
    normalizedPath.includes('.spec.') ||
    /\.(?:md|feature|snap)$/.test(normalizedPath)
  );
}

function isScannablePath(filePath) {
  return !hasGeneratedDirectory(filePath);
}

function isPlaceholder(value) {
  return /^(?:change[_-]?me|example|fake|dummy|placeholder|password|secret|token|your[_-]?|test|xxx|replace[_-]?with)/i.test(
    value
  );
}

export function scanContent(filePath, content) {
  const findings = [];

  if (knownTokenPatterns.some(pattern => pattern.test(content))) {
    findings.push('known token format');
  }

  if (!isDocumentationOrTestPath(filePath)) {
    secretAssignmentPattern.lastIndex = 0;
    if ([...content.matchAll(secretAssignmentPattern)].some(match => !isPlaceholder(match[1]))) {
      findings.push('credential-like assignment');
    }
  }

  return [...new Set(findings)];
}

function stagedFiles() {
  return execFileSync('git', ['diff', '--cached', '--name-only', '-z', '--diff-filter=ACMR'], {
    cwd: repositoryRoot,
  })
    .toString('utf8')
    .split('\0')
    .filter(Boolean);
}

function trackedFiles() {
  return execFileSync('git', ['ls-files', '-z'], { cwd: repositoryRoot })
    .toString('utf8')
    .split('\0')
    .filter(Boolean);
}

function readFileContent(filePath, mode) {
  if (mode === 'staged') {
    return execFileSync('git', ['show', `:${filePath}`], { cwd: repositoryRoot });
  }

  return fs.readFileSync(path.join(repositoryRoot, filePath));
}

function reportFinding(filePath, finding) {
  console.error(`Secret/artifact guard: ${finding} detected in ${filePath}.`);
}

export function scanFiles(files, mode) {
  const findings = [];

  for (const filePath of files) {
    if (!isScannablePath(filePath)) {
      continue;
    }

    if (isCredentialArtifactPath(filePath)) {
      findings.push({ filePath, finding: 'credential artifact path' });
      continue;
    }

    let content;
    try {
      content = readFileContent(filePath, mode).toString('utf8');
    } catch {
      findings.push({ filePath, finding: 'unreadable tracked content' });
      continue;
    }

    for (const finding of scanContent(filePath, content)) {
      findings.push({ filePath, finding });
    }

    if (/-----BEGIN (?:RSA|EC|OPENSSH|DSA|PRIVATE) KEY-----/.test(content)) {
      findings.push({ filePath, finding: 'private-key material' });
    }
  }

  return findings;
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  const mode = process.argv.includes('--tracked') ? 'tracked' : 'staged';
  const files = mode === 'tracked' ? trackedFiles() : stagedFiles();
  const findings = scanFiles(files, mode);

  for (const finding of findings) {
    reportFinding(finding.filePath, finding.finding);
  }

  if (findings.length > 0) {
    console.error(
      'Commit blocked. Remove the credential material or use an approved external secret store.'
    );
    process.exit(1);
  }
}
