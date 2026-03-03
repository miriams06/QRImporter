#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT_DIR"

# Procura URLs/hosts locais apenas quando aparecem como string literal entre aspas.
PATTERN='"[^"\n]*(https?://|\b(localhost|127\.0\.0\.1)\b)[^"\n]*"'

EXCLUDES=(
  "--glob=!**/bin/**"
  "--glob=!**/obj/**"
  "--glob=!**/.git/**"
  "--glob=!Importer.Client/wwwroot/css/bootstrap/**"
  "--glob=!Importer.Client/wwwroot/pdfjs/**"
  "--glob=!Importer.Client/wwwroot/workers/lib/**"
  "--glob=!Importer.Client/Layout/NavMenu.razor.css"
  "--glob=!**/Properties/launchSettings.json"
  "--glob=!Importer.Api/Importer.Api.http"
  "--glob=!**/*.md"
  "--glob=!**/appsettings.Development.json"
)

TARGETS=(Importer.Client Importer.Api Importer.Core)

set +e
RESULT="$(rg -n "$PATTERN" "${TARGETS[@]}" "${EXCLUDES[@]}")"
STATUS=$?
set -e

if [[ $STATUS -eq 1 ]]; then
  echo "✅ Sem URLs/endpoints hardcoded fora da allowlist."
  exit 0
fi

if [[ $STATUS -ne 0 ]]; then
  echo "❌ Erro ao executar verificação com ripgrep."
  exit $STATUS
fi

echo "❌ Foram encontrados potenciais hardcodes de URL/endpoints:"
echo "$RESULT"
exit 1
