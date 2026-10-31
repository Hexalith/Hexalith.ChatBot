#!/usr/bin/env bash

# Prints the folder of the pinned, published Hexalith.Builds tooling. Under CI, Platform's own build guard refuses
# its source reference to Hexalith.Builds.Tooling and requires the published assembly instead, bound through
# HexalithBuildsPublishedToolRoot. The module tool pinned in .config/dotnet-tools.json ships that assembly.

set -euo pipefail

repository_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)"
manifest="${repository_root}/.config/dotnet-tools.json"

dotnet tool restore --tool-manifest "$manifest" >&2

# Assigned separately so a failing substitution stops the script instead of yielding an empty path.
version="$(jq -er '.tools["hexalith.builds.module.cli"].version' "$manifest")"
packages="$(dotnet nuget locals global-packages --list | sed -n 's/^global-packages: //p')"
if [[ -z "$packages" ]]; then
    printf 'Unable to resolve the NuGet global packages folder.\n' >&2
    exit 1
fi

tool_root="${packages%/}/hexalith.builds.module.cli/${version}/tools/net10.0/any"
if [[ ! -f "${tool_root}/Hexalith.Builds.Tooling.dll" ]]; then
    printf 'The restored module tool %s has no Hexalith.Builds.Tooling.dll under %s.\n' "$version" "$tool_root" >&2
    exit 1
fi

printf '%s\n' "$tool_root"
