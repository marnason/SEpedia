#!/usr/bin/env bash
set -euo pipefail

script_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
repo_root=$(cd -- "$script_dir/.." && pwd)

if rg -n \
  '#pragma[[:space:]]+warning|VRageRender|MyRenderProxy|MyTransparentMaterials|new[[:space:]]+MyTransparentMaterial[[:space:]]*\(' \
  "$repo_root/SEpediaSession.cs" "$repo_root/Core" "$repo_root/UI"; then
  echo "First-party script source contains constructs rejected by the Space Engineers mod compiler." >&2
  exit 1
fi
