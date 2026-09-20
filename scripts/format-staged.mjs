import { execFileSync, spawnSync } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';
import { pathToFileURL } from 'node:url';
import process from 'node:process';

const repositoryRoot = execFileSync('git', ['rev-parse', '--show-toplevel'], { encoding: 'utf8' }).trim();
const solutionPath = path.join(repositoryRoot, 'MorWalPizVideo.sln');
const frontendRoot = path.join(repositoryRoot, 'frontend');
const formatterPath = path.join(frontendRoot, 'node_modules', 'prettier', 'index.mjs');
const eslintPath = path.join(frontendRoot, 'node_modules', 'eslint', 'bin', 'eslint.js');
const eslintProjectRoots = new Map([
  ['frontend/back-office-spa', path.join(frontendRoot, 'back-office-spa')],
  ['frontend/morwalpizvideo.client', path.join(frontendRoot, 'morwalpizvideo.client')],
]);
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

function run(command, args, options = {}) {
  const result = spawnSync(command, args, {
    cwd: repositoryRoot,
    encoding: 'utf8',
    stdio: 'inherit',
    ...options,
  });

  if (result.error) {
    throw result.error;
  }

  if (result.status !== 0) {
    throw new Error(`${command} exited with code ${result.status ?? 'unknown'}`);
  }
}

function stagedFiles() {
  const output = execFileSync(
    'git',
    ['diff', '--cached', '--name-only', '-z', '--diff-filter=ACMR'],
    { cwd: repositoryRoot }
  );

  return output.toString('utf8').split('\0').filter(Boolean);
}

function stagedContent(filePath) {
  return execFileSync('git', ['show', `:${filePath}`], { cwd: repositoryRoot });
}

function hasSeparateUnstagedEdits(filePath) {
  const result = spawnSync('git', ['diff', '--quiet', '--no-ext-diff', '--', filePath], {
    cwd: repositoryRoot,
    stdio: 'ignore',
  });

  if (result.error) {
    throw result.error;
  }

  if (result.status === 0) {
    return false;
  }

  if (result.status === 1) {
    return true;
  }

  throw new Error(`git diff exited with code ${result.status ?? 'unknown'}`);
}

function updateStagedFile(filePath, content) {
  const stageEntry = execFileSync('git', ['ls-files', '--stage', '--', filePath], {
    cwd: repositoryRoot,
    encoding: 'utf8',
  }).trim();
  const mode = stageEntry ? stageEntry.split(/\s+/)[0] : '100644';
  const objectId = execFileSync('git', ['hash-object', '-w', '--stdin'], {
    cwd: repositoryRoot,
    input: content,
    encoding: 'utf8',
  }).trim();

  run('git', ['update-index', '--add', '--cacheinfo', `${mode},${objectId},${filePath}`]);
}

function hasGeneratedDirectory(filePath) {
  return filePath.split('/').some(directory => generatedDirectoryNames.has(directory));
}

function isSecretFile(filePath) {
  const fileName = path.posix.basename(filePath).toLowerCase();
  return (
    fileName === '.env' ||
    fileName.startsWith('.env.') ||
    fileName === 'credentials.json' ||
    fileName === 'secrets.json'
  );
}

function isFrontendFile(filePath) {
  if (!filePath.startsWith('frontend/') || hasGeneratedDirectory(filePath) || isSecretFile(filePath)) {
    return false;
  }

  const fileName = path.posix.basename(filePath).toLowerCase();
  if (fileName === 'yarn.lock' || fileName === 'package-lock.json') {
    return false;
  }

  return ['.js', '.jsx', '.ts', '.tsx', '.css', '.scss', '.json', '.md'].some(extension =>
    fileName.endsWith(extension)
  );
}

function eslintProjectRoot(filePath) {
  for (const [relativeRoot, projectRoot] of eslintProjectRoots) {
    if (filePath.startsWith(`${relativeRoot}/`)) {
      return projectRoot;
    }
  }

  return undefined;
}

function isEslintFile(filePath) {
  return (
    eslintProjectRoot(filePath) !== undefined &&
    ['.js', '.jsx', '.ts', '.tsx'].some(extension => filePath.toLowerCase().endsWith(extension))
  );
}

function isDotnetFile(filePath) {
  if (!filePath.endsWith('.cs') || hasGeneratedDirectory(filePath) || isSecretFile(filePath)) {
    return false;
  }

  let directory = path.dirname(path.join(repositoryRoot, filePath));
  while (directory.startsWith(repositoryRoot)) {
    if (fs.readdirSync(directory).some(entry => entry.endsWith('.csproj'))) {
      return true;
    }

    const parent = path.dirname(directory);
    if (parent === directory) {
      break;
    }
    directory = parent;
  }

  return false;
}

