<script lang="ts">
	import { onMount } from "svelte";

	type Activity = {
		date: string;
		distance: number;
		climb: number;
		speed: number;
	};

	type Workout = {
		date: string;
		minutes: number;
	};

	type MetricKey =
		| "distanceKm"
		| "runCount"
		| "climb"
		| "hillyCount"
		| "fastCount"
		| "strengthMinutes"
		| "strengthCount";

	type YearStats = {
		year: number;
		distanceKm: number;
		runCount: number;
		climb: number;
		hillyCount: number;
		fastCount: number;
		strengthMinutes: number;
		strengthCount: number;
	};

	type MetricConfig = {
		key: MetricKey;
		title: string;
		barClass: string;
	};

	const YEARS_TO_SHOW = 10;
	const FAST_RUN_SPEED_THRESHOLD = 1000 / 330; // 5:30 min/km

	const METRICS: MetricConfig[] = [
		{ key: "distanceKm", title: "Total distance", barClass: "distance" },
		{ key: "runCount", title: "Number of runs", barClass: "runs" },
		{ key: "climb", title: "Total climb", barClass: "climb" },
		{ key: "hillyCount", title: "Number of runs with climb higher than 100m", barClass: "hilly" },
		{ key: "fastCount", title: "Number of runs faster than 5:30 min/km", barClass: "fast" },
		{ key: "strengthMinutes", title: "Total strength minutes", barClass: "strength-minutes" },
		{ key: "strengthCount", title: "Number of strength sessions", barClass: "strength-sessions" }
	];

	let isLoading = true;
	let error = "";
	let yearStats: YearStats[] = [];
	let currentYear = new Date().getFullYear();

	function parseDate(value: string): Date | null {
		const parsed = new Date(value);
		return Number.isNaN(parsed.getTime()) ? null : parsed;
	}

	function startOfDay(value: Date): Date {
		return new Date(value.getFullYear(), value.getMonth(), value.getDate());
	}

	function yearCutoff(today: Date, year: number): Date {
		const month = today.getMonth();
		const maxDayInMonth = new Date(year, month + 1, 0).getDate();
		const day = Math.min(today.getDate(), maxDayInMonth);
		return new Date(year, month, day, 23, 59, 59, 999);
	}

	function buildYearStats(activities: Activity[], workouts: Workout[], today: Date): YearStats[] {
		const years = Array.from({ length: YEARS_TO_SHOW }, (_, index) => currentYear - index);

		return years.map((year) => {
			const cutoff = yearCutoff(today, year);
			const activityBucket = activities.filter((activity) => {
				const date = parseDate(activity.date);
				if (!date) {
					return false;
				}

				const day = startOfDay(date);
				return day.getFullYear() === year && day.getTime() <= cutoff.getTime();
			});

			const workoutBucket = workouts.filter((workout) => {
				const date = parseDate(workout.date);
				if (!date) {
					return false;
				}

				const day = startOfDay(date);
				return day.getFullYear() === year && day.getTime() <= cutoff.getTime();
			});

			const distanceKm = Math.round(activityBucket.reduce((sum, activity) => sum + ((activity.distance ?? 0) / 1000), 0));
			const runCount = activityBucket.length;
			const climb = Math.round(activityBucket.reduce((sum, activity) => sum + (activity.climb ?? 0), 0));
			const hillyCount = activityBucket.filter((activity) => (activity.climb ?? 0) > 100).length;
			const fastCount = activityBucket.filter((activity) => (activity.speed ?? 0) >= FAST_RUN_SPEED_THRESHOLD).length;
			const strengthMinutes = Math.round(workoutBucket.reduce((sum, workout) => sum + (workout.minutes ?? 0), 0));
			const strengthCount = workoutBucket.length;

			return {
				year,
				distanceKm,
				runCount,
				climb,
				hillyCount,
				fastCount,
				strengthMinutes,
				strengthCount
			};
		});
	}

	function metricValue(stats: YearStats, key: MetricKey): number {
		return stats[key] ?? 0;
	}

	function barWidth(metric: MetricConfig, stats: YearStats): string {
		const values = yearStats.map((item) => metricValue(item, metric.key));
		const maxValue = Math.max(0, ...values);
		if (maxValue <= 0) {
			return "0%";
		}

		const value = metricValue(stats, metric.key);
		return `${Math.max((value / maxValue) * 100, value > 0 ? 2 : 0)}%`;
	}

	function formatMetricValue(metric: MetricConfig, stats: YearStats): string {
		const value = metricValue(stats, metric.key);
		if (metric.key === "distanceKm") {
			return `${value} km`;
		}

		if (metric.key === "climb") {
			return `${value} m`;
		}

		if (metric.key === "strengthMinutes") {
			return `${value} m`;
		}

		return String(value);
	}

	async function loadY2D() {
		isLoading = true;
		error = "";

		try {
				const [activitiesResponse, workoutsResponse] = await Promise.all([
					fetch("/api/activities/", {
						method: "GET",
						credentials: "include"
					}),
					fetch("/api/workouts/", {
						method: "GET",
						credentials: "include"
					})
				]);

				if (activitiesResponse.status === 401 || workoutsResponse.status === 401) {
				throw new Error("You are not logged in.");
			}

				if (!activitiesResponse.ok || !workoutsResponse.ok) {
				throw new Error("Could not load year-to-date data.");
			}

				const activitiesPayload = await activitiesResponse.json();
				const workoutsPayload = await workoutsResponse.json();
				const activities = (Array.isArray(activitiesPayload) ? activitiesPayload : []) as Activity[];
				const workouts = (Array.isArray(workoutsPayload) ? workoutsPayload : []) as Workout[];
			const today = startOfDay(new Date());
			currentYear = today.getFullYear();
				yearStats = buildYearStats(activities, workouts, today);
		} catch (err) {
			yearStats = [];
			error = err instanceof Error ? err.message : "Could not load year-to-date data.";
		} finally {
			isLoading = false;
		}
	}

	onMount(loadY2D);
