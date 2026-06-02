#!/usr/bin/env bash
# check-file-names.sh — Scans frontend source files for camelCase filenames
# that should be kebab-case according to project standards.
#
# Usage: bash check-file-names.sh <project-root>
#
# Exits 0 if no violations found, 1 if violations detected.
# Output: one violation per line in format "VIOLATION: <filepath> -> <suggested>"

set -euo pipefail

PROJECT_ROOT="${1:-.}"
VIOLATIONS=0

# Convert camelCase to kebab-case
to_kebab_case() {
  local name="$1"
  # Insert hyphen before uppercase letters, then lowercase everything
  echo "$name" | sed -E 's/([a-z])([A-Z])/\1-\2/g' | tr '[:upper:]' '[:lower:]'
}

# Check if a filename is camelCase (starts lowercase, has uppercase letters)
is_camel_case() {
  local name="$1"
  # Match: starts with lowercase, contains at least one uppercase letter
  [[ "$name" =~ ^[a-z]+[a-zA-Z]*[A-Z]+[a-zA-Z]*$ ]]
}

# Find frontend source files and check naming
check_frontend_files() {
  local search_dir="$PROJECT_ROOT/frontend/src"
  
  if [[ ! -d "$search_dir" ]]; then
    search_dir="$PROJECT_ROOT/src"
  fi
  
  if [[ ! -d "$search_dir" ]]; then
    echo "INFO: No frontend source directory found at $PROJECT_ROOT/frontend/src or $PROJECT_ROOT/src"
    return 0
  fi

  while IFS= read -r filepath; do
    # Skip excluded patterns
    [[ "$filepath" == *"node_modules"* ]] && continue
    [[ "$filepath" == *".git/"* ]] && continue
    [[ "$filepath" == *".agents/"* ]] && continue
    [[ "$filepath" == *"/bin/"* ]] && continue
    [[ "$filepath" == *"/obj/"* ]] && continue
    
    local filename
    filename=$(basename "$filepath")
    
    # Skip agent and test files
    [[ "$filename" == *".agent.md"* ]] && continue
    [[ "$filename" == *".test."* ]] && continue
    [[ "$filename" == *".spec."* ]] && continue
    
    # Skip known config files at root level
    [[ "$filename" == "vite.config."* ]] && continue
    [[ "$filename" == "tailwind.config."* ]] && continue
    [[ "$filename" == "postcss.config."* ]] && continue
    [[ "$filename" == "tsconfig"* ]] && continue
    [[ "$filename" == "eslint"* ]] && continue
    [[ "$filename" == "prettier"* ]] && continue
    
    # Get the name without extension
    local name_no_ext
    name_no_ext="${filename%%.*}"
    
    # Skip index files and single-word lowercase files
    [[ "$name_no_ext" == "index" ]] && continue
    [[ "$name_no_ext" == "main" ]] && continue
    [[ "$name_no_ext" == "App" ]] && continue
    
    # Check if it's a component file (PascalCase in a PascalCase directory is OK)
    local parent_dir
    parent_dir=$(basename "$(dirname "$filepath")")
    if [[ "$name_no_ext" == "$parent_dir" ]] && [[ "$name_no_ext" =~ ^[A-Z] ]]; then
      # PascalCase component in matching directory — this is fine
      continue
    fi

    # Check for camelCase files (should be kebab-case)
    if is_camel_case "$name_no_ext"; then
      local ext="${filename#*.}"
      local suggested
      suggested="$(to_kebab_case "$name_no_ext").$ext"
      echo "VIOLATION: $filepath -> $suggested"
      ((VIOLATIONS++)) || true
    fi
  done < <(find "$search_dir" -type f \( -name "*.ts" -o -name "*.tsx" -o -name "*.css" -o -name "*.js" -o -name "*.jsx" \) 2>/dev/null)
}

echo "=== File Naming Convention Check ==="
echo "Scanning: $PROJECT_ROOT"
echo "Rule: Frontend files should use kebab-case (excluding components, tests, agent files)"
echo ""

check_frontend_files

echo ""
if [[ $VIOLATIONS -eq 0 ]]; then
  echo "✅ No file naming violations found."
  exit 0
else
  echo "❌ Found $VIOLATIONS file naming violation(s)."
  exit 1
fi
