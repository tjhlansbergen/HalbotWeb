<script>
  import { onMount } from "svelte";

  let isLoggedIn = false;
  let activities = [];
  let workouts = [];
  let usernameInput = "";
  let passwordInput = "";
  let isCheckingAuth = true;
  let isSubmitting = false;
  let error = "";
  let logs = [];
  let isLoadingLogs = false;
  let logsError = "";
  const THEME_STORAGE_KEY = "halbot-theme";
  const currentYear = new Date().getFullYear();
  const LONG_MAX = 9223372036854775807n;
  const LOG_SEVERITY_LEVEL = Object.freeze({
    0: "Info",
    1: "Warning",
    2: "Error"
  });

  function getTodayDateInput() {
    const now = new Date();
    now.setMinutes(now.getMinutes() - now.getTimezoneOffset());
    return now.toISOString().slice(0, 10);
  }

  async function fetchActivities() {
    const response = await fetch("/api/activities/", {
      method: "GET",
      credentials: "include"
    });

    if (response.status === 401) {
      return { loggedIn: false, items: [] };
    }

    if (!response.ok) {
      throw new Error("Unexpected response while loading activities.");
    }

    const items = await response.json();
    return { loggedIn: true, items: Array.isArray(items) ? items : [] };
  }

  async function fetchWorkouts() {
    try {
      const response = await fetch("/api/workouts/", {
        method: "GET",
        credentials: "include"
      });
      if (!response.ok) return [];
      const items = await response.json();
      return Array.isArray(items) ? items : [];
    } catch {
      return [];
    }
  }

  async function checkAuth() {
    isCheckingAuth = true;
    error = "";

    try {
      const result = await fetchActivities();
      isLoggedIn = result.loggedIn;
      activities = result.items;
      if (result.loggedIn) {
        workouts = await fetchWorkouts();
      }
    } catch {
      isLoggedIn = false;
      activities = [];
      workouts = [];
      error = "Could not reach the server.";
    } finally {
      isCheckingAuth = false;
    }
  }

  async function login(event) {
    event.preventDefault();
    isSubmitting = true;
    error = "";

    try {
      const response = await fetch("/api/login", {
        method: "POST",
        credentials: "include",
        headers: {
          "Content-Type": "application/json"
        },
        body: JSON.stringify({
          username: usernameInput,
          password: passwordInput
        })
      });

      if (!response.ok) {
        error = "Invalid username or password.";
        return;
      }

      passwordInput = "";
      await checkAuth();
    } catch {
      error = "Could not reach the server.";
    } finally {
      isSubmitting = false;
    }
  }

  async function logout() {
    isSubmitting = true;
    error = "";

    try {
      await fetch("/api/logout", {
        method: "POST",
        credentials: "include"
      });
      isLoggedIn = false;
      activities = [];
      workouts = [];
      logs = [];
      currentPage = "home";
      passwordInput = "";
    } catch {
      error = "Could not reach the server.";
    } finally {
      isSubmitting = false;
    }
  }

  function formatDate(value) {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return value;
    }

    return new Intl.DateTimeFormat("en-US", {
      weekday: "long",
      month: "long",
      day: "numeric",
      year: "numeric"
    }).format(date);
  }

  function formatDateTime(value) {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return value;
    }

    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, "0");
    const day = String(date.getDate()).padStart(2, "0");
    const hour = String(date.getHours()).padStart(2, "0");
    const minute = String(date.getMinutes()).padStart(2, "0");
    return `${year}-${month}-${day} ${hour}:${minute}`;
  }

  function formatLogSeverity(value) {
    if (typeof value === "number") {
      return LOG_SEVERITY_LEVEL[value] ?? `Unknown (${value})`;
    }

    if (typeof value === "string") {
      const trimmed = value.trim();
      return trimmed || "Unknown";
    }

    return "Unknown";
  }

  function formatDistance(value) {
    if (typeof value !== "number") {
      return "";
    }

    const kilometers = value / 1000;
    return `${kilometers.toFixed(2).replace(".", ",")} km`;
  }

  function formatClimb(value) {
    if (typeof value !== "number" || value <= 0) {
      return "-";
    }

    return `${Math.round(value)}m`;
  }

  function getRunningIdWarning(value) {
    const raw = String(value).trim();

    if (!raw) {
      return "";
    }

    if (!/^\d+$/.test(raw)) {
      return "Running ID must be numeric only.";
    }

    try {
      const numeric = BigInt(raw);
      if (numeric < 1n || numeric > LONG_MAX) {
        return "Running ID must be between 1 and 9223372036854775807.";
      }
    } catch {
      return "Running ID is invalid.";
    }

    return "";
  }

  async function refreshLists() {
    const result = await fetchActivities();
    isLoggedIn = result.loggedIn;
    activities = result.items;
    workouts = result.loggedIn ? await fetchWorkouts() : [];
  }

  async function submitRunningImport(event) {
    event.preventDefault();
    runningImportMessage = "";
    runningImportError = "";

    if (runningIdWarning) {
      runningImportError = runningIdWarning;
      return;
    }

    isImportingRunning = true;

    try {
      const params = new URLSearchParams({
        garminId: runningIdInput.trim(),
        date: runningDateInput
      });

      const response = await fetch(`/api/activities/?${params.toString()}`, {
        method: "POST",
        credentials: "include"
      });

      if (!response.ok) {
        throw new Error("Failed to import running activity.");
      }

      await refreshLists();
      runningImportMessage = "Running activity imported.";
      runningIdInput = "";
      runningDateInput = getTodayDateInput();
    } catch {
      runningImportError = "Could not import running activity.";
    } finally {
      isImportingRunning = false;
    }
  }

  async function submitWorkoutImport(event) {
    event.preventDefault();
    workoutImportMessage = "";
    workoutImportError = "";
    isImportingWorkout = true;

    try {
      const response = await fetch("/api/workouts/", {
        method: "POST",
        credentials: "include",
        headers: {
          "Content-Type": "application/json"
        },
        body: JSON.stringify({
          minutes: Number(workoutMinutesInput),
          date: workoutDateInput
        })
      });

      if (!response.ok) {
        throw new Error("Failed to import strength workout.");
      }

      await refreshLists();
      workoutImportMessage = "Strength workout imported.";
      workoutDateInput = getTodayDateInput();
    } catch {
      workoutImportError = "Could not import strength workout.";
    } finally {
      isImportingWorkout = false;
    }
  }

  async function openLogsPage() {
    if (!isLoggedIn) {
      return;
    }

    currentPage = "logs";
    isLoadingLogs = true;
    logsError = "";

    try {
      const response = await fetch("/api/logs/", {
        method: "GET",
        credentials: "include"
      });

      if (response.status === 401) {
        isLoggedIn = false;
        logs = [];
        currentPage = "home";
        return;
      }

      if (!response.ok) {
        throw new Error("Unexpected response while loading logs.");
      }

      const items = await response.json();
      logs = Array.isArray(items) ? items : [];
    } catch {
      logsError = "Could not load logs.";
    } finally {
      isLoadingLogs = false;
    }
  }

  function readStoredTheme() {
    if (typeof window === "undefined") {
      return null;
    }

    const storedTheme = window.localStorage.getItem(THEME_STORAGE_KEY);
    if (storedTheme === "light" || storedTheme === "dark") {
      return storedTheme;
    }

    return null;
  }

  function saveStoredTheme(theme) {
    if (typeof window === "undefined") {
      return;
    }

    if (theme === "light" || theme === "dark") {
      window.localStorage.setItem(THEME_STORAGE_KEY, theme);
    }
  }

  function toggleTheme() {
    manualTheme = isDarkTheme ? "light" : "dark";
    saveStoredTheme(manualTheme);
  }

  const workoutMinuteOptions = Array.from({ length: 18 }, (_, i) => (i + 1) * 5);

  let showRunning = true;
  let showStrength = true;
  let currentPage = "home";
  let runningIdInput = "";
  let runningDateInput = getTodayDateInput();
  let workoutMinutesInput = "20";
  let workoutDateInput = getTodayDateInput();
  let isImportingRunning = false;
  let isImportingWorkout = false;
  let runningImportMessage = "";
  let workoutImportMessage = "";
  let runningImportError = "";
  let workoutImportError = "";
  let prefersDark = typeof window !== "undefined"
    ? window.matchMedia("(prefers-color-scheme: dark)").matches
    : false;
  let manualTheme = readStoredTheme();
  $: resolvedTheme = manualTheme ?? (prefersDark ? "dark" : "light");
  $: isDarkTheme = resolvedTheme === "dark";
  $: if (typeof document !== "undefined") {
    document.documentElement.dataset.theme = resolvedTheme;
  }
  $: runningIdWarning = getRunningIdWarning(runningIdInput);

  $: rows = [
    ...(showRunning ? activities.map(a => ({ type: "activity", date: a.date, data: a })) : []),
    ...(showStrength ? workouts.map(w => ({ type: "workout", date: w.date, data: w })) : [])
  ].sort((a, b) => new Date(b.date) - new Date(a.date));

  onMount(() => {
    checkAuth();

    if (typeof window === "undefined") {
      return;
    }

    const mediaQuery = window.matchMedia("(prefers-color-scheme: dark)");
    prefersDark = mediaQuery.matches;
    manualTheme = readStoredTheme();

    const onThemeChange = event => {
      prefersDark = event.matches;
    };

    if (typeof mediaQuery.addEventListener === "function") {
      mediaQuery.addEventListener("change", onThemeChange);
      return () => mediaQuery.removeEventListener("change", onThemeChange);
    }

    mediaQuery.addListener(onThemeChange);
    return () => mediaQuery.removeListener(onThemeChange);
  });
