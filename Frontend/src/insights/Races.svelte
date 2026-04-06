<script lang="ts">
	import { createEventDispatcher } from "svelte";
	import { onMount } from "svelte";
	import {
		formatDate,
		formatDistance,
		formatClimb,
		formatDuration,
		formatPace
	} from "../lib/utils";

	type RaceActivity = {
		id: number;
		date: string;
		description?: string | null;
		distance: number;
		climb: number;
		duration: number;
		pace: string;
		effort: number;
		isRace: boolean;
	};

	type YearSection = {
		year: number;
		items: RaceActivity[];
	};

	const dispatch = createEventDispatcher();

	let isLoading = true;
	let error = "";
	let yearSections: YearSection[] = [];

	function parseDate(value: string): Date | null {
		const parsed = new Date(value);
		return Number.isNaN(parsed.getTime()) ? null : parsed;
	}

	function formatClimbForCard(value: number): string {
		const formatted = formatClimb(value);
		return formatted === "-" ? "-" : `${formatted} m`;
	}

	function buildSections(items: RaceActivity[]): YearSection[] {
		const sorted = [...items].sort((a, b) => {
			const aDate = parseDate(a.date)?.getTime() ?? 0;
			const bDate = parseDate(b.date)?.getTime() ?? 0;
			return bDate - aDate;
		});

		const grouped = new Map<number, RaceActivity[]>();
		for (const race of sorted) {
			const year = parseDate(race.date)?.getFullYear() ?? 0;
			const current = grouped.get(year) ?? [];
			current.push(race);
			grouped.set(year, current);
		}

		return [...grouped.entries()]
			.sort((a, b) => b[0] - a[0])
			.map(([year, sectionItems]) => ({
				year,
				items: sectionItems
			}));
	}

	function openRaceDetail(race: RaceActivity) {
		dispatch("openDetail", {
			type: "activity",
			date: race.date,
			data: race
		});
	}

	async function loadRaces() {
		isLoading = true;
		error = "";

		try {
			const response = await fetch("/api/activities/", {
				method: "GET",
				credentials: "include"
			});

			if (response.status === 401) {
				throw new Error("You are not logged in.");
			}

			if (!response.ok) {
				throw new Error("Could not load races.");
			}

			const payload = await response.json();
			const items = Array.isArray(payload) ? payload : [];
			const races = items.filter((item) => item?.isRace === true) as RaceActivity[];
			yearSections = buildSections(races);
		} catch (err) {
			yearSections = [];
			error = err instanceof Error ? err.message : "Could not load races.";
		} finally {
			isLoading = false;
		}
	}

	onMount(loadRaces);
</script>

{#if isLoading}
	<p class="insights-placeholder">Loading races...</p>
{:else if error}
	<p class="insights-placeholder">{error}</p>
{:else if yearSections.length === 0}
	<p class="insights-placeholder">No races found.</p>
{:else}
	<section class="races-list" aria-label="Races by year">
		{#each yearSections as section (section.year)}
			<h3 class="races-year-header">{section.year}</h3>

			{#each section.items as race (race.id)}
				<button type="button" class="race-card" on:click={() => openRaceDetail(race)}>
					<h4 class="race-title">{race.description?.trim() || "Race"}</h4>
					<p class="race-date">{formatDate(race.date)}</p>

					<table class="race-metrics">
						<tbody>
							<tr>
								<th>Distance</th>
								<td>{formatDistance(race.distance)} km</td>
							</tr>
							<tr>
								<th>Climb</th>
								<td>{formatClimbForCard(race.climb)}</td>
							</tr>
							<tr>
								<th>Duration</th>
								<td>{formatDuration(race.duration)}</td>
							</tr>
							<tr>
								<th>Pace</th>
								<td>{formatPace(race.pace)}</td>
							</tr>
							<tr>
								<th>Effort</th>
								<td>{race.effort}</td>
							</tr>
						</tbody>
					</table>
				</button>
			{/each}
		{/each}
	</section>
{/if}

<style>
	.races-list {
		display: grid;
		gap: 0.9rem;
	}

	.races-year-header {
		margin: 0.35rem 0 0;
		font-size: 1.05rem;
		color: var(--muted-text);
		border-bottom: 1px solid var(--divider-color);
		padding-bottom: 0.25rem;
	}

	.race-card {
		width: 100%;
		border: 1px solid var(--import-border);
		border-radius: 10px;
		background: var(--import-bg);
		color: var(--text-color);
		text-align: left;
		padding: 0.85rem 0.95rem;
		box-shadow: 0 1px 2px color-mix(in srgb, var(--text-color) 9%, transparent);
		cursor: pointer;
		transition: border-color 0.15s ease, box-shadow 0.15s ease, transform 0.1s ease;
	}

	.race-card:hover {
		border-color: var(--field-border);
		box-shadow: 0 8px 16px color-mix(in srgb, var(--shadow-color) 55%, transparent);
	}

	.race-card:active {
		transform: translateY(1px);
	}

	.race-card:focus-visible {
		outline: 2px solid var(--focus-ring);
		outline-offset: 2px;
	}

	.race-title {
		margin: 0;
		font-size: 1rem;
		font-weight: 700;
		color: var(--insight-title-color);
	}

	.race-date {
		margin: 0.18rem 0 0.65rem;
		color: var(--muted-text);
		font-size: 0.86rem;
	}

	.race-metrics {
		width: 100%;
		border-collapse: collapse;
		font-size: 0.94rem;
	}

	.race-metrics th,
	.race-metrics td {
		padding: 0.2rem 0;
		vertical-align: top;
	}

	.race-metrics th {
		font-weight: 600;
		color: var(--muted-text);
		width: 6.4rem;
	}

	.race-metrics td {
		text-align: right;
		font-variant-numeric: tabular-nums;
	}

	@media (max-width: 600px) {
		.races-list {
			gap: 0.75rem;
		}

		.race-card {
			padding: 0.75rem 0.8rem;
		}

		.race-metrics {
			font-size: 0.9rem;
		}

		.race-metrics th {
			width: 5.6rem;
		}
	}
</style>
