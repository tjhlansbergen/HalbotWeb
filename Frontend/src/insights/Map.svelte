<script lang="ts">
	import { createEventDispatcher, onMount, tick } from "svelte";
	import L from "leaflet";
	import "leaflet/dist/leaflet.css";
	import "leaflet.markercluster";
	import "leaflet.markercluster/dist/MarkerCluster.css";
	import "leaflet.markercluster/dist/MarkerCluster.Default.css";
	import { formatDate, formatDistance } from "../lib/utils";

	type Activity = {
		id: number;
		date: string;
		distance: number;
		lat: number;
		lng: number;
		isRace?: boolean;
		[key: string]: unknown;
	};

	const dispatch = createEventDispatcher();

	let isLoading = true;
	let error = "";
	let allPlottedActivities: Activity[] = [];
	let showRacesOnly = false;
	let mapHost: HTMLDivElement | null = null;
	let map: ReturnType<typeof L.map> | null = null;
	let clusterLayer: L.MarkerClusterGroup | null = null;

	function getVisibleActivities(): Activity[] {
		if (!showRacesOnly) {
			return allPlottedActivities;
		}

		return allPlottedActivities.filter((activity) => activity.isRace === true);
	}

	function hasValidCoords(activity: Activity): boolean {
		const lat = Number(activity.lat);
		const lng = Number(activity.lng);

		if (!Number.isFinite(lat) || !Number.isFinite(lng)) {
			return false;
		}

		if (lat === 0 && lng === 0) {
			return false;
		}

		return lat >= -90 && lat <= 90 && lng >= -180 && lng <= 180;
	}

	function openDetail(activity: Activity): void {
		dispatch("openDetail", {
			type: "activity",
			date: activity.date,
			data: activity
		});
	}

	function setupMap(activities: Activity[]): void {
		if (!mapHost) {
			return;
		}

		if (map) {
			map.remove();
			map = null;
		}

		clusterLayer = null;

		map = L.map(mapHost, {
			zoomControl: true,
			attributionControl: true
		});

		L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
			maxZoom: 19,
			attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
		}).addTo(map);

		const bounds = L.latLngBounds([]);
		const byId = new Map<number, Activity>();
		clusterLayer = L.markerClusterGroup({
			showCoverageOnHover: false,
			maxClusterRadius: 44,
			disableClusteringAtZoom: 14,
			spiderfyOnMaxZoom: true
		});

		for (const activity of activities) {
			const lat = Number(activity.lat);
			const lng = Number(activity.lng);
			const distanceLabel = `${formatDistance(activity.distance)} km`;
			const dateLabel = formatDate(activity.date);
			const marker = L.circleMarker([lat, lng], {
				radius: 6,
				color: "#2e66a9",
				weight: 2,
				fillColor: "#2e66a9",
				fillOpacity: 0.55
			});

			const detailsLink = Number.isFinite(activity.id)
				? `<a href="#" class="map-detail-link" data-activity-id="${activity.id}">Open details</a>`
				: "";

			marker.bindPopup(
				`<div class="map-popup"><div class="map-popup-date">${dateLabel}</div><div class="map-popup-distance">${distanceLabel}</div>${detailsLink}</div>`
			);
			clusterLayer.addLayer(marker);

			if (Number.isFinite(activity.id)) {
				byId.set(activity.id, activity);
			}

			bounds.extend([lat, lng]);
		}

		clusterLayer.addTo(map);

		map.on("popupopen", (event) => {
			const popupElement = event.popup.getElement();
			if (!popupElement) {
				return;
			}

			const link = popupElement.querySelector(".map-detail-link") as HTMLAnchorElement | null;
			if (!link) {
				return;
			}

			const onClick = (clickEvent: MouseEvent) => {
				clickEvent.preventDefault();
				const rawId = link.dataset.activityId;
				const id = rawId ? Number.parseInt(rawId, 10) : Number.NaN;
				if (!Number.isFinite(id)) {
					return;
				}

				const selected = byId.get(id);
				if (selected) {
					openDetail(selected);
				}
			};

			link.addEventListener("click", onClick, { once: true });
		});

		if (bounds.isValid()) {
			map.fitBounds(bounds.pad(0.15));
		} else {
			map.setView([20, 0], 2);
		}

		setTimeout(() => map?.invalidateSize(), 0);
	}

	function refreshMapForCurrentFilter(): void {
		if (!mapHost || error || isLoading || allPlottedActivities.length === 0) {
			return;
		}

		setupMap(getVisibleActivities());
	}

	function onToggleRacesOnly(): void {
		refreshMapForCurrentFilter();
	}

	async function loadMapData(): Promise<void> {
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
				throw new Error("Could not load map data.");
			}

			const payload = await response.json();
			const items = (Array.isArray(payload) ? payload : []) as Activity[];
			allPlottedActivities = items.filter((activity) => hasValidCoords(activity));
		} catch (err) {
			allPlottedActivities = [];
			error = err instanceof Error ? err.message : "Could not load map data.";
		} finally {
			isLoading = false;
			await tick();
			if (!error && allPlottedActivities.length > 0) {
				setupMap(getVisibleActivities());
			}
		}
	}

	onMount(() => {
		void loadMapData();

		return () => {
			if (clusterLayer && map) {
				clusterLayer.removeFrom(map);
				clusterLayer = null;
			}

			if (map) {
				map.remove();
				map = null;
			}
		};
	});
