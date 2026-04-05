---
적용: 항상
---

# Commit Message Template Rules
# - All commit messages must be written in Korean.
# - Even if you receive an English diff summary or an auto-generated draft, rewrite the final commit message into natural Korean.
# - The first line must follow the `type : subject` format.
# - Keep the title within 50 characters and do not end it with a period.
# - If the title is enough on its own, the body and footer may be omitted.
# - Write the body after one blank line, briefly and concretely.
# - Do not leave English sentences as-is except for proper nouns that should not be translated, such as file paths, code identifiers, or branch names.
#
# Allowed type values
# feat    : add a new feature
# update  : modify an existing feature
# fix     : bug fix
# docs    : documentation or comment change
# design  : UI design change
# style   : formatting-only change with no behavior change, such as typos, spacing, or semicolons
# rename  : rename a file or identifier
# delete  : remove unnecessary files
# refactor: refactoring
# test    : add or improve tests
# chore   : maintenance such as build settings, project settings, import changes, or function-name cleanup
#
# squash merge
# [squash] branch-name

type : subject

# body

# footer
