# Husk

En enkel huskeliste med stemmestyring.

Husk is a small self-hosted reminder app. The first version focuses on a fast
voice-first workflow: open the app, speak or type what you need to remember, and
keep the list on your own Raspberry Pi.

## Structure

```text
Husk.sln       .NET solution
src/Husk.Api   ASP.NET Core API with JSON-file persistence
src/Husk.Web   ASP.NET Core web frontend with browser speech recognition
compose.yaml   Docker Compose setup for homelab deployment
```

## Run Locally

In one terminal:

```bash
make restore
make run-api
```

In another terminal:

```bash
make run-web
```

Open:

```text
http://localhost:5080
```

## Run With Docker

```bash
make docker-up
```

Open:

```text
http://localhost:8088
```

Tasks are stored in the `husk-data` Docker volume.

## Images

GitHub Actions publishes images to GitHub Container Registry on every push to
`main`:

```text
ghcr.io/espenhal/husk-app-api:main
ghcr.io/espenhal/husk-app-web:main
```

Each image is also tagged with `sha-<commit-sha>` for exact rollbacks.

If the repository or packages are private, the Raspberry Pi must be logged in to
GHCR before pulling. For the simplest homelab setup, make the two container
packages public after the first workflow run.

## Microphone Access

The browser microphone requires a secure context. `localhost` works during local
development. On the homelab, use HTTPS through Nginx Proxy Manager before
expecting microphone input to work reliably from phones and browsers.

## Homelab Notes

The app is designed to sit behind Nginx Proxy Manager:

- public container: `husk-web`
- internal API container: `husk-api`
- frontend port: `8088`
- persistent Docker volume: `husk-data`

For a first Pi deployment, clone this repo on the Pi and run `docker compose up
-d --build`. A later step can publish images to GHCR and add Husk as a normal
service in `pi-homelab`.
