<script lang="ts">
	import { onMount } from "svelte";
	import { LayerCake, Svg } from "layercake";
	import { scaleLinear, scalePoint } from "d3-scale";

	type Activity = {
		date: string;
		speed: number;
		distance: number;
		climb: number;
	};

	type Workout = {
		date: string;
		minutes: number;
	};

	type BucketSeries = {
		title: string;
		labels: string[];
		values: Array<number | null>;
		currentLabel: string;
	};

	type ChartPoint = {
		index: number;
		label: string;
		pace: number | null;
	};

	type PlotPoint = {
		x: number;
		y: number;
		label: string;
		pace: number;
	};

	type VolumeSeries = {
		title: string;
		labels: string[];
		distanceKm: number[];
		climbMeters: number[];
		workoutMinutes: number[];
	};

	type VolumeChartPoint = {
		index: number;
		label: string;
		distanceKm: number;
		climbMeters: number;
		workoutMinutes: number;
		distanceHeight: number;
		climbHeight: number;
		workoutHeight: number;
		totalHeight: number;
	};

	type VolumePlotPoint = {
		x: number;
		distanceTop: number;
		climbTop: number;
		workoutTop: number;
	};

	const BUCKET_COUNT = 14;
	const PACE_TOP_SECONDS = 300; // 5:00 min/km
	const PACE_BOTTOM_SECONDS = 420; // 7:00 min/km
	const HEIGHT_PER_KM = 1;
	const HEIGHT_PER_CLIMB_METER = 10 / 500;
	const HEIGHT_PER_WORKOUT_MINUTE = 10 / 60;

	let isLoading = true;
	let error = "";

	let weekSeries: BucketSeries = {
		title: "Average pace for last 14 weeks",
		labels: [],
		values: [],
		currentLabel: ""
	};

	let monthSeries: BucketSeries = {
		title: "Average pace for last 14 months",
		labels: [],
		values: [],
		currentLabel: ""
	};

	let yearSeries: BucketSeries = {
		title: "Average pace for last 14 years",
		labels: [],
		values: [],
		currentLabel: ""
	};

	let weekVolumeSeries: VolumeSeries = {
		title: "Volume for last 14 weeks",
		labels: [],
		distanceKm: [],
		climbMeters: [],
		workoutMinutes: []
	};

	let monthVolumeSeries: VolumeSeries = {
		title: "Volume for last 14 months",
		labels: [],
		distanceKm: [],
		climbMeters: [],
		workoutMinutes: []
	};

	let yearVolumeSeries: VolumeSeries = {
		title: "Volume for last 14 years",
		labels: [],
		distanceKm: [],
		climbMeters: [],
		workoutMinutes: []
	};

	function parseDate(value: string): Date | null {
		const parsed = new Date(value);
		return Number.isNaN(parsed.getTime()) ? null : parsed;
	}

	function startOfDay(value: Date): Date {
		return new Date(value.getFullYear(), value.getMonth(), value.getDate());
	}

	function weekStartMonday(value: Date): Date {
		const base = startOfDay(value);
		const mondayOffset = (base.getDay() + 6) % 7;
		base.setDate(base.getDate() - mondayOffset);
		return base;
	}

	function paceFromSpeed(speed: number): number | null {
		if (!Number.isFinite(speed) || speed <= 0) {
			return null;
		}

		return 1000 / speed;
	}

	function formatPace(secondsPerKm: number | null): string {
		if (secondsPerKm === null || !Number.isFinite(secondsPerKm) || secondsPerKm <= 0) {
			return "-";
		}

		const rounded = Math.round(secondsPerKm);
		const minutes = Math.floor(rounded / 60);
		const seconds = rounded % 60;
		return `${minutes}:${String(seconds).padStart(2, "0")}`;
	}

	function clamp(value: number, min: number, max: number): number {
		return Math.max(min, Math.min(max, value));
	}

	function safePositiveNumber(value: unknown): number {
		const numeric = Number(value);
		return Number.isFinite(numeric) && numeric > 0 ? numeric : 0;
	}

	function normalizeDistanceKm(distanceMeters: number): number {
		return distanceMeters / 1000;
	}

	function markerLabelForSeries(seriesTitle: string, label: string): string {
		if (seriesTitle.includes("last 14 weeks")) {
			return label.replace(" ", "\n");
		}

		if (seriesTitle.includes("last 14 months")) {
			return label.split(" ")[0] ?? label;
		}

		return label;
	}

	function formatMinutesValue(minutes: number): string {
		if (!Number.isFinite(minutes) || minutes <= 0) {
			return "";
		}

		return `${Math.round(minutes)}'`;
	}

	function formatClimbValue(climbMeters: number): string {
		if (!Number.isFinite(climbMeters) || climbMeters <= 0) {
			return "";
		}

		return `${Math.round(climbMeters)}m`;
	}

	function formatDistanceValue(distanceKm: number): string {
		if (!Number.isFinite(distanceKm) || distanceKm <= 0) {
			return "";
		}

		return `${Math.round(distanceKm)}km`;
	}

	function toChartData(series: BucketSeries): ChartPoint[] {
		return series.labels.map((label, index) => ({
			index,
			label,
			pace: series.values[index] ?? null
		}));
	}

	function hasAnyData(points: ChartPoint[]): boolean {
		return points.some((point) => point.pace !== null);
	}

	function toVolumeChartData(series: VolumeSeries): VolumeChartPoint[] {
		return series.labels.map((label, index) => {
			const distanceKm = series.distanceKm[index] ?? 0;
			const climbMeters = series.climbMeters[index] ?? 0;
			const workoutMinutes = series.workoutMinutes[index] ?? 0;
			const distanceHeight = distanceKm * HEIGHT_PER_KM;
			const climbHeight = climbMeters * HEIGHT_PER_CLIMB_METER;
			const workoutHeight = workoutMinutes * HEIGHT_PER_WORKOUT_MINUTE;

			return {
				index,
				label,
				distanceKm,
				climbMeters,
				workoutMinutes,
				distanceHeight,
				climbHeight,
				workoutHeight,
				totalHeight: distanceHeight + climbHeight + workoutHeight
			};
		});
	}

	function hasAnyVolumeData(points: VolumeChartPoint[]): boolean {
		return points.some((point) => point.totalHeight > 0);
	}

	function volumeDomainMax(points: VolumeChartPoint[]): number {
		const maxValue = points.reduce((max, point) => Math.max(max, point.totalHeight), 0);
		return maxValue > 0 ? maxValue : 1;
	}

	function plottedVolumePoints(points: VolumeChartPoint[], xScale: (value: string) => number | undefined): VolumePlotPoint[] {
		return points.map((point) => ({
			x: xScale(point.label) ?? 0,
			distanceTop: point.distanceHeight,
			climbTop: point.distanceHeight + point.climbHeight,
			workoutTop: point.totalHeight
		}));
	}

	function stackedAreaPath(
		points: VolumePlotPoint[],
		yScale: (value: number) => number | undefined,
		topValue: (point: VolumePlotPoint) => number,
		bottomValue: (point: VolumePlotPoint) => number
	): string {
		if (points.length === 0) {
			return "";
		}

		const yTop = (point: VolumePlotPoint) => yScale(topValue(point)) ?? 0;
		const yBottom = (point: VolumePlotPoint) => yScale(bottomValue(point)) ?? 0;

		let path = `M ${points[0].x} ${yTop(points[0])}`;
		for (let i = 1; i < points.length; i += 1) {
			path += ` L ${points[i].x} ${yTop(points[i])}`;
		}

		for (let i = points.length - 1; i >= 0; i -= 1) {
			path += ` L ${points[i].x} ${yBottom(points[i])}`;
		}

		path += " Z";
		return path;
	}

	function plottedPoints(
		points: ChartPoint[],
		xScale: (value: string) => number | undefined,
		yScale: (value: number) => number | undefined
	): PlotPoint[] {
		return points
			.map((point) => {
				if (point.pace === null) {
					return null;
				}

				const clamped = clamp(point.pace, PACE_TOP_SECONDS, PACE_BOTTOM_SECONDS);
				return {
					x: xScale(point.label) ?? 0,
					y: yScale(clamped) ?? 0,
					label: point.label,
					pace: clamped
				};
			})
			.filter((point) => point !== null) as PlotPoint[];
	}

	function curvedPath(points: PlotPoint[]): string {
		if (points.length === 0) {
			return "";
		}

		if (points.length === 1) {
			const single = points[0];
			return `M ${single.x} ${single.y}`;
		}

		let path = `M ${points[0].x} ${points[0].y}`;

		for (let i = 0; i < points.length - 1; i += 1) {
			const p0 = i > 0 ? points[i - 1] : points[i];
			const p1 = points[i];
			const p2 = points[i + 1];
			const p3 = i < points.length - 2 ? points[i + 2] : p2;

			const cp1x = p1.x + (p2.x - p0.x) / 6;
			const cp1y = p1.y + (p2.y - p0.y) / 6;
			const cp2x = p2.x - (p3.x - p1.x) / 6;
			const cp2y = p2.y - (p3.y - p1.y) / 6;

			path += ` C ${cp1x} ${cp1y}, ${cp2x} ${cp2y}, ${p2.x} ${p2.y}`;
		}

		return path;
	}

	function addDays(value: Date, days: number): Date {
		const next = new Date(value);
		next.setDate(next.getDate() + days);
		return startOfDay(next);
	}

	function buildWeekBuckets(activities: Array<Activity & { parsedDate: Date }>, today: Date): BucketSeries {
		const currentWeekStart = weekStartMonday(today);
		const labels: string[] = [];
		const values: Array<number | null> = [];

		for (let i = BUCKET_COUNT - 1; i >= 0; i -= 1) {
			const start = addDays(currentWeekStart, -7 * i);
			const end = addDays(start, 7);
			const speeds = activities
				.filter((activity) => activity.parsedDate >= start && activity.parsedDate < end)
				.map((activity) => Number(activity.speed))
				.filter((speed) => Number.isFinite(speed) && speed > 0);

			const avgSpeed = speeds.length > 0
				? speeds.reduce((sum, speed) => sum + speed, 0) / speeds.length
				: null;
			const pace = avgSpeed === null ? null : paceFromSpeed(avgSpeed);

			const thursdayOfWeek = new Date(start);
			thursdayOfWeek.setDate(start.getDate() + 3);
			const jan4 = new Date(thursdayOfWeek.getFullYear(), 0, 4);
			const weekNum = Math.ceil(((thursdayOfWeek.getTime() - jan4.getTime()) / 86400000 + ((jan4.getDay() + 6) % 7) + 1) / 7);
			labels.push(`W${weekNum}`);
			values.push(pace);
		}

		return {
			title: "Average pace for last 14 weeks",
			labels,
			values,
			currentLabel: labels[labels.length - 1] ?? ""
		};
	}

	function buildMonthBuckets(activities: Array<Activity & { parsedDate: Date }>, today: Date): BucketSeries {
		const current = new Date(today.getFullYear(), today.getMonth(), 1);
		const labels: string[] = [];
		const values: Array<number | null> = [];

		for (let i = BUCKET_COUNT - 1; i >= 0; i -= 1) {
			const start = new Date(current.getFullYear(), current.getMonth() - i, 1);
			const end = new Date(start.getFullYear(), start.getMonth() + 1, 1);
			const speeds = activities
				.filter((activity) => activity.parsedDate >= start && activity.parsedDate < end)
				.map((activity) => Number(activity.speed))
				.filter((speed) => Number.isFinite(speed) && speed > 0);

			const avgSpeed = speeds.length > 0
				? speeds.reduce((sum, speed) => sum + speed, 0) / speeds.length
				: null;
			const pace = avgSpeed === null ? null : paceFromSpeed(avgSpeed);

			const monthLabel = start.toLocaleString("en-US", { month: "short" });
			labels.push(`${monthLabel} ${start.getFullYear()}`);
			values.push(pace);
		}

		return {
			title: "Average pace for last 14 months",
			labels,
			values,
			currentLabel: labels[labels.length - 1] ?? ""
		};
	}

	function buildYearBuckets(activities: Array<Activity & { parsedDate: Date }>, today: Date): BucketSeries {
		const currentYear = today.getFullYear();
		const labels: string[] = [];
		const values: Array<number | null> = [];

		for (let i = BUCKET_COUNT - 1; i >= 0; i -= 1) {
			const year = currentYear - i;
			const start = new Date(year, 0, 1);
			const end = new Date(year + 1, 0, 1);
			const speeds = activities
				.filter((activity) => activity.parsedDate >= start && activity.parsedDate < end)
				.map((activity) => Number(activity.speed))
				.filter((speed) => Number.isFinite(speed) && speed > 0);

			const avgSpeed = speeds.length > 0
				? speeds.reduce((sum, speed) => sum + speed, 0) / speeds.length
				: null;
			const pace = avgSpeed === null ? null : paceFromSpeed(avgSpeed);

			labels.push(String(year));
			values.push(pace);
		}

		return {
			title: "Average pace for last 14 years",
			labels,
			values,
			currentLabel: labels[labels.length - 1] ?? ""
		};
	}

	function buildWeekVolumeBuckets(
		activities: Array<Activity & { parsedDate: Date }>,
		workouts: Array<Workout & { parsedDate: Date }>,
		today: Date
	): VolumeSeries {
		const currentWeekStart = weekStartMonday(today);
		const labels: string[] = [];
		const distanceKm: number[] = [];
		const climbMeters: number[] = [];
		const workoutMinutes: number[] = [];

		for (let i = BUCKET_COUNT - 1; i >= 0; i -= 1) {
			const start = addDays(currentWeekStart, -7 * i);
			const end = addDays(start, 7);
			const activityBucket = activities.filter((activity) => activity.parsedDate >= start && activity.parsedDate < end);
			const workoutBucket = workouts.filter((workout) => workout.parsedDate >= start && workout.parsedDate < end);

			const totalDistanceMeters = activityBucket.reduce((sum, activity) => sum + safePositiveNumber(activity.distance), 0);
			const totalClimbMeters = activityBucket.reduce((sum, activity) => sum + safePositiveNumber(activity.climb), 0);
			const totalWorkoutMinutes = workoutBucket.reduce((sum, workout) => sum + safePositiveNumber(workout.minutes), 0);

			const thursdayOfWeek = new Date(start);
			thursdayOfWeek.setDate(start.getDate() + 3);
			const jan4 = new Date(thursdayOfWeek.getFullYear(), 0, 4);
			const weekNum = Math.ceil(((thursdayOfWeek.getTime() - jan4.getTime()) / 86400000 + ((jan4.getDay() + 6) % 7) + 1) / 7);

			labels.push(`W${weekNum}`);
			distanceKm.push(normalizeDistanceKm(totalDistanceMeters));
			climbMeters.push(totalClimbMeters);
			workoutMinutes.push(totalWorkoutMinutes);
		}

		return {
			title: "Volume for last 14 weeks",
			labels,
			distanceKm,
			climbMeters,
			workoutMinutes
		};
	}

	function buildMonthVolumeBuckets(
		activities: Array<Activity & { parsedDate: Date }>,
		workouts: Array<Workout & { parsedDate: Date }>,
		today: Date
	): VolumeSeries {
		const current = new Date(today.getFullYear(), today.getMonth(), 1);
		const labels: string[] = [];
		const distanceKm: number[] = [];
		const climbMeters: number[] = [];
		const workoutMinutes: number[] = [];

		for (let i = BUCKET_COUNT - 1; i >= 0; i -= 1) {
			const start = new Date(current.getFullYear(), current.getMonth() - i, 1);
			const end = new Date(start.getFullYear(), start.getMonth() + 1, 1);
			const activityBucket = activities.filter((activity) => activity.parsedDate >= start && activity.parsedDate < end);
			const workoutBucket = workouts.filter((workout) => workout.parsedDate >= start && workout.parsedDate < end);

			const totalDistanceMeters = activityBucket.reduce((sum, activity) => sum + safePositiveNumber(activity.distance), 0);
			const totalClimbMeters = activityBucket.reduce((sum, activity) => sum + safePositiveNumber(activity.climb), 0);
			const totalWorkoutMinutes = workoutBucket.reduce((sum, workout) => sum + safePositiveNumber(workout.minutes), 0);

			const monthLabel = start.toLocaleString("en-US", { month: "short" });
			labels.push(`${monthLabel} ${start.getFullYear()}`);
			distanceKm.push(normalizeDistanceKm(totalDistanceMeters));
			climbMeters.push(totalClimbMeters);
			workoutMinutes.push(totalWorkoutMinutes);
		}

		return {
			title: "Volume for last 14 months",
			labels,
			distanceKm,
			climbMeters,
			workoutMinutes
		};
	}

	function buildYearVolumeBuckets(
		activities: Array<Activity & { parsedDate: Date }>,
		workouts: Array<Workout & { parsedDate: Date }>,
		today: Date
	): VolumeSeries {
		const currentYear = today.getFullYear();
		const labels: string[] = [];
		const distanceKm: number[] = [];
		const climbMeters: number[] = [];
		const workoutMinutes: number[] = [];

		for (let i = BUCKET_COUNT - 1; i >= 0; i -= 1) {
			const year = currentYear - i;
			const start = new Date(year, 0, 1);
			const end = new Date(year + 1, 0, 1);
			const activityBucket = activities.filter((activity) => activity.parsedDate >= start && activity.parsedDate < end);
			const workoutBucket = workouts.filter((workout) => workout.parsedDate >= start && workout.parsedDate < end);

			const totalDistanceMeters = activityBucket.reduce((sum, activity) => sum + safePositiveNumber(activity.distance), 0);
			const totalClimbMeters = activityBucket.reduce((sum, activity) => sum + safePositiveNumber(activity.climb), 0);
			const totalWorkoutMinutes = workoutBucket.reduce((sum, workout) => sum + safePositiveNumber(workout.minutes), 0);

			labels.push(String(year));
			distanceKm.push(normalizeDistanceKm(totalDistanceMeters));
			climbMeters.push(totalClimbMeters);
			workoutMinutes.push(totalWorkoutMinutes);
		}

		return {
			title: "Volume for last 14 years",
			labels,
			distanceKm,
			climbMeters,
			workoutMinutes
		};
	}

	function compute(activities: Activity[], workouts: Workout[]): void {
		const today = startOfDay(new Date());
		const dated = activities
			.map((activity) => ({
				...activity,
				parsedDate: parseDate(activity.date)
			}))
			.filter((activity) => activity.parsedDate !== null) as Array<Activity & { parsedDate: Date }>;
		const datedWorkouts = workouts
			.map((workout) => ({
				...workout,
				parsedDate: parseDate(workout.date)
			}))
			.filter((workout) => workout.parsedDate !== null) as Array<Workout & { parsedDate: Date }>;

		weekSeries = buildWeekBuckets(dated, today);
		monthSeries = buildMonthBuckets(dated, today);
		yearSeries = buildYearBuckets(dated, today);
		weekVolumeSeries = buildWeekVolumeBuckets(dated, datedWorkouts, today);
		monthVolumeSeries = buildMonthVolumeBuckets(dated, datedWorkouts, today);
		yearVolumeSeries = buildYearVolumeBuckets(dated, datedWorkouts, today);
	}

	async function load(): Promise<void> {
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
				throw new Error("Could not load load data.");
			}

			const [activitiesPayload, workoutsPayload] = await Promise.all([
				activitiesResponse.json(),
				workoutsResponse.json()
			]);
			const activities = (Array.isArray(activitiesPayload) ? activitiesPayload : []) as Activity[];
			const workouts = (Array.isArray(workoutsPayload) ? workoutsPayload : []) as Workout[];
			compute(activities, workouts);
		} catch (err) {
			error = err instanceof Error ? err.message : "Could not load load data.";
		} finally {
			isLoading = false;
		}
	}

	onMount(load);
