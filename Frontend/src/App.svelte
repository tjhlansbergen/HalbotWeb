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

  $: rows = [
    ...(showRunning ? activities.map(a => ({ type: 'activity', date: a.date, data: a })) : []),
    ...(showStrength ? workouts.map(w => ({ type: 'workout', date: w.date, data: w })) : [])
  ].sort((a, b) => new Date(b.date) - new Date(a.date));

  let showRunning = true;
  let showStrength = true;
  let currentPage = 'home';

  onMount(checkAuth);
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
          <button type="button" class="nav-btn" class:active={currentPage === 'home'} on:click={() => currentPage = 'home'}>Home</button>
          <button type="button" class="nav-btn" class:active={currentPage === 'insights'} on:click={() => currentPage = 'insights'}>Insights</button>
          <button type="button" class="nav-btn" class:active={currentPage === 'import'} on:click={() => currentPage = 'import'}>Import</button>
        </div>
        <button type="button" on:click={logout} disabled={isSubmitting}>Logout</button>
      </div>

      {#if currentPage === 'home'}
        <div class="filters">
          <label><input type="checkbox" bind:checked={showRunning} /> Running</label>
          <label><input type="checkbox" bind:checked={showStrength} /> Strength</label>
        </div>

        <table>
          <tbody>
            {#each rows as row}
              {#if row.type === 'activity'}
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
      {:else if currentPage === 'insights'}
        <p>Insights page coming soon...</p>
      {:else if currentPage === 'import'}
        <p>Import page coming soon...</p>
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
</main>
