# Documentation Index

All project documentation lives here. Different docs for different audiences.

## Start here

| If you are... | Read |
|---------------|------|
| **A future maintainer of Dev A's work** | [HANDOVER.md](HANDOVER.md) |
| **New to WinUI / MVVM / DI** | [client-guide/README.md](client-guide/README.md) |
| **Looking for a specific file** | [IMPLEMENTATION-MAP.md](IMPLEMENTATION-MAP.md) |
| **Debugging an issue** | [FAQ.md](FAQ.md) → [FLOWS.md](FLOWS.md) |
| **About to install/build the app** | [install.md](install.md) |
| **Joining the team** | [plans/project-roadmap-3-weeks.md](plans/project-roadmap-3-weeks.md) |

## Folder map

```
docs/
├── README.md                           ← you are here
├── HANDOVER.md                         ← Dev A handover (start here for anything Dev A built)
├── IMPLEMENTATION-MAP.md               ← feature → file path lookup
├── FLOWS.md                            ← step-by-step runtime flows for each user action
├── FAQ.md                              ← common questions and answers
├── install.md                          ← build / install / sideload / production deploy
├── responsive-pattern.md               ← layout pattern (used by all devs)
├── erd.md                              ← ER diagram of the database
├── feature-specs.md                    ← feature requirements from the instructor
├── plans/
│   ├── project-roadmap-3-weeks.md     ← team roadmap (Dev A + Dev B + Dev C)
│   └── dev-a-master-plan.md           ← Dev A's 7-phase plan with status checkboxes
├── features/                           ← feature handover (one .md per feature)
│   ├── README.md
│   ├── 01-login-config.md
│   ├── 02-dashboard.md
│   ├── 03-settings.md
│   ├── 04-installer.md
│   ├── 05-role-based-access.md
│   ├── 06-backup-restore.md
│   ├── 07-obfuscator.md
│   ├── 08-trial-mode.md
│   ├── 09-onboarding.md
│   ├── 10-responsive-layout.md
│   ├── 11-image-upload.md
│   └── 12-data-seeding.md
└── client-guide/                       ← teaching material for new WinUI devs
    ├── README.md
    ├── 01-anatomy-of-a-page.md
    ├── 02-mvvm-and-di.md
    ├── 03-app-startup-flow.md
    ├── 04-login-flow.md
    ├── 05-config-flow.md
    ├── 06-graphql-client.md
    ├── 07-dashboard-flow.md
    ├── 08-role-based-ui.md
    └── 09-how-to-add-a-feature.md
```

## Reading order recommendations

### "I need to keep building Dev A's features"
1. [HANDOVER.md](HANDOVER.md) — what's done, file inventory, coordination notes
2. [plans/dev-a-master-plan.md](plans/dev-a-master-plan.md) — phase plan
3. [IMPLEMENTATION-MAP.md](IMPLEMENTATION-MAP.md) — file paths
4. Specific feature in [features/](features/) — for the architecture

### "I'm a new WinUI dev joining the team"
1. [client-guide/README.md](client-guide/README.md) — orientation
2. [client-guide/01-anatomy-of-a-page.md](client-guide/01-anatomy-of-a-page.md) — XAML/code-behind basics
3. [client-guide/02-mvvm-and-di.md](client-guide/02-mvvm-and-di.md) — patterns
4. [client-guide/04-login-flow.md](client-guide/04-login-flow.md) — full trace example
5. [client-guide/09-how-to-add-a-feature.md](client-guide/09-how-to-add-a-feature.md) — recipe to start contributing

### "I need to debug something"
1. [FAQ.md](FAQ.md) — check known issues first
2. [FLOWS.md](FLOWS.md) — find the user action and trace
3. [IMPLEMENTATION-MAP.md](IMPLEMENTATION-MAP.md) — locate the file

### "I need to install on a fresh machine"
1. [install.md](install.md)

## Conventions

- Code references use file paths from project root, e.g. `hcmus-shop/Services/Auth/AuthService.cs`
- All documentation is markdown, Mermaid for diagrams (renders in GitHub + VS Code with extension)
- Feature docs follow a consistent template (Summary → Architecture → Files → Data flow → Business rules → Edge cases → Verification → Extension points)

## What's NOT in docs (and why)

- **Database connection details** — those are in `hcmus-shop-server/.env` and `.env.example`
- **Default credentials** — see [HANDOVER.md](HANDOVER.md) (admin/admin123, sale/sale123)
- **API contract** — derivable from `hcmus-shop-server/src/features/*/typeDef.graphql` files (run server and use Apollo Sandbox introspection)
- **CI/CD pipelines** — none configured (manual builds in Visual Studio for now)
