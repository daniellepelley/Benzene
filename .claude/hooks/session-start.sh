#!/bin/bash
# SessionStart hook for Claude Code on the web: installs the .NET SDK so
# `dotnet build`/`dotnet test` work locally instead of requiring a push to
# GitHub Actions CI just to find out whether something compiles.
#
# Only runs in remote (web) sessions - a local Claude Code CLI session
# assumes the developer's own machine already has a .NET SDK.
#
# Deliberately non-fatal: if the SDK download is unreachable (e.g. an
# egress policy blocks builds.dotnet.microsoft.com, as observed in at least
# one sandboxed session this was developed in), this prints a warning and
# exits 0 rather than failing session start - the session still works, it
# just falls back to CI as the verification loop, same as before this hook
# existed.
set -uo pipefail

if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

# Must match the SDK version .github/workflows/build-benzene.yml installs
# (actions/setup-dotnet, dotnet-version: 10.0.x) and README.md's "Requires
# .NET 10." - keep these three in sync if the target framework ever moves.
DOTNET_CHANNEL="10.0"
INSTALL_DIR="$HOME/.dotnet"

if [ ! -x "$INSTALL_DIR/dotnet" ]; then
  if ! curl -sSL --fail https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh; then
    echo "session-start.sh: could not download dotnet-install.sh (network/egress-policy issue) - skipping .NET SDK setup; dotnet build/test won't be available locally this session, CI remains the verification loop." >&2
    exit 0
  fi
  if ! bash /tmp/dotnet-install.sh --channel "$DOTNET_CHANNEL" --install-dir "$INSTALL_DIR"; then
    echo "session-start.sh: dotnet-install.sh failed - skipping .NET SDK setup; dotnet build/test won't be available locally this session, CI remains the verification loop." >&2
    exit 0
  fi
fi

{
  echo "export DOTNET_ROOT=\"$INSTALL_DIR\""
  echo "export PATH=\"$INSTALL_DIR:\$PATH\""
  echo "export DOTNET_CLI_TELEMETRY_OPTOUT=1"
  echo "export DOTNET_NOLOGO=1"
} >> "$CLAUDE_ENV_FILE"

export DOTNET_ROOT="$INSTALL_DIR"
export PATH="$INSTALL_DIR:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

# Warm the NuGet package cache now, while the container's post-hook state
# gets cached, so it doesn't have to happen again during the session (or in
# future sessions that reuse this cached container). Also non-fatal: a
# restore failure (e.g. nuget.org blocked too) shouldn't block the session -
# it just means the first `dotnet build`/`dotnet test` call pays that cost.
if [ -f "$CLAUDE_PROJECT_DIR/Benzene.sln" ]; then
  dotnet restore "$CLAUDE_PROJECT_DIR/Benzene.sln" || echo "session-start.sh: dotnet restore failed - the first build/test in-session will need to restore instead." >&2
fi

exit 0
