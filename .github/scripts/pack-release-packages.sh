#!/usr/bin/env bash

set -euo pipefail

if (( $# != 2 )); then
    printf 'Usage: %s <output-directory> <version>\n' "$0" >&2
    exit 64
fi

output_directory="$1"
version="$2"
repository_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)"

if [[ ! "$version" =~ ^[0-9]+\.[0-9]+\.[0-9]+([-.][0-9A-Za-z.-]+)?$ ]]; then
    printf 'Refusing to pack unsupported version %s.\n' "$version" >&2
    exit 1
fi

mkdir -p -- "$output_directory"
find "$output_directory" -maxdepth 1 -type f \( -name '*.nupkg' -o -name '*.snupkg' \) -delete

cd -- "$repository_root"
dotnet pack Hexalith.ChatBot.slnx \
    --configuration Release \
    --output "$output_directory" \
    -p:UseHexalithProjectReferences=false \
    -p:Version="$version" \
    -p:FileVersion="$version" \
    -p:IncludeSymbols=true \
    -p:SymbolPackageFormat=snupkg \
    -p:ContinuousIntegrationBuild=true \
    -m:1 \
    /nr:false

mapfile -t packages < <(find "$output_directory" -maxdepth 1 -type f -name '*.nupkg' | sort)
if (( ${#packages[@]} != 3 )); then
    printf 'Expected the three packable ChatBot packages, found %s.\n' "${#packages[@]}" >&2
    exit 1
fi
