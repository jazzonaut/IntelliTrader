#!/bin/bash

dotnet publish -f netcoreapp2.1 -c Release /p:PublishProfile="IntelliTrader/Properties/PublishProfiles/FolderProfile.pubxml" -o "../Publish/bin"
dotnet publish -f netcoreapp2.1 -c Release /p:PublishProfile="IntelliTrader.Web/Properties/PublishProfiles/FolderProfile.pubxml" -o "../Publish/bin"