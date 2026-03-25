# Radio CLI with Docker

This project packages a .NET 10 command-line application in Docker. The container can:

- play WAV files with `aplay`
- parse a DAB+ service list from `si468x_service_list`

## Run locally

```bash
dotnet restore src/RadioApi/RadioApi.csproj
dotnet run --project src/RadioApi/RadioApi.csproj
```

Interactive commands:

```text
help
sample
play
play /app/samples/DaveRaindance.wav
services
services /sys/bus/spi/devices/spi0.1/si468x_service_list
exit
```

The default `services` path is:

```text
/sys/bus/spi/devices/spi0.1/si468x_service_list
```

## Build Docker image

```bash
docker build -t radio-api .
```

## Run with Docker

Use an interactive terminal for stdin commands:

```bash
docker run --rm -it --device /dev/snd radio-api
```

If the DAB+ service list exists on the host, mount the sysfs path read-only:

```bash

docker run --rm -it -v /root:/audio -v /sys/bus/spi/devices/spi0.1/si468x_service_list:/sys/bus/spi/devices/spi0.1/si468x_service_list:ro --device /dev/snd ghcr.io/hoiton/hackathon-scp-radio:latest --mqtt-host <broker-host> --mqtt-port 1883
```

## Publish on GitHub

The repository includes a GitHub Actions workflow at `.github/workflows/docker-publish.yml`.

When you push to `main`, GitHub Actions will:

- build the image from `DockerRadioApi/Dockerfile`
- publish it to `ghcr.io/<owner>/<repo>`
- tag the image with the branch name, commit SHA, and `latest` on the default branch

Version tags like `v1.0.0` will also be published as container tags.
