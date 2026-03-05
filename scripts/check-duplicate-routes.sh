#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT_DIR"

python - <<'PY'
from pathlib import Path
import re
from collections import defaultdict

pages_dir = Path('Importer.Client/Pages')
route_re = re.compile(r'^\s*@page\s+"([^"]+)"')

routes = defaultdict(list)
for file in sorted(pages_dir.glob('*.razor')):
    for i, line in enumerate(file.read_text(encoding='utf-8').splitlines(), start=1):
        m = route_re.match(line)
        if m:
            route = m.group(1)
            routes[route].append((str(file).replace('\\','/'), i))

duplicates = {r: hits for r, hits in routes.items() if len(hits) > 1}
if duplicates:
    print('❌ Rotas duplicadas encontradas:')
    for route, hits in sorted(duplicates.items()):
        print(f'  - "{route}"')
        for f, ln in hits:
            print(f'      {f}:{ln}')
    raise SystemExit(1)

print('✅ Sem rotas duplicadas em Importer.Client/Pages.')
PY
