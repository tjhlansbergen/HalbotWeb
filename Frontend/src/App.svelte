<script>
  import { onMount } from "svelte";

  let isLoggedIn = false;
  let activities = [];
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

  async function checkAuth() {
    isCheckingAuth = true;
    error = "";

    try {
      const result = await fetchActivities();
      isLoggedIn = result.loggedIn;
      activities = result.items;
    } catch {
      isLoggedIn = false;
      activities = [];
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

  onMount(checkAuth);
</script>

<main>
  {#if isCheckingAuth}
    <section class="card">
      <h1>Halbot</h1>
      <p>Checking login status...</p>
    </section>
  {:else if isLoggedIn}
    <section class="card">
      <h1>Welcome</h1>
      <p>Logged in.</p>
      <button type="button" on:click={logout} disabled={isSubmitting}>Logout</button>

      <table>
        <thead>
          <tr>
            <th>distance</th>
            <th>pace</th>
            <th>climb</th>
            <th>date</th>
            <th>effort</th>
          </tr>
        </thead>
        <tbody>
          {#each activities as activity}
            <tr>
              <td>{formatDistance(activity.distance)}</td>
              <td>{activity.pace}</td>
              <td>{formatClimb(activity.climb)}</td>
              <td>{formatDate(activity.date)}</td>
              <td>{activity.effort}</td>
            </tr>
          {/each}
        </tbody>
      </table>
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
