# HalbotWeb
Fitness activity tracker revisited

# Frontend (Svelte)
- Source is in `Frontend`.
- Production files are generated into `wwwroot` and served by ASP.NET Core.

## Run in development
1. Run backend with HTTPS profile (`https://localhost:7263`).
2. In a second terminal:

```bash
cd Frontend
npm install
npm run dev
```

3. Open the Vite URL (default `http://localhost:5173`).
	API requests are proxied to the backend and use cookie auth.

## Build frontend for backend hosting

```bash
cd Frontend
npm run build
```

This writes static assets to `wwwroot`.

## VS Code dev task (frontend + backend)
Run the combined task from VS Code:

1. `Terminal` -> `Run Task...`
2. Select `dev: full stack`

This starts:
- `dev: backend` (`dotnet watch run --launch-profile https`)
- `dev: frontend` (`npm run dev` in `Frontend`)

Use `Terminal` -> `Terminate Task...` to stop them.

# Todo
- Remaining endpoints
- Validation (fluent?)
- Fix database column name(s)
- Setup logging
- Test cachings
