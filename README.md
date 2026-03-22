# Research Agent

Research Agent is a repo-aware AI development platform with a canonical orchestration flow in `agents/root_agent.py`.

It includes:
- repo-aware analysis, review, spec, draft, change, and implementation flows
- temp-workspace validation before real apply
- run tracking, approval, retry, cancel, and explorer commands
- optional Postgres-backed metadata
- FastAPI endpoints in `web_app.py`
- a minimal plain HTML UI under `/ui`

## Architecture

The current source of truth is:
- `agents/root_agent.py` for orchestration
- `main.py` as the thin CLI entrypoint
- `web_app.py` as the thin HTTP entrypoint
- `services/` for stateful and integration logic
- `contracts/` for typed models
- `tools/` for canonical tool wrappers
- file-based artifacts for large logs, diffs, temp workspaces, and indexes

This repository already supports optional integrations such as Postgres, Bitbucket, and Crucible, but the app must still start cleanly when those integrations are not configured.

## Local Run

Create and activate a virtual environment, then install dependencies:

```bash
python -m venv .venv
.venv\Scripts\python.exe -m pip install -r requirements.txt
```

Run the API locally:

```bash
.venv\Scripts\python.exe -m uvicorn web_app:app --host 127.0.0.1 --port 8000
```

Useful local URLs:
- UI: `http://127.0.0.1:8000/ui/index.html`
- Swagger: `http://127.0.0.1:8000/docs`
- Health: `http://127.0.0.1:8000/health`

Postgres is optional for local development. If `POSTGRES_DSN` is empty, the platform continues to use file-based artifacts and metadata fallbacks where supported.

## Docker Run

The repository includes a production-oriented Docker setup for:
- the app container
- a Postgres container

Start both services with:

```bash
docker compose up --build
```

After startup:
- UI: `http://127.0.0.1:8000/ui/index.html`
- Repo onboarding UI: `http://127.0.0.1:8000/ui/repos.html`
- Swagger: `http://127.0.0.1:8000/docs`

In Docker and production-style environments, Postgres is expected to run in a container and the app should use the container hostname `postgres`, not `localhost`.

Example Docker DSN:

```dotenv
POSTGRES_DSN=postgresql://research_agent:research_agent@postgres:5432/research_agent
```

Docker onboarding uses an internal clone root for registered repositories. The compose setup mounts a persistent volume at `/repos`, and onboarded repositories are cloned under:

```text
/repos/<repo_id>
```

The app container includes `git`, so repo onboarding can clone remote repositories directly inside Docker.

## Environment Setup

Copy `.env.example` to `.env` and fill in the values you need.

Minimum useful settings:

```dotenv
OPENAI_API_KEY=
POSTGRES_DSN=
DEFAULT_ACTOR_ROLE=admin
DEFAULT_ACTOR_DISPLAY_NAME=Local CLI
REPO_CLONE_ROOT=repos
```

Optional implementation and publication controls:

```dotenv
AUTO_PUBLISH_IMPLEMENTATION_RUNS=false
ALLOW_REAL_APPLY=false
ALLOW_PR_CREATION=false
ALLOW_REVIEW_CREATION=false
```

Optional integration placeholders already supported by the app:

```dotenv
BITBUCKET_API_BASE_URL=https://api.bitbucket.org/2.0
BITBUCKET_REPO_TOKEN=
BITBUCKET_USERNAME=
BITBUCKET_APP_PASSWORD=
BITBUCKET_API_TOKEN=

CRUCIBLE_BASE_URL=
CRUCIBLE_USERNAME=
CRUCIBLE_PASSWORD=
CRUCIBLE_API_TOKEN=
CRUCIBLE_PROJECT_KEY=
CRUCIBLE_DEFAULT_REVIEWERS=
```

## Repository Onboarding

Repositories can be onboarded without manual Docker volume setup or manual SQL edits.

API:

```http
POST /repos/onboard
GET /repos
```

UI:

```text
http://127.0.0.1:8000/ui/repos.html
```

Run detail pages now surface debugging data directly from canonical run metadata:
- root cause summary
- draft summary
- validation summary with failed test cases and bounded stdout/stderr
- apply summary
- diff preview with explicit fallback reasons when no diff was produced

Onboarding stores:
- `remote_url`: the external Git URL used for cloning
- `local_path`: the internal runtime path used by the app

For Docker deployments, `local_path` is typically `/repos/<repo_id>`.

