---
title: 'Initialize Hexalith.Works submodule'
type: 'chore'
created: '2026-07-19'
status: 'done'
route: 'one-shot'
---

# Initialize Hexalith.Works submodule

## Intent

**Problem:** Timesheets already tracked `Hexalith.Works`, but its checkout was absent in this ChatBot workspace; Jerome explicitly authorized initializing that normally untouched nested workspace dependency.

**Approach:** From `references/Hexalith.Timesheets`, run `git submodule update --init -- Hexalith.Works` to check out the existing gitlink `f2259daab922096113262fc9e0a5588182918e0a` without recursion, then verify it with `git submodule status`. Run `Hexalith.Timesheets.Works.Tests` in Release (76/76 pass) and the serialized Release restore/build with the existing umbrella EventStore and Polymorphic root properties; the documented immediate rerun after the transient 15-error `IDE0065` pass succeeds with zero warnings and zero errors.

## Suggested Review Order

- The existing root declaration fixes the repository and URL without manifest drift.
  [`.gitmodules:28`](../../references/Hexalith.Timesheets/.gitmodules#L28)

- Timesheets discovers the initialized Works contracts through its established root property.
  [`Directory.Build.props:7`](../../references/Hexalith.Timesheets/Directory.Build.props#L7)
