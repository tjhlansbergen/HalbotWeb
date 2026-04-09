<script lang="ts">
	import { onMount } from "svelte";
	import { LayerCake, Svg } from "layercake";
	import { scaleLinear } from "d3-scale";

	type Activity = {
		date: string;
		distance: number;
		speed: number;
		climb: number;
		duration: number;
	};

	type FingerprintPoint = {
		id: string;
		date: Date;
		distanceKm: number;
		paceSeconds: number;
		climbPerKm: number;
		daysAgo: number;
		radius: number;
		color: string;
		tooltip: string;
	};

	const LOOKBACK_DAYS = 365;
	const MIN_RADIUS = 2.2;
	const MAX_RADIUS = 7.2;
	const FIXED_MIN_PACE_SECONDS = 300; // 5:00 /km
	const FIXED_MAX_PACE_SECONDS = 600; // 10:00 /km
	const FIXED_MAX_DISTANCE_KM = 80;

	let isLoading = true;
	let error = "";
	let points: FingerprintPoint[] = [];
	let maxDistanceDomain = 10;
	let minPaceDomain = 300;
	let maxPaceDomain = 420;
	let paceTicks: number[] = [];
	let distanceTicks: number[] = [];

	function startOfDay(value: Date): Date {
		return new Date(value.getFullYear(), value.getMonth(), value.getDate());
	}

	function parseDate(value: string): Date | null {
		const parsed = new Date(value);
		return Number.isNaN(parsed.getTime()) ? null : parsed;
	}

	function paceFromActivity(activity: Activity): number | null {
		const speed = Number(activity.speed);
		if (Number.isFinite(speed) && speed > 0) {
			return 1000 / speed;
		}

		const distance = Number(activity.distance);
		const duration = Number(activity.duration);
		if (Number.isFinite(distance) && distance > 0 && Number.isFinite(duration) && duration > 0) {
			return duration / (distance / 1000);
		}

		return null;
	}

	function clamp(value: number, min: number, max: number): number {
		return Math.max(min, Math.min(max, value));
	}

	function quantile(values: number[], q: number): number {
		if (values.length === 0) {
			return 0;
		}

		const sorted = [...values].sort((a, b) => a - b);
		const index = (sorted.length - 1) * clamp(q, 0, 1);
		const low = Math.floor(index);
		const high = Math.ceil(index);
		if (low === high) {
			return sorted[low];
		}

		const weight = index - low;
		return sorted[low] * (1 - weight) + sorted[high] * weight;
	}

	function formatPace(secondsPerKm: number): string {
		const rounded = Math.round(secondsPerKm);
		const minutes = Math.floor(rounded / 60);
		const seconds = rounded % 60;
		return `${minutes}:${String(seconds).padStart(2, "0")}`;
	}

	function colorForAge(daysAgo: number): string {
		const freshness = 1 - clamp(daysAgo / LOOKBACK_DAYS, 0, 1);
		const lightness = 36 + freshness * 28;
		return `hsl(195 72% ${lightness}%)`;
	}

	function radiusForClimbRate(climbPerKm: number, maxClimbPerKm: number): number {
		if (maxClimbPerKm <= 0 || climbPerKm <= 0) {
			return MIN_RADIUS;
		}

		const scaled = Math.sqrt(clamp(climbPerKm / maxClimbPerKm, 0, 1));
		return MIN_RADIUS + (MAX_RADIUS - MIN_RADIUS) * scaled;
	}

	function buildDistanceTicks(maxDistance: number): number[] {
		const safeMax = Math.max(4, maxDistance);
		const step = safeMax <= 12
			? 2
			: safeMax <= 30
				? 5
				: 10;
		const ticks: number[] = [];
		for (let tick = 0; tick <= safeMax; tick += step) {
			ticks.push(tick);
		}

		if (ticks[ticks.length - 1] !== Math.round(safeMax)) {
			ticks.push(Math.round(safeMax));
		}

		return ticks;
	}

	function buildPaceTicks(minPace: number, maxPace: number): number[] {
		const start = Math.floor(minPace / 30) * 30;
		const end = Math.ceil(maxPace / 30) * 30;
		const ticks: number[] = [];
		for (let tick = start; tick <= end; tick += 30) {
			ticks.push(tick);
		}
		return ticks;
	}

	function xTickPercent(tick: number): number {
		const range = maxPaceDomain - minPaceDomain;
		if (range <= 0) {
			return 50;
		}

		return ((tick - minPaceDomain) / range) * 100;
	}

	function yTickPercent(tick: number): number {
		const range = maxDistanceDomain;
		if (range <= 0) {
			return 50;
		}

		return 100 - (tick / range) * 100;
	}

	function yTickStyle(tick: number): string {
		const pct = yTickPercent(tick);
		const transform = pct <= 0.001
			? "translateY(0)"
			: pct >= 99.999
				? "translateY(-100%)"
				: "translateY(-50%)";

		return `top: ${pct}%; transform: ${transform};`;
	}

	function compute(activities: Activity[]): void {
		const today = startOfDay(new Date());
		const cutoff = new Date(today);
		cutoff.setDate(cutoff.getDate() - LOOKBACK_DAYS);

		const prepped = activities
			.map((activity, index) => {
				const parsedDate = parseDate(activity.date);
				if (!parsedDate) {
					return null;
				}

				const runDate = startOfDay(parsedDate);
				if (runDate < cutoff || runDate > today) {
					return null;
				}

				const distanceMeters = Number(activity.distance);
				if (!Number.isFinite(distanceMeters) || distanceMeters <= 0) {
					return null;
				}

				const paceSeconds = paceFromActivity(activity);
				if (paceSeconds === null || !Number.isFinite(paceSeconds) || paceSeconds <= 0) {
					return null;
				}

				const climbMeters = Math.max(0, Number(activity.climb) || 0);
				const distanceKm = distanceMeters / 1000;
				const climbPerKm = distanceKm > 0 ? climbMeters / distanceKm : 0;
				const daysAgo = Math.floor((today.getTime() - runDate.getTime()) / 86400000);

				return {
					id: `${runDate.toISOString()}-${index}`,
					date: runDate,
					distanceKm,
					paceSeconds,
					climbPerKm,
					daysAgo
				};
			})
			.filter((point) => point !== null) as Array<{
				id: string;
				date: Date;
				distanceKm: number;
				paceSeconds: number;
				climbPerKm: number;
				daysAgo: number;
			}>;

		if (prepped.length === 0) {
			points = [];
			maxDistanceDomain = FIXED_MAX_DISTANCE_KM;
			minPaceDomain = FIXED_MIN_PACE_SECONDS;
			maxPaceDomain = FIXED_MAX_PACE_SECONDS;
			distanceTicks = buildDistanceTicks(maxDistanceDomain);
			paceTicks = buildPaceTicks(minPaceDomain, maxPaceDomain);
			return;
		}

		const climbRateValues = prepped.map((point) => point.climbPerKm);
		const maxClimbRate = quantile(climbRateValues, 0.95);

		maxDistanceDomain = FIXED_MAX_DISTANCE_KM;
		minPaceDomain = FIXED_MIN_PACE_SECONDS;
		maxPaceDomain = FIXED_MAX_PACE_SECONDS;
		distanceTicks = buildDistanceTicks(maxDistanceDomain);
		paceTicks = buildPaceTicks(minPaceDomain, maxPaceDomain);

		points = prepped
			.map((point) => {
				const cappedDistance = clamp(point.distanceKm, 0, maxDistanceDomain);
				const cappedPace = clamp(point.paceSeconds, minPaceDomain, maxPaceDomain);
				const radius = radiusForClimbRate(point.climbPerKm, maxClimbRate);
				const color = colorForAge(point.daysAgo);
				const dateLabel = new Intl.DateTimeFormat("en-US", {
					month: "short",
					day: "numeric",
					year: "numeric"
				}).format(point.date);

				return {
					...point,
					distanceKm: cappedDistance,
					paceSeconds: cappedPace,
					radius,
					color,
					tooltip: `${dateLabel} | ${point.distanceKm.toFixed(1)} km | ${formatPace(point.paceSeconds)} /km | ${Math.round(point.climbPerKm)} m/km`
				};
			})
			.sort((a, b) => b.daysAgo - a.daysAgo);
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
				throw new Error("Could not load fingerprint data.");
			}

			const payload = await response.json();
			const activities = (Array.isArray(payload) ? payload : []) as Activity[];
			compute(activities);
		} catch (err) {
			error = err instanceof Error ? err.message : "Could not load fingerprint data.";
			points = [];
		} finally {
			isLoading = false;
		}
	}

	onMount(load);