</script>

<main>
  {#if isCheckingAuth}
    <section class="card">
      <h1>Halbot</h1>
      <p>Checking login status...</p>
    </section>
  {:else if isLoggedIn}
    <section class="card wide">
      <div class="card-header">
        <div class="nav-buttons">
          <button type="button" class="nav-btn" class:active={currentPage === "home"} on:click={() => currentPage = "home"}>Home</button>
          <button type="button" class="nav-btn" class:active={currentPage === "insights"} on:click={() => currentPage = "insights"}>Insights</button>
          <button type="button" class="nav-btn" class:active={currentPage === "import"} on:click={() => currentPage = "import"}>Import</button>
        </div>
        <div class="header-actions">
          <button
            type="button"
            class="theme-toggle-btn"
            on:click={toggleTheme}
            aria-label={isDarkTheme ? "Switch to light mode" : "Switch to dark mode"}
            title={isDarkTheme ? "Switch to light mode" : "Switch to dark mode"}
          >
            {#if isDarkTheme}
              <svg viewBox="0 0 24 24" aria-hidden="true" focusable="false">
                <circle cx="12" cy="12" r="4.5"></circle>
                <line x1="12" y1="2.5" x2="12" y2="5"></line>
                <line x1="12" y1="19" x2="12" y2="21.5"></line>
                <line x1="2.5" y1="12" x2="5" y2="12"></line>
                <line x1="19" y1="12" x2="21.5" y2="12"></line>
                <line x1="5.3" y1="5.3" x2="7.1" y2="7.1"></line>
                <line x1="16.9" y1="16.9" x2="18.7" y2="18.7"></line>
                <line x1="5.3" y1="18.7" x2="7.1" y2="16.9"></line>
                <line x1="16.9" y1="7.1" x2="18.7" y2="5.3"></line>
              </svg>
            {:else}
              <svg viewBox="0 0 24 24" aria-hidden="true" focusable="false">
                <path d="M20 14.1A8.4 8.4 0 0 1 9.9 4 8.8 8.8 0 1 0 20 14.1z"></path>
              </svg>
            {/if}
          </button>
          <button type="button" class="nav-btn logout-btn" on:click={logout} disabled={isSubmitting}>Logout</button>
        </div>
      </div>

      {#if currentPage === "home"}
        <div class="filters">
          <label><input type="checkbox" bind:checked={showRunning} /> Running</label>
          <label><input type="checkbox" bind:checked={showStrength} /> Strength</label>
        </div>

        <table>
          <tbody>
            {#each rows as row}
              {#if row.type === "activity"}
                <tr>
                  <td>{formatDistance(row.data.distance)}</td>
                  <td>{row.data.pace}</td>
                  <td>{formatClimb(row.data.climb)}</td>
                  <td>{formatDate(row.data.date)}</td>
                  <td>{row.data.effort}</td>
                </tr>
              {:else}
                <tr>
                  <td>{row.data.minutes} min</td>
                  <td>Strength</td>
                  <td>-</td>
                  <td>{formatDate(row.data.date)}</td>
                  <td>-</td>
                </tr>
              {/if}
            {/each}
          </tbody>
        </table>
      {:else if currentPage === "insights"}
        <p>Insights page coming soon...</p>
      {:else if currentPage === "import"}
        <div class="import-grid">
          <section class="import-section">
            <h2>Running</h2>
            <form class="import-form" on:submit={submitRunningImport}>
              <label>
                Activity ID
                <input
                  type="text"
                  inputmode="numeric"
                  placeholder="e.g. 12341234123"
                  bind:value={runningIdInput}
                  required
                />
              </label>

              {#if runningIdWarning}
                <p class="inline-warning">{runningIdWarning}</p>
              {/if}

              <label>
                Date
                <input type="date" bind:value={runningDateInput} required />
              </label>

              <button type="submit" disabled={isImportingRunning || !runningIdInput.trim() || !!runningIdWarning}>
                {#if isImportingRunning}Importing...{:else}Import{/if}
              </button>
            </form>

            {#if runningImportError}
              <p class="inline-warning">{runningImportError}</p>
            {/if}
            {#if runningImportMessage}
              <p class="inline-success">{runningImportMessage}</p>
            {/if}
          </section>

          <section class="import-section">
            <h2>Strength</h2>
            <form class="import-form" on:submit={submitWorkoutImport}>
              <label>
                Minutes
                <select bind:value={workoutMinutesInput}>
                  {#each workoutMinuteOptions as minutes}
                    <option value={String(minutes)}>{minutes}</option>
                  {/each}
                </select>
              </label>

              <label>
                Date
                <input type="date" bind:value={workoutDateInput} required />
              </label>

              <button type="submit" disabled={isImportingWorkout}>
                {#if isImportingWorkout}Importing...{:else}Import{/if}
              </button>
            </form>

            {#if workoutImportError}
              <p class="inline-warning">{workoutImportError}</p>
            {/if}
            {#if workoutImportMessage}
              <p class="inline-success">{workoutImportMessage}</p>
            {/if}
          </section>
        </div>
      {:else if currentPage === "logs"}
        <section class="logs-section">
          <hr class="logs-divider" />

          {#if logsError}
            <p class="inline-warning">{logsError}</p>
          {:else if isLoadingLogs}
            <p>Loading logs...</p>
          {:else}
            <table class="logs-table">
              <thead>
                <tr>
                  <th>Date/Time</th>
                  <th>Severity</th>
                  <th>Message</th>
                </tr>
              </thead>
              <tbody>
                {#if logs.length === 0}
                  <tr>
                    <td colspan="3">No log entries found.</td>
                  </tr>
                {:else}
                  {#each logs as logEntry}
                    <tr>
                      <td>{formatDateTime(logEntry.dateTime)}</td>
                      <td>{formatLogSeverity(logEntry.severity)}</td>
                      <td>{logEntry.message ?? "-"}</td>
                    </tr>
                  {/each}
                {/if}
              </tbody>
            </table>
          {/if}
        </section>
      {/if}
    </section>
  {:else}
    <section class="card">
      <h1>Sign in</h1>
      <form on:submit={login}>
        <label>
          Username
          <input bind:value={usernameInput} autocomplete="username" required />
        </label>

        <label>
          Password
          <input type="password" bind:value={passwordInput} autocomplete="current-password" required />
        </label>

        <button type="submit" disabled={isSubmitting}>
          {#if isSubmitting}Signing in...{:else}Login{/if}
        </button>
      </form>
    </section>
  {/if}

  {#if error}
    <p class="error">{error}</p>
  {/if}

  <footer class="app-footer">
    <span>&copy; {currentYear}</span>
    <span aria-hidden="true"> | </span>
    <button type="button" class="footer-link" on:click={openLogsPage}>View log</button>
  </footer>
</main>
