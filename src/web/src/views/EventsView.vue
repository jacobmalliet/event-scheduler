<script setup lang="ts">
import { ref, watch } from 'vue';
import { api, ApiError } from '@/api/client';
import type { EventInput, EventSummary, PagedResponse } from '@/api/types';
import EventForm from '@/components/EventForm.vue';
import PagerNav from '@/components/PagerNav.vue';
import { usePageQuery } from '@/composables/usePageQuery';
import { formatDateRange } from '@/utils/dates';

const result = ref<PagedResponse<EventSummary> | null>(null);
const loading = ref(true);
const loadError = ref<string | null>(null);

const fieldErrors = ref<Record<string, string>>({});
const formError = ref<string | null>(null);
const saving = ref(false);
// Bumped after a successful create to re-mount (and so clear) the form.
const formKey = ref(0);

const { page, goToPage } = usePageQuery();

async function load() {
  loading.value = true;
  loadError.value = null;
  try {
    const loaded = await api.listEvents(page.value);
    // e.g. the last event on the last page was deleted: show the new last page instead.
    if (loaded.items.length === 0 && loaded.totalPages > 0 && page.value > loaded.totalPages) {
      goToPage(loaded.totalPages);
      return;
    }
    result.value = loaded;
  } catch (error) {
    loadError.value = error instanceof Error ? error.message : 'Failed to load events';
  } finally {
    loading.value = false;
  }
}

async function create(values: EventInput) {
  saving.value = true;
  fieldErrors.value = {};
  formError.value = null;
  try {
    await api.createEvent(values);
    formKey.value++;
    await load();
  } catch (error) {
    if (error instanceof ApiError && error.problem.errors) fieldErrors.value = error.fieldErrors;
    else formError.value = error instanceof Error ? error.message : 'Failed to create event';
  } finally {
    saving.value = false;
  }
}

watch(page, load, { immediate: true });
</script>

<template>
  <div class="events-layout">
    <section class="card events-sidebar" aria-label="New event">
      <h2>New event</h2>
      <EventForm
        :key="formKey"
        submit-label="Create event"
        submitting-label="Creating…"
        :submitting="saving"
        :field-errors="fieldErrors"
        :form-error="formError"
        @submit="create"
      />
    </section>

    <section class="card">
      <h2>Events</h2>
      <p v-if="loading && !result">Loading…</p>
      <p v-else-if="loadError" class="error">{{ loadError }}</p>
      <p v-else-if="!result || result.totalCount === 0" class="muted">
        No events yet. Create one with the New event form.
      </p>
      <template v-else>
        <table>
          <thead>
            <tr>
              <th>Title</th>
              <th>Date</th>
              <th class="num">Registered</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="event in result.items" :key="event.id">
              <td>
                <RouterLink :to="{ name: 'event-registrations', params: { id: event.id } }">{{
                  event.title
                }}</RouterLink>
              </td>
              <td>{{ formatDateRange(event.startsAt, event.endsAt) }}</td>
              <td class="num">{{ event.registeredCount }} / {{ event.maxCapacity }}</td>
            </tr>
          </tbody>
        </table>

        <PagerNav
          :page="result.page"
          :total-pages="result.totalPages"
          :total-count="result.totalCount"
          item-label="events"
          label="Event pages"
          :disabled="loading"
          @change="goToPage"
        />
      </template>
    </section>
  </div>
</template>
