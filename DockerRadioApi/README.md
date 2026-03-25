# C# Command-Line Project with Docker

This is a minimal .NET 10 command-line application packaged with Docker.

## Run locally

```bash
dotnet restore src/CliDockerApp/CliDockerApp.csproj
dotnet run --project src/CliDockerApp/CliDockerApp.csproj -- --name Michael
```

## Build Docker image

```bash
docker build -t radio-api .
```

## Run with Docker

```bash
docker run --rm radio-api --name Michael
```

Expected output:

```text
Hello, Michael!
Running on .NET 8.x.x
OS: Unix ...
```
