<script lang="ts">
  import { tick } from "svelte";
  import {
    formatDate,
    formatDistance,
    formatClimb,
    formatDuration,
    formatPace,
    formatActivityType,
    isGarminActivity,
    parseDurationInputToSeconds,
    normalizeNumberInput,
    GARMIN_CONNECT_LOGO_PATH
  } from "./lib/utils";

  export let selectedDetailItem: any = null;
  export let detailPageTitle = "";
  export let selectedRunIsGarmin = false;
  export let isSavingDetail = false;
  export let isDeletingDetail = false;
  export let error = "";
  export let workoutMinuteOptions: number[] = [];

  // Activity (run) input states
  export let detailRunNotesInput = "";
  export let detailRunDescriptionInput = "";
  export let detailRunIsRaceInput = false;
  export let detailRunDateInput = "";
  export let detailRunDistanceInput = "0.00";
  export let detailRunClimbInput = "0";
  export let detailRunDurationInput = "0:00";
  export let detailRunPaceInput = "";
  export let activeRunInlineField: string | null = null;

  // Workout input states
  export let detailWorkoutMinutesInput = "20";
  export let detailWorkoutDateInput = "";
  export let detailWorkoutNotesInput = "";

  // Template refs
  let runDateEditor: HTMLInputElement | null;
  let runDistanceEditor: HTMLInputElement | null;
  let runClimbEditor: HTMLInputElement | null;
  let runDurationEditor: HTMLInputElement | null;
  let runPaceEditor: HTMLInputElement | null;
  let runDescriptionEditor: HTMLInputElement | null;
  let showGarminLogoImage = true;

  // Callbacks
  export let onSave: () => void;
  export let onDelete: () => void;
  export let onClose: () => void;

  async function activateField(fieldName: string) {
    if (!selectedRunIsGarmin) return;
    activeRunInlineField = fieldName;
    await tick();

    const editor = 
      fieldName === "date" ? runDateEditor
      : fieldName === "distance" ? runDistanceEditor
      : fieldName === "climb" ? runClimbEditor
      : fieldName === "duration" ? runDurationEditor
      : fieldName === "pace" ? runPaceEditor
      : fieldName === "description" ? runDescriptionEditor
      : null;

    if (editor?.focus) {
      editor.focus();
      if (editor.select) editor.select();
    }
  }

  function closeEditor() {
    activeRunInlineField = null;
  }
</script>

