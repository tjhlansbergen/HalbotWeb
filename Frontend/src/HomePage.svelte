<script lang="ts">
  import { formatDistance, formatClimb, formatDate, formatDateCompact, getRunBand } from "./lib/utils";

  export let rows: any[] = [];
  export let showRunning = true;
  export let showStrength = true;
  export let onOpenDetail: (row: any) => void;
</script>

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
        <tr class="data-row {getRunBand(row.data.distance)}" on:click={() => onOpenDetail(row)}>
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
        <tr class="data-row row-strength" on:click={() => onOpenDetail(row)}>
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

<style>
  /* Styles are inherited from main app.css */
</style>
