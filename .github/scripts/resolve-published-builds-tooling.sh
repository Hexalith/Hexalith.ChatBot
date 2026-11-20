#!/usr/bin/env bash
set -euo pipefail
root=$(cd "$(dirname "$0")/../.." && pwd)
cd "$root"
dotnet tool restore --tool-manifest .config/dotnet-tools.json >&2
tool_root=$(dotnet msbuild eng/ChatBot.Composition.props -nologo -p:UseHexalithProjectReferences=false -getProperty:_ChatBotPublishedToolRoot)
if [[ ! -f "$tool_root/Hexalith.Builds.Module.Cli.dll" || ! -f "$tool_root/Hexalith.Builds.Tooling.dll" ]]; then
  echo 'The restored Builds package is missing its .NET 11 CLI or tooling assembly.' >&2
  exit 1
fi
printf '%s\n' "$tool_root"
