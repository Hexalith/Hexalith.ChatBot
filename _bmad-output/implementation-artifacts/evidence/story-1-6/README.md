# Owner-service live evidence

[The historical receipt](owner-service-provider-2026-10-25.json) is bound to the exact tested
[producer source](owner-service-provider.cs.txt) and actual invocation by
[its provenance record](owner-service-provider-2026-10-25.provenance.json). The producer SHA-256 is
a2c3e9280085a05b48d104c3f01ab2e657829f6103797f9f19ac3b8c7c855f32.
The receipt bytes remain unchanged; the provenance was recorded after the run.

This is a test-evidence producer copied into a temporary directory, outside the repository.
The project template uses the existing test assembly friendship to call the production registered
owner providers. It references existing Debug DLLs through the supplied ChatBotServerOutput property.
Neither text file participates in the application build.

To reproduce on Linux, use .NET 10, trusted local Aspire certificates, Docker/Dapr, and current
ChatBot Server/AppHost Debug outputs. Start from the ChatBot repository root with its AppHost stopped,
the existing mailbox secret configured, and no external/static owner overrides. The probes expect
the ordinary unprovisioned owner state. They create no grants, tenant records, or owner fixtures.
Leave other AppHosts untouched.

Copy and build the retained source:

~~~bash
repo_root="$(pwd)"
evidence="$repo_root/_bmad-output/implementation-artifacts/evidence/story-1-6"
probe_dir="$(mktemp -d /tmp/chatbot-owner-provider-replay.XXXXXX)"
server_output="$repo_root/src/Hexalith.ChatBot.Server/bin/Debug/net10.0"
cp "$evidence/owner-service-provider.cs.txt" "$probe_dir/Program.cs"
cp "$evidence/owner-service-provider.csproj.txt" "$probe_dir/Probe.csproj"
sha256sum "$probe_dir/Program.cs"
dotnet build "$probe_dir/Probe.csproj" --configuration Debug -m:1 -nodeReuse:false \
    -p:ChatBotServerOutput="$server_output"
~~~

Run against a fresh exact-target AppHost. The subshell stops only the AppHost it starts, including
after probe failure. Runtime snapshots and logs stay in the temporary directory:

~~~bash
(
    set -e
    apphost="src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj"
    trap 'aspire stop --apphost "$apphost" --non-interactive > "$probe_dir/stop.log" 2>&1' EXIT
    aspire start --apphost "$apphost" --non-interactive --no-build --format Json > "$probe_dir/start.json"
    for resource in security memories projects chatbot
    do
        aspire wait "$resource" --apphost "$apphost" --timeout 45 --non-interactive
    done
    aspire describe --apphost "$apphost" --non-interactive --format Json > "$probe_dir/resources.json"
    owner_pid="$(python3 -c 'import json,sys; print(json.load(open(sys.argv[1]))["appHostPid"])' "$probe_dir/start.json")"
    dotnet "$probe_dir/bin/Debug/net10.0/Hexalith.ChatBot.IntegrationTests.dll" \
        "$probe_dir/resources.json" "$owner_pid" > "$probe_dir/probe.log"
    cp /tmp/chatbot-owner-round2-live-token-proof.json "$probe_dir/token-proof.json"
    cp /tmp/chatbot-owner-round2-live-provider.json "$probe_dir/receipt.json"
)
~~~

The unchanged producer writes its two receipt files at the fixed /tmp paths above; a new run replaces
those scratch outputs. Count a run only when its invocation exits zero and writes a fresh receipt.
The owner PID selects the run's owner-marked generated realm; credentials are read there in memory
and never printed or retained in this packet. Retain only sanitized receipts, never generated realms
or runtime snapshots containing environment values.

A Projects 404 demonstrates the preserved denial, not positive native Projects authorization.
This producer does not establish native isolation, owner provisioning completion, or production approval.

The [final post-review receipt](owner-service-provider-final-2026-10-25.json) repeats the live proof
with the final patched Server assembly. Its [provenance](owner-service-provider-final-2026-10-25.provenance.json)
binds the same retained producer, final assembly hash and actual invocation.
The [final verification summary](owner-service-verification-final-2026-10-25.json) reconciles CTRF/TRX
and records 180 affected passes separately from the seven unchanged Folders failures.