</script>

{#if isLoading}
	<p class="insights-placeholder">Loading year to date...</p>
{:else if error}
	<p class="insights-placeholder">{error}</p>
{:else}
	<section class="y2d" aria-label="Year to date comparison over the last ten years">
		<h2 class="y2d-title">This year compared to last years, up to today</h2>

		{#each METRICS as metric (metric.key)}
			<div class="metric-section">
				<h3 class="metric-title">{metric.title}</h3>

				<div class="metric-rows">
					{#each yearStats as stats (stats.year)}
						<div class="metric-row {stats.year === currentYear ? 'is-current' : ''}">
							<span class="metric-year">{stats.year}</span>
							<span class="metric-value">{formatMetricValue(metric, stats)}</span>
							<div class="metric-bar-wrap" aria-hidden="true">
								<span class={`metric-bar ${metric.barClass}`} style={`width:${barWidth(metric, stats)};`}></span>
							</div>
						</div>
					{/each}
				</div>
			</div>
		{/each}
	</section>
{/if}

<style>
	.y2d {
		width: 100%;
		display: grid;
		gap: 1.05rem;
	}

	.y2d-title {
		margin: 0;
		font-size: 1.25rem;
		color: var(--insight-title-color);
		font-weight: 500;
	}

	.metric-section {
		display: grid;
		gap: 0.35rem;
	}

	.metric-section + .metric-section {
		margin-top: 0.55rem;
	}

	.metric-title {
		margin: 0.2rem 0 0;
		font-size: 1.03rem;
		color: var(--muted-text);
		border-bottom: 1px solid var(--divider-color);
		padding-bottom: 0.25rem;
		font-weight: 700;
	}

	.metric-rows {
		display: grid;
		gap: 0.18rem;
	}

	.metric-row {
		display: grid;
		grid-template-columns: 4rem 6rem 1fr;
		gap: 0.05rem;
		align-items: center;
		min-height: 1.6rem;
		font-variant-numeric: tabular-nums;
	}

	.metric-year,
	.metric-value {
		font-size: 1.08rem;
		color: var(--text-color);
	}

	.metric-year {
		font-weight: 500;
	}

	.metric-value {
		text-align: right;
		padding-right: 0.2rem;
	}

	.metric-bar-wrap {
		width: min(18.5rem, 100%);
		height: 1.3rem;
		display: flex;
		align-items: center;
	}

	.metric-bar {
		display: inline-block;
		height: 1.3rem;
		border-radius: 0;
		opacity: 0.8;
		transition: width 0.25s ease;
	}

	.metric-bar.distance { background: #d9a620; }
	.metric-bar.runs { background: #d88545; }
	.metric-bar.climb { background: #86af84; }
	.metric-bar.hilly { background: #6c8f6a; }
	.metric-bar.fast { background: #79adb2; }
	.metric-bar.strength-minutes { background: #f0d060; }
	.metric-bar.strength-sessions { background: #e0bd45; }

	.metric-row.is-current .metric-bar {
		filter: saturate(1.12) brightness(0.9);
		opacity: 1;
	}

	.metric-row.is-current .metric-year,
	.metric-row.is-current .metric-value {
		font-weight: 700;
	}

	@media (max-width: 700px) {
		.y2d {
			gap: 0.88rem;
		}

		.y2d-title {
			font-size: 1rem;
			line-height: 1.3;
		}

		.metric-title {
			font-size: 0.92rem;
			padding-bottom: 0.2rem;
		}

		.metric-section + .metric-section {
			margin-top: 0.38rem;
		}

		.metric-row {
			grid-template-columns: 3.1rem 5rem 1fr;
			min-height: 1.25rem;
		}

		.metric-year,
		.metric-value {
			font-size: 0.82rem;
		}

		.metric-value {
			padding-right: 0.14rem;
		}

		.metric-bar-wrap,
		.metric-bar {
			height: 1rem;
		}
	}
</style>