async function formatFrontendFiles(files) {
  const prettier = await import(pathToFileURL(formatterPath).href);
  const backups = files.map(filePath => {
    const absolutePath = path.join(repositoryRoot, filePath);
    const existed = fs.existsSync(absolutePath);
    const content = existed ? fs.readFileSync(absolutePath) : undefined;
    const hasUnstagedEdits = hasSeparateUnstagedEdits(filePath);
    return { absolutePath, content, existed, hasUnstagedEdits };
  });

  const formattedContents = new Map();
  for (const filePath of files) {
    const source = stagedContent(filePath).toString('utf8');
    const config = (await prettier.resolveConfig(path.join(repositoryRoot, filePath))) ?? {};
    formattedContents.set(filePath, await prettier.format(source, {
      ...config,
      filepath: path.join(repositoryRoot, filePath),
    }));
  }

  const eslintFilesByProject = new Map();
  for (const filePath of files) {
    const projectRoot = eslintProjectRoot(filePath);
    if (projectRoot === undefined || !isEslintFile(filePath)) {
      continue;
    }

    const projectFiles = eslintFilesByProject.get(projectRoot) ?? [];
    projectFiles.push(filePath);
    eslintFilesByProject.set(projectRoot, projectFiles);
  }

  let succeeded = false;
  try {
    for (const filePath of files) {
      if (isEslintFile(filePath)) {
        fs.writeFileSync(path.join(repositoryRoot, filePath), formattedContents.get(filePath));
      }
    }

    if (eslintFilesByProject.size > 0) {
      if (!fs.existsSync(eslintPath)) {
        throw new Error('ESLint is not installed. Run `yarn --cwd frontend install`.');
      }

      for (const [projectRoot, projectFiles] of eslintFilesByProject) {
        run(
          process.execPath,
          [
            eslintPath,
            '--fix',
            ...projectFiles.map(filePath => path.relative(projectRoot, path.join(repositoryRoot, filePath))),
          ],
          { cwd: projectRoot }
        );
      }
    }

    for (const filePath of files) {
      const absolutePath = path.join(repositoryRoot, filePath);
      const content = isEslintFile(filePath)
        ? fs.readFileSync(absolutePath)
        : formattedContents.get(filePath);
      updateStagedFile(filePath, content);

      const backup = backups.find(item => item.absolutePath === absolutePath);
      if (!backup.hasUnstagedEdits) {
        fs.writeFileSync(absolutePath, content);
      }
    }

    succeeded = true;
  } finally {
    for (const backup of backups) {
      if (succeeded && !backup.hasUnstagedEdits) {
        continue;
      }

      if (backup.existed) {
        fs.writeFileSync(backup.absolutePath, backup.content);
      } else if (fs.existsSync(backup.absolutePath)) {
        fs.unlinkSync(backup.absolutePath);
      }
    }
  }
}

function formatDotnetFiles(files) {
  const backups = files.map(filePath => {
    const absolutePath = path.join(repositoryRoot, filePath);
    const existed = fs.existsSync(absolutePath);
    const content = existed ? fs.readFileSync(absolutePath) : undefined;
    const hasUnstagedEdits = hasSeparateUnstagedEdits(filePath);
    return { absolutePath, existed, content, hasUnstagedEdits };
  });

  let succeeded = false;
  try {
    for (const filePath of files) {
      fs.writeFileSync(path.join(repositoryRoot, filePath), stagedContent(filePath));
    }

    run('dotnet', ['format', solutionPath, 'whitespace', '--include', ...files]);

    for (const filePath of files) {
      updateStagedFile(filePath, fs.readFileSync(path.join(repositoryRoot, filePath)));
    }

    succeeded = true;
  } finally {
    for (const backup of backups) {
      if (succeeded && !backup.hasUnstagedEdits) {
        continue;
      }

      if (backup.existed) {
        fs.writeFileSync(backup.absolutePath, backup.content);
      } else if (fs.existsSync(backup.absolutePath)) {
        fs.unlinkSync(backup.absolutePath);
      }
    }
  }
}

async function formatFiles(files) {
  const frontendFiles = files.filter(isFrontendFile);
  const dotnetFiles = files.filter(isDotnetFile);

  if (frontendFiles.length > 0) {
    if (!fs.existsSync(formatterPath)) {
      throw new Error('Prettier is not installed. Run `yarn --cwd frontend install`.');
    }
    await formatFrontendFiles(frontendFiles);
  }

  if (dotnetFiles.length > 0) {
    formatDotnetFiles(dotnetFiles);
  }
}

const files = stagedFiles();
const formatFilesToProcess = files.filter(filePath => isFrontendFile(filePath) || isDotnetFile(filePath));

if (formatFilesToProcess.length === 0) {
  process.exit(0);
}

await formatFiles(formatFilesToProcess);