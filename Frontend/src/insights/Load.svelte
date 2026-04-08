<script lang="ts">
	import { BarChart } from "layerchart";

	type BucketType = "Daily" | "Weekly" | "Monthly" | "Yearly";

	type RunBucket = {
		bucketKey: string;
		distance: number;
		climb: number;
		speed: number;
	};

	type WorkoutBucket = {
		bucketKey: string;
		minutes: number;
	};

	type LoadRow = {
		bucketKey: string;
		distanceKm: number;
		climbM: number;
		paceSecPerKm: number;
		workoutMinutes: number;
	};

	type ChartSection = {
		key: BucketType;
		title: string;
		rows: LoadRow[];
	};

	const sectionConfig: Array<{ key: BucketType; title: string }> = [
		{ key: "Daily", title: "Last 14 days" },
		{ key: "Weekly", title: "Last 14 weeks" },
		{ key: "Monthly", title: "Last 14 months" },
		{ key: "Yearly", title: "Last 14 years" }
	];

	const series = [
		{ key: "distanceKm", label: "Distance (km)", color: "var(--insight-color-distance-azure)" },
		{ key: "climbM", label: "Climb (m)", color: "var(--insight-color-climb-coral)" },
		{ key: "paceSecPerKm", label: "Avg pace (min/km)", color: "var(--insight-color-pace-mint)" },
		{ key: "workoutMinutes", label: "Workout minutes", color: "var(--insight-color-workout-sun)" }
	];

	let isLoading = true;
	let loadError = "";
	let sections: ChartSection[] = [];
	const chartHeight = 320;

	function paceSecondsFromSpeed(speed: number): number {
		if (typeof speed !== "number" || speed <= 0) {
			return 0;
		}

		return 1000 / speed;
	}

	async function fetchRunBuckets(bucket: BucketType): Promise<RunBucket[]> {
		const params = new URLSearchParams({
			bucket,
			count: "14"
		});

		const response = await fetch(`/api/insights/lastxruns?${params.toString()}`, {
			method: "GET",
			credentials: "include"
		});

		if (!response.ok) {
			throw new Error(`Failed to load run buckets for ${bucket}.`);
		}

		const items = await response.json();
		return Array.isArray(items) ? items : [];
	}

	async function fetchWorkoutBuckets(bucket: BucketType): Promise<WorkoutBucket[]> {
		const params = new URLSearchParams({
			bucket,
			count: "14"
		});

		const response = await fetch(`/api/insights/lastxworkouts?${params.toString()}`, {
			method: "GET",
			credentials: "include"
		});

		if (!response.ok) {
			throw new Error(`Failed to load workout buckets for ${bucket}.`);
		}

		const items = await response.json();
		return Array.isArray(items) ? items : [];
	}

	function mergeBucketData(runBuckets: RunBucket[], workoutBuckets: WorkoutBucket[]): LoadRow[] {
		const workoutMap = new Map(workoutBuckets.map(item => [item.bucketKey, item]));

		return runBuckets.map(runBucket => {
			const workoutBucket = workoutMap.get(runBucket.bucketKey);

			return {
				bucketKey: runBucket.bucketKey,
				distanceKm: (runBucket.distance ?? 0) / 1000,
				climbM: runBucket.climb ?? 0,
				paceSecPerKm: paceSecondsFromSpeed(runBucket.speed ?? 0),
				workoutMinutes: workoutBucket?.minutes ?? 0
			};
		});
	}

	async function loadSections() {
		isLoading = true;
		loadError = "";

		try {
			const sectionPromises = sectionConfig.map(async section => {
				const [runBuckets, workoutBuckets] = await Promise.all([
					fetchRunBuckets(section.key),
					fetchWorkoutBuckets(section.key)
				]);

				return {
					key: section.key,
					title: section.title,
					rows: mergeBucketData(runBuckets, workoutBuckets)
				} satisfies ChartSection;
			});

			sections = await Promise.all(sectionPromises);
		} catch {
			loadError = "Could not load insights.";
			sections = [];
		} finally {
			isLoading = false;
		}
	}

	loadSections();
</script>

{#if isLoading}
	<p class="insights-placeholder">Loading load insights...</p>
{:else if loadError}
	<p class="insights-placeholder">{loadError}</p>
{:else}
	<div class="load-page">
		{#each sections as section}
			<section class="load-section">
				<h3>{section.title}</h3>

				<div class="load-chart-wrap">
					<div class="load-chart-canvas">
						<BarChart
							data={section.rows}
							x="bucketKey"
							height={chartHeight}
							series={series}
							seriesLayout="group"
							labels={false}
							legend={{ placement: "bottom" }}
							props={{
								xAxis: {
									tickLength: 3,
									ticks: 14,
									format: (value) => value
								},
								tooltip: {
									hideTotal: true,
									header: {
										format: (value) => value
									}
								}
							}}
						/>
					</div>
				</div>
			</section>
		{/each}
	</div>
{/if}
