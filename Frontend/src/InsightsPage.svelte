<script lang="ts">
  import Overall   from "./insights/Overall.svelte";
  import Y2D       from "./insights/Y2D.svelte";
  import Stats     from "./insights/Stats.svelte";
  import Weeks     from "./insights/Weeks.svelte";
  import Eddington from "./insights/Eddington.svelte";
  import Map       from "./insights/Map.svelte";

  const subPages = [
    { title: "Overall",   component: Overall   },
    { title: "Y2D",       component: Y2D       },
    { title: "Stats",     component: Stats     },
    { title: "Weeks",     component: Weeks     },
    { title: "Eddington", component: Eddington },
    { title: "Map",       component: Map       },
  ] as const;

  let index = 0;

  function prev() {
    index = (index - 1 + subPages.length) % subPages.length;
  }

  function next() {
    index = (index + 1) % subPages.length;
  }

  $: current = subPages[index];
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
    <svelte:component this={current.component} />
  </div>
</div>
