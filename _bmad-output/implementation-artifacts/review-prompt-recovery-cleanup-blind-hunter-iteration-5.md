Conduct a review of CONTENT.
Look for what's missing, not only what's wrong.
Find at least ten issues to fix or improve.
Output a Markdown list of findings only — no severity, priority, or ranking.
If the content is empty, stop and say so.
If you have zero findings, re-check and keep thinking; do not stop with an empty list.

CONTENT:
The complete change set in /home/administrator/projects/hexalith/chatbot since baseline commit 9fb71f24bbb9148eb6a406f889e046118c81e491, covering tracked and untracked files as they existed when this prompt was generated. Inspect tracked changes with `git diff --no-ext-diff 9fb71f24bbb9148eb6a406f889e046118c81e491 -- .` and `git status --short`. Read these untracked implementation files completely: `tests/Hexalith.ChatBot.IntegrationTests/Recovery/RecoveryDurableStateMessageHandler.cs`, `tests/Hexalith.ChatBot.IntegrationTests/Recovery/RecoverySandboxOperationsTestSeam.cs`, and `tests/Hexalith.ChatBot.IntegrationTests/Recovery/ScopedOutageOperationsTestSeam.cs`. The governing implementation spec is `_bmad-output/implementation-artifacts/spec-recovery-cleanup-state-isolation.md`; review the implementation and verification against it, while recognizing that `references/Hexalith.Builds` is a pre-existing unrelated user worktree change. Exclude the three `review-prompt-recovery-cleanup-*-iteration-5.md` workflow handoff files, which were created only after the review content was frozen.

Do not invoke any skill. Return only the review result.
