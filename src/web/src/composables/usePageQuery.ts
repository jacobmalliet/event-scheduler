import { computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';

/**
 * The current 1-based page number, kept in the URL (`?page=2`) so Back, refresh and shared links keep it.
 * Page 1 is left out of the URL.
 */
export function usePageQuery() {
  const route = useRoute();
  const router = useRouter();

  const page = computed(() => Math.max(1, Math.floor(Number(route.query.page)) || 1));

  function goToPage(target: number) {
    void router.push({ query: { ...route.query, page: target > 1 ? String(target) : undefined } });
  }

  return { page, goToPage };
}
