#!/usr/bin/env fish

set -l project SignalRDemo.Client/SignalRDemo.Client.csproj
set -l configuration Release
set -l output_root artifacts/publish

set -l supported_rids linux-x64 win-x64 osx-x64 osx-arm64

if test (count $argv) -eq 0
    set rids linux-x64 win-x64 osx-x64 osx-arm64
else
    set rids $argv
end

for rid in $rids
    if not contains $rid $supported_rids
        echo "Unsupported RID: $rid"
        echo "Supported: $supported_rids"
        exit 1
    end
end

for rid in $rids
    set -l out_dir "$output_root/$rid"
    echo "==> Publishing $rid -> $out_dir"
    dotnet publish $project \
        -c $configuration \
        -r $rid \
        --self-contained false \
        -o $out_dir
    or exit 1
end

echo ""
echo "Publish complete."
echo "Output root: $output_root"
