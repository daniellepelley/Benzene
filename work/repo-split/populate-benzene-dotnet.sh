#!/usr/bin/env bash
#
# Phase 1 populate script — stand up benzene-dotnet from a clean snapshot of the benzene repo.
#
# This is the exact, verified recipe used to build the benzene-dotnet working tree. It was run
# against benzene HEAD and the result (a) builds Benzene.sln standalone with 0 errors and
# (b) passes the conformance suite (134/0) against the vendored fixtures — proving the .NET port is
# self-contained with no reference escaping back to benzene.
#
# It is NOT yet possible to create the benzene-dotnet repo from this session (the GitHub integration
# returns 403 on repo creation). Once the empty repo exists, this script + the git push at the end
# completes Phase 1.
#
# Usage:
#   BENZENE=/path/to/benzene  STAGE=/path/to/benzene-dotnet  ./populate-benzene-dotnet.sh
#
set -euo pipefail

BENZENE="${BENZENE:-/home/user/Benzene}"
STAGE="${STAGE:-/home/user/benzene-dotnet-staging}"
OVERLAY="$BENZENE/work/repo-split/overlay"

echo "==> Export the entire tracked tree (tracked files only — no bin/obj, no history)"
rm -rf "$STAGE"; mkdir -p "$STAGE"
git -C "$BENZENE" archive HEAD | tar -x -C "$STAGE"

echo "==> Vendor the conformance fixtures BEFORE pruning docs/specification"
mkdir -p "$STAGE/test/conformance-fixtures"
cp "$STAGE"/docs/specification/conformance/*.json "$STAGE/test/conformance-fixtures/"
git -C "$BENZENE" rev-parse HEAD > "$STAGE/test/conformance-fixtures/SPEC_VERSION"
cp "$OVERLAY/test/conformance-fixtures/README.md" "$STAGE/test/conformance-fixtures/README.md"

echo "==> Repoint the conformance test project at the vendored dir"
sed -i 's#..\\..\\docs\\specification\\conformance\\\*.json#..\\conformance-fixtures\\*.json#' \
  "$STAGE/test/Benzene.Conformance.Test/Benzene.Conformance.Test.csproj"
sed -i 's#Loads the language-neutral fixture files from docs/specification/conformance/ (copied to the test#Loads the language-neutral fixture files from the vendored test/conformance-fixtures/ snapshot (copied to the test#' \
  "$STAGE/test/Benzene.Conformance.Test/ConformanceFixtures.cs"

echo "==> Prune the STAYS set (cross-language material that remains in benzene)"
rm -rf "$STAGE/website" "$STAGE/blog" "$STAGE/docs/specification"
rm -f  "$STAGE/.github/workflows/deploy-website.yml" "$STAGE/.github/workflows/promote-website.yml"
rm -f  "$STAGE/work/repo-split-plan.md" "$STAGE/work/repo-split-manifest.md"
rm -rf "$STAGE/work/repo-split"

echo "==> Apply the overlay (standalone repo docs, drift-check workflow, index.md spec pointer)"
cp "$OVERLAY/README.md"                                   "$STAGE/README.md"
cp "$OVERLAY/AGENTS.md"                                   "$STAGE/AGENTS.md"
cp "$OVERLAY/CLAUDE.md"                                   "$STAGE/CLAUDE.md"
cp "$OVERLAY/docs/index.md"                               "$STAGE/docs/index.md"
mkdir -p "$STAGE/.github/workflows"
cp "$OVERLAY/.github/workflows/conformance-drift-check.yml" "$STAGE/.github/workflows/conformance-drift-check.yml"
cp "$OVERLAY/.github/workflows/notify-website.yml"          "$STAGE/.github/workflows/notify-website.yml"

echo "==> Verify: build standalone + conformance tests"
( cd "$STAGE" && dotnet build Benzene.sln --nologo -v q )
( cd "$STAGE" && dotnet test test/Benzene.Conformance.Test/Benzene.Conformance.Test.csproj --nologo -v q )

echo "==> Initialise git and commit as the maintainer (history: clean snapshot)"
cd "$STAGE"
git init -q -b main
git config user.name "daniellepelley"
git config user.email "daniellepelley@gmail.com"
git add -A
git commit -q -m "Benzene .NET port — initial snapshot

The .NET implementation of Benzene, split out of the cross-language benzene
repo. Self-contained: builds and tests with no submodule and no build-time
dependency on benzene. The language-neutral spec and the website stay in
benzene; the conformance fixtures are vendored under test/conformance-fixtures/
and kept in sync by the conformance-drift-check workflow."

echo ""
echo "Staging tree ready and committed. To publish once the empty repo exists:"
echo "  git -C \"$STAGE\" remote add origin https://github.com/daniellepelley/benzene-dotnet.git"
echo "  git -C \"$STAGE\" push -u origin main"
