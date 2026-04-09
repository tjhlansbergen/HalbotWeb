# HalbotWeb
Fitness activity tracker revisited

# Todo
- Fix database column name(s)
- Make heartrate editable & make reporting

# Palatte
{"Charcoal Blue":"264653","Verdigris":"2a9d8f","Tuscan Sun":"e9c46a","Sandy Brown":"f4a261","Burnt Peach":"e76f51"}

# Frontend (Svelte)
- Source is in `Frontend`.
- Production files are generated into `wwwroot` and served by ASP.NET Core.
- Put static files that must persist across builds (icons, manifest, robots.txt) in `Frontend/public`.
	Vite copies them to `wwwroot` on each build.

## Run in development (as seperate tasks)
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

Use ctrl-c in each terminal to terminate


## Deploy to prod

This project is deployed as one ASP.NET Core app that serves:

Backend API endpoints
Built Svelte frontend static files from wwwroot
1. Build frontend and publish backend
From the repository root:

```bash
cd Frontend
npm ci
npm run build
cd ..
dotnet publish -c Release -o ./publish
```

What this does:

npm run build writes frontend assets to wwwroot
dotnet publish creates the deployable output in publish

2. Deploy to App Service

VS Code Azure App Service extension:
Right-click the publish folder
Choose "Deploy to Web App"
Select the target App Service

Want to deploy (overwrite) the database? Add it to the publish folder, and move it into c:\home\data in Azure (using Konsole)