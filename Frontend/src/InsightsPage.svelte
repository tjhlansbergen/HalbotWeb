<script lang="ts">
  import { createEventDispatcher, onMount } from "svelte";
  import Load      from "./insights/Load.svelte";
  import Y2D       from "./insights/Y2D.svelte";
  import Stats     from "./insights/Stats.svelte";
  import Weeks     from "./insights/Weeks.svelte";
  import Eddington from "./insights/Eddington.svelte";
  import Races     from "./insights/Races.svelte";
  import Map       from "./insights/Map.svelte";

  const INSIGHTS_TAB_KEY = "halbot-insights-active-index";
  const DEFAULT_SUBPAGE_TITLE = "Weeks";
  const dispatch = createEventDispatcher();
  let activeIndex = 0;
  let hasLoadedStoredIndex = false;

  const subPages = [
    { title: "Weeks",     component: Weeks     },
    { title: "Load",      component: Load      },
    { title: "Y2D",       component: Y2D       },
    { title: "Stats",     component: Stats     },
    { title: "Races",     component: Races     },
    { title: "Eddington", component: Eddington },
    { title: "Map",       component: Map       },
  ] as const;

  const defaultIndex = Math.max(0, subPages.findIndex((page) => page.title === DEFAULT_SUBPAGE_TITLE));
  activeIndex = defaultIndex;

  function prev() {
    activeIndex = (activeIndex - 1 + subPages.length) % subPages.length;
  }

  function next() {
    activeIndex = (activeIndex + 1) % subPages.length;
  }

  function openRaceDetail(event: CustomEvent<any>) {
    dispatch("openDetail", event.detail);
  }

  onMount(() => {
    if (typeof window === "undefined") {
      hasLoadedStoredIndex = true;
      return;
    }

    const stored = Number.parseInt(window.sessionStorage.getItem(INSIGHTS_TAB_KEY) ?? String(defaultIndex), 10);
    if (Number.isFinite(stored)) {
      activeIndex = Math.min(Math.max(stored, 0), subPages.length - 1);
    }

    hasLoadedStoredIndex = true;
  });

  $: current = subPages[activeIndex] ?? subPages[0];
  $: if (hasLoadedStoredIndex && typeof window !== "undefined") {
    window.sessionStorage.setItem(INSIGHTS_TAB_KEY, String(activeIndex));
  }
</script>

<div class="insights-page">
  <nav class="insights-nav">
    <button type="button" class="insights-nav-btn" on:click={prev} aria-label="Previous">
      <svg viewBox="0 0 24 24" aria-hidden="true" focusable="false">
        <polygon points="19,5 10,12 19,19"/>
        <line x1="5" y1="5" x2="5" y2="19"/>
      </svg>
    </button>

    <span class="insights-nav-title">{current.title}</span>

    <button type="button" class="insights-nav-btn" on:click={next} aria-label="Next">
      <svg viewBox="0 0 24 24" aria-hidden="true" focusable="false">
        <polygon points="5,5 14,12 5,19"/>
        <line x1="19" y1="5" x2="19" y2="19"/>
      </svg>
    </button>
  </nav>

  <div class="insights-content">
    {#if current.title === "Races"}
      <Races on:openDetail={openRaceDetail} />
    {:else}
      <svelte:component this={current.component} />
    {/if}
  </div>
</div>
