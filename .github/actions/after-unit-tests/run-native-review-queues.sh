#!/usr/bin/env bash
set -euo pipefail

scratch=$(mktemp -d)
instance="chatbot-review-$(basename "$scratch" | tr '[:upper:].' '[:lower:]-')"
cleanup() {
    if [[ -z "${reuse_endpoint:-}" ]]; then
        docker rm -f "$instance-dapr" "$instance-redis" >/dev/null 2>&1 || true
        docker network rm "$instance" >/dev/null 2>&1 || true
    fi
    rm -rf -- "$scratch"
}
reuse_endpoint=${HEXALITH_CHATBOT_TEST_DAPR_GRPC_ENDPOINT:-}
trap cleanup EXIT
if [[ -z "$reuse_endpoint" ]]; then
    docker network create "$instance" >/dev/null
    docker run -d --network "$instance" --name "$instance-redis" redis:7-alpine >/dev/null
    mkdir -m 0755 "$scratch/components"
    cat > "$scratch/components/store.yaml" <<EOF
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: chatbot-statestore
spec:
  type: state.redis
  version: v1
  metadata:
    - name: redisHost
      value: "$instance-redis:6379"
EOF
    chmod 0644 "$scratch/components/store.yaml"
    docker create --network "$instance" --name "$instance-dapr" -p 127.0.0.1::3500 -p 127.0.0.1::50001 \
        daprio/daprd:1.19.2 ./daprd --app-id chatbot --dapr-listen-addresses 0.0.0.0 --resources-path /components >/dev/null
    docker cp "$scratch/components" "$instance-dapr:/components"
    docker start "$instance-dapr" >/dev/null
    http=$(docker port "$instance-dapr" 3500/tcp)
    curl --retry 30 --retry-delay 1 --retry-all-errors --fail --silent --show-error "http://$http/v1.0/healthz/outbound" >/dev/null
    export HEXALITH_CHATBOT_TEST_DAPR_GRPC_ENDPOINT="http://$(docker port "$instance-dapr" 50001/tcp)"
fi
dotnet tests/Hexalith.ChatBot.IntegrationTests/bin/Release/net11.0/Hexalith.ChatBot.IntegrationTests.dll \
    -class Hexalith.ChatBot.IntegrationTests.NativeReviewQueueTests -result-ctrf "$scratch/results.json"
python3 - "$scratch/results.json" <<'PY'
import json
import sys
with open(sys.argv[1], encoding="utf-8") as report:
    cases = json.load(report)["results"]["tests"]
if len(cases) != 2 or not all(case["status"] == "passed" for case in cases):
    raise SystemExit("Both native review queue cases must pass with zero skips.")
PY
