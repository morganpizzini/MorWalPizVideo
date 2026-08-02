#!/bin/sh
set -e

# Create env-config.js with runtime environment variables (loaded by src/main.tsx before mount)
cat > /usr/share/nginx/html/env-config.js << EOF
window.ENV = {
  VITE_API_BASE_URL: '${VITE_API_BASE_URL:-}',
  VITE_RECAPTCHA_KEY: '${VITE_RECAPTCHA_KEY:-}'
};
EOF

echo "Environment configuration created:"
cat /usr/share/nginx/html/env-config.js

# Execute the main container command
exec "$@"