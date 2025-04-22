FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/DNS-BLM.Api/DNS-BLM.Api.csproj", "src/DNS-BLM.Api/"]
RUN dotnet restore "src/DNS-BLM.Api/DNS-BLM.Api.csproj"

COPY . .
WORKDIR "/src/src/DNS-BLM.Api"
RUN dotnet publish "DNS-BLM.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
COPY --from=build-env /app/publish .
ENTRYPOINT ["dotnet", "DNS-BLM.Api.dll"]
