#!/usr/bin/env bash
set -euo pipefail

script_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
repo_root=$(cd -- "$script_dir/.." && pwd)

"$script_dir/check-script-sandbox.sh"

dotnet restore "$repo_root/SEpedia.sln" --locked-mode
dotnet build "$repo_root/SEpedia.sln" \
  --configuration Debug \
  --no-restore \
  -p:MdkBuildConfiguration=CompileOnly \
  -p:MdkInteractive=no
