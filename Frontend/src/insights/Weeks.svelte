<script lang="ts">
	import { onMount } from "svelte";
	import { getRunBand } from "../lib/utils";

	type Activity = {
		date: string;
		effort: number;
		distance: number;
	};

	type Workout = {
		date: string;
		minutes: number;
	};

	type DayAggregate = {
		runEffort: number;
		runDistance: number;
		workoutMinutes: number;
	};

	type DayCell = {
		key: string;
		date: Date;
		dateLabel: string;
		runEffort: number;
		runDistance: number;
		workoutMinutes: number;
		runRadius: number;
		workoutRadius: number;
		runBand: string;
		hasRun: boolean;
		hasWorkout: boolean;
	};

	type WeekRow = {
		key: string;
		days: DayCell[];
	};

	const WEEK_COUNT = 14;
	const DAY_NAMES = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];
	const RUN_MIN_RADIUS = 9;
	const RUN_MAX_RADIUS = 24;
	const WORKOUT_MIN_RADIUS = 4;
	const WORKOUT_MAX_RADIUS = 10;

	let isLoading = true;
	let error = "";
	let weeks: WeekRow[] = [];

	function startOfDay(value: Date): Date {
		return new Date(value.getFullYear(), value.getMonth(), value.getDate());
	}

	function addDays(value: Date, days: number): Date {
		const next = new Date(value);
		next.setDate(next.getDate() + days);
		return startOfDay(next);
	}

	function weekStartMonday(value: Date): Date {
		const dayStart = startOfDay(value);
		const mondayOffset = (dayStart.getDay() + 6) % 7;
		dayStart.setDate(dayStart.getDate() - mondayOffset);
		return dayStart;
	}

	function toDayKey(value: Date): string {
		const year = value.getFullYear();
		const month = String(value.getMonth() + 1).padStart(2, "0");
		const day = String(value.getDate()).padStart(2, "0");
		return `${year}-${month}-${day}`;
	}

	function parseApiDate(value: string): Date | null {
		const parsed = new Date(value);
		if (Number.isNaN(parsed.getTime())) {
			return null;
		}

		return startOfDay(parsed);
	}

	function formatDayLabel(value: Date): string {
		const day = String(value.getDate()).padStart(2, "0");
		const month = value.toLocaleString("en-US", { month: "short" });
		return `${day} ${month}`;
	}

	function computeRadius(value: number, maxValue: number, minRadius: number, maxRadius: number): number {
		if (value <= 0 || maxValue <= 0) {
			return 0;
		}

		const scaled = Math.sqrt(value / maxValue);
		return minRadius + (maxRadius - minRadius) * scaled;
	}

	function aggregateByDay(activities: Activity[], workouts: Workout[]): Map<string, DayAggregate> {
		const buckets = new Map<string, DayAggregate>();

		const ensure = (key: string): DayAggregate => {
			const existing = buckets.get(key);
			if (existing) {
				return existing;
			}

			const created: DayAggregate = {
				runEffort: 0,
				runDistance: 0,
				workoutMinutes: 0
			};
			buckets.set(key, created);
			return created;
		};

		for (const activity of activities) {
			const parsedDate = parseApiDate(activity.date);
			if (!parsedDate) {
				continue;
			}

			const key = toDayKey(parsedDate);
			const day = ensure(key);
			day.runEffort += Math.max(0, Number(activity.effort) || 0);
			day.runDistance += Math.max(0, Number(activity.distance) || 0);
		}

		for (const workout of workouts) {
			const parsedDate = parseApiDate(workout.date);
			if (!parsedDate) {
				continue;
			}

			const key = toDayKey(parsedDate);
			const day = ensure(key);
			day.workoutMinutes += Math.max(0, Number(workout.minutes) || 0);
		}

		return buckets;
	}

	function buildWeekRows(dayData: Map<string, DayAggregate>): WeekRow[] {
		const today = startOfDay(new Date());
		const currentWeekStart = weekStartMonday(today);
		const built: WeekRow[] = [];

		let maxRunEffort = 0;
		let maxWorkoutMinutes = 0;

		for (let weekOffset = 0; weekOffset < WEEK_COUNT; weekOffset += 1) {
			const weekStart = addDays(currentWeekStart, -weekOffset * 7);

			for (let dayOffset = 0; dayOffset < 7; dayOffset += 1) {
				const dayDate = addDays(weekStart, dayOffset);
				const aggregate = dayData.get(toDayKey(dayDate));
				if (!aggregate) {
					continue;
				}

				maxRunEffort = Math.max(maxRunEffort, aggregate.runEffort);
				maxWorkoutMinutes = Math.max(maxWorkoutMinutes, aggregate.workoutMinutes);
			}
		}

		for (let weekOffset = 0; weekOffset < WEEK_COUNT; weekOffset += 1) {
			const weekStart = addDays(currentWeekStart, -weekOffset * 7);
			const days: DayCell[] = [];

			for (let dayOffset = 0; dayOffset < 7; dayOffset += 1) {
				const dayDate = addDays(weekStart, dayOffset);
				const key = toDayKey(dayDate);
				const aggregate = dayData.get(key) ?? {
					runEffort: 0,
					runDistance: 0,
					workoutMinutes: 0
				};
				const runEffort = aggregate.runEffort;
				const runDistance = aggregate.runDistance;
				const workoutMinutes = aggregate.workoutMinutes;

				days.push({
					key,
					date: dayDate,
					dateLabel: formatDayLabel(dayDate),
					runEffort,
					runDistance,
					workoutMinutes,
					runRadius: computeRadius(runEffort, maxRunEffort, RUN_MIN_RADIUS, RUN_MAX_RADIUS),
					workoutRadius: computeRadius(workoutMinutes, maxWorkoutMinutes, WORKOUT_MIN_RADIUS, WORKOUT_MAX_RADIUS),
					runBand: getRunBand(runDistance),
					hasRun: runEffort > 0,
					hasWorkout: workoutMinutes > 0
				});
			}

			built.push({
				key: toDayKey(weekStart),
				days
			});
		}

		return built;
	}

	function runStyle(cell: DayCell): string {
		const diameter = Math.round(cell.runRadius * 2);
		return `width:${diameter}px;height:${diameter}px;transform:translate(-55%, -50%);z-index:${cell.hasWorkout && cell.runRadius <= cell.workoutRadius ? 2 : 1};`;
	}

	function workoutStyle(cell: DayCell): string {
		const diameter = Math.round(cell.workoutRadius * 2);
		return `width:${diameter}px;height:${diameter}px;transform:translate(-45%, -50%);z-index:${cell.hasRun && cell.workoutRadius <= cell.runRadius ? 2 : 1};`;
	}

	async function loadWeeks(): Promise<void> {
		isLoading = true;
		error = "";

		try {
			const [activitiesResponse, workoutsResponse] = await Promise.all([
				fetch("/api/activities/", { method: "GET", credentials: "include" }),
				fetch("/api/workouts/", { method: "GET", credentials: "include" })
			]);

			if (activitiesResponse.status === 401 || workoutsResponse.status === 401) {
				throw new Error("You are not logged in.");
			}

			if (!activitiesResponse.ok || !workoutsResponse.ok) {
				throw new Error("Could not load weekly data.");
			}

			const activitiesPayload = await activitiesResponse.json();
			const workoutsPayload = await workoutsResponse.json();

			const activities = (Array.isArray(activitiesPayload) ? activitiesPayload : []) as Activity[];
			const workouts = (Array.isArray(workoutsPayload) ? workoutsPayload : []) as Workout[];
			const dayMap = aggregateByDay(activities, workouts);
			weeks = buildWeekRows(dayMap);
		} catch (err) {
			weeks = [];
			error = err instanceof Error ? err.message : "Could not load weekly data.";
		} finally {
			isLoading = false;
		}
	}

	onMount(loadWeeks);
