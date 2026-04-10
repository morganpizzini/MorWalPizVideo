#!/bin/sh

# Replace environment variables in JavaScript files
# This allows runtime configuration without rebuilding the image

echo "Injecting runtime environment variables..."

# Find all JavaScript files in the dist directory
for file in /usr/share/nginx/html/assets/*.js; do
  if [ -f "$file" ]; then
    # Replace placeholder with actual environment variable
    # Format in code should be: window.__RUNTIME_CONFIG__ = { API_URL: 'VITE_API_URL_PLACEHOLDER' }
    if [ ! -z "$VITE_API_URL" ]; then
      sed -i "s|VITE_API_URL_PLACEHOLDER|$VITE_API_URL|g" "$file"
    fi
    
    if [ ! -z "$VITE_RECAPTCHA_SITE_KEY" ]; then
      sed -i "s|VITE_RECAPTCHA_SITE_KEY_PLACEHOLDER|$VITE_RECAPTCHA_SITE_KEY|g" "$file"
    fi
  fi
done

echo "Environment variables injected successfully"

# Execute the main container command
exec "$@"