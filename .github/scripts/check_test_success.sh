#!/bin/bash
set -e

echo "Starting .NET checks..."

echo "Running: dotnet restore"
dotnet restore

echo "Running: dotnet build --no-restore"
dotnet build --no-restore

echo "Running: dotnet test --no-build"
dotnet test --no-build

echo "All .NET checks passed successfully!"
