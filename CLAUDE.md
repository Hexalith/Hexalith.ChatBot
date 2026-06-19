## Shared Hexalith LLM Instructions

Before starting any work in this repository, read and follow
[`Hexalith.AI.Tools/hexalith-llm-instructions.md`](./Hexalith.AI.Tools/hexalith-llm-instructions.md).

## Git Submodules

- Initialize or update only submodules declared in the repository root `.gitmodules` file.
- Never initialize or update nested submodules unless the user explicitly asks for nested submodules.
- Do not use recursive submodule commands such as `git submodule update --init --recursive` or `git submodule foreach --recursive`.
- Prefer non-recursive root-level initialization, for example `git submodule update --init` from the repository root.