</script>

{#if isLoading}
	<p class="insights-placeholder">Loading map...</p>
{:else if error}
	<p class="insights-placeholder">{error}</p>
{:else if allPlottedActivities.length === 0}
	<p class="insights-placeholder">No runs with valid coordinates found.</p>
{:else}
	<section class="map-section" aria-label="Map of all runs with valid coordinates">
		<div bind:this={mapHost} class="map-canvas"></div>
		<div class="map-controls-row">
			<label class="map-filter-toggle">
				<input type="checkbox" bind:checked={showRacesOnly} on:change={onToggleRacesOnly} />
				<span>Races only</span>
			</label>
			<a
				class="citystrides-link"
				href="https://citystrides.com/users/36287/map"
				target="_blank"
				rel="noopener noreferrer"
			>
				CityStrides
			</a>
		</div>
	</section>
{/if}

<style>
	.map-section {
		width: 100%;
	}

	.map-canvas {
		width: 100%;
		height: 26rem;
		border: 1px solid var(--divider-color);
		border-radius: 10px;
		overflow: hidden;
	}

	.map-filter-toggle {
		display: inline-flex;
		align-items: center;
		gap: 0.38rem;
		font-size: 0.88rem;
		color: var(--text-color);
		user-select: none;
	}

	.map-controls-row {
		margin-top: .5rem;
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: 0.75rem;
	}

	.map-filter-toggle input {
		margin: 0;
		accent-color: #2e66a9;
	}

	.citystrides-link {
		font-size: 0.88rem;
		font-weight: 600;
		color: #2e66a9;
		text-decoration: underline;
		white-space: nowrap;
	}

	.citystrides-link:hover {
		color: #1f4f86;
	}

	:global(.map-popup) {
		display: grid;
		gap: 0.2rem;
		font-size: 0.86rem;
		line-height: 1.2;
	}

	:global(.map-popup-date) {
		font-weight: 600;
	}

	:global(.map-popup-distance) {
		font-variant-numeric: tabular-nums;
	}

	:global(.map-detail-link) {
		color: #2e66a9;
		text-decoration: underline;
		font-weight: 600;
	}

	@media (max-width: 700px) {
		.map-canvas {
			height: 30rem;
			border-radius: 8px;
		}

		.map-filter-toggle {
			font-size: 0.8rem;
		}

		.map-controls-row {
			margin-top: 1rem;
			gap: 0.5rem;
		}

		.citystrides-link {
			font-size: 0.8rem;
		}

		:global(.map-popup) {
			font-size: 0.8rem;
		}
	}
</style>
