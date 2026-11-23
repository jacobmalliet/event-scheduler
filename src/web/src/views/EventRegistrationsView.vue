<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import { useRouter } from 'vue-router';
import { api, ApiError } from '@/api/client';
import type { EventDetails, PagedResponse, Registration } from '@/api/types';
import PagerNav from '@/components/PagerNav.vue';
import { usePageQuery } from '@/composables/usePageQuery';
import { formatDate, formatDateRange, hasStarted } from '@/utils/dates';

const props = defineProps<{ id: string }>();
const router = useRouter();
const { page, goToPage } = usePageQuery();

const event = ref<EventDetails | null>(null);
const registrations = ref<PagedResponse<Registration> | null>(null);
const loading = ref(true);
const loadError = ref<string | null>(null);
const loadingRegistrations = ref(false);

const email = ref('');
const submitError = ref<string | null>(null);
const submitting = ref(false);

const actionError = ref<string | null>(null);
const unregistering = ref<string | null>(null);

const spotsLeft = computed(() =>
  event.value ? Math.max(event.value.maxCapacity - event.value.registeredCount, 0) : 0,
);
// The API enforces this too; the UI just hides actions that would be rejected.
const started = computed(() => (event.value ? hasStarted(event.value.startsAt) : false));

/** Reloads the event and the current page of attendees. Returns false if it moved to another page instead. */
async function refresh(): Promise<boolean> {
  const [loadedEvent, loadedRegistrations] = await Promise.all([
    api.getEvent(props.id),
    api.listRegistrations(props.id, page.value),
  ]);
  event.value = loadedEvent;
  // e.g. the last attendee on the last page unregistered: show the new last page instead.
  if (
    loadedRegistrations.items.length === 0 &&
    loadedRegistrations.totalPages > 0 &&
    page.value > loadedRegistrations.totalPages
  ) {
    goToPage(loadedRegistrations.totalPages);
    return false;
  }
  registrations.value = loadedRegistrations;
  return true;
}

// Changing page reloads just the attendee list, not the whole view.
watch(page, async () => {
  loadingRegistrations.value = true;
  actionError.value = null;
  try {
    await refresh();
  } catch (error) {
    actionError.value = error instanceof Error ? error.message : 'Failed to load attendees';
  } finally {
    loadingRegistrations.value = false;
  }
});

async function load() {
  loading.value = true;
  loadError.value = null;
  try {
    await refresh();
  } catch (error) {
    loadError.value = error instanceof Error ? error.message : 'Failed to load event';
  } finally {
    loading.value = false;
  }
}

async function register() {
  submitting.value = true;
  submitError.value = null;
  try {
    await api.register(props.id, { email: email.value });
    email.value = '';
    await refresh();
    // Attendees are listed in registration order, so the new one is on the last page.
    if (registrations.value && registrations.value.totalPages > page.value) {
      goToPage(registrations.value.totalPages);
    }
  } catch (error) {
    submitError.value =
      error instanceof ApiError && error.problem.errors
        ? Object.values(error.fieldErrors).join(' ')
        : error instanceof Error
          ? error.message
          : 'Registration failed';
  } finally {
    submitting.value = false;
  }
}

const deleting = ref(false);
const deleteError = ref<string | null>(null);

async function deleteEvent() {
  if (!event.value) return;
  const attendees = event.value.registeredCount;
  const warning =
    attendees > 0
      ? `Delete "${event.value.title}"? Its ${attendees} registration${attendees === 1 ? '' : 's'} will be removed too.`
      : `Delete "${event.value.title}"?`;
  if (!window.confirm(warning)) return;

  deleting.value = true;
  deleteError.value = null;
  try {
    await api.deleteEvent(props.id);
    await router.push({ name: 'events' });
  } catch (error) {
    deleteError.value = error instanceof Error ? error.message : 'Deleting the event failed';
  } finally {
    deleting.value = false;
  }
}

async function unregister(registration: Registration) {
  unregistering.value = registration.id;
  actionError.value = null;
  try {
    await api.unregister(props.id, registration.id);
    await refresh();
  } catch (error) {
    actionError.value = error instanceof Error ? error.message : 'Unregistering failed';
  } finally {
    unregistering.value = null;
  }
}

onMounted(load);
</script>

<template>
  <RouterLink to="/" class="back">← All events</RouterLink>

  <p v-if="loading">Loading…</p>
  <p v-else-if="loadError" class="error">{{ loadError }}</p>

  <template v-else-if="event">
    <section class="card">
      <div class="card-header">
        <h2>{{ event.title }}</h2>
        <div v-if="!started" class="header-actions">
          <RouterLink :to="{ name: 'edit-event', params: { id } }">Edit</RouterLink>
          <button type="button" class="secondary" :disabled="deleting" @click="deleteEvent">
            {{ deleting ? 'Deleting…' : 'Delete' }}
          </button>
        </div>
      </div>
      <p v-if="deleteError" class="error">{{ deleteError }}</p>
      <p class="muted">
        {{ formatDateRange(event.startsAt, event.endsAt) }} · {{ spotsLeft }} of
        {{ event.maxCapacity }} spots left
      </p>
      <p v-if="event.description">{{ event.description }}</p>

      <p v-if="started" class="muted">This event has started; registrations are closed.</p>
      <template v-else>
        <form class="inline-form" novalidate @submit.prevent="register">
          <input
            v-model="email"
            type="email"
            placeholder="you@example.com"
            aria-label="Email address"
            required
          />
          <button type="submit" :disabled="submitting">
            {{ submitting ? 'Registering…' : 'Register' }}
          </button>
        </form>
        <p v-if="submitError" class="error">{{ submitError }}</p>
      </template>
    </section>

    <section class="card">
      <h2>Attendees ({{ event.registeredCount }})</h2>
      <p v-if="actionError" class="error">{{ actionError }}</p>
      <p v-if="!registrations || registrations.totalCount === 0" class="muted">
        No one has registered yet.
      </p>
      <table v-else>
        <thead>
          <tr>
            <th>Email</th>
            <th>Registered</th>
            <th v-if="!started"><span class="visually-hidden">Actions</span></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="registration in registrations.items" :key="registration.id">
            <td>{{ registration.email }}</td>
            <td>{{ formatDate(registration.registeredAt) }}</td>
            <td v-if="!started" class="num">
              <button
                type="button"
                class="secondary"
                :disabled="unregistering !== null"
                @click="unregister(registration)"
              >
                {{ unregistering === registration.id ? 'Removing…' : 'Unregister' }}
              </button>
            </td>
          </tr>
        </tbody>
      </table>
      <PagerNav
        v-if="registrations"
        :page="registrations.page"
        :total-pages="registrations.totalPages"
        :total-count="registrations.totalCount"
        item-label="attendees"
        label="Attendee pages"
        :disabled="loadingRegistrations"
        @change="goToPage"
      />
    </section>
  </template>
</template>
