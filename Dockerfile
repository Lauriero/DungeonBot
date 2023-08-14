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
RUN apt-get install -y libopus-dev libsodium-dev tcpreplay libpcap-dev pkg-config libssl-dev && \
    apt-get install -y build-essential file && \
    cd opt && \
    bash install-opus-tools.sh && \
    apt-get remove -y build-essential && \
    apt-get clean

WORKDIR /out
RUN wget -O ffmpeg_build.tar.xz https://www.johnvansickle.com/ffmpeg/old-releases/ffmpeg-3.3.4-64bit-static.tar.xz && \
    tar -xf ffmpeg_build.tar.xz && \
    rm ffmpeg_build.tar.xz && \
    mv ffmpeg-3.3.4-64bit-static ffmpeg && \
    chmod +x ./ffmpeg/ffmpeg
    

ENTRYPOINT ["dotnet", "DungeonDiscordBot.dll"]