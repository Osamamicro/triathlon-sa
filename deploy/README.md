# Deploying the Triathlon platform

One ASP.NET Core application serves both the public website and the staff dashboard, so a
deployment is one `dotnet publish` output plus a set of environment variables. Staging and
production are **two copies of that same output** on the same server: two services (or two IIS
sites), two databases, two media folders, two sets of environment variables. Nothing is shared
between them, so a staging migration can never touch production data.

Nothing here has been applied to the Federation's server yet — there is no server to apply it to.
These files are the reference the first deployment will follow.

| | Production | Staging |
|---|---|---|
| Host name | `triathlon.sa` | `staging.triathlon.sa` |
| Kestrel port (Linux) | `127.0.0.1:5000` | `127.0.0.1:5001` |
| Service / site name | `triathlon` | `triathlon-staging` |
| Database | `triathlon` | `triathlon_staging` |
| Media root | `/var/lib/triathlon/media` | `/var/lib/triathlon-staging/media` |
| `Site__Staging` | `false` | `true` |
| Indexed by search engines | yes | no — Basic auth and `noindex` |

## Files here

| File | What it is |
|---|---|
| `web.config` | IIS hosting (in-process ANCM). `dotnet publish` writes its own; this is the reference and the copy to edit on the server. |
| `triathlon.service` | systemd unit for Kestrel behind nginx. |
| `nginx.conf` | TLS termination, reverse proxy, and `/media` served straight off disk. |
| `backup.sh` | Nightly database + media backup on Linux, 30-day retention. |
| `backup.ps1` | The same on Windows, for either database provider. |

## Environment variables

Everything below is set per deployment. Secrets belong in the environment file
(`/etc/triathlon/production.env`, root-owned, mode `0600`) or in the IIS site's environment
variables — never in `appsettings.json`, which is in source control.

| Variable | Example | Notes |
|---|---|---|
| `ConnectionStrings__Default` | `Host=localhost;Port=5432;Database=triathlon;Username=triathlon;Password=…` | Also read by the backup scripts. |
| `Database__Provider` | `Postgres` | Or `SqlServer`. Chooses the EF provider *and* the Hangfire storage. |
| `Database__MigrateOnStartup` | `false` | Keep it off on a server; migrations are a deployment step (below). |
| `Seed__AdminEmail` | `admin@triathlon.sa` | Only used when the user table is empty. |
| `Seed__AdminPassword` | *(strong, one-time)* | Change it through the dashboard after the first sign-in. |
| `Site__Staging` | `false` / `true` | `true` turns on the Basic-auth gate and `noindex`. |
| `Site__BasicAuth__User` | `stf` | Required when `Site__Staging` is `true`; the app refuses to start without it. |
| `Site__BasicAuth__Password` | *(shared with the client)* | One shared credential — a keep-out sign, not an account. |
| `Site__BehindProxy` | `true` | `true` behind nginx or ARR; `false` when Kestrel is exposed directly. See the trust note below. |
| `Email__Host` | `smtp.example.com` | **Empty disables sending** and logs the subject and recipient instead. |
| `Email__Port` | `587` | |
| `Email__UseStartTls` | `true` | |
| `Email__User` / `Email__Password` | | Omit both for an unauthenticated relay. |
| `Email__FromName` / `Email__FromAddress` | `Saudi Triathlon Federation` / `no-reply@triathlon.sa` | |
| `Media__Root` | `/var/lib/triathlon/media` | **Point this outside the deployment folder**, or a redeploy deletes every upload. |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Staging uses `Production` too — `Site__Staging` is what makes it staging, not the framework's environment name. |

### The `Site__BehindProxy` trust assumption

With `Site__BehindProxy=true` the application accepts `X-Forwarded-For` and `X-Forwarded-Proto`
from **any** caller: the known-proxy allow list is deliberately cleared, because the proxy's address
on this server is loopback and pinning it adds nothing.

That is only safe while nginx is the sole thing that can reach Kestrel. `triathlon.service` binds
`ASPNETCORE_URLS` to `127.0.0.1`, which is what enforces it. If Kestrel is ever bound to a public
interface, set `Site__BehindProxy=false` — otherwise any visitor could forge their own address and
walk around the public rate limiter.

## Deploying

```bash
# On a build machine, or from the CI artifact.
dotnet publish src/Triathlon.Web -c Release -o publish

# Migrate first, while the old version is still serving — every migration in this project is
# additive, so the running application tolerates the new schema.
dotnet ef database update --project src/Triathlon.Web --context PostgresDbContext
#   ... or, on SQL Server:
dotnet ef database update --project src/Triathlon.Web --context SqlServerDbContext

# Then swap the files and restart.
sudo systemctl stop triathlon
sudo rsync -a --delete --exclude 'logs' publish/ /var/www/triathlon/
sudo systemctl start triathlon
curl -fsS https://triathlon.sa/health     # must print "Healthy"
```

Hangfire creates and upgrades its own tables on first start, so it needs no migration step.

On IIS, replace the `systemctl` lines with `Stop-WebAppPool` / `Start-WebAppPool` around the file
copy — the app pool must be stopped or the DLLs are locked.

## Health and jobs

- `GET /health` — anonymous, uncached, and the one path the staging gate lets through. It checks
  that the database is reachable, which is the failure that actually takes the site down.
- `/dashboard/jobs` — the Hangfire dashboard, restricted to the `SuperAdmin` role.

## Restore drill

Run this on a scratch database once before go-live, and again whenever the backup script changes.
An untested backup is not a backup.

1. Pick a backup: `ls -lh /var/backups/triathlon/`.
2. Create an empty target: `createdb triathlon_restore_test`.
3. Restore the database:
   `pg_restore --dbname=triathlon_restore_test --no-owner /var/backups/triathlon/triathlon-<stamp>.dump`
   (SQL Server: `RESTORE DATABASE [triathlon_restore_test] FROM DISK = N'…\triathlon-<stamp>.bak' WITH MOVE …, RECOVERY;`)
4. Restore media to a scratch folder:
   `mkdir -p /tmp/restore && tar -xzf /var/backups/triathlon/media-<stamp>.tar.gz -C /tmp/restore`
5. Point a scratch instance at both — `ConnectionStrings__Default` on the restored database,
   `Media__Root=/tmp/restore/media`, `Site__Staging=true` — and start it on a spare port.
6. Confirm: `/health` is `Healthy`, the home page renders, an administrator can sign in, and an
   image that was uploaded before the backup still loads.
7. Write down how long steps 1–6 took. That number is the recovery time to quote the Federation.
8. Tear the scratch database and folder down.
