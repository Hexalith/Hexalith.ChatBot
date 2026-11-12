#!/usr/bin/env bash

set -euo pipefail

readonly dapr_version="1.19.1"
readonly dapr_sha256="6e9a339d261e430e5914909cc4443572573c197c174c3e14504c34116831de3b"
readonly archive="${RUNNER_TEMP:?RUNNER_TEMP is required}/dapr_linux_amd64-${dapr_version}.tar.gz"
readonly install_directory="${RUNNER_TEMP}/dapr-cli-${dapr_version}"

curl --fail --show-error --silent --location \
  --proto '=https' --tlsv1.2 \
  "https://github.com/dapr/cli/releases/download/v${dapr_version}/dapr_linux_amd64.tar.gz" \
  --output "$archive"
printf '%s  %s\n' "$dapr_sha256" "$archive" | sha256sum --check --strict
mkdir -p "$install_directory"
tar -xzf "$archive" -C "$install_directory" dapr
printf '%s\n' "$install_directory" >> "${GITHUB_PATH:?GITHUB_PATH is required}"
"$install_directory/dapr" --version
