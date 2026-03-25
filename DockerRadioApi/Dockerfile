FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/RadioApi/RadioApi.csproj src/RadioApi/
RUN dotnet restore src/RadioApi/RadioApi.csproj

COPY src/RadioApi/ src/RadioApi/
RUN dotnet publish src/RadioApi/RadioApi.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends alsa-utils \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
COPY samples/ /app/samples/
ENTRYPOINT ["dotnet", "RadioApi.dll"]
