#!/usr/bin/env bash
set -euo pipefail

corepack enable
corepack prepare yarn@1.22.22 --activate
yarn --cwd frontend install --frozen-lockfile

playwright_cli="$PWD/frontend/node_modules/.bin/playwright"
if [[ ! -x "$playwright_cli" ]]; then
  echo "Playwright CLI was not installed from frontend/yarn.lock." >&2
  exit 1
fi

playwright_version="$("$playwright_cli" --version)"
echo "Installing system dependencies and Chromium for $playwright_version."
sudo env "PATH=$PATH" "$playwright_cli" install-deps chromium
"$playwright_cli" install chromium
dotnet dev-certs https