</script>

{#if isLoading}
	<p class="insights-placeholder">Loading weekly load...</p>
{:else if error}
	<p class="insights-placeholder">{error}</p>
{:else}
	<section class="weeks" aria-label="Weekly running and workout load over the last 14 weeks">
		<div class="weeks-header">
			{#each DAY_NAMES as dayName}
				<div class="day-name">{dayName}</div>
			{/each}
		</div>

		<div class="weeks-grid">
			{#each weeks as week (week.key)}
				{#each week.days as cell (cell.key)}
					<div class="day-cell">
						<span class="date-label">{cell.dateLabel}</span>

						<div class="bubble-stack" aria-label={`${cell.dateLabel}, running effort ${Math.round(cell.runEffort)}, workout ${Math.round(cell.workoutMinutes)} minutes`}>
							{#if cell.hasRun}
								<span class={`bubble run ${cell.runBand}`} style={runStyle(cell)}></span>
							{/if}

							{#if cell.hasWorkout}
								<span class="bubble workout" style={workoutStyle(cell)}></span>
							{/if}

							{#if !cell.hasRun && !cell.hasWorkout}
								<span class="bubble-empty"></span>
							{/if}
						</div>

						<div class="value-labels">
							{#if cell.hasRun}
								<span class="run-value">{Math.round(cell.runEffort)}e</span>
							{/if}
							{#if cell.hasWorkout}
								<span class="workout-value">{Math.round(cell.workoutMinutes)}m</span>
							{/if}
						</div>
					</div>
				{/each}
			{/each}
		</div>
	</section>
{/if}

<style>
	.weeks {
		width: 100%;
		overflow-x: auto;
	}

	.weeks-header,
	.weeks-grid {
		min-width: 760px;
	}

	.weeks-header {
		display: grid;
		grid-template-columns: repeat(7, minmax(5rem, 1fr));
		align-items: center;
		margin-bottom: 0.35rem;
	}

	.day-name {
		text-align: center;
		font-size: 0.8rem;
		color: var(--muted-text);
		font-weight: 700;
		letter-spacing: 0.04em;
		text-transform: uppercase;
	}

	.weeks-grid {
		display: grid;
		grid-template-columns: repeat(7, minmax(5rem, 1fr));
		gap: 0;
		border-top: 1px solid var(--table-border-color);
	}

	.day-cell {
		border-bottom: 1px solid var(--table-border-color);
		padding: 0.4rem 0.2rem 0.45rem;
		display: grid;
		justify-items: center;
		grid-template-rows: 1.05rem 3.3rem auto;
		align-items: center;
		row-gap: 0.2rem;
	}

	.date-label {
		color: var(--muted-text);
		font-size: 0.7rem;
		line-height: 1;
		font-variant-numeric: tabular-nums;
	}

	.bubble-stack {
		position: relative;
		width: 100%;
		height: 3rem;
	}

	.bubble {
		position: absolute;
		left: 50%;
		top: 50%;
		border-radius: 999px;
	}

	.bubble.run.run-xs {
		background: #c8e8c5;
	}

	.bubble.run.run-s {
		background: #9dd99a;
	}

	.bubble.run.run-m {
		background: #66c063;
	}

	.bubble.run.run-l {
		background: #3fa33c;
	}

	.bubble.run.run-xl {
		background: #277f25;
	}

	.bubble.run.run-xxl {
		background: #165a14;
	}

	.bubble.workout {
		background: #f0d060;
	}

	.bubble-empty {
		position: absolute;
		left: 50%;
		top: 50%;
		width: 0.35rem;
		height: 0.35rem;
		transform: translate(-50%, -50%);
		border-radius: 999px;
		background: color-mix(in srgb, var(--muted-text) 33%, transparent);
	}

	.value-labels {
		display: grid;
		justify-items: center;
		align-content: start;
		gap: 0.05rem;
		min-height: 1.65rem;
		font-size: 0.68rem;
		line-height: 1.1;
		font-weight: 700;
		font-variant-numeric: tabular-nums;
	}

	.run-value {
		color: #277f25;
	}

	.workout-value {
		color: #aa8600;
	}

	@media (max-width: 700px) {
		.weeks {
			overflow-x: hidden;
		}

		.weeks-header,
		.weeks-grid {
			min-width: 0;
			width: 100%;
			grid-template-columns: repeat(7, minmax(0, 1fr));
		}

		.weeks-header {
			margin-bottom: 0.15rem;
		}

		.day-name {
			font-size: 0.62rem;
			letter-spacing: 0.01em;
		}

		.day-cell {
			padding: 0.18rem 0.04rem 0.22rem;
			grid-template-rows: 0.85rem 2.35rem auto;
			row-gap: 0.08rem;
		}

		.date-label {
			font-size: 0.6rem;
		}

		.bubble-stack {
			height: 2.35rem;
			transform: scale(0.88);
			transform-origin: center;
		}

		.value-labels {
			font-size: 0.58rem;
			min-height: 1.2rem;
		}
	}
</style>
