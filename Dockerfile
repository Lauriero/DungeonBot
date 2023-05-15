FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /App

RUN apt-get update
RUN apt-get -y install libopus-dev
RUN apt-get -y install libsodium-dev

# Copy everything
COPY . ./

RUN apt-get install -y tcpreplay libpcap-dev pkg-config libssl-dev && \
    apt-get install -y build-essential file && \
    bash install-opus-tools.sh && \
    apt-get remove -y build-essential && \
    apt-get clean

RUN dotnet restore
RUN dotnet publish -c Release -o out

COPY Dockerfile ./DungeonDiscordBot/appsettings.Production.json? App/out/

# Build runtime image
FROM mcr.microsoft.com/dotnet/runtime:6.0
COPY --from=build-env /App/out .
ENTRYPOINT ["dotnet", "DungeonDiscordBot.dll"]
