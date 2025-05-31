FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/aspnet:9.0-noble-chiseled AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG TARGETARCH
RUN dotnet workload install wasm-tools;
WORKDIR /src
COPY ["*.props", "*.config", "./"]
COPY ["BlazorAuthExample/BlazorAuthExample/BlazorAuthExample.csproj", "BlazorAuthExample/BlazorAuthExample/"]
COPY ["BlazorAuthExample/BlazorAuthExample.Client/BlazorAuthExample.Client.csproj", "BlazorAuthExample/BlazorAuthExample.Client/"]
RUN dotnet restore "BlazorAuthExample/BlazorAuthExample/BlazorAuthExample.csproj" -a $TARGETARCH
COPY . .
WORKDIR "/src/BlazorAuthExample/BlazorAuthExample"

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./BlazorAuthExample.csproj" -c $BUILD_CONFIGURATION -o /app/publish -a $TARGETARCH

FROM base AS final
COPY --from=alexaka1/distroless-dotnet-healthchecks:1.1.0 / /healthchecks
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BlazorAuthExample.dll"]
