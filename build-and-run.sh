#!/bin/bash
echo "========================================"
echo "Building osu! Mania Player Mode"
echo "========================================"
echo ""

cd "$(dirname "$0")"

echo "Step 1: Building in Release mode..."
dotnet build osu.Desktop -c Release

if [ $? -ne 0 ]; then
    echo ""
    echo "========================================"
    echo "BUILD FAILED!"
    echo "========================================"
    exit 1
fi

echo ""
echo "========================================"
echo "Build completed successfully!"
echo "========================================"
echo ""
echo "Step 2: Running the game..."
echo ""
echo "Look for [ManiaMode] logs to track the process"
echo "========================================"
echo ""

dotnet run --project osu.Desktop -c Release

