#!/usr/bin/env bash
#
# Generates the idempotent EF Core migration script and applies it to a PostgreSQL database.
#
# The application never migrates at startup: run this script as an explicit deploy step.
#
# Connection (psql standard environment variables, never passed on the command line):
#   PGHOST      PostgreSQL host
#   PGDATABASE  target database (for example controlpeso)
#   PGUSER      role allowed to create tables and the citext extension
#   PGPASSWORD  password for that role
#   PGPORT      optional, defaults to 5432
#
# Usage:
#   scripts/migrate-postgres.sh             generate the script and apply it with psql
#   scripts/migrate-postgres.sh --dry-run   generate the script and print it to stdout only
#   scripts/migrate-postgres.sh --help
#
# Optional environment:
#   EF_TOOL_PATH   directory holding the local dotnet-ef tool (default: ${TMPDIR:-/tmp}/controlpeso-ef-tools)
#   EF_VERSION     dotnet-ef version to install (default: 10.0.3)
#
# The script is safe to run more than once: the generated SQL is idempotent
# (it checks __EFMigrationsHistory before applying each migration).

set -euo pipefail

EF_VERSION="${EF_VERSION:-10.0.3}"
EF_TOOL_PATH="${EF_TOOL_PATH:-${TMPDIR:-/tmp}/controlpeso-ef-tools}"
INFRA_PROJECT="src/ControlPeso.Infrastructure"

usage() {
  sed -n '3,25p' "$0" | sed 's/^# \{0,1\}//'
}

dry_run=false
for arg in "$@"; do
  case "$arg" in
    --dry-run) dry_run=true ;;
    -h|--help) usage; exit 0 ;;
    *) echo "Unknown option: $arg" >&2; usage >&2; exit 2 ;;
  esac
done

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet SDK not found in PATH." >&2
  exit 1
fi

if [ "$dry_run" = false ]; then
  missing=()
  for var in PGHOST PGDATABASE PGUSER PGPASSWORD; do
    [ -n "${!var:-}" ] || missing+=("$var")
  done
  if [ "${#missing[@]}" -gt 0 ]; then
    echo "Missing required environment variables: ${missing[*]}" >&2
    exit 1
  fi
  if ! command -v psql >/dev/null 2>&1; then
    echo "psql not found in PATH." >&2
    exit 1
  fi
fi

if [ ! -x "$EF_TOOL_PATH/dotnet-ef" ]; then
  echo "Installing dotnet-ef $EF_VERSION into $EF_TOOL_PATH" >&2
  dotnet tool install dotnet-ef --version "$EF_VERSION" --tool-path "$EF_TOOL_PATH" >&2
fi

script_file="$(mktemp "${TMPDIR:-/tmp}/controlpeso-migrate-XXXXXX.sql")"
trap 'rm -f "$script_file"' EXIT

echo "Generating idempotent migration script" >&2
"$EF_TOOL_PATH/dotnet-ef" migrations script --idempotent \
  --project "$INFRA_PROJECT" --output "$script_file" >&2

# dotnet-ef writes a UTF-8 BOM, which psql would treat as part of the first statement.
sed -i '1s/^\xEF\xBB\xBF//' "$script_file"

if [ "$dry_run" = true ]; then
  cat "$script_file"
  exit 0
fi

echo "Applying migrations to ${PGDATABASE} on ${PGHOST}:${PGPORT:-5432} as ${PGUSER}" >&2
psql --no-psqlrc --set ON_ERROR_STOP=1 --quiet --file "$script_file"
echo "Migrations applied." >&2
