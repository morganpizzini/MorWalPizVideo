# Formatting Workflow

The repository uses one root pre-commit hook for staged formatting. The hook is
tracked in `.githooks/pre-commit` and routes only staged, non-deleted files to
the formatter that owns their scope:

- Frontend JavaScript, TypeScript, JSX, TSX, CSS, SCSS, JSON, and Markdown
  files under `frontend/` use the Prettier installed by the authoritative
  `frontend` Yarn workspace. Existing app-local Prettier configurations remain
  authoritative for their files.
- C# files below .NET project directories use `dotnet format whitespace` from
  the repository solution and SDK. XAML is excluded.

Generated directories, lockfiles, secrets, deleted files, and unsupported
paths are skipped. The hook formats staged blobs directly in the Git index, so
tracked unstaged edits and untracked files are not rewritten or included.

## Installation

From the repository root, after installing frontend dependencies:

```bash
yarn --cwd frontend install
node scripts/setup-git-hooks.mjs
```

The setup command configures `core.hooksPath` for this clone. It is safe to run
again and is required once per clone. The existing `frontend/back-office-spa`
Husky hook remains app-local; the root hook is used for repository-wide scope.

## Bypass and CI

Use `git commit --no-verify` only when the formatting hook cannot run or when a
deliberate exception is being reviewed. Run the setup command again if hooks
are not active. CI builds and tests the existing applications; it does not
format the repository or rewrite files. A bypassed commit can be checked
locally by running `yarn --cwd frontend prettier --check` for frontend files
and `dotnet format MorWalPizVideo.sln whitespace --verify-no-changes` for .NET
formatting.