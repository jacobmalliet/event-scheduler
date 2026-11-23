<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import { api, ApiError } from '@/api/client';
import type { EventDetails, EventInput } from '@/api/types';
import EventForm from '@/components/EventForm.vue';
import { hasStarted } from '@/utils/dates';

const props = defineProps<{ id: string }>();
const router = useRouter();

const event = ref<EventDetails | null>(null);
const loading = ref(true);
const loadError = ref<string | null>(null);

const fieldErrors = ref<Record<string, string>>({});
const formError = ref<string | null>(null);
const saving = ref(false);

async function load() {
  loading.value = true;
  loadError.value = null;
  try {
    event.value = await api.getEvent(props.id);
  } catch (error) {
    loadError.value = error instanceof Error ? error.message : 'Failed to load event';
  } finally {
    loading.value = false;
  }
}

async function save(values: EventInput) {
  saving.value = true;
  fieldErrors.value = {};
  formError.value = null;
  try {
    await api.updateEvent(props.id, values);
    await router.push({ name: 'event-registrations', params: { id: props.id } });
  } catch (error) {
    if (error instanceof ApiError && error.problem.errors) fieldErrors.value = error.fieldErrors;
    else formError.value = error instanceof Error ? error.message : 'Failed to save event';
  } finally {
    saving.value = false;
  }
}

onMounted(load);
</script>

<template>
  <RouterLink :to="{ name: 'event-registrations', params: { id } }" class="back">
    ← Back to event
  </RouterLink>

  <p v-if="loading">Loading…</p>
  <p v-else-if="loadError" class="error">{{ loadError }}</p>

  <section v-else-if="event" class="card">
    <h2>Edit event</h2>
    <!-- Reachable by a bookmarked or shared link; the API would reject the save with event_in_past. -->
    <p v-if="hasStarted(event.startsAt)" class="muted">
      This event has started, so it can no longer be edited.
    </p>
    <template v-else>
      <p class="muted">
        {{ event.registeredCount }} registered, so max capacity can't go below that.
      </p>
      <EventForm
        :initial-values="event"
        submit-label="Save changes"
        submitting-label="Saving…"
        :submitting="saving"
        :field-errors="fieldErrors"
        :form-error="formError"
        @submit="save"
      >
        <template #actions>
          <RouterLink :to="{ name: 'event-registrations', params: { id } }">Cancel</RouterLink>
        </template>
      </EventForm>
    </template>
  </section>
</template>