Use a clean Bitbucket remote URL when onboarding:

```text
https://bitbucket.org/<workspace>/<repo>.git
```

Do not embed credentials or tokens in `remote_url`.

Supported Bitbucket auth modes for clone/push/PR creation:
- `BITBUCKET_REPO_TOKEN` or `BITBUCKET_API_TOKEN`
  Runtime auth uses `x-token-auth` and injects credentials only while git/HTTP commands run.
- `BITBUCKET_USERNAME` + `BITBUCKET_APP_PASSWORD`
  Used as a fallback when token-based auth is not configured.
- Public repository access
  The clean remote URL is used without credentials.

Credential precedence:
1. `BITBUCKET_REPO_TOKEN`
2. `BITBUCKET_API_TOKEN`
3. `BITBUCKET_USERNAME` + `BITBUCKET_APP_PASSWORD`
4. unauthenticated clean remote URL

## Metadata And Artifacts

Postgres is used only for operational metadata such as:
- users
- roles and capabilities
- repos
- runs
- run steps
- policy decisions

The following remain file-based by design:
- raw diff bodies
- JSON run logs
- temp workspaces
- repo indexes and manifests
- other heavy artifacts

## Authentication And Admin

The app now supports real login with server-side session cookies for the UI and API:

- `POST /auth/login`
- `POST /auth/logout`
- `GET /auth/me`
- `POST /auth/change-password`

Admin JSON endpoints:

- `GET /admin/users`
- `POST /admin/users`
- `PUT /admin/users/{user_id}`
- `POST /admin/users/{user_id}/reset-password`
- `POST /admin/users/{user_id}/activate`
- `POST /admin/users/{user_id}/deactivate`
- `GET /admin/roles`
- `POST /admin/roles`
- `PUT /admin/roles/{role_name}`
- `GET /admin/roles/{role_name}/capabilities`
- `PUT /admin/roles/{role_name}/capabilities`
- `GET /admin/policies`
- `PUT /admin/policies/{role_name}`

Admin UI pages:

- `http://127.0.0.1:8000/ui/login.html`
- `http://127.0.0.1:8000/ui/admin/index.html`
- `http://127.0.0.1:8000/ui/admin/users.html`
- `http://127.0.0.1:8000/ui/admin/roles.html`
- `http://127.0.0.1:8000/ui/admin/policies.html`

The login page is branded as `TELEMART AI Delivery Workflows`.

Supported UI languages:

- `uk` (default)
- `en`

Language selection is persisted in `localStorage` and a `ui_lang` cookie. User-facing workflow, run, and pre-review messages are localized from the shared catalog, while code identifiers such as file paths, modules, symbols, endpoint names, and issue keys remain untranslated.

Seeded default roles:

- `analyst`
- `developer`
- `techlead`
- `admin`

The technical runs table now includes:

- initiating user
- start datetime

Bootstrap the first admin only when the user directory is empty:

```dotenv
BOOTSTRAP_ADMIN_USERNAME=admin
BOOTSTRAP_ADMIN_PASSWORD=change-me-now
BOOTSTRAP_ADMIN_DISPLAY_NAME=Platform Admin
```

For local development only, passwordless login can be enabled explicitly:

```dotenv
ALLOW_DEV_LOGIN=true
```

When enabled, existing users can log in by username without a password. Keep this off outside local dev.

Local fallback password reset command:

```bash
.venv\Scripts\python.exe main.py reset-password <username> --generate
```

Password handling:

- plain passwords are never stored
- generated passwords are shown only once on create/reset
- custom passwords must pass basic strength checks
- `must_change_password=true` forces the user to change the password after admin create/reset

Development-only fallback:

- `ALLOW_HEADER_ACTOR_FALLBACK=false` by default
- when explicitly enabled, old header-based actor identity can still be used for local/dev compatibility

## Validation

Run tests locally with:

```bash
.venv\Scripts\python.exe -m unittest
```

You can also start the app without Docker:

```bash
.venv\Scripts\python.exe -m uvicorn web_app:app --host 127.0.0.1 --port 8000
```

For Docker:

```bash
docker compose up --build
```

## Production Note

The intended production shape is:
- app in Docker
- Postgres in Docker
- environment-driven configuration from `.env` or container environment variables

This change set keeps the existing runtime architecture intact and only adds UTF-8 cleanup, Docker packaging, and clearer operational documentation.
