#!/bin/bash

mono "./bin/NuGet.exe" restore "./PiServerLite.sln"

msbuild "./PiServerLite.Publishing/PiServerLite.Publishing.csproj" /t:Rebuild /p:Configuration="Debug" /p:Platform="Any CPU" /p:OutputPath="bin\Debug" /v:m
