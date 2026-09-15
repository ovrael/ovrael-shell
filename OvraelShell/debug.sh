#!/usr/bin/env bash

set -e

export LD_PRELOAD=/usr/lib/libgtk4-layer-shell.so.0

echo "Starting OvraelShell..."
echo "LD_PRELOAD=$LD_PRELOAD"

dotnet watch run
#dotnet watch --non-interactive run "$@"
#dotnet run "$@"