<section class="detail-page">
  <h2>{detailPageTitle}</h2>

  {#if error}
    <p class="inline-warning">{error}</p>
  {/if}

  {#if selectedDetailItem?.type === "activity"}
    <p class="detail-meta">
      <span>{formatActivityType(selectedDetailItem.data.dataType)}</span>
      <span>{selectedDetailItem.data.id}</span>
    </p>
    <section class="import-section">
      <table class="detail-table">
        <tbody>
          {#if selectedDetailItem.data.description || selectedRunIsGarmin}
            <tr>
              <th>Description</th>
              <td>
                {#if selectedRunIsGarmin}
                  {#if activeRunInlineField === "description"}
                    <input type="text" bind:value={detailRunDescriptionInput} placeholder="Optional description" bind:this={runDescriptionEditor} on:blur={closeEditor} />
                  {:else}
                    <button type="button" class="inline-edit-trigger" on:click={() => activateField("description")} on:focus={() => activateField("description")}>
                      <span>{selectedDetailItem.data.description || "-"}</span>
                      <svg viewBox="0 0 24 24" aria-hidden="true" focusable="false"><path d="M17 3a2.828 2.828 0 1 1 4 4L7.5 20.5 2 22l1.5-5.5L17 3z"/></svg>
                    </button>
                  {/if}
                {:else}
                  {selectedDetailItem.data.description}
                {/if}
              </td>
            </tr>
          {/if}
          <tr>
            <th>Date</th>
            <td>
              {#if selectedRunIsGarmin}
                {#if activeRunInlineField === "date"}
                  <input type="date" bind:value={detailRunDateInput} required bind:this={runDateEditor} on:blur={closeEditor} />
                {:else}
                  <button type="button" class="inline-edit-trigger" on:click={() => activateField("date")} on:focus={() => activateField("date")}>
                    <span>{formatDate(selectedDetailItem.data.date)}</span>
                    <svg viewBox="0 0 24 24" aria-hidden="true" focusable="false"><path d="M17 3a2.828 2.828 0 1 1 4 4L7.5 20.5 2 22l1.5-5.5L17 3z"/></svg>
                  </button>
                {/if}
              {:else}
                {formatDate(selectedDetailItem.data.date)}
              {/if}
            </td>
          </tr>
          <tr>
            <th>Race</th>
            <td>
              <label class="detail-race-toggle">
                <input type="checkbox" bind:checked={detailRunIsRaceInput} disabled={!selectedRunIsGarmin} />
                <span>{detailRunIsRaceInput ? "Yes" : "No"}</span>
              </label>
            </td>
          </tr>
          <tr>
            <th>Distance</th>
            <td>
              {#if selectedRunIsGarmin}
                {#if activeRunInlineField === "distance"}
                  <input type="number" min="0" step="0.01" bind:value={detailRunDistanceInput} bind:this={runDistanceEditor} on:blur={closeEditor} />
                {:else}
                  <button type="button" class="inline-edit-trigger" on:click={() => activateField("distance")} on:focus={() => activateField("distance")}>
                    <span>{formatDistance(selectedDetailItem.data.distance)} km</span>
                    <svg viewBox="0 0 24 24" aria-hidden="true" focusable="false"><path d="M17 3a2.828 2.828 0 1 1 4 4L7.5 20.5 2 22l1.5-5.5L17 3z"/></svg>
                  </button>
                {/if}
              {:else}
                {formatDistance(selectedDetailItem.data.distance)} km
              {/if}
            </td>
          </tr>
          <tr>
            <th>Climb</th>
            <td>
              {#if selectedRunIsGarmin}
                {#if activeRunInlineField === "climb"}
                  <input type="number" min="0" step="1" bind:value={detailRunClimbInput} bind:this={runClimbEditor} on:blur={closeEditor} />
                {:else}
                  <button type="button" class="inline-edit-trigger" on:click={() => activateField("climb")} on:focus={() => activateField("climb")}>
                    <span>{typeof selectedDetailItem.data.climb === "number" && selectedDetailItem.data.climb > 0 ? `${Math.round(selectedDetailItem.data.climb)} meters` : "-"}</span>
                    <svg viewBox="0 0 24 24" aria-hidden="true" focusable="false"><path d="M17 3a2.828 2.828 0 1 1 4 4L7.5 20.5 2 22l1.5-5.5L17 3z"/></svg>
                  </button>
                {/if}
              {:else}
                {typeof selectedDetailItem.data.climb === "number" && selectedDetailItem.data.climb > 0 ? `${Math.round(selectedDetailItem.data.climb)} meters` : "-"}
              {/if}
            </td>
          </tr>
          <tr>
            <th>Duration</th>
            <td>
              {#if selectedRunIsGarmin}
                {#if activeRunInlineField === "duration"}
                  <input type="text" bind:value={detailRunDurationInput} placeholder="h:mm:ss or m:ss" bind:this={runDurationEditor} on:blur={closeEditor} />
                {:else}
                  <button type="button" class="inline-edit-trigger" on:click={() => activateField("duration")} on:focus={() => activateField("duration")}>
                    <span>{formatDuration(selectedDetailItem.data.duration)}</span>
                    <svg viewBox="0 0 24 24" aria-hidden="true" focusable="false"><path d="M17 3a2.828 2.828 0 1 1 4 4L7.5 20.5 2 22l1.5-5.5L17 3z"/></svg>
                  </button>
                {/if}
              {:else}
                {formatDuration(selectedDetailItem.data.duration)}
              {/if}
            </td>
          </tr>
          <tr>
            <th>Pace</th>
            <td>
              {#if selectedRunIsGarmin}
                {#if activeRunInlineField === "pace"}
                  <input type="text" bind:value={detailRunPaceInput} placeholder="m:ss" bind:this={runPaceEditor} on:blur={closeEditor} />
                {:else}
                  <button type="button" class="inline-edit-trigger" on:click={() => activateField("pace")} on:focus={() => activateField("pace")}>
                    <span>{formatPace(selectedDetailItem.data.pace)}</span>
                    <svg viewBox="0 0 24 24" aria-hidden="true" focusable="false"><path d="M17 3a2.828 2.828 0 1 1 4 4L7.5 20.5 2 22l1.5-5.5L17 3z"/></svg>
                  </button>
                {/if}
              {:else}
                {formatPace(selectedDetailItem.data.pace)}
              {/if}
            </td>
          </tr>
          <tr>
            <th>Effort</th>
            <td>{selectedDetailItem.data.effort}</td>
          </tr>
          {#if typeof selectedDetailItem.data.heartrate === "number" && selectedDetailItem.data.heartrate > 1}
            <tr>
              <th>Heartrate</th>
              <td>{Math.round(selectedDetailItem.data.heartrate)}</td>
            </tr>
          {/if}
        </tbody>
      </table>

      <div class="detail-notes">
        <label>
          Notes
          <textarea rows="4" bind:value={detailRunNotesInput} placeholder="Optional notes" disabled={!selectedRunIsGarmin}></textarea>
        </label>
      </div>

      {#if selectedDetailItem.data.url && isGarminActivity(selectedDetailItem.data.dataType)}
        <a class="detail-garmin-link" href={selectedDetailItem.data.url} target="_blank" rel="noopener noreferrer">
          {#if showGarminLogoImage}
            <img
              class="detail-garmin-logo"
              src={GARMIN_CONNECT_LOGO_PATH}
              alt=""
              aria-hidden="true"
              on:error={() => showGarminLogoImage = false}
            />
          {/if}
          <span>View on Garmin Connect</span>
        </a>
      {/if}
    </section>
  {/if}

  {#if selectedDetailItem?.type === "workout"}
    <section class="import-section">
      <form class="import-form" on:submit|preventDefault={onSave}>
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
      <button type="button" on:click={onSave} disabled={isSavingDetail || isDeletingDetail}>Save</button>
      <button type="button" class="nav-btn-logout" on:click={onDelete} disabled={isDeletingDetail || isSavingDetail}>Delete</button>
    </div>
    <button type="button" on:click={onClose} disabled={isDeletingDetail || isSavingDetail}>Close</button>
  </div>
</section>

<style>
  /* Styles are inherited from main app.css */
</style>
