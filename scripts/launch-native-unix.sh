#!/usr/bin/env bash
set -euo pipefail

HERE="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
if [[ "$(uname -s)" == Darwin ]]; then
  mkdir -p "$HOME/Library/Logs/MiniPdm"
  exec >>"$HOME/Library/Logs/MiniPdm/launcher.log" 2>&1
fi
BACKEND_DIR="${PDM_BACKEND_DIR:-$HERE/backend}"
API_PORT="${PDM_API_PORT:-5000}"
export PDM_API_PORT="$API_PORT"
API_URL="http://127.0.0.1:${API_PORT}"
PROJECT_NAME="${PDM_COMPOSE_PROJECT_NAME:-cad}"
STARTUP_TIMEOUT="${PDM_STARTUP_TIMEOUT_SECONDS:-180}"

fail() {
  printf 'Mini-PDM: %s\n' "$*" >&2
  if [[ "$(uname -s)" == Darwin ]] && command -v osascript >/dev/null 2>&1; then
    osascript -e 'display dialog "Mini-PDM could not start. See ~/Library/Logs/MiniPdm/launcher.log for details." buttons {"OK"} with title "Mini-PDM"' >/dev/null 2>&1 || true
  fi
  exit 1
}
if ! command -v docker >/dev/null 2>&1 && [[ "$(uname -s)" == Darwin ]]; then
  for candidate in /usr/local/bin/docker /opt/homebrew/bin/docker /Applications/Docker.app/Contents/Resources/bin/docker; do
    if [[ -x "$candidate" ]]; then PATH="$(dirname "$candidate"):$PATH"; export PATH; break; fi
  done
fi
command -v docker >/dev/null 2>&1 || fail 'Docker is required. Install Docker Desktop or Docker Engine, then retry.'
command -v curl >/dev/null 2>&1 || fail 'curl is required by this launcher.'
if ! docker info >/dev/null 2>&1 && [[ "$(uname -s)" == Darwin ]]; then
  [[ -d /Applications/Docker.app ]] && open -a Docker || true
  for _ in {1..45}; do docker info >/dev/null 2>&1 && break; sleep 2; done
fi
docker info >/dev/null 2>&1 || fail 'Docker is installed but its daemon is unavailable. Start Docker and retry.'
[[ -f "$BACKEND_DIR/docker-compose.yml" ]] || fail "Backend sources are missing: $BACKEND_DIR"

compose_args=(--project-directory "$BACKEND_DIR" -p "$PROJECT_NAME" -f "$BACKEND_DIR/docker-compose.yml" -f "$BACKEND_DIR/compose.native.yml")
[[ -f "$BACKEND_DIR/.env" ]] && compose_args+=(--env-file "$BACKEND_DIR/.env")
docker compose "${compose_args[@]}" up -d --build api || fail 'Could not start the API and its database/migration dependencies. Check Docker output above.'

ready=0
deadline=$((SECONDS + STARTUP_TIMEOUT))
while (( SECONDS < deadline )); do
  if curl --silent --show-error --fail --max-time 2 "$API_URL/health" >/dev/null 2>&1; then ready=1; break; fi
  sleep 1
done
[[ "$ready" == 1 ]] || fail "API did not become healthy within ${STARTUP_TIMEOUT} seconds at $API_URL/health. Check Docker logs with: docker compose --project-directory '$BACKEND_DIR' -p '$PROJECT_NAME' -f '$BACKEND_DIR/docker-compose.yml' -f '$BACKEND_DIR/compose.native.yml' logs api"

unset PDM_BROWSER_PICKER
export PDM_API_BASE_URL="$API_URL"
if [[ -x "$HERE/desktop/MiniPdm.Desktop" ]]; then
  exec "$HERE/desktop/MiniPdm.Desktop" "$@"
elif [[ -x "$HERE/../Resources/desktop/MiniPdm.Desktop" ]]; then
  exec "$HERE/../Resources/desktop/MiniPdm.Desktop" "$@"
elif [[ -n "${PDM_DESKTOP_PROJECT:-}" ]] && command -v dotnet >/dev/null 2>&1; then
  exec dotnet run --project "$PDM_DESKTOP_PROJECT" -c Release -- "$@"
elif [[ -f "$BACKEND_DIR/src/MiniPdm.Desktop/MiniPdm.Desktop.csproj" ]] && command -v dotnet >/dev/null 2>&1; then
  exec dotnet run --project "$BACKEND_DIR/src/MiniPdm.Desktop/MiniPdm.Desktop.csproj" -c Release -- "$@"
else
  fail 'Desktop application executable was not found in this distribution.'
fi
