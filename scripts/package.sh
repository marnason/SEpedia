#!/usr/bin/env bash
set -euo pipefail

script_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
repo_root=$(cd -- "$script_dir/.." && pwd)
thumbnail_module="github.com/marnason/se-mod-thumbnails/cmd/thumbnail@3e6e2e55b896abe19a7e9fb43776fb58a8cf63ef"

game_bin=""
stage_dir=""
archive_path=""
temporary_ini=""
local_ini_backup=""

usage() {
  cat <<'EOF'
Usage: scripts/package.sh [options]

Build and package SEpedia with MDK.

Options:
  --game-bin PATH  Space Engineers binary directory used by CI.
  --stage PATH     Isolated MDK output directory used by CI.
  --archive PATH   ZIP path to create from the staged SEpedia folder.
  -h, --help       Show this help.

With no options, the script uses the developer's ignored MDK local settings and
deploys normally. CI must supply --game-bin and --stage together. --archive
requires --stage and also creates PATH.sha256.
EOF
}

fail() {
  printf 'package: %s\n' "$*" >&2
  exit 1
}

cleanup() {
  if [[ -n "$temporary_ini" && -f "$temporary_ini" ]]; then
    rm -f -- "$temporary_ini"
  fi
  if [[ -n "$local_ini_backup" && -f "$local_ini_backup" ]]; then
    mv -- "$local_ini_backup" "$repo_root/mdk.local.ini"
  fi
}
trap cleanup EXIT

while (($# > 0)); do
  case "$1" in
    --game-bin)
      (($# >= 2)) || fail "--game-bin requires a path"
      game_bin=$2
      shift 2
      ;;
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

if [[ -n "$game_bin" || -n "$stage_dir" ]]; then
  [[ -n "$game_bin" && -n "$stage_dir" ]] || fail "--game-bin and --stage must be supplied together"
  [[ -d "$game_bin" ]] || fail "game binary directory does not exist: $game_bin"
fi
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
  game_bin=$(cd -- "$game_bin" && pwd)
  [[ ! -e "$stage_dir/SEpedia" ]] || fail "stage already contains a SEpedia package: $stage_dir"

  temporary_ini="$repo_root/mdk.local.ini"
  if [[ -e "$temporary_ini" ]]; then
    backup_candidate=$(mktemp "${TMPDIR:-/tmp}/sepedia-mdk-local.XXXXXX")
    cp -p -- "$temporary_ini" "$backup_candidate"
    local_ini_backup=$backup_candidate
    rm -f -- "$temporary_ini"
  fi
  {
    printf '[mdk]\n'
    printf 'binarypath=%s\n' "$game_bin"
    printf 'output=%s\n' "$stage_dir"
    printf 'interactive=DoNothing\n'
  } > "$temporary_ini"
fi

dotnet restore "$repo_root/SEpedia.sln" --locked-mode
dotnet build "$repo_root/SEpedia.sln" \
  --configuration Release \
  --no-restore \
  -p:MdkBuildConfiguration=Release \
  -p:MdkInteractive=no

if [[ -z "$stage_dir" ]]; then
  exit 0
fi

package_dir="$stage_dir/SEpedia"
[[ -d "$package_dir/Data/Scripts/SEpedia" ]] || fail "MDK package is missing Data/Scripts/SEpedia"
[[ -f "$package_dir/Data/Scripts/SEpedia/SEpediaSession.cs" ]] || fail "MDK package is missing SEpediaSession.cs"
[[ -f "$package_dir/Licenses/RichHudFramework.Client.LICENSE.txt" ]] || fail "MDK package is missing the Rich HUD client license"
[[ -f "$package_dir/thumb.png" ]] || fail "MDK package is missing thumb.png"
cmp --silent "$repo_root/thumb.png" "$package_dir/thumb.png" || fail "packaged thumbnail differs from generated thumb.png"

# MDK uses these files to manage its output folder; the game and Workshop do not.
rm -f -- "$package_dir/mdk.ini" "$package_dir/mdk.meta"

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
if (width, height) != (720, 450):
    raise SystemExit("package: thumb.png must be 720x450, got %dx%d" % (width, height))
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
