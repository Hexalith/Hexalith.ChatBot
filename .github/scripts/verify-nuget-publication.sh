#!/usr/bin/env bash

set -euo pipefail

if (( $# != 1 )); then
    printf 'Usage: %s <version>\n' "$0" >&2
    exit 64
fi

version="$1"
if [[ ! "$version" =~ ^[0-9]+\.[0-9]+\.[0-9]+([-.][0-9A-Za-z.-]+)?$ ]]; then
    printf 'Refusing to verify unsupported version %s.\n' "$version" >&2
    exit 1
fi

package_ids=(
    Hexalith.ChatBot.Contracts
    Hexalith.ChatBot.Client
    Hexalith.ChatBot.Testing
)

deadline=$((SECONDS + 600))
for package_id in "${package_ids[@]}"; do
    package_path="$(printf '%s' "$package_id" | tr '[:upper:]' '[:lower:]')"
    url="https://api.nuget.org/v3-flatcontainer/${package_path}/${version}/${package_path}.${version}.nupkg"
    until curl --fail --silent --show-error --output /dev/null --retry 0 "$url"; do
        if (( SECONDS >= deadline )); then
            printf 'NuGet did not list %s %s within 10 minutes: %s\n' "$package_id" "$version" "$url" >&2
            exit 1
        fi
        printf 'Waiting for %s %s to appear on NuGet.\n' "$package_id" "$version"
        sleep 20
    done
    printf 'Published %s %s.\n' "$package_id" "$version"
done
