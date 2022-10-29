FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["TunnelClient/TunnelClient.csproj", "TunnelClient/"]
RUN dotnet restore "TunnelClient/TunnelClient.csproj"
COPY . .
WORKDIR "/src/TunnelClient"
RUN dotnet build "TunnelClient.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TunnelClient.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TunnelClient.dll"]
