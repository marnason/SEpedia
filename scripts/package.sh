#!/usr/bin/env bash
set -euo pipefail

script_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
repo_root=$(cd -- "$script_dir/.." && pwd)
thumbnail_module="github.com/marnason/se-mod-thumbnails/cmd/thumbnail@main"

stage_dir=""
archive_path=""

usage() {
  cat <<'EOF'
Usage: scripts/package.sh [options]

Build or package SEpedia.

Options:
  --stage PATH     Assemble a source-only Workshop package for CI.
  --archive PATH   ZIP path to create from the staged SEpedia folder.
  -h, --help       Show this help.

With no options, the script uses the developer's ignored MDK local settings and
performs a normal MDK Release build/deployment. CI supplies --stage; --archive
requires --stage and also creates PATH.sha256. Source-only packages are compiled
by Space Engineers when the mod is loaded.
EOF
}

fail() {
  printf 'package: %s\n' "$*" >&2
  exit 1
}

while (($# > 0)); do
  case "$1" in
    --stage)
      (($# >= 2)) || fail "--stage requires a path"
      stage_dir=$2
      shift 2
      ;;
    --archive)
      (($# >= 2)) || fail "--archive requires a path"
      archive_path=$2
      shift 2
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      fail "unknown option: $1"
      ;;
  esac
done

if [[ -n "$archive_path" && -z "$stage_dir" ]]; then
  fail "--archive requires --stage"
fi

for command_name in dotnet go; do
  command -v "$command_name" >/dev/null 2>&1 || fail "required command not found: $command_name"
done
if [[ -n "$archive_path" ]]; then
  for command_name in python3 sha256sum unzip zip; do
    command -v "$command_name" >/dev/null 2>&1 || fail "required command not found: $command_name"
  done
fi

"$script_dir/check-script-sandbox.sh"

go run "$thumbnail_module" \
  -text "SEpedia" \
  -background-color "#0000" \
  -padding 100 \
  -output "$repo_root/thumb.png"

if [[ -n "$stage_dir" ]]; then
  mkdir -p -- "$stage_dir"
  stage_dir=$(cd -- "$stage_dir" && pwd)
  [[ ! -e "$stage_dir/SEpedia" ]] || fail "stage already contains a SEpedia package: $stage_dir"
fi

dotnet restore "$repo_root/SEpedia.sln" --locked-mode

if [[ -z "$stage_dir" ]]; then
  dotnet build "$repo_root/SEpedia.sln" \
    --configuration Release \
    --no-restore \
    -p:MdkBuildConfiguration=Release \
    -p:MdkInteractive=no
  exit 0
fi

package_dir="$stage_dir/SEpedia"
scripts_dir="$package_dir/Data/Scripts/SEpedia"
mkdir -p -- "$scripts_dir"

while IFS= read -r -d '' source_file; do
  relative_path=${source_file#./}
  destination="$scripts_dir/$relative_path"
  mkdir -p -- "$(dirname -- "$destination")"
  cp -p -- "$repo_root/$relative_path" "$destination"
done < <(
  cd -- "$repo_root"
  find . \
    \( -type d \( -name '.git' -o -iname 'bin' -o -iname 'obj' \) \) -prune -o \
    -type f -name '*.cs' ! -name '*.debug.cs' -print0
)

if [[ -d "$repo_root/Content" ]]; then
  while IFS= read -r -d '' content_file; do
    relative_path=${content_file#./}
    destination="$package_dir/$relative_path"
    mkdir -p -- "$(dirname -- "$destination")"
    cp -p -- "$repo_root/Content/$relative_path" "$destination"
  done < <(
    cd -- "$repo_root/Content"
    find . -type f ! -name '*.cs' -print0
  )
fi
cp -p -- "$repo_root/thumb.png" "$package_dir/thumb.png"

[[ -d "$package_dir/Data/Scripts/SEpedia" ]] || fail "package is missing Data/Scripts/SEpedia"
[[ -f "$package_dir/Data/Scripts/SEpedia/SEpediaSession.cs" ]] || fail "package is missing SEpediaSession.cs"
[[ -f "$package_dir/Licenses/RichHudFramework.Client.LICENSE.txt" ]] || fail "package is missing the Rich HUD client license"
[[ -f "$package_dir/thumb.png" ]] || fail "package is missing thumb.png"
cmp --silent "$repo_root/thumb.png" "$package_dir/thumb.png" || fail "packaged thumbnail differs from generated thumb.png"

for ownership_file in metadata.mod modinfo.sbmi; do
  [[ ! -e "$package_dir/$ownership_file" ]] || fail "fresh package contains Workshop ownership file: $ownership_file"
done

for forbidden_dir in bin obj; do
  if find "$package_dir" -type d -iname "$forbidden_dir" -print -quit | grep -q .; then
    fail "package contains forbidden directory: $forbidden_dir"
  fi
done

forbidden_file=$(find "$package_dir" -type f \( \
  -iname '*.dll' -o \
  -iname '*.exe' -o \
  -iname '*.pdb' -o \
  -iname '*.so' -o \
  -iname '*.dylib' -o \
  -iname 'mdk.ini' -o \
  -iname 'mdk.meta' -o \
  -iname '*.mdk.local.ini' \
\) -print -quit)
[[ -z "$forbidden_file" ]] || fail "package contains forbidden file: $forbidden_file"

python3 - "$package_dir/thumb.png" <<'PY'
import pathlib
import struct
import sys

thumbnail = pathlib.Path(sys.argv[1])
data = thumbnail.read_bytes()
if len(data) >= 1024 * 1024:
    raise SystemExit("package: thumb.png must be smaller than 1 MiB")
if data[:8] != b"\x89PNG\r\n\x1a\n" or data[12:16] != b"IHDR":
    raise SystemExit("package: thumb.png is not a valid PNG")
width, height = struct.unpack(">II", data[16:24])
if (width, height) != (1280, 720):
    raise SystemExit("package: thumb.png must be 1280x720, got %dx%d" % (width, height))
PY

if [[ -z "$archive_path" ]]; then
  exit 0
fi

archive_dir=$(dirname -- "$archive_path")
archive_name=$(basename -- "$archive_path")
mkdir -p -- "$archive_dir"
archive_dir=$(cd -- "$archive_dir" && pwd)
archive_path="$archive_dir/$archive_name"
checksum_path="$archive_path.sha256"
rm -f -- "$archive_path" "$checksum_path"

(
  cd -- "$stage_dir"
  zip -X -q -r "$archive_path" SEpedia
)
unzip -q -t "$archive_path"
(
  cd -- "$archive_dir"
  sha256sum "$archive_name" > "$archive_name.sha256"
)

printf 'Package: %s\n' "$package_dir"
printf 'Archive: %s\n' "$archive_path"
printf 'Checksum: %s\n' "$checksum_path"