</script>

{#if isLoading}
	<p class="insights-placeholder">Loading fingerprint...</p>
{:else if error}
	<p class="insights-placeholder">{error}</p>
{:else if points.length === 0}
	<p class="insights-placeholder">No activity data available for the past year.</p>
{:else}
	<section class="fingerprint" aria-label="Activity fingerprint for the last year: distance, pace, climb intensity, and recency">
		<h3 class="fingerprint-title">Run Fingerprint (Last 365 Days)</h3>
		<p class="fingerprint-subtitle">X: pace, Y: distance, size: climb per km, color: recency</p>

		<div class="fingerprint-chart-wrap" role="img" aria-label="Scatter plot of runs by pace and distance, with bubble size for climb per kilometer and lighter color for newer activities">
			<LayerCake
				let:xScale
				let:yScale
				let:width
				let:height
				ssr={false}
				data={points}
				x="paceSeconds"
				y="distanceKm"
				xScale={scaleLinear()}
				yScale={scaleLinear()}
				xDomain={[minPaceDomain, maxPaceDomain]}
				yDomain={[0, maxDistanceDomain]}
				yPadding={[0.04, 0.04]}
			>
				<Svg>
					{#each paceTicks as tick (`x-${tick}`)}
						<line x1={xScale(tick) ?? 0} y1="0" x2={xScale(tick) ?? 0} y2={height} class="fingerprint-grid" />
					{/each}

					{#each distanceTicks as tick (`y-${tick}`)}
						<line x1="0" y1={yScale(tick) ?? 0} x2={width} y2={yScale(tick) ?? 0} class="fingerprint-grid" />
					{/each}

					{#each points as point (point.id)}
						<circle
							cx={xScale(point.paceSeconds) ?? 0}
							cy={yScale(point.distanceKm) ?? 0}
							r={point.radius}
							fill={point.color}
							class="fingerprint-point"
						>
							<title>{point.tooltip}</title>
						</circle>
					{/each}
				</Svg>
			</LayerCake>

			<div class="fingerprint-y-axis" aria-hidden="true">
				{#each distanceTicks.filter((tick) => tick > 0) as tick (`yl-${tick}`)}
					<span class="fingerprint-y-tick" style={yTickStyle(tick)}>{tick}km</span>
				{/each}
			</div>

			<div class="fingerprint-x-axis" aria-hidden="true">
				{#each paceTicks as tick (`xl-${tick}`)}
					<span class="fingerprint-x-tick" style={`left: ${xTickPercent(tick)}%;`}>{formatPace(tick)}</span>
				{/each}
			</div>
		</div>

		<div class="fingerprint-legend" aria-hidden="true">
			<span class="legend-title">Recency</span>
			<span class="legend-dot old"></span><span class="legend-label">older</span>
			<span class="legend-dot mid"></span><span class="legend-label">middle</span>
			<span class="legend-dot new"></span><span class="legend-label">newer</span>
			<span class="legend-size">Bubble size scales with climb/km</span>
		</div>
	</section>
{/if}

<style>
	.fingerprint {
		display: grid;
		gap: 0.5rem;
	}

	.fingerprint-title {
		margin: 0;
		font-size: 0.98rem;
		font-weight: 650;
		color: var(--text-color);
	}

	.fingerprint-subtitle {
		margin: 0;
		font-size: 0.72rem;
		color: var(--muted-text);
	}

	.fingerprint-chart-wrap {
		position: relative;
		width: min(100%, 24rem);
		aspect-ratio: 1 / 1.75;
		padding: 0 0.2rem 2rem 2.2rem;
		margin: 0 auto;
	}

	:global(.fingerprint-chart-wrap .layercake-container),
	:global(.fingerprint-chart-wrap .layercake-container-inner) {
		width: 100%;
		height: 100%;
	}

	.fingerprint-grid {
		stroke: var(--table-border-color);
		stroke-width: 0.65;
		opacity: 0.75;
	}

	.fingerprint-point {
		stroke: #ffffff;
		stroke-width: 0.6;
		opacity: 0.88;
	}

	.fingerprint-x-axis {
		position: relative;
		height: 1.65rem;
		margin-top: 0.25rem;
		font-size: 0.61rem;
		color: var(--muted-text);
		font-variant-numeric: tabular-nums;
	}

	.fingerprint-y-axis {
		position: absolute;
		top: 0;
		bottom: 2rem;
		left: 0;
		width: 2rem;
		font-size: 0.61rem;
		color: var(--muted-text);
		font-variant-numeric: tabular-nums;
		pointer-events: none;
		text-align: right;
	}

	.fingerprint-y-tick {
		position: absolute;
		right: 0.22rem;
		transform: translateY(-50%);
		white-space: nowrap;
	}

	.fingerprint-x-tick {
		position: absolute;
		transform: translateX(-50%);
		white-space: nowrap;
	}

	.fingerprint-legend {
		display: flex;
		align-items: center;
		gap: 0.38rem;
		font-size: 0.66rem;
		color: var(--muted-text);
	}

	.legend-title {
		margin-right: 0.25rem;
		font-weight: 600;
		color: var(--text-color);
	}

	.legend-dot {
		width: 0.62rem;
		height: 0.62rem;
		border-radius: 999px;
		display: inline-block;
	}

	.legend-dot.old {
		background: hsl(195 72% 36%);
	}

	.legend-dot.mid {
		background: hsl(195 72% 48%);
	}

	.legend-dot.new {
		background: hsl(195 72% 64%);
	}

	.legend-label {
		margin-right: 0.32rem;
	}

	.legend-size {
		margin-left: 0.58rem;
	}

	@media (max-width: 700px) {
		.fingerprint-chart-wrap {
			width: 100%;
			aspect-ratio: 1 / 1.6;
			padding: 0 0.05rem 2rem 1.9rem;
		}

		.fingerprint-y-axis {
			font-size: 0.54rem;
			left: 0;
			width: 1.72rem;
		}

		.fingerprint-x-axis {
			font-size: 0.54rem;
		}

		.fingerprint-legend {
			font-size: 0.58rem;
			gap: 0.27rem;
		}
	}
</style>
