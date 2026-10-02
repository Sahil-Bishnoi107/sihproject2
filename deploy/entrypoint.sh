#!/bin/sh
set -e

# Hosts such as Render, Railway, Heroku and Neon hand out postgres:// URLs; translate to an Npgsql connection string.
if [ -z "$ConnectionStrings__Database" ] && [ -n "$DATABASE_URL" ]; then
  ConnectionStrings__Database="$(/opt/ai-venv/bin/python - <<'PY'
import os
from urllib.parse import urlparse, parse_qs, unquote
u = urlparse(os.environ["DATABASE_URL"])
q = parse_qs(u.query)
ssl = q.get("sslmode", ["prefer"])[0]
ssl = {"require": "Require", "verify-full": "VerifyFull", "verify-ca": "VerifyCA", "disable": "Disable"}.get(ssl, "Prefer")
print(f"Host={u.hostname};Port={u.port or 5432};Database={u.path.lstrip('/')};"
      f"Username={unquote(u.username or '')};Password={unquote(u.password or '')};SSL Mode={ssl}")
PY
)"
  export ConnectionStrings__Database
fi

# Database and JWT settings come from appsettings.json unless overridden by environment variables.

# Render the nginx config for the port the platform assigns (nginx itself cannot read env vars).
envsubst '${PORT}' < /etc/bidguard/nginx.conf.template > /tmp/nginx.conf

exec /usr/bin/supervisord -c /etc/bidguard/supervisord.conf
