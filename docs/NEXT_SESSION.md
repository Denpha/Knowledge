# Next Session Quick Start

## Current State (as of 2026-04-08)
- Frontend login/register pages integrated with free-react-tailwind-admin-dashboard UI template.
- Admin dashboard layout integrated (sidebar, header, theme toggle, dark mode).
- API build succeeded (.NET 10, net10.0 target).
- pgvector extension installed (postgresql-18-pgvector), migrations applied to KMS and KMS_Dev.
- Database seeded: 4 users, 5 categories, 10 tags, 2 articles.
- Admin account: username `denpha` / email `denpha.sa@rmuti.ac.th` / password `Denpha_@$&2022`
- Frontend login flow fixed to match backend auth contract (username mapped from email).
- API CORS enabled for local frontend origins (http://localhost:5173 and http://localhost:5174).
- Tailwind v4/PostCSS setup fixed:
  - Uses @tailwindcss/postcss plugin.
  - Global CSS updated for Tailwind v4 compatibility.
- Home page now shows auth status, current user info, roles, and logout action.
- RAG benchmark shared history features are implemented:
  - shared history list + filters
  - load run from history
  - replay fidelity via persisted compare input snapshot
  - analytics endpoint + UI metrics

## Verified Working
- API login endpoint returns 200 with admin credentials (username: denpha / password: Denpha_@$&2022).
- CORS preflight to /api/auth/login returns 204 with Access-Control-Allow-Origin.
- Frontend build passes via Vite build.
- Database seeded successfully in KMS_Dev (Development) database.

## Quick Run Commands
1) API
- cd /home/denpha/Knowledge
- ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/KMS.Api --urls http://localhost:5000
- (หรือรัน DLL โดยตรง) ASPNETCORE_ENVIRONMENT=Development dotnet src/KMS.Api/bin/Debug/net10.0/KMS.Api.dll

2) Frontend
- cd /home/denpha/Knowledge/kms-web
- npm run dev

## Smoke Check (first 5 minutes)
- Open frontend URL shown by Vite (usually http://localhost:5173 or http://localhost:5174).
- Login with seeded admin account.
- Confirm header shows Logged in + user identity.
- Open /admin/rag and verify:
  - benchmark runner loads
  - shared history table loads
  - analytics panel loads

## Suggested Next Work
1) Deployment hardening
- production appsettings review
- secret management pass
- CORS/host validation for staging/prod

2) Security remediation
- update vulnerable dependencies (notably Npgsql advisory)
- rebuild and run targeted smoke tests

3) Release readiness
- create short go-live and rollback checklist

## Notes
- Keep sensitive keys out of committed files before production rollout.
- If login fails again, verify API is running and browser is calling http://localhost:5000.
