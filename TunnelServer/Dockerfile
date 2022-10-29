FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["NetworkTunneling/NetworkTunneling.csproj", "NetworkTunneling/"]
RUN dotnet restore "NetworkTunneling/NetworkTunneling.csproj"
COPY . .
WORKDIR "/src/NetworkTunneling"
RUN dotnet build "NetworkTunneling.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NetworkTunneling.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NetworkTunneling.dll"]
