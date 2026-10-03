#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
PDM_BACKEND_DIR="${PDM_BACKEND_DIR:-$ROOT}"
export PDM_BACKEND_DIR
PDM_DESKTOP_PROJECT="${PDM_DESKTOP_PROJECT:-$ROOT/src/MiniPdm.Desktop/MiniPdm.Desktop.csproj}"
export PDM_DESKTOP_PROJECT
exec "$ROOT/scripts/launch-native-unix.sh" "$@"
