# C# Command-Line Project with Docker

This is a minimal .NET 8 command-line application packaged with Docker.

## Project structure

```
csharp-cli-docker/
├── .dockerignore
├── Dockerfile
├── README.md
└── src/
    └── CliDockerApp/
        ├── CliDockerApp.csproj
        └── Program.cs
```

## Run locally

```bash
dotnet restore src/CliDockerApp/CliDockerApp.csproj
dotnet run --project src/CliDockerApp/CliDockerApp.csproj -- --name Michael
```

## Build Docker image

```bash
docker build -t cli-docker-app .
```

## Run with Docker

```bash
docker run --rm cli-docker-app --name Michael
```

Expected output:

```text
Hello, Michael!
Running on .NET 8.x.x
OS: Unix ...
```
