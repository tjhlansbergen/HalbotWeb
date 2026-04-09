<script lang="ts">
	import { onMount } from "svelte";
	import { LayerCake, Svg } from "layercake";
	import { scaleLinear, scalePoint } from "d3-scale";

	type Activity = {
		date: string;
		speed: number;
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

	const BUCKET_COUNT = 14;
	const PACE_TOP_SECONDS = 300; // 5:00 min/km
	const PACE_BOTTOM_SECONDS = 420; // 7:00 min/km

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

	function compute(activities: Activity[]): void {
		const today = startOfDay(new Date());
		const dated = activities
			.map((activity) => ({
				...activity,
				parsedDate: parseDate(activity.date)
			}))
			.filter((activity) => activity.parsedDate !== null) as Array<Activity & { parsedDate: Date }>;

		weekSeries = buildWeekBuckets(dated, today);
		monthSeries = buildMonthBuckets(dated, today);
		yearSeries = buildYearBuckets(dated, today);
	}

	async function load(): Promise<void> {
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
				throw new Error("Could not load load data.");
			}

			const payload = await response.json();
			const activities = (Array.isArray(payload) ? payload : []) as Activity[];
			compute(activities);
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
		<div class="load-section">
			<h3 class="load-section-title">Volume</h3>
		</div>

		<div class="load-section">
			<h3 class="load-section-title">Pace</h3>

			{#each [weekSeries, monthSeries, yearSeries] as series (series.title)}
				{@const chartData = toChartData(series)}
				<div class="pace-chart-section">
					<h4 class="pace-chart-title">{series.title}</h4>
					<div class="pace-chart-wrap" role="img" aria-label={`${series.title}, pace range from 5:00 to 7:00 per kilometer`}>
						<LayerCake
							let:xScale
							let:yScale
							let:width
							ssr={false}
							data={chartData}
							x="label"
							y="pace"
							xScale={scalePoint()}
							yScale={scaleLinear()}
							xDomain={chartData.map((point) => point.label)}
							yDomain={[PACE_BOTTOM_SECONDS, PACE_TOP_SECONDS]}
							yPadding={[0, 0]}
						>
							<Svg>
								{@const plotted = plottedPoints(chartData, xScale, yScale)}
								{#each [PACE_TOP_SECONDS, 330, 360, 390, PACE_BOTTOM_SECONDS] as tick (`${series.title}-${tick}`)}
									<line x1="0" y1={yScale(tick) ?? 0} x2={width} y2={yScale(tick) ?? 0} class="grid-line" />
								{/each}

								{#if hasAnyData(chartData)}
									<path d={curvedPath(plotted)} class="pace-line"></path>
									{#each plotted as point (`${series.title}-${point.label}`)}
										<circle cx={point.x} cy={point.y} r="2.25" class="pace-point" />
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
							{#each chartData as point, idx (`${series.title}-${idx}`)}
								{@const position = chartData.length > 1 ? (idx / (chartData.length - 1)) * 100 : 50}
								{@const isWeekSeries = series.title === "Average pace for last 14 weeks"}
							{@const isMonthSeries = series.title === "Average pace for last 14 months"}
							<span class={`x-marker ${isWeekSeries ? "x-marker-week" : ""}`} style={`left: ${position}%;`}>
								{isWeekSeries ? point.label.replace(" ", "\n") : isMonthSeries ? point.label.split(" ")[0] : point.label}
								</span>
							{/each}
						</div>
					</div>
				</div>
			{/each}
		</div>
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

	:global(.pace-chart-wrap .layercake-container),
	:global(.pace-chart-wrap .layercake-container-inner) {
		width: 100%;
		height: 100%;
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

	.pace-point {
		fill: #2e66a9;
		stroke: #ffffff;
		stroke-width: 0.8;
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
		height: 2.8rem;
		margin-top: 0.2rem;
		font-size: 0.64rem;
		color: var(--muted-text);
		font-variant-numeric: tabular-nums;
	}

	.x-marker {
		position: absolute;
		top: 0;
		width: 2rem;
		text-align: center;
		white-space: normal;
		overflow: hidden;
		text-overflow: clip;
		line-height: 1.05;
		transform: translateX(-50%);
	}

	@media (max-width: 700px) {
		.pace-chart-section {
			margin-top: 1.9rem;
			margin-bottom: 1rem;
		}

		.pace-chart-title {
			margin-bottom: 0.7rem;
		}

		.pace-chart-wrap {
			height: 10rem;
			padding: 0 0.35rem 0 0;
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
			height: 3.6rem;
		}

		.x-marker {
			width: 1.8rem;
			word-break: break-word;
			line-height: 1;
		}

		.x-marker-week {
			white-space: pre-line;
		}
	}
</style>
