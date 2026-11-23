<script setup lang="ts">
/**
 * Previous / Next controls for a `PagedResponse`. Renders nothing when everything fits on one page.
 */
defineProps<{
  page: number;
  totalPages: number;
  totalCount: number;
  /** Plural noun for the summary, e.g. "events" → "Page 1 of 3 · 25 events". */
  itemLabel: string;
  /** Accessible name for the navigation landmark, e.g. "Event pages". */
  label: string;
  /** Disables both buttons, e.g. while a page is loading. */
  disabled?: boolean;
}>();

defineEmits<{ change: [page: number] }>();
</script>

<template>
  <nav v-if="totalPages > 1" class="pager" :aria-label="label">
    <button type="button" :disabled="page <= 1 || disabled" @click="$emit('change', page - 1)">
      ← Previous
    </button>
    <span class="muted">
      Page {{ page }} of {{ totalPages }} · {{ totalCount }} {{ itemLabel }}
    </span>
    <button
      type="button"
      :disabled="page >= totalPages || disabled"
      @click="$emit('change', page + 1)"
    >
      Next →
    </button>
  </nav>
</template>
