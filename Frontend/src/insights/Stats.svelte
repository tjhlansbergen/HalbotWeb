<script lang="ts">
	import { onMount } from "svelte";
	import { formatActivityType } from "../lib/utils";

	type Activity = {
		id?: number;
		date: string;
		distance: number;
		climb: number;
		speed: number;
		duration: number;
		effort: number;
		heartrate?: number | null;
		maxElevation?: number;
		dataType?: number | string;
	};

	type LifetimeStats = {
		activityCount: number;
		totalEffort: number;
		totalDistanceKm: number;
		totalClimb: number;
		averagePaceSecondsPerKm: number | null;
		averageHeartrate: number | null;
		averageEffort: number;
		averageDistanceKm: number;
	};

	type TopActivityRow = {
		rank: number;
		valueLabel: string;
		dateLabel: string;
		barValue: number;
	};

	type VolumeRow = {
		rank: number;
		label: string;
		valueKm: number;
	};

	type TypeRow = {
		label: string;
		count: number;
	};

	let isLoading = true;
	let error = "";

	let lifetime: LifetimeStats = {
		activityCount: 0,
		totalEffort: 0,
		totalDistanceKm: 0,
		totalClimb: 0,
		averagePaceSecondsPerKm: null,
		averageHeartrate: null,
		averageEffort: 0,
		averageDistanceKm: 0
	};

	let topEffort: TopActivityRow[] = [];
	let topDistance: TopActivityRow[] = [];
	let topClimb: TopActivityRow[] = [];
	let topHighestPoint: TopActivityRow[] = [];
	let topFastestPace: TopActivityRow[] = [];

	let topWeeks: VolumeRow[] = [];
	let topMonths: VolumeRow[] = [];
	let topYears: VolumeRow[] = [];

	let projectedYear = new Date().getFullYear();
	let projectedDistanceKm = 0;
	let typeCounts: TypeRow[] = [];

	function toDate(value: string): Date | null {
		const parsed = new Date(value);
		return Number.isNaN(parsed.getTime()) ? null : parsed;
	}

	function startOfDay(value: Date): Date {
		return new Date(value.getFullYear(), value.getMonth(), value.getDate());
	}

	function formatNumber(value: number, digits: number): string {
		return value.toFixed(digits);
	}

	function formatDateFull(value: Date): string {
		return new Intl.DateTimeFormat("en-US", {
			weekday: "long",
			month: "long",
			day: "numeric",
			year: "numeric"
		}).format(value);
	}

	function formatDateMonthDay(value: Date): string {
		return new Intl.DateTimeFormat("en-US", {
			day: "2-digit",
			month: "long"
		}).format(value);
	}

	function paceSecondsPerKm(activity: Activity): number | null {
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

	function formatPaceSeconds(secondsPerKm: number | null): string {
		if (secondsPerKm === null || !Number.isFinite(secondsPerKm) || secondsPerKm <= 0) {
			return "-";
		}

		const rounded = Math.round(secondsPerKm);
		const minutes = Math.floor(rounded / 60);
		const seconds = rounded % 60;
		return `${minutes}:${String(seconds).padStart(2, "0")}`;
	}

	function calcEffort(activity: Activity): number {
		const effort = Number(activity.effort);
		if (Number.isFinite(effort) && effort > 0) {
			return effort;
		}

		const distance = Math.max(0, Number(activity.distance) || 0);
		const climb = Math.max(0, Number(activity.climb) || 0);
		const speed = Math.max(0, Number(activity.speed) || 0);
		return Math.round(((distance + (climb * 8)) * speed) / 1000);
	}

	function daysPastInYear(today: Date): number {
		const start = new Date(today.getFullYear(), 0, 1);
		const diffMs = startOfDay(today).getTime() - start.getTime();
		return Math.floor(diffMs / 86400000) + 1;
	}

	function getIsoWeek(date: Date): { year: number; week: number } {
		const utc = new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()));
		const day = utc.getUTCDay() || 7;
		utc.setUTCDate(utc.getUTCDate() + 4 - day);
		const weekYear = utc.getUTCFullYear();
		const yearStart = new Date(Date.UTC(weekYear, 0, 1));
		const week = Math.ceil((((utc.getTime() - yearStart.getTime()) / 86400000) + 1) / 7);
		return { year: weekYear, week };
	}

	function mondayOfWeek(date: Date): Date {
		const day = (date.getDay() + 6) % 7;
		const result = startOfDay(date);
		result.setDate(result.getDate() - day);
		return result;
	}

	function topActivities(
		activities: Array<Activity & { parsedDate: Date }>,
		measure: (activity: Activity & { parsedDate: Date }) => number | null,
		formatValue: (value: number) => string,
		maxItems = 5,
		sortDirection: "desc" | "asc" = "desc"
	): TopActivityRow[] {
		const valid = activities
			.map((activity) => ({
				activity,
				value: measure(activity)
			}))
			.filter((item) => item.value !== null && Number.isFinite(item.value as number)) as Array<{
				activity: Activity & { parsedDate: Date };
				value: number;
			}>;

		valid.sort((a, b) => {
			if (sortDirection === "asc") {
				if (a.value !== b.value) {
					return a.value - b.value;
				}
			} else if (a.value !== b.value) {
				return b.value - a.value;
			}

			return b.activity.parsedDate.getTime() - a.activity.parsedDate.getTime();
		});

		const selected = valid.slice(0, maxItems);
		if (selected.length === 0) {
			return [];
		}

		const minValue = Math.min(...selected.map((item) => item.value));
		const maxValue = Math.max(...selected.map((item) => item.value));

		return selected.map((item, index) => {
			const normalized = sortDirection === "asc"
				? (item.value > 0 ? minValue / item.value : 0)
				: (maxValue > 0 ? item.value / maxValue : 0);

			const barValue = Number.isFinite(normalized)
				? Math.max(0, Math.min(1, normalized))
				: 0;

			return {
				rank: index + 1,
				valueLabel: formatValue(item.value),
				dateLabel: formatDateFull(item.activity.parsedDate),
				barValue
			};
		});
	}

	function topByVolume(map: Map<string, { label: string; distanceKm: number }>, maxItems = 5): VolumeRow[] {
		return [...map.values()]
			.sort((a, b) => b.distanceKm - a.distanceKm)
			.slice(0, maxItems)
			.map((item, index) => ({
				rank: index + 1,
				label: item.label,
				valueKm: item.distanceKm
			}));
	}

	function compute(activities: Activity[]): void {
		const dated = activities
			.map((activity) => ({ ...activity, parsedDate: toDate(activity.date) }))
			.filter((activity) => activity.parsedDate !== null) as Array<Activity & { parsedDate: Date }>;

		const count = dated.length;
		const totalDistanceMeters = dated.reduce((sum, activity) => sum + Math.max(0, Number(activity.distance) || 0), 0);
		const totalDistanceKm = totalDistanceMeters / 1000;
		const totalClimb = dated.reduce((sum, activity) => sum + Math.max(0, Number(activity.climb) || 0), 0);
		const totalEffort = dated.reduce((sum, activity) => sum + calcEffort(activity), 0);

		const totalDuration = dated.reduce((sum, activity) => sum + Math.max(0, Number(activity.duration) || 0), 0);
		const averagePaceSeconds = totalDistanceMeters > 0 && totalDuration > 0
			? (totalDuration / totalDistanceMeters) * 1000
			: null;

		const validHr = dated
			.map((activity) => Number(activity.heartrate))
			.filter((hr) => Number.isFinite(hr) && hr >= 50);

		const averageHeartrate = validHr.length > 0
			? validHr.reduce((sum, value) => sum + value, 0) / validHr.length
			: null;

		lifetime = {
			activityCount: count,
			totalEffort: Math.round(totalEffort),
			totalDistanceKm: totalDistanceKm,
			totalClimb: Math.round(totalClimb),
			averagePaceSecondsPerKm: averagePaceSeconds,
			averageHeartrate,
			averageEffort: count > 0 ? totalEffort / count : 0,
			averageDistanceKm: count > 0 ? totalDistanceKm / count : 0
		};

		topEffort = topActivities(
			dated,
			(activity) => calcEffort(activity),
			(value) => `${Math.round(value)}`
		);

		topDistance = topActivities(
			dated,
			(activity) => Math.max(0, Number(activity.distance) || 0),
			(value) => `${formatNumber(value / 1000, 2)} km`
		);

		topClimb = topActivities(
			dated,
			(activity) => Math.max(0, Number(activity.climb) || 0),
			(value) => `${Math.round(value)} meter`
		);

		topHighestPoint = topActivities(
			dated,
			(activity) => {
				const elevation = Number(activity.maxElevation);
				return Number.isFinite(elevation) && elevation > 0 ? elevation : null;
			},
			(value) => `${Math.round(value)} meter`
		);

		topFastestPace = topActivities(
			dated,
			(activity) => paceSecondsPerKm(activity),
			(value) => `${formatPaceSeconds(value)} m/km`,
			5,
			"asc"
		);

		const weekMap = new Map<string, { label: string; distanceKm: number }>();
		const monthMap = new Map<string, { label: string; distanceKm: number }>();
		const yearMap = new Map<string, { label: string; distanceKm: number }>();

		for (const activity of dated) {
			const distanceKm = Math.max(0, Number(activity.distance) || 0) / 1000;
			const date = activity.parsedDate;

			const iso = getIsoWeek(date);
			const monday = mondayOfWeek(date);
			const sunday = new Date(monday);
			sunday.setDate(sunday.getDate() + 6);
			const weekKey = `${iso.year}-W${String(iso.week).padStart(2, "0")}`;
			const weekLabel = `${iso.year} week ${iso.week} (${formatDateMonthDay(monday)} - ${formatDateMonthDay(sunday)})`;
			const weekRow = weekMap.get(weekKey) ?? { label: weekLabel, distanceKm: 0 };
			weekRow.distanceKm += distanceKm;
			weekMap.set(weekKey, weekRow);

			const monthKey = `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}`;
			const monthName = date.toLocaleString("en-US", { month: "long" });
			const monthLabel = `${date.getFullYear()} ${monthName}`;
			const monthRow = monthMap.get(monthKey) ?? { label: monthLabel, distanceKm: 0 };
			monthRow.distanceKm += distanceKm;
			monthMap.set(monthKey, monthRow);

			const yearKey = String(date.getFullYear());
			const yearLabel = yearKey;
			const yearRow = yearMap.get(yearKey) ?? { label: yearLabel, distanceKm: 0 };
			yearRow.distanceKm += distanceKm;
			yearMap.set(yearKey, yearRow);
		}

		topWeeks = topByVolume(weekMap);
		topMonths = topByVolume(monthMap);
		topYears = topByVolume(yearMap);

		const today = startOfDay(new Date());
		projectedYear = today.getFullYear();
		const ytdDistanceKm = dated
			.filter((activity) => {
				const date = activity.parsedDate;
				return date.getFullYear() === projectedYear && date.getTime() <= today.getTime();
			})
			.reduce((sum, activity) => sum + Math.max(0, Number(activity.distance) || 0) / 1000, 0);
		const daysPast = daysPastInYear(today);
		projectedDistanceKm = daysPast > 0 ? Math.round((ytdDistanceKm / daysPast) * 365) : 0;

		const typeMap = new Map<string, number>();
		for (const activity of dated) {
			const key = formatActivityType(activity.dataType ?? "Unknown");
			typeMap.set(key, (typeMap.get(key) ?? 0) + 1);
		}

		typeCounts = ["Classic", "TomTom", "Garmin"]
			.map((label) => ({ label, count: typeMap.get(label) ?? 0 }))
			.filter((row) => row.count > 0);
	}

	async function loadStats(): Promise<void> {
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
				throw new Error("Could not load stats data.");
			}

			const payload = await response.json();
			const activities = (Array.isArray(payload) ? payload : []) as Activity[];
			compute(activities);
		} catch (err) {
			error = err instanceof Error ? err.message : "Could not load stats data.";
		} finally {
			isLoading = false;
		}
	}

	onMount(loadStats);
