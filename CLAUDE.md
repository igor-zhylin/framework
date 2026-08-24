# Working rules for this repo

- Never commit files that Claude authored as working artifacts — design specs,
  implementation plans, and any other `.md`/plan documents created during a
  session (e.g. everything under `docs/superpowers/`). These stay local and
  untracked; write them to disk for reference, but do not `git add`/commit
  them unless the user explicitly asks to commit that specific file.
- Never add a `Co-Authored-By: Claude ...` (or similar) trailer to commit
  messages in this repo.
