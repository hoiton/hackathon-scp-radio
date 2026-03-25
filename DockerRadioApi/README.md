# C# Command-Line Project with Docker

This is a minimal .NET 10 command-line application packaged with Docker and ready to publish to GitHub Container Registry.

## Run locally

```bash
dotnet restore src/RadioApi/RadioApi.csproj
dotnet run --project src/RadioApi/RadioApi.csproj -- --name Michael
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
Running on .NET 10.x.x
OS: Unix ...
```

## Publish on GitHub

The repository includes a GitHub Actions workflow at `.github/workflows/docker-publish.yml`.

When you push to `main`, GitHub Actions will:

- build the image from `DockerRadioApi/Dockerfile`
- publish it to `ghcr.io/<owner>/<repo>`
- tag the image with the branch name, commit SHA, and `latest` on the default branch

Version tags like `v1.0.0` will also be published as container tags.

To pull the published image:

```bash
docker pull ghcr.io/<owner>/<repo>:latest
```

If the package is private, authenticate first:

```bash
echo <github-token> | docker login ghcr.io -u <github-username> --password-stdin
```
