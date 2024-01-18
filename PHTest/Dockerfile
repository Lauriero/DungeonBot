FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["PHTest/PHTest.csproj", "PHTest/"]
RUN dotnet restore "PHTest/PHTest.csproj"
COPY . .
WORKDIR "/src/PHTest"
RUN dotnet build "PHTest.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PHTest.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PHTest.dll"]