</script>

{#if isLoading}
	<p class="insights-placeholder">Loading load...</p>
{:else if error}
	<p class="insights-placeholder">{error}</p>
{:else}
	<section class="load" aria-label="Load insights">
		{#each [
			{ key: "weeks", volume: weekVolumeSeries, pace: weekSeries },
			{ key: "months", volume: monthVolumeSeries, pace: monthSeries },
			{ key: "years", volume: yearVolumeSeries, pace: yearSeries }
		] as group (group.key)}
			{@const volumeChartData = toVolumeChartData(group.volume)}
			{@const maxY = volumeDomainMax(volumeChartData)}
			{@const paceChartData = toChartData(group.pace)}
			<div class="load-section">
				<div class="volume-chart-section">
					<h4 class="pace-chart-title">{group.volume.title}</h4>
					<div class="volume-chart-wrap" role="img" aria-label={`${group.volume.title}, stacked volume with distance, climb, and workout minutes`}>
						<LayerCake
							let:xScale
							let:yScale
							ssr={false}
							data={volumeChartData}
							x="label"
							y="totalHeight"
							xScale={scalePoint()}
							yScale={scaleLinear()}
							xDomain={volumeChartData.map((point) => point.label)}
							yDomain={[0, maxY]}
							yPadding={[0, 0]}
						>
							<Svg>
								{#if hasAnyVolumeData(volumeChartData)}
									{@const plotted = plottedVolumePoints(volumeChartData, xScale)}
									<path d={stackedAreaPath(plotted, yScale, (point) => point.distanceTop, () => 0)} class="volume-area volume-area-distance"></path>
									<path d={stackedAreaPath(plotted, yScale, (point) => point.climbTop, (point) => point.distanceTop)} class="volume-area volume-area-climb"></path>
									<path d={stackedAreaPath(plotted, yScale, (point) => point.workoutTop, (point) => point.climbTop)} class="volume-area volume-area-workout"></path>
									{@const latestPoint = plotted[plotted.length - 1]}
									{@const previousPoint = plotted[plotted.length - 2]}
									{@const dividerX = previousPoint?.x ?? latestPoint?.x}
									{#if latestPoint}
										<line
											x1={dividerX ?? latestPoint.x}
											y1={yScale(maxY) ?? 0}
											x2={dividerX ?? latestPoint.x}
											y2={yScale(0) ?? 0}
											class="volume-incomplete-guide"
										/>
									{/if}
								{:else}
									<text x="50%" y="52" text-anchor="middle" class="no-data">No volume data</text>
								{/if}
							</Svg>
						</LayerCake>

						<div class="x-axis-markers x-axis-markers-volume" aria-hidden="true">
							{#each volumeChartData as point, idx (`${group.volume.title}-${idx}`)}
								{@const position = volumeChartData.length > 1 ? (idx / (volumeChartData.length - 1)) * 100 : 50}
								<span class={`x-marker x-marker-volume ${idx === volumeChartData.length - 1 ? "x-marker-incomplete" : ""}`} style={`left: ${position}%;`}>
									<span class="x-marker-label">{markerLabelForSeries(group.volume.title, point.label)}</span>
									<span class="x-marker-value x-marker-workout">{formatMinutesValue(point.workoutMinutes)}</span>
									<span class="x-marker-value x-marker-climb">{formatClimbValue(point.climbMeters)}</span>
									<span class="x-marker-value x-marker-distance">{formatDistanceValue(point.distanceKm)}</span>
								</span>
							{/each}
						</div>
					</div>
				</div>

				<div class="pace-chart-section">
					<h4 class="pace-chart-title">{group.pace.title}</h4>
					<div class="pace-chart-wrap" role="img" aria-label={`${group.pace.title}, pace range from 5:00 to 7:00 per kilometer`}>
						<LayerCake
							let:xScale
							let:yScale
							let:width
							ssr={false}
							data={paceChartData}
							x="label"
							y="pace"
							xScale={scalePoint()}
							yScale={scaleLinear()}
							xDomain={paceChartData.map((point) => point.label)}
							yDomain={[PACE_BOTTOM_SECONDS, PACE_TOP_SECONDS]}
							yPadding={[0, 0]}
						>
							<Svg>
								{@const plotted = plottedPoints(paceChartData, xScale, yScale)}
								{#each [PACE_TOP_SECONDS, 330, 360, 390, PACE_BOTTOM_SECONDS] as tick (`${group.pace.title}-${tick}`)}
									<line x1="0" y1={yScale(tick) ?? 0} x2={width} y2={yScale(tick) ?? 0} class="grid-line" />
								{/each}

								{#if hasAnyData(paceChartData)}
									{#if plotted.length > 1}
										<path d={curvedPath(plotted.slice(0, -1))} class="pace-line"></path>
										<path d={curvedPath(plotted.slice(-2))} class="pace-line pace-line-incomplete"></path>
									{:else}
										<path d={curvedPath(plotted)} class="pace-line"></path>
									{/if}
									{#each plotted as point, pointIndex (`${group.pace.title}-${point.label}`)}
										<circle
											cx={point.x}
											cy={point.y}
											r="2.25"
											class={`pace-point ${pointIndex === plotted.length - 1 ? "pace-point-incomplete" : ""}`}
										/>
									{/each}
								{:else}
									<text x={width / 2} y="52" text-anchor="middle" class="no-data">No pace data</text>
								{/if}
							</Svg>
						</LayerCake>

						<div class="y-axis y-axis-left">
							<span class="y-tick" style="top: 0%;">5:00</span>
							<span class="y-tick" style="top: 25%;">5:30</span>
							<span class="y-tick" style="top: 50%;">6:00</span>
							<span class="y-tick" style="top: 75%;">6:30</span>
						</div>

						<div class="x-axis-markers" aria-hidden="true">
							{#each paceChartData as point, idx (`${group.pace.title}-${idx}`)}
								{@const position = paceChartData.length > 1 ? (idx / (paceChartData.length - 1)) * 100 : 50}
								<span class={`x-marker ${group.pace.title === "Average pace for last 14 weeks" ? "x-marker-week" : ""}`} style={`left: ${position}%;`}>
									<span class="x-marker-label">{markerLabelForSeries(group.pace.title, point.label)}</span>
									<span class="x-marker-value">{formatPace(point.pace)}</span>
								</span>
							{/each}
						</div>
					</div>
				</div>
			</div>
		{/each}
	</section>
{/if}

<style>
	.load {
		display: grid;
		gap: 1rem;
		width: 100%;
	}

	.load-section {
		border-top: 1px solid var(--table-border-color);
		padding-top: 0.28rem;
	}

	.load-section:not(:last-child) {
		margin-bottom: 2rem;
	}

	.load-section:first-child {
		border-top: 0;
		padding-top: 0;
	}

	.load-section-title {
		margin: 0;
		font-size: 1.02rem;
		font-weight: 700;
		color: var(--insight-title-color);
	}

	.pace-chart-section {
		margin-top: 2rem;
		margin-bottom: 3rem;
	}

	.volume-chart-section {
		margin-top: 2rem;
		margin-bottom: 5rem;
	}

	.pace-chart-title {
		margin: 0 0 1rem;
		font-size: 0.95rem;
		font-weight: 600;
		color: var(--text-color);
	}

	.pace-chart-wrap {
		position: relative;
		height: 11.5rem;
		padding: 0 2.7rem;
	}

	.volume-chart-wrap {
		position: relative;
		height: 11.5rem;
		padding: 0 0.35rem;
	}

	:global(.pace-chart-wrap .layercake-container),
	:global(.pace-chart-wrap .layercake-container-inner),
	:global(.volume-chart-wrap .layercake-container),
	:global(.volume-chart-wrap .layercake-container-inner) {
		width: 100%;
		height: 100%;
	}

	.volume-area {
		stroke: none;
	}

	.volume-area-distance {
		fill: #5f9ea0;
		opacity: 0.95;
	}

	.volume-area-climb {
		fill: darkseagreen;
		opacity: 0.9;
	}

	.volume-area-workout {
		fill: #f0d060;
		opacity: 0.88;
	}

	.volume-incomplete-guide {
		stroke: var(--muted-text);
		stroke-width: 0.7;
		stroke-dasharray: 2 2;
		opacity: 0.45;
	}

	.grid-line {
		stroke: var(--table-border-color);
		stroke-width: 0.65;
	}

	.pace-line {
		fill: none;
		stroke: #2e66a9;
		stroke-width: 1.8;
		stroke-linecap: round;
		stroke-linejoin: round;
	}

	.pace-line-incomplete {
		stroke-dasharray: 3.2 2.4;
		opacity: 0.5;
	}

	.pace-point {
		fill: #2e66a9;
		stroke: #ffffff;
		stroke-width: 0.8;
	}

	.pace-point-incomplete {
		fill: #c77700;
	}

	.no-data {
		fill: var(--muted-text);
		font-size: 7px;
	}

	.y-axis {
		position: absolute;
		top: 0;
		bottom: 0;
		width: 2.3rem;
		font-size: 0.74rem;
		color: var(--muted-text);
		font-variant-numeric: tabular-nums;
		pointer-events: none;
	}

	.y-axis-left {
		left: 0;
		text-align: right;
	}

	.y-tick {
		position: absolute;
		right: 0.3rem;
		white-space: nowrap;
		display: inline-block;
		transform: translateY(-50%);
	}

	.x-axis-markers {
		position: relative;
		width: 100%;
		height: 3.9rem;
		margin-top: 0.28rem;
		font-size: 0.64rem;
		color: var(--muted-text);
		font-variant-numeric: tabular-nums;
	}

	.x-marker {
		position: absolute;
		top: 0;
		width: 2.25rem;
		text-align: center;
		white-space: normal;
		overflow: hidden;
		text-overflow: clip;
		line-height: 1.05;
		transform: translateX(-50%);
		display: flex;
		flex-direction: column;
		align-items: center;
		gap: 0.1rem;
	}

	.x-marker-label {
		display: block;
	}

	.x-marker-value {
		display: block;
		font-size: 0.58rem;
		color: var(--insight-title-color);
	}

	.x-axis-markers-volume {
		height: 5.2rem;
	}

	.x-axis-markers-volume .x-marker-value {
		min-height: 1em;
	}

	.x-marker-volume {
		width: 2.35rem;
		gap: 0.08rem;
	}

	.x-marker-incomplete {
		opacity: 0.72;
	}

	.x-marker-workout {
		color: #f0d060;
	}

	.x-marker-climb {
		color: darkseagreen;
	}

	.x-marker-distance {
		color: #5f9ea0;
	}

	@media (max-width: 700px) {
		.pace-chart-section {
			margin-top: 1.9rem;
			margin-bottom: 3rem;
		}

		.volume-chart-section {
			margin-top: 1.9rem;
			margin-bottom: 4rem;
		}

		.pace-chart-title {
			margin-bottom: 0.7rem;
		}

		.pace-chart-wrap {
			height: 10rem;
			padding: 0 0.35rem 0 0;
		}

		.volume-chart-wrap {
			height: 10rem;
		}

		.y-axis {
			font-size: 0.68rem;
			width: 1.55rem;
		}

		.y-axis-left {
			left: -0.15rem;
		}

		.y-tick {
			right: 0.15rem;
		}

		.x-axis-markers {
			font-size: 0.53rem;
			height: 4.2rem;
		}

		.x-axis-markers-volume {
			height: 5.35rem;
		}

		.x-marker {
			width: 1.95rem;
			word-break: break-word;
			line-height: 1;
		}

		.x-marker-volume {
			width: 2rem;
		}

		.x-marker-value {
			font-size: 0.5rem;
		}

		.x-marker-week {
			white-space: pre-line;
		}
	}
</style>
