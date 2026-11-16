#!/usr/bin/env bash
set -euo pipefail

# The legacy recovery schedule ran on Sunday UTC. Central nightly invokes this
# hook daily; keep the destructive product sweep weekly without filtering tests.
weekday="$(date -u +%u)"
if [[ ! "$weekday" =~ ^[1-7]$ ]]; then
    printf 'Unable to determine the UTC recovery weekday.\n' >&2
    exit 1
fi
if [[ "$weekday" != "7" ]]; then
    printf 'Weekly ChatBot recovery is due on Sunday UTC.\n'
    exit 0
fi

: "${RUNNER_TEMP:?The prepared nightly runner must provide RUNNER_TEMP}"
export HEXALITH_CHATBOT_RECOVERY_RETENTION_FAILURE_DIR="$RUNNER_TEMP/chatbot-recovery-retention-failures"
# Recovery's finally blocks run at the 250-minute in-process deadline, before
# SIGINT at 280 minutes and the 300-minute step limit. The registered job needs
# the separately released 330-minute allowance; do not enable against 45 minutes.
timeout --signal=TERM --kill-after=1m 300m \
    timeout --signal=INT --kill-after=15m 16800s \
    dotnet test --project tests/Hexalith.ChatBot.Recovery.Tests/Hexalith.ChatBot.Recovery.Tests.csproj \
    --configuration Release --no-build --no-restore
