<script lang="ts">
	import { onMount } from "svelte";

	type Activity = {
		date: string;
		distance: number;
	};

	type EddingtonRow = {
		target: number;
		count: number;
		met: boolean;
		dateReached: string | null;
		remaining: number;
	};

	const MAX_TARGET = 50;

	let isLoading = true;
	let error = "";
	let rows: EddingtonRow[] = [];
	let score = 0;

	function toDate(value: string): Date | null {
		const parsed = new Date(value);
		return Number.isNaN(parsed.getTime()) ? null : parsed;
	}

	function formatDate(value: Date): string {
		const day = String(value.getDate()).padStart(2, "0");
		const month = String(value.getMonth() + 1).padStart(2, "0");
		const year = String(value.getFullYear());
		return `${day}-${month}-${year}`;
	}

	function computeEddington(activities: Activity[]): { rows: EddingtonRow[]; score: number } {
		const valid = activities
			.filter((activity) => typeof activity.distance === "number" && activity.distance > 0)
			.map((activity) => ({ ...activity, parsedDate: toDate(activity.date) }))
			.filter((activity) => activity.parsedDate !== null) as Array<Activity & { parsedDate: Date }>;

		const sortedAsc = [...valid].sort((a, b) => a.parsedDate.getTime() - b.parsedDate.getTime());
		const cumulative = Array(MAX_TARGET + 1).fill(0);
		const reachedOn: Array<Date | null> = Array(MAX_TARGET + 1).fill(null);

		for (const activity of sortedAsc) {
			for (let target = 1; target <= MAX_TARGET; target += 1) {
				if (activity.distance >= target * 1000) {
					cumulative[target] += 1;
					if (cumulative[target] === target && reachedOn[target] === null) {
						reachedOn[target] = activity.parsedDate;
					}
				}
			}
		}

		const computedRows: EddingtonRow[] = [];
		for (let target = 1; target <= MAX_TARGET; target += 1) {
			const count = cumulative[target];
			const met = count >= target;
			computedRows.push({
				target,
				count,
				met,
				dateReached: reachedOn[target] ? formatDate(reachedOn[target] as Date) : null,
				remaining: Math.max(0, target - count)
			});
		}

		const computedScore = computedRows.reduce((max, row) => (row.met ? row.target : max), 0);
		return { rows: computedRows, score: computedScore };
	}

	async function load() {
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
				throw new Error("Could not load activities.");
			}

			const items = await response.json();
			const list = Array.isArray(items) ? items : [];
			const result = computeEddington(list);
			rows = result.rows;
			score = result.score;
		} catch (err) {
			rows = [];
			score = 0;
			error = err instanceof Error ? err.message : "Could not load Eddington data.";
		} finally {
			isLoading = false;
		}
	}

	onMount(load);
</script>

{#if isLoading}
	<p class="insights-placeholder">Loading Eddington…</p>
{:else if error}
	<p class="insights-placeholder">{error}</p>
{:else}
	<section class="eddington" aria-label="Eddington score">
		<p class="eddington-score">My Eddington Number: <strong>{score}</strong></p>

		<div class="eddington-table" role="table" aria-label="Eddington targets one through fifty">
			{#each rows as row (row.target)}
				<div class="eddington-row {row.met ? 'met' : 'todo'}" role="row">
					<span class="target" role="cell">{row.target}</span>

					{#if row.met}
						<span class="status" role="cell">Completed on: {row.dateReached}</span>
					{:else}
						<span class="status" role="cell">{row.remaining} todo</span>
					{/if}

					<span class="count" role="cell">{row.count}</span>
				</div>
			{/each}
		</div>
	</section>
{/if}

<style>
	.eddington {
		width: 100%;
	}

	.eddington-score {
		margin: 0 0 0.65rem;
		color: #194a84;
		font-weight: 500;
	}

	.eddington-table {
		display: grid;
		gap: 0.2rem;
	}

	.eddington-row {
		display: grid;
		grid-template-columns: 2rem 1fr 3rem;
		align-items: center;
		column-gap: 0.5rem;
		font-size: 0.97rem;
		line-height: 1.25;
		white-space: nowrap;
	}

	.eddington-row .status {
		overflow: hidden;
		text-overflow: ellipsis;
	}

	.eddington-row .count {
		text-align: right;
		justify-self: end;
		font-variant-numeric: tabular-nums;
	}

	.eddington-row.met {
		color: #0d8a11;
	}

	.eddington-row.todo {
		color: #c22;
	}

	@media (max-width: 600px) {
		.eddington-row {
			grid-template-columns: 1.6rem 1fr 2.3rem;
			column-gap: 0.35rem;
			font-size: 0.9rem;
		}
	}
</style>
