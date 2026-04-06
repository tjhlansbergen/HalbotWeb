<script lang="ts">
  import { formatDateTime, formatDateCompact, formatLogSeverity } from "./lib/utils";

  export let logs: any[] = [];
  export let isLoading = false;
  export let error: string = "";
</script>

<section class="logs-section">
  {#if error}
    <p class="inline-warning">{error}</p>
  {:else if isLoading}
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

<style>
  /* Styles are inherited from main app.css */
</style>
