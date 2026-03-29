---
적용: 항상
---

# Commit Message Template Rules
# - All commit messages must be written in Korean.
# - Even if an English diff summary or auto-generated draft exists, rewrite the final commit message in Korean.
# - The first line must follow the format: type : subject
# - Keep the title within 50 characters and do not end it with a period.
# - If the title is already sufficient, body and footer may be omitted.
# - Write the body after one blank line below the title.
# - Do not leave English sentences unchanged unless they are untranslatable identifiers such as file paths, code identifiers, or branch names.
#
# Allowed type values
# feat    : add a new feature
# update  : modify an existing feature
# fix     : fix a bug
# docs    : update documents or comments
# design  : change CSS or UI design
# style   : no behavior change such as typos, formatting, semicolons, or spacing
# rename  : rename files
# delete  : remove unnecessary files
# refactor: refactor code
# test    : add tests
# chore   : build settings, project settings, import changes, function renames, and similar maintenance work
#
# squash merge
# [squash] hotfix/blabla

type : subject

# body

# footer
