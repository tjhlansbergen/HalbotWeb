<script lang="ts">
  import { getTodayDateInput, getRunningIdWarning } from "./lib/utils";

  export let runningIdInput = "";
  export let runningDateInput = getTodayDateInput();
  export let workoutMinutesInput = "20";
  export let workoutDateInput = getTodayDateInput();
  export let isImportingRunning = false;
  export let isImportingWorkout = false;
  export let runningImportMessage = "";
  export let workoutImportMessage = "";
  export let runningImportError = "";
  export let workoutImportError = "";
  export let workoutMinuteOptions: number[] = [];
  export let onSubmitRunningImport: (event: Event) => void;
  export let onSubmitWorkoutImport: (event: Event) => void;

  $: runningIdWarning = getRunningIdWarning(runningIdInput);
</script>

<div class="import-grid">
  <section class="import-section">
    <h2>Running</h2>
    <form class="import-form" on:submit={onSubmitRunningImport}>
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
    <form class="import-form" on:submit={onSubmitWorkoutImport}>
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

<style>
  /* Styles are inherited from main app.css */
</style>
