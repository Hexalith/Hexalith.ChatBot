#!/usr/bin/env bash
set -euo pipefail

# Keep the legacy 30-minute release allowance separate from recovery's timers.
# SIGINT gives the topology one minute to restore/close before the hard bound.
timeout --signal=INT --kill-after=1m 29m \
    dotnet test --project tests/Hexalith.ChatBot.Release.Tests/Hexalith.ChatBot.Release.Tests.csproj \
    --configuration Release --no-build --no-restore
