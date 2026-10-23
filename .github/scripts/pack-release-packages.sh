#!/usr/bin/env bash

set -euo pipefail

if (( $# != 2 )); then
    printf 'Usage: %s <output-directory> <version>\n' "$0" >&2
    exit 64
fi

output_directory="$1"
version="$2"
repository_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)"

if [[ ! "$version" =~ ^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(-[0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*)?$ ]]; then
    printf 'Refusing to pack unsupported version %s.\n' "$version" >&2
    exit 1
fi

mkdir -p -- "$output_directory"
find "$output_directory" -maxdepth 1 -type f \( -name '*.nupkg' -o -name '*.snupkg' \) -delete

cd -- "$repository_root"
package_inventory="$(jq -er '.inventory.packages | select(length == 3) | .[] | [.id, .project] | @tsv' release-metadata.json)"
file_version="${version%%-*}"
while IFS=$'\t' read -r package_id project; do
    dotnet pack "$project" \
        --configuration Release \
        --output "$output_directory" \
        -p:UseHexalithProjectReferences=false \
        -p:Version="$version" \
        -p:FileVersion="$file_version" \
        -p:IncludeSymbols=true \
        -p:SymbolPackageFormat=snupkg \
        -p:ContinuousIntegrationBuild=true \
        -m:1 \
        /nr:false

    for extension in nupkg snupkg; do
        if [[ ! -s "$output_directory/$package_id.$version.$extension" ]]; then
            printf 'Missing declared release artifact %s %s (%s).\n' "$package_id" "$version" "$extension" >&2
            exit 1
        fi
    done
done <<< "$package_inventory"

mapfile -t packages < <(find "$output_directory" -maxdepth 1 -type f -name '*.nupkg' | sort)
mapfile -t symbols < <(find "$output_directory" -maxdepth 1 -type f -name '*.snupkg' | sort)
if (( ${#packages[@]} != 3 || ${#symbols[@]} != 3 )); then
    printf 'Expected three ChatBot packages and three symbol packages, found %s and %s.\n' \
        "${#packages[@]}" "${#symbols[@]}" >&2
    exit 1
fi
