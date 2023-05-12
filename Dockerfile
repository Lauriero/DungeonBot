FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /App

RUN apt-get update
RUN apt-get -y install libopus-dev
RUN apt-get -y install libsodium-dev

# Copy everything
COPY . ./

WORKDIR /App/Yandex.Music.Api
RUN dotnet restore

WORKDIR /App/DungeonDiscordBot
RUN dotnet restore
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/runtime:6.0
WORKDIR /App/DungeonDiscordBot
COPY --from=build-env /App/out .
ENTRYPOINT ["dotnet", "DungeonDiscordBot.dll"]
