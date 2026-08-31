# Changelog

Internal, developer-facing changelog. One file per version, newest first.

This is **not** the NuGet release notes: it records _why_ each change was made, what was
debated and rejected, and what is deliberately left for a later version. Write it for the
version of us that comes back in six months.

## Conventions

- One file per version: `v<major>.<minor>.<patch>.md`.
- Sections in order: `Breaking`, `Added`, `Changed`, `Fixed`, `Deprecated`, `Deferred`, `Notes`.
- Omit empty sections.
- Every entry states the _reason_, not just the diff. The diff is in git.
- Mark deprecations with the version that will remove them.

## Index

| Version              | Date       | Summary                                            |
| -------------------- | ---------- | -------------------------------------------------- |
| [1.3.0](./v1.3.0.md) | unreleased | Transient/terminal exception model, retry overhaul |
