FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /App

# Copy everything
COPY . ./

RUN dotnet restore
RUN dotnet publish -c Release -o out

COPY install-opus-tools.sh ./DungeonDiscordBot/appsettings.Production.json* /App/out/

# Build runtime image
FROM mcr.microsoft.com/dotnet/runtime:6.0
COPY --from=build-env /App/out ./out
RUN cp ./out/install-opus-tools.sh ./opt/install-opus-tools.sh

RUN apt-get update
RUN apt-get -y install libopus-dev
RUN apt-get -y install libsodium-dev

RUN apt-get install -y libsodium-dev ffmpeg tcpreplay libpcap-dev pkg-config libssl-dev && \
    apt-get install -y build-essential file && \
    cd opt && \
    bash install-opus-tools.sh && \
    apt-get remove -y build-essential && \
    apt-get clean

WORKDIR /out
ENTRYPOINT ["dotnet", "DungeonDiscordBot.dll"]
