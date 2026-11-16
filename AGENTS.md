# AGENTS.md

<!-- harness:managed v1 sha256:4748970e5308f31520e3ca033395a1248dd88a993327b29cc9a56f36b9784f50 -->
## Working Agreement

- Keep exactly one task `in_progress`. Do not start a second task until the first has passing acceptance evidence.
- Before editing, state the task's in-scope paths, out-of-scope work, and acceptance command.
- A change outside the declared scope is a scope violation: revert it or split it into a new task.
- Done means the acceptance command passed and its output is recorded. Code inspection is not evidence.
- A one-file edit may skip the task card, but must still state its acceptance check in the commit or report.
- Record the current state in `docs/status.md` and the active task in `docs/tasks/active.md`.
- Project facts belong in `README.md` and the `## Project` section. Do not copy commands into this block.
- This block is generated. Change the baseline and run `harness sync`; do not edit it by hand.
<!-- /harness:managed -->

## Project

- Stack:
- Build:
- Test:
- Commands: `README.md`
- Boundaries:
