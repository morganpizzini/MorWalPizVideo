#!/bin/sh
set -e

# Create env-config.js with runtime environment variables
cat > /usr/share/nginx/html/env-config.js << EOF
window.ENV = {
  VITE_API_BASE_URL: '${VITE_API_BASE_URL:-}',
  API_BASE_URL: '${API_BASE_URL:-}',
  REACT_APP_API_URL: '${REACT_APP_API_URL:-}'
};
EOF

echo "Environment configuration created:"
cat /usr/share/nginx/html/env-config.js

# Execute the command passed to the script
exec "$@"