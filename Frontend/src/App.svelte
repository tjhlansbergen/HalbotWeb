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

  function formatDateCompact(value) {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return value;
    }

    return new Intl.DateTimeFormat("en-US", {
      month: "short",
      day: "numeric",
      year: "2-digit"
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
    return kilometers.toFixed(2).replace(".", ",");
  }

  function formatClimb(value) {
    if (typeof value !== "number" || value <= 0) {
      return "-";
    }

    return `${Math.round(value)}`;
  }

  function getRunBand(distanceMeters) {
    const km = (distanceMeters ?? 0) / 1000;
    if (km < 5)  return "run-xs";
    if (km < 10) return "run-s";
    if (km < 20) return "run-m";
    if (km < 35) return "run-l";
    if (km < 60) return "run-xl";
    return "run-xxl";
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

  function toDateInput(value) {
    const raw = String(value ?? "");
    const matched = raw.match(/^\d{4}-\d{2}-\d{2}/);
    if (matched) {
      return matched[0];
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return getTodayDateInput();
    }

    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, "0");
    const day = String(date.getDate()).padStart(2, "0");
    return `${year}-${month}-${day}`;
  }

  function openDetailPage(row) {
    selectedDetailItem = row;
    detailPageTitle = row.type === "activity" ? "Run" : "Strength training";

    if (row.type === "workout") {
      detailWorkoutMinutesInput = String(row.data.minutes ?? 20);
      detailWorkoutDateInput = toDateInput(row.data.date);
      detailWorkoutNotesInput = row.data.notes ?? "";
    }

    currentPage = "detail";
  }

  function closeDetailPage() {
    selectedDetailItem = null;
    detailPageTitle = "";
    currentPage = "home";
  }

  async function saveSelectedDetail() {
    if (!selectedDetailItem || selectedDetailItem.type !== "workout") {
      return;
    }

    isSavingDetail = true;
    error = "";

    try {
      const response = await fetch(`/api/workouts/${selectedDetailItem.data.id}`, {
        method: "PUT",
        credentials: "include",
        headers: {
          "Content-Type": "application/json"
        },
        body: JSON.stringify({
          id: selectedDetailItem.data.id,
          minutes: Number(detailWorkoutMinutesInput),
          date: detailWorkoutDateInput,
          notes: detailWorkoutNotesInput.trim() ? detailWorkoutNotesInput.trim() : null
        })
      });

      if (response.status === 401) {
        isLoggedIn = false;
        activities = [];
        workouts = [];
        closeDetailPage();
        return;
      }

      if (!response.ok) {
        throw new Error("Save failed.");
      }

      await refreshLists();
      selectedDetailItem = {
        ...selectedDetailItem,
        data: {
          ...selectedDetailItem.data,
          minutes: Number(detailWorkoutMinutesInput),
          date: detailWorkoutDateInput,
          notes: detailWorkoutNotesInput.trim() ? detailWorkoutNotesInput.trim() : null
        }
      };
      closeDetailPage();
    } catch {
      error = "Could not save workout changes.";
    } finally {
      isSavingDetail = false;
    }
  }

  async function deleteSelectedDetail() {
    if (!selectedDetailItem) {
      return;
    }

    isDeletingDetail = true;
    error = "";

    try {
      const endpoint = selectedDetailItem.type === "activity"
        ? `/api/activities/${selectedDetailItem.data.id}`
        : `/api/workouts/${selectedDetailItem.data.id}`;

      const response = await fetch(endpoint, {
        method: "DELETE",
        credentials: "include"
      });

      if (response.status === 401) {
        isLoggedIn = false;
        activities = [];
        workouts = [];
        closeDetailPage();
        return;
      }

      if (!response.ok) {
        throw new Error("Delete failed.");
      }

      await refreshLists();
      closeDetailPage();
    } catch {
      error = "Could not delete item.";
    } finally {
      isDeletingDetail = false;
    }
  }

  const workoutMinuteOptions = Array.from({ length: 18 }, (_, i) => (i + 1) * 5);

  let showRunning = true;
  let showStrength = true;
  let currentPage = "home";
  let detailPageTitle = "";
  let selectedDetailItem = null;
  let detailWorkoutMinutesInput = "20";
  let detailWorkoutDateInput = getTodayDateInput();
  let detailWorkoutNotesInput = "";
  let isSavingDetail = false;
  let isDeletingDetail = false;
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
          <button type="button" class="nav-btn nav-btn-home" class:active={currentPage === "home"} on:click={() => currentPage = "home"} title="Home">
            <svg class="nav-icon" viewBox="0 0 24 24" aria-hidden="true" focusable="false">
              <path d="M10 20v-6h4v6h5v-8h3L12 3 2 12h3v8z"/>
            </svg>
            <span class="nav-label">Home</span>
          </button>
          <button type="button" class="nav-btn nav-btn-insights" class:active={currentPage === "insights"} on:click={() => currentPage = "insights"} title="Insights">
            <svg class="nav-icon" viewBox="0 0 24 24" aria-hidden="true" focusable="false">
              <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-2h2v2zm0-4h-2V9h2v4z"/>
            </svg>
            <span class="nav-label">Insights</span>
          </button>
          <button type="button" class="nav-btn nav-btn-import" class:active={currentPage === "import"} on:click={() => currentPage = "import"} title="Import">
            <svg class="nav-icon" viewBox="0 0 24 24" aria-hidden="true" focusable="false">
              <path d="M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z"/>
            </svg>
            <span class="nav-label">Import</span>
          </button>
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
          <button type="button" class="nav-btn nav-btn-logout" on:click={logout} disabled={isSubmitting} title="Logout">
            <svg class="nav-icon" viewBox="0 0 24 24" aria-hidden="true" focusable="false">
              <path d="M17 7l-1.41 1.41L18.17 11H8v2h10.17l-2.58 2.58L17 17l5-5zM4 5h8V3H4c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h8v-2H4V5z"/>
            </svg>
            <span class="nav-label">Logout</span>
          </button>
        </div>
      </div>

      {#if currentPage === "home"}
        <div class="filters">
          <label><input class="filter-running" type="checkbox" bind:checked={showRunning} /> Running</label>
          <label><input class="filter-strength" type="checkbox" bind:checked={showStrength} /> Strength</label>
        </div>

        <table class="home-table">
          <thead>
            <tr>
              <th>Duration</th>
              <th>Pace</th>
              <th>Climb</th>
              <th>Date</th>
              <th class="effort-col">Effort</th>
            </tr>
          </thead>
          <tbody>
            {#each rows as row}
              {#if row.type === "activity"}
                <tr class="data-row {getRunBand(row.data.distance)}" on:click={() => openDetailPage(row)}>
                  <td>{formatDistance(row.data.distance)}</td>
                  <td><svg class="row-icon row-icon-pre" viewBox="0 0 24 24" aria-hidden="true" focusable="false"><circle cx="12" cy="13" r="7"/><polyline points="12 10 12 14"/><path d="M10 3h4"/><line x1="12" y1="3" x2="12" y2="6"/></svg>{row.data.pace}</td>
                  <td>{#if typeof row.data.climb === 'number' && row.data.climb > 0}<svg class="row-icon row-icon-pre" viewBox="0 0 24 24" aria-hidden="true" focusable="false"><polyline points="2 20 12 4 22 20"/><line x1="2" y1="20" x2="22" y2="20"/><polyline points="9 13 12 9 15 13"/></svg>{/if}{formatClimb(row.data.climb)}</td>
                  <td>
                    <span class="date-desktop">{formatDate(row.data.date)}</span>
                    <span class="date-mobile">{formatDateCompact(row.data.date)}</span>
                  </td>
                  <td class="effort-col">{row.data.effort}</td>
                </tr>
              {:else}
                <tr class="data-row row-strength" on:click={() => openDetailPage(row)}>
                  <td>{row.data.minutes}'</td>
                  <td><svg class="row-icon row-icon-pre" viewBox="0 0 24 24" aria-hidden="true" focusable="false"><line x1="6" y1="12" x2="18" y2="12"/><rect x="2" y="9.5" width="4" height="5" rx="1"/><rect x="18" y="9.5" width="4" height="5" rx="1"/><rect x="5" y="7.5" width="3" height="9" rx="1"/><rect x="16" y="7.5" width="3" height="9" rx="1"/></svg></td>
                  <td></td>
                  <td>
                    <span class="date-desktop">{formatDate(row.data.date)}</span>
                    <span class="date-mobile">{formatDateCompact(row.data.date)}</span>
                  </td>
                  <td class="effort-col">{Math.ceil(row.data.minutes / 2)}</td>
                </tr>
              {/if}
            {/each}
          </tbody>
        </table>
      {:else if currentPage === "detail"}
        <section class="detail-page">
          <h2>{detailPageTitle}</h2>

          {#if selectedDetailItem?.type === "workout"}
            <section class="import-section">
              <form class="import-form" on:submit|preventDefault={saveSelectedDetail}>
                <label>
                  Minutes
                  <select bind:value={detailWorkoutMinutesInput}>
                    {#each workoutMinuteOptions as minutes}
                      <option value={String(minutes)}>{minutes}</option>
                    {/each}
                  </select>
                </label>

                <label>
                  Date
                  <input type="date" bind:value={detailWorkoutDateInput} required />
                </label>

                <label>
                  Notes
                  <textarea rows="4" bind:value={detailWorkoutNotesInput} placeholder="Optional notes"></textarea>
                </label>
              </form>
            </section>
          {/if}

          <div class="detail-actions">
            <div class="detail-actions-main">
              <button type="button" on:click={saveSelectedDetail} disabled={isDeletingDetail || isSavingDetail}>Save</button>
              <button type="button" class="nav-btn-logout" on:click={deleteSelectedDetail} disabled={isDeletingDetail || isSavingDetail}>Delete</button>
            </div>
            <button type="button" on:click={closeDetailPage} disabled={isDeletingDetail || isSavingDetail}>Close</button>
          </div>
        </section>
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

              <button type="submit" class="import-btn" disabled={isImportingRunning || !runningIdInput.trim() || !!runningIdWarning}>
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

              <button type="submit" class="import-btn" disabled={isImportingWorkout}>
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
          {#if logsError}
            <p class="inline-warning">{logsError}</p>
          {:else if isLoadingLogs}
            <p>Loading logs...</p>
          {:else}
            <table class="logs-table">
              <thead>
                <tr>
                  <th>
                    <span class="date-desktop">Date/Time</span>
                    <span class="date-mobile">Date</span>
                  </th>
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
                      <td>
                        <span class="date-desktop">{formatDateTime(logEntry.dateTime)}</span>
                        <span class="date-mobile">{formatDateCompact(logEntry.dateTime)}</span>
                      </td>
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

  {#if isLoggedIn}
    <footer class="app-footer">
      <span>&copy; {currentYear}</span>
      <span aria-hidden="true"> | </span>
      <button type="button" class="footer-link" on:click={openLogsPage}>View log</button>
    </footer>
  {/if}
</main>
