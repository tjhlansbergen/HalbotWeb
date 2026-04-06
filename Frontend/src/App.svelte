<script lang="ts">
  import { onMount } from "svelte";
  import HomePage from "./HomePage.svelte";
  import LogsPage from "./LogsPage.svelte";
  import DetailsPage from "./DetailsPage.svelte";
  import ImportPage from "./ImportPage.svelte";
  import {
    getTodayDateInput,
    readStoredTheme,
    saveStoredTheme,
    toDateInput,
    formatDurationInput,
    formatPaceInput,
    isGarminActivity,
    normalizeNumberInput,
    parseDurationInputToSeconds
  } from "./lib/utils";

  let isLoggedIn = false;
  let activities: any[] = [];
  let workouts: any[] = [];
  let usernameInput = "";
  let passwordInput = "";
  let isCheckingAuth = true;
  let isSubmitting = false;
  let error = "";
  let logs: any[] = [];
  let isLoadingLogs = false;
  let logsError = "";

  const currentYear = new Date().getFullYear();
  let currentPage = "home";

  // Home page state
  let showRunning = true;
  let showStrength = true;

  // Detail page state
  let selectedDetailItem: any = null;
  let detailPageTitle = "";
  let detailRunNotesInput = "";
  let detailRunDescriptionInput = "";
  let detailRunIsRaceInput = false;
  let detailRunDateInput = getTodayDateInput();
  let detailRunDistanceInput = "0.00";
  let detailRunClimbInput = "0";
  let detailRunDurationInput = "0:00";
  let detailRunPaceInput = "";
  let activeRunInlineField: string | null = null;
  let detailWorkoutMinutesInput = "20";
  let detailWorkoutDateInput = getTodayDateInput();
  let detailWorkoutNotesInput = "";
  let isSavingDetail = false;
  let isDeletingDetail = false;

  // Import page state
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

  // Theme state
  let prefersDark = typeof window !== "undefined"
    ? window.matchMedia("(prefers-color-scheme: dark)").matches
    : false;
  let manualTheme = readStoredTheme();

  const workoutMinuteOptions = Array.from({ length: 18 }, (_, i) => (i + 1) * 5);

  $: resolvedTheme = manualTheme ?? (prefersDark ? "dark" : "light");
  $: isDarkTheme = resolvedTheme === "dark";
  $: if (typeof document !== "undefined") {
    document.documentElement.dataset.theme = resolvedTheme;
  }

  $: rows = [
    ...(showRunning ? activities.map(a => ({ type: "activity", date: a.date, data: a })) : []),
    ...(showStrength ? workouts.map(w => ({ type: "workout", date: w.date, data: w })) : [])
  ].sort((a: any, b: any) => new Date(b.date).getTime() - new Date(a.date).getTime());

  $: selectedRunIsGarmin = selectedDetailItem?.type === "activity"
    && isGarminActivity(selectedDetailItem.data.dataType);

  async function fetchActivities() {
    try {
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
    } catch {
      return { loggedIn: false, items: [] };
    }
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
      workouts = result.loggedIn ? await fetchWorkouts() : [];
    } catch {
      isLoggedIn = false;
      activities = [];
      workouts = [];
      error = "Could not reach the server.";
    } finally {
      isCheckingAuth = false;
    }
  }

  async function login(event: Event) {
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

  async function refreshLists() {
    const result = await fetchActivities();
    isLoggedIn = result.loggedIn;
    activities = result.items;
    workouts = result.loggedIn ? await fetchWorkouts() : [];
  }

  async function submitRunningImport(event: Event) {
    event.preventDefault();
    runningImportMessage = "";
    runningImportError = "";
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

  async function submitWorkoutImport(event: Event) {
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

  function toggleTheme() {
    manualTheme = isDarkTheme ? "light" : "dark";
    saveStoredTheme(manualTheme);
  }

  function openDetailPage(row: any) {
    selectedDetailItem = row;
    detailPageTitle = row.type === "activity" ? "Run" : "Strength training";

    if (row.type === "activity") {
      detailRunNotesInput = row.data.journal ?? "";
      detailRunDescriptionInput = row.data.description ?? "";
      detailRunIsRaceInput = row.data.isRace === true;
      detailRunDateInput = toDateInput(row.data.date);
      detailRunDistanceInput = ((row.data.distance ?? 0) / 1000).toFixed(2);
      detailRunClimbInput = String(Math.round(row.data.climb ?? 0));
      detailRunDurationInput = formatDurationInput(row.data.duration);
      detailRunPaceInput = formatPaceInput(row.data.pace);
    }

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
    activeRunInlineField = null;
    currentPage = "home";
  }

  async function saveSelectedDetail() {
    if (!selectedDetailItem) {
      return;
    }

    isSavingDetail = true;
    error = "";

    try {
      if (selectedDetailItem.type === "activity") {
        if (!isGarminActivity(selectedDetailItem.data.dataType)) {
          return;
        }

        const distanceKm = normalizeNumberInput(detailRunDistanceInput);
        const climbMeters = normalizeNumberInput(detailRunClimbInput);
        const durationSeconds = parseDurationInputToSeconds(detailRunDurationInput);
        const pace = detailRunPaceInput.trim();

        if (Number.isNaN(distanceKm) || distanceKm < 0) {
          throw new Error("Distance is invalid.");
        }

        if (Number.isNaN(climbMeters) || climbMeters < 0) {
          throw new Error("Climb is invalid.");
        }

        if (durationSeconds === null || durationSeconds <= 0) {
          throw new Error("Duration must be m:ss or h:mm:ss.");
        }

        if (!/^\d{1,2}:\d{2}$/.test(pace)) {
          throw new Error("Pace must be m:ss.");
        }

        const response = await fetch(`/api/activities/${selectedDetailItem.data.id}`, {
          method: "PUT",
          credentials: "include",
          headers: {
            "Content-Type": "application/json"
          },
          body: JSON.stringify({
            date: detailRunDateInput,
            isRace: detailRunIsRaceInput,
            distance: distanceKm * 1000,
            climb: climbMeters,
            duration: durationSeconds,
            pace,
            description: detailRunDescriptionInput.trim() ? detailRunDescriptionInput.trim() : null,
            notes: detailRunNotesInput.trim() ? detailRunNotesInput.trim() : null
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
        closeDetailPage();
        return;
      }

      if (selectedDetailItem.type !== "workout") {
        return;
      }

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
      closeDetailPage();
    } catch (saveError) {
      if (selectedDetailItem?.type === "activity") {
        error = saveError instanceof Error ? saveError.message : "Could not save run changes.";
      } else {
        error = "Could not save workout changes.";
      }
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

  onMount(() => {
    checkAuth();

    if (typeof window === "undefined") {
      return;
    }

    const mediaQuery = window.matchMedia("(prefers-color-scheme: dark)");
    prefersDark = mediaQuery.matches;
    manualTheme = readStoredTheme();

    const onThemeChange = (event: MediaQueryListEvent) => {
      prefersDark = event.matches;
    };

    if (typeof mediaQuery.addEventListener === "function") {
      mediaQuery.addEventListener("change", onThemeChange);
      return () => mediaQuery.removeEventListener("change", onThemeChange);
    }

    (mediaQuery as any).addListener(onThemeChange);
    return () => (mediaQuery as any).removeListener(onThemeChange);
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
            <svg class="theme-icon" viewBox="0 0 24 24" aria-hidden="true" focusable="false">
              {#if isDarkTheme}
                <circle cx="12" cy="12" r="5"/><line x1="12" y1="1" x2="12" y2="3"/><line x1="12" y1="21" x2="12" y2="23"/><line x1="4.22" y1="4.22" x2="5.64" y2="5.64"/><line x1="18.36" y1="18.36" x2="19.78" y2="19.78"/><line x1="1" y1="12" x2="3" y2="12"/><line x1="21" y1="12" x2="23" y2="12"/><line x1="4.22" y1="19.78" x2="5.64" y2="18.36"/><line x1="18.36" y1="5.64" x2="19.78" y2="4.22"/>
              {:else}
                <path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/>
              {/if}
            </svg>
          </button>
          <button type="button" class="nav-btn-logout" on:click={logout} title="Logout">
            <svg class="nav-icon" viewBox="0 0 24 24" aria-hidden="true" focusable="false">
              <path d="M17 7l-1.41 1.41L18.17 11H8v2h10.17l-2.58 2.58L17 17l5-5z"/><path d="M4 5h8V3H4c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h8v-2H4V5z"/>
            </svg>
            <span class="nav-label">Logout</span>
          </button>
        </div>
      </div>

      {#if currentPage === "home"}
        <HomePage {rows} bind:showRunning bind:showStrength onOpenDetail={openDetailPage} />
      {:else if currentPage === "detail"}
        <DetailsPage
          {selectedDetailItem}
          {detailPageTitle}
          {selectedRunIsGarmin}
          {isSavingDetail}
          {isDeletingDetail}
          {error}
          {workoutMinuteOptions}
          bind:detailRunNotesInput
          bind:detailRunDescriptionInput
          bind:detailRunIsRaceInput
          bind:detailRunDateInput
          bind:detailRunDistanceInput
          bind:detailRunClimbInput
          bind:detailRunDurationInput
          bind:detailRunPaceInput
          bind:activeRunInlineField
          bind:detailWorkoutMinutesInput
          bind:detailWorkoutDateInput
          bind:detailWorkoutNotesInput
          onSave={saveSelectedDetail}
          onDelete={deleteSelectedDetail}
          onClose={closeDetailPage}
        />
      {:else if currentPage === "insights"}
        <p>Insights page coming soon...</p>
      {:else if currentPage === "import"}
        <ImportPage
          bind:runningIdInput
          bind:runningDateInput
          bind:workoutMinutesInput
          bind:workoutDateInput
          {isImportingRunning}
          {isImportingWorkout}
          {runningImportMessage}
          {workoutImportMessage}
          {runningImportError}
          {workoutImportError}
          {workoutMinuteOptions}
          onSubmitRunningImport={submitRunningImport}
          onSubmitWorkoutImport={submitWorkoutImport}
        />
      {:else if currentPage === "logs"}
        <LogsPage {logs} isLoading={isLoadingLogs} error={logsError} />
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

      {#if error}
        <p class="inline-warning">{error}</p>
      {/if}
    </section>
  {/if}

  {#if isLoggedIn}
    <footer class="app-footer">
      <span>&copy; {currentYear}</span>
      <span aria-hidden="true"> | </span>
      <button type="button" class="footer-link" on:click={openLogsPage}>View log</button>
    </footer>
  {/if}
</main>

<style global>
  @import "./app.css";
</style>
