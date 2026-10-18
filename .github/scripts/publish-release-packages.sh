#!/usr/bin/env bash

set -euo pipefail

if (( $# != 1 )); then
    printf 'Usage: %s <package-directory>\n' "$0" >&2
    exit 64
fi

package_directory="$1"
if [[ -z "${NUGET_API_KEY:-}" ]]; then
    printf 'NUGET_API_KEY is required to publish packages.\n' >&2
    exit 1
fi

mapfile -t packages < <(find "$package_directory" -maxdepth 1 -type f \( -name '*.nupkg' -o -name '*.snupkg' \) | sort)
if (( ${#packages[@]} == 0 )); then
    printf 'No NuGet packages found in %s.\n' "$package_directory" >&2
    exit 1
fi

for package in "${packages[@]}"; do
    dotnet nuget push "$package" \
        --source https://api.nuget.org/v3/index.json \
        --api-key "$NUGET_API_KEY" \
        --skip-duplicate
done
