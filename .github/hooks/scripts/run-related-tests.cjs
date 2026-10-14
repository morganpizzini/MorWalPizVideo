const { execSync } = require('node:child_process');
const path = require('node:path');
const { fileURLToPath } = require('node:url');

const repositoryRoot = path.resolve(__dirname, '../../..');
const suiteName = process.argv[2];
const dryRun = process.argv.includes('--dry-run');

const suites = {
  backoffice: {
    workflow: '.github/workflows/deploy-backoffice-api.yml',
    paths: [
      'MorWalPizVideo.BackOffice/**',
      'MorWalPizVideo.Domain/**',
      'MorWalPizVideo.Models/**',
      'MorWalPizVideo.MvcHelpers/**',
      'MorWalPizVideo.ServiceDefaults/**',
      'MorWalPizVideo.YouTubeUtilities/**',
      'MorWalPiz.Contracts/**',
      '.github/workflows/deploy-backoffice-api.yml',
    ],
    commands: [
      'dotnet test MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --configuration Release --no-restore --filter "Category=TestGroup:BackOffice|Category=TestGroup:Shared|Category=TestGroup:CrossApi" --verbosity minimal',
      'dotnet test MorWalPizVideo.YouTubeUtilities.Tests/MorWalPizVideo.YouTubeUtilities.Tests.csproj --configuration Release --no-restore --verbosity minimal',
    ],
  },
  serverapi: {
    workflow: '.github/workflows/deploy-serverapi.yml',
    paths: [
      'MorWalPizVideo.ServerAPI/**',
      'MorWalPizVideo.Domain/**',
      'MorWalPizVideo.Models/**',
      'MorWalPizVideo.MvcHelpers/**',
      'MorWalPizVideo.ServiceDefaults/**',
      'MorWalPizVideo.YouTubeUtilities/**',
      'MorWalPiz.Contracts/**',
      '.github/workflows/deploy-serverapi.yml',
    ],
    commands: [
      'dotnet test MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --configuration Release --no-restore --filter "Category=TestGroup:ServerAPI|Category=TestGroup:Shared|Category=TestGroup:CrossApi" --verbosity minimal',
      'dotnet test MorWalPizVideo.YouTubeUtilities.Tests/MorWalPizVideo.YouTubeUtilities.Tests.csproj --configuration Release --no-restore --verbosity minimal',
    ],
  },
  'back-office-spa': {
    workflow: '.github/workflows/deploy-back-office-spa.yml',
    paths: [
      'frontend/back-office-spa/**',
      'frontend/fe-packages/**',
      'frontend/package.json',
      'frontend/yarn.lock',
      '.github/workflows/deploy-back-office-spa.yml',
    ],
    commands: [
      'yarn --cwd frontend build:shared',
      'yarn --cwd frontend/back-office-spa test',
      'yarn --cwd frontend workspace @morwalpizvideo/services test --passWithNoTests=false --reporter=junit --outputFile=test-results/services-junit.xml',
    ],
  },
  'morwalpizvideo-client': {
    workflow: '.github/workflows/deploy-morwalpizvideo-client.yml',
    paths: [
      'frontend/morwalpizvideo.client/**',
      'frontend/fe-packages/**',
      'frontend/package.json',
      'frontend/yarn.lock',
      '.github/workflows/deploy-morwalpizvideo-client.yml',
    ],
    commands: [
      'yarn --cwd frontend build:shared',
      'yarn --cwd frontend workspace @morwalpizvideo/services test --passWithNoTests=false --reporter=junit --outputFile=test-results/services-junit.xml',
      'yarn --cwd frontend workspace morwalpizvideo.client test --passWithNoTests=false --reporter=junit --outputFile=test-results/morwalpizvideo-client-junit.xml',
    ],
  },
};

function normalizePath(value) {
  let candidate = value.trim();
  if (!candidate) return null;

  if (candidate.startsWith('file://')) {
    try {
      candidate = fileURLToPath(candidate);
    } catch {
      return null;
    }
  }

  const absolutePath = path.isAbsolute(candidate)
    ? path.resolve(candidate)
    : path.resolve(repositoryRoot, candidate);
  const relativePath = path.relative(repositoryRoot, absolutePath);
  if (relativePath === '..' || relativePath.startsWith(`..${path.sep}`) || path.isAbsolute(relativePath)) {
    return null;
  }

  return relativePath.split(path.sep).join('/');
}

function collectPatchPaths(value, paths) {
  const patchPathPattern = /^\*\*\* (?:Update|Add|Delete) File:\s*(.+?)\s*$/gm;
  for (const match of value.matchAll(patchPathPattern)) {
    const normalized = normalizePath(match[1]);
    if (normalized) paths.add(normalized);
  }
}

function collectChangedPaths(value, paths, key = '') {
  if (typeof value === 'string') {
    const normalizedKey = key.toLowerCase();
    if (['filepath', 'file_path', 'path', 'uri', 'filename', 'file_name'].includes(normalizedKey)) {
      const normalized = normalizePath(value);
      if (normalized) paths.add(normalized);
    }
    if (['input', 'patch', 'diff'].includes(normalizedKey)) collectPatchPaths(value, paths);
    return;
  }

  if (Array.isArray(value)) {
    for (const item of value) collectChangedPaths(item, paths, key);
    return;
  }

  if (value && typeof value === 'object') {
    for (const [childKey, childValue] of Object.entries(value)) {
      collectChangedPaths(childValue, paths, childKey);
    }
  }
}

function pathMatchesPattern(filePath, pattern) {
  if (pattern.endsWith('/**')) return filePath.startsWith(pattern.slice(0, -2));
  return filePath === pattern;
}

async function readEvent() {
  let input = '';
  for await (const chunk of process.stdin) input += chunk;
  return JSON.parse(input);
}

async function main() {
  const suite = suites[suiteName];
  if (!suite) throw new Error(`Unknown test suite: ${suiteName || '(missing)'}`);

  const event = await readEvent();
  const toolInput = event.toolArgs ?? event.tool_input ?? event.toolInput ?? event;
  const changedPaths = new Set();
  collectChangedPaths(toolInput, changedPaths);
  if (typeof toolInput === 'string') collectPatchPaths(toolInput, changedPaths);

  const matchedPaths = [...changedPaths].filter((filePath) =>
    suite.paths.some((pattern) => pathMatchesPattern(filePath, pattern)),
  );

  if (matchedPaths.length === 0) {
    console.log(`Skipping ${suiteName}: no changed path matches ${suite.workflow}.`);
    return;
  }

  console.log(`Running ${suiteName} tests for: ${matchedPaths.join(', ')}`);
  for (const command of suite.commands) {
    console.log(`> ${command}`);
    if (!dryRun) execSync(command, { cwd: repositoryRoot, stdio: 'inherit' });
  }
}

main().catch((error) => {
  console.error(error.message);
  process.exitCode = 1;
});