</script>

{#if isLoading}
	<p class="insights-placeholder">Loading stats...</p>
{:else if error}
	<p class="insights-placeholder">{error}</p>
{:else}
	<section class="stats" aria-label="Running statistics">
		<div class="stats-section">
			<h3 class="stats-section-title">Lifetime:</h3>
			<div class="kv-list">
				<div class="kv-row"><span class="key">Activities:</span><span class="value">{lifetime.activityCount}</span></div>
				<div class="kv-row"><span class="key">Effort:</span><span class="value">{lifetime.totalEffort}</span></div>
				<div class="kv-row"><span class="key">Distance:</span><span class="value">{formatNumber(lifetime.totalDistanceKm, 2)}km</span></div>
				<div class="kv-row"><span class="key">Climb:</span><span class="value">{lifetime.totalClimb}m</span></div>
				<div class="kv-row"><span class="key">Average pace:</span><span class="value">{formatPaceSeconds(lifetime.averagePaceSecondsPerKm)}m/km</span></div>
				<div class="kv-row"><span class="key">Average heartrate:</span><span class="value">{lifetime.averageHeartrate === null ? "-" : `${Math.round(lifetime.averageHeartrate)}bpm`}</span></div>
				<div class="kv-row"><span class="key">Average effort:</span><span class="value">{Math.round(lifetime.averageEffort)}</span></div>
				<div class="kv-row"><span class="key">Average distance:</span><span class="value">{formatNumber(lifetime.averageDistanceKm, 1)}km</span></div>
			</div>
		</div>

		<div class="stats-section spacer-before">
			<h3 class="stats-section-title">Top 5 best effort:</h3>
			<div class="top-list">
				{#if topEffort.length === 0}
					<p class="mini-placeholder">No data.</p>
				{:else}
					{#each topEffort as row (row.rank)}
						<div class="top-row has-bar">
							<span class="rank">{row.rank}.</span>
							<span class="content"><span class="v">{row.valueLabel}</span> on {row.dateLabel}</span>
							<span class="bar effort" style={`width:${row.barValue * 100}%;`}></span>
						</div>
					{/each}
				{/if}
			</div>
		</div>

		<div class="stats-section">
			<h3 class="stats-section-title">Top 5 longest distance:</h3>
			<div class="top-list">
				{#if topDistance.length === 0}
					<p class="mini-placeholder">No data.</p>
				{:else}
					{#each topDistance as row (row.rank)}
						<div class="top-row has-bar">
							<span class="rank">{row.rank}.</span>
							<span class="content"><span class="v">{row.valueLabel}</span> on {row.dateLabel}</span>
							<span class="bar distance" style={`width:${row.barValue * 100}%;`}></span>
						</div>
					{/each}
				{/if}
			</div>
		</div>

		<div class="stats-section">
			<h3 class="stats-section-title">Top 5 total climb:</h3>
			<div class="top-list">
				{#if topClimb.length === 0}
					<p class="mini-placeholder">No data.</p>
				{:else}
					{#each topClimb as row (row.rank)}
						<div class="top-row has-bar">
							<span class="rank">{row.rank}.</span>
							<span class="content"><span class="v">{row.valueLabel}</span> on {row.dateLabel}</span>
							<span class="bar climb" style={`width:${row.barValue * 100}%;`}></span>
						</div>
					{/each}
				{/if}
			</div>
		</div>

		<div class="stats-section">
			<h3 class="stats-section-title">Top 5 highest point:</h3>
			<div class="top-list">
				{#if topHighestPoint.length === 0}
					<p class="mini-placeholder">No data.</p>
				{:else}
					{#each topHighestPoint as row (row.rank)}
						<div class="top-row has-bar">
							<span class="rank">{row.rank}.</span>
							<span class="content"><span class="v">{row.valueLabel}</span> on {row.dateLabel}</span>
							<span class="bar highest" style={`width:${row.barValue * 100}%;`}></span>
						</div>
					{/each}
				{/if}
			</div>
		</div>

		<div class="stats-section">
			<h3 class="stats-section-title">Top 5 fastest pace:</h3>
			<div class="top-list">
				{#if topFastestPace.length === 0}
					<p class="mini-placeholder">No data.</p>
				{:else}
					{#each topFastestPace as row (row.rank)}
						<div class="top-row has-bar">
							<span class="rank">{row.rank}.</span>
							<span class="content"><span class="v">{row.valueLabel}</span> on {row.dateLabel}</span>
							<span class="bar pace" style={`width:${row.barValue * 100}%;`}></span>
						</div>
					{/each}
				{/if}
			</div>
		</div>

		<div class="stats-section spacer-before">
			<h3 class="stats-section-title">Top 5 best week (by volume):</h3>
			<div class="kv-list compact">
				{#if topWeeks.length === 0}
					<p class="mini-placeholder">No data.</p>
				{:else}
					{#each topWeeks as row (row.rank)}
						<div class="kv-row"><span class="key">{row.label}</span><span class="value">{formatNumber(row.valueKm, 2)} km</span></div>
					{/each}
				{/if}
			</div>
		</div>

		<div class="stats-section">
			<h3 class="stats-section-title">Top 5 best months (by volume):</h3>
			<div class="kv-list compact">
				{#if topMonths.length === 0}
					<p class="mini-placeholder">No data.</p>
				{:else}
					{#each topMonths as row (row.rank)}
						<div class="kv-row"><span class="key">{row.label}</span><span class="value">{formatNumber(row.valueKm, 2)} km</span></div>
					{/each}
				{/if}
			</div>
		</div>

		<div class="stats-section">
			<h3 class="stats-section-title">Top 5 best years (by volume):</h3>
			<div class="kv-list compact">
				{#if topYears.length === 0}
					<p class="mini-placeholder">No data.</p>
				{:else}
					{#each topYears as row (row.rank)}
						<div class="kv-row"><span class="key">{row.label}</span><span class="value">{formatNumber(row.valueKm, 2)} km</span></div>
					{/each}
				{/if}
			</div>
		</div>

		<div class="stats-section">
			<h3 class="stats-section-title">Year total (projected):</h3>
			<div class="kv-list compact">
				<div class="kv-row"><span class="key">{projectedYear}</span><span class="value">{projectedDistanceKm} km</span></div>
			</div>
		</div>

		<div class="stats-section spacer-before">
			<h3 class="stats-section-title">Activities by type:</h3>
			<div class="kv-list compact">
				{#if typeCounts.length === 0}
					<p class="mini-placeholder">No data.</p>
				{:else}
					{#each typeCounts as row (row.label)}
						<div class="kv-row"><span class="key">{row.label}</span><span class="value">{row.count}</span></div>
					{/each}
				{/if}
			</div>
		</div>
	</section>
{/if}

<style>
	.stats {
		display: grid;
		gap: 0.82rem;
		width: 100%;
		font-size: 0.9rem;
		line-height: 1.2;
		font-variant-numeric: tabular-nums;
	}

	.stats-section {
		border-top: 1px solid var(--table-border-color);
		padding-top: 0.28rem;
	}

	.stats-section:first-child {
		border-top: 0;
		padding-top: 0;
	}

	.stats-section + .stats-section {
		margin-top: 0.5rem;
	}

	.stats-section.spacer-before {
		margin-top: 2.00rem;
	}

	.stats-section-title {
		margin: 0 0 0.28rem;
		font-size: 1.02rem;
		font-weight: 700;
		color: var(--insight-title-color);
	}

	.kv-list {
		display: grid;
		gap: 0.06rem;
	}

	.kv-list.compact {
		gap: 0.1rem;
	}

	.kv-row {
		display: grid;
		grid-template-columns: minmax(0, 1fr) auto;
		column-gap: 0.5rem;
		align-items: baseline;
	}

	.key {
		color: var(--text-color);
		min-width: 0;
	}

	.value {
		text-align: right;
		white-space: nowrap;
	}

	.top-list {
		display: grid;
		gap: 0.08rem;
	}

	.top-row {
		display: grid;
		grid-template-columns: 1.3rem minmax(0, 1fr);
		column-gap: 0.35rem;
		align-items: center;
		min-width: 0;
	}

	.top-row.has-bar {
		grid-template-columns: 1.3rem minmax(0, 1fr) 3.2rem;
	}

	.rank {
		color: var(--text-color);
	}

	.content {
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
	}

	.v {
		font-weight: 500;
	}

	.bar {
		height: 0.92rem;
		justify-self: end;
		width: 100%;
		max-width: 3.2rem;
		border-radius: 0;
		opacity: 0.9;
	}

	.bar.effort { background: #d86f24; }
	.bar.distance { background: #d4a415; }
	.bar.climb { background: #84af86; }
	.bar.highest { background: #b58ebd; }
	.bar.pace { background: #5d9ba0; }

	.mini-placeholder {
		margin: 0;
		color: var(--muted-text);
	}

	@media (max-width: 700px) {
		.stats {
			font-size: 0.79rem;
			gap: 0.7rem;
		}

		.stats-section {
			padding-top: 0.24rem;
		}

		.stats-section + .stats-section {
			margin-top: 0.5rem;
		}

		.stats-section.spacer-before {
			margin-top: 2.0rem;
		}

		.stats-section-title {
			font-size: 0.95rem;
			margin-bottom: 0.24rem;
		}

		.kv-row {
			column-gap: 0.3rem;
		}

		.top-row {
			grid-template-columns: 1.1rem minmax(0, 1fr);
			column-gap: 0.24rem;
		}

		.top-row.has-bar {
			grid-template-columns: 1.1rem minmax(0, 1fr) 2.85rem;
		}

		.bar {
			height: 0.78rem;
			max-width: 2.85rem;
		}
	}
</style>
