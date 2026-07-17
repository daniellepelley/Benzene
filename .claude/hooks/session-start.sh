#!/bin/bash
# SessionStart hook for Claude Code on the web: installs the .NET SDK so
# `dotnet build`/`dotnet test` work locally instead of requiring a push to
# GitHub Actions CI just to find out whether something compiles.
#
# Only runs in remote (web) sessions - a local Claude Code CLI session
# assumes the developer's own machine already has a .NET SDK.
#
# Two install routes, tried in order:
#   1. dotnet-install.sh from dot.net - needs egress to
#      builds.dotnet.microsoft.com / ci.dot.net (open network policies).
#   2. apt via packages.microsoft.com (registered non-fatally; on Ubuntu
#      24.04+ the distro archive carries dotnet-sdk-10.0 anyway) - this is
#      the route that works under egress policies where
#      builds.dotnet.microsoft.com is blocked but packages.microsoft.com
#      and archive.ubuntu.com are allowed (observed in real web sessions).
#
# Deliberately non-fatal throughout: if neither route works, this prints a
# warning and exits 0 rather than failing session start - the session still
# works, it just falls back to CI as the verification loop, same as before
# this hook existed.
set -uo pipefail

if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

# Must match the SDK version .github/workflows/build-benzene.yml installs
# (actions/setup-dotnet, dotnet-version: 10.0.x) and README.md's "Requires
# .NET 10." - keep these three in sync if the target framework ever moves.
DOTNET_CHANNEL="10.0"
INSTALL_DIR="$HOME/.dotnet"

write_env() {
  {
    echo "export DOTNET_CLI_TELEMETRY_OPTOUT=1"
    echo "export DOTNET_NOLOGO=1"
    if [ -x "$INSTALL_DIR/dotnet" ]; then
      echo "export DOTNET_ROOT=\"$INSTALL_DIR\""
      echo "export PATH=\"$INSTALL_DIR:\$PATH\""
    fi
  } >> "$CLAUDE_ENV_FILE"
  export DOTNET_CLI_TELEMETRY_OPTOUT=1
  export DOTNET_NOLOGO=1
  if [ -x "$INSTALL_DIR/dotnet" ]; then
    export DOTNET_ROOT="$INSTALL_DIR"
    export PATH="$INSTALL_DIR:$PATH"
  fi
}

have_dotnet() {
  command -v dotnet > /dev/null 2>&1 || [ -x "$INSTALL_DIR/dotnet" ]
}

install_via_script() {
  curl -sSL --fail https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh || return 1
  bash /tmp/dotnet-install.sh --channel "$DOTNET_CHANNEL" --install-dir "$INSTALL_DIR" || return 1
}

install_via_apt() {
  [ "$(id -u)" = "0" ] || return 1
  # shellcheck disable=SC1091
  . /etc/os-release 2> /dev/null || return 1
  case "${ID:-}" in
    ubuntu | debian) ;;
    *) return 1 ;;
  esac
  # Register Microsoft's package repo for distro releases whose own archive
  # doesn't carry the SDK; failure here is fine on Ubuntu 24.04+, whose own
  # archive has dotnet-sdk-10.0.
  if ! dpkg -s packages-microsoft-prod > /dev/null 2>&1; then
    if curl -sSL --fail "https://packages.microsoft.com/config/$ID/$VERSION_ID/packages-microsoft-prod.deb" -o /tmp/packages-microsoft-prod.deb; then
      dpkg -i /tmp/packages-microsoft-prod.deb > /dev/null 2>&1 || true
    fi
  fi
  apt-get update -qq 2> /dev/null || true
  DEBIAN_FRONTEND=noninteractive apt-get install -y -qq "dotnet-sdk-$DOTNET_CHANNEL" || return 1
}

if ! have_dotnet; then
  if ! install_via_script && ! install_via_apt; then
    echo "session-start.sh: could not install the .NET SDK via dot.net or apt (network/egress-policy issue) - dotnet build/test won't be available locally this session, CI remains the verification loop." >&2
    exit 0
  fi
fi

write_env

# Warm the NuGet package cache now, while the container's post-hook state
# gets cached, so it doesn't have to happen again during the session (or in
# future sessions that reuse this cached container). Also non-fatal: a
# restore failure (e.g. nuget.org blocked too) shouldn't block the session -
# it just means the first `dotnet build`/`dotnet test` call pays that cost.
if [ -f "$CLAUDE_PROJECT_DIR/Benzene.sln" ]; then
  dotnet restore "$CLAUDE_PROJECT_DIR/Benzene.sln" || echo "session-start.sh: dotnet restore failed - the first build/test in-session will need to restore instead." >&2
fi

exit 0
