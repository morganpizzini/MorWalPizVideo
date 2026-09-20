# Formatting Workflow

The repository uses one root pre-commit hook for staged formatting. There is no
app-local Husky hook. The hook is tracked in `.githooks/pre-commit` and routes
only staged, non-deleted files to the formatter that owns their scope:

- Frontend JavaScript, TypeScript, JSX, TSX, CSS, SCSS, JSON, and Markdown
  files under `frontend/` use the Prettier installed by the authoritative
  `frontend` Yarn workspace. Existing app-local Prettier configurations remain
  authoritative for their files. Staged JavaScript, TypeScript, JSX, and TSX
  files in `back-office-spa` and `morwalpizvideo.client` also use their local
  ESLint configuration with `--fix`, invoked from each app directory. After
  formatting and lint fixing, a file with no separate unstaged edits is
  synchronized with the resulting staged content; separate worktree edits are
  preserved untouched. The hook uses Git's normalized worktree/index
  comparison, so line-ending conversion is not mistaken for a separate edit.
- C# files below .NET project directories use `dotnet format whitespace` from
  the repository solution and SDK. XAML is excluded. Because `dotnet format`
  requires worktree files, the hook writes the staged blob to disk, formats
  it, and re-stages the result. A file with no separate unstaged edits is left
  synchronized with the formatted worktree copy; a file with separate
  unstaged edits has its original worktree content restored untouched after
  the staged blob is updated.

Generated directories, lockfiles, secrets, deleted files, and unsupported
paths are skipped. The root hook handles Prettier, app-scoped ESLint `--fix`,
and `dotnet format`. It formats staged blobs directly in the Git index, so
tracked unstaged edits and untracked files are not rewritten or included.

## Installation

From the repository root, after installing frontend dependencies:

```bash
yarn --cwd frontend install
node scripts/setup-git-hooks.mjs
```

The setup command configures `core.hooksPath` for this clone. It is safe to run
again and is required once per clone. The root hook is the repository's only
pre-commit owner; the applications do not use an app-local Husky hook,
`prepare`, or `lint-staged` configuration.

## Bypass and CI

Use `git commit --no-verify` only when the formatting hook cannot run or when a
deliberate exception is being reviewed. Run the setup command again if hooks
are not active. CI builds and tests the existing applications; it does not
format the repository or rewrite files. A bypassed commit can be checked
locally by running the relevant workspace `lint` and `format:check` scripts for
frontend files, and `dotnet format MorWalPizVideo.sln whitespace
--verify-no-changes` for .NET formatting.