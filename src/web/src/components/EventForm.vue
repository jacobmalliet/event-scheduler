<script setup lang="ts">
import { computed, reactive, ref } from 'vue';
import type { EventInput } from '@/api/types';
import { fromDateAndTimeInputs, toDateAndTimeInputs } from '@/utils/dates';

/**
 * Shared create/edit form. Emits `submit` with an API-ready `EventInput` (each date and time pair combined
 * into one UTC timestamp); the parent sends it and passes server errors back in via `fieldErrors` / `formError`.
 */
const props = defineProps<{
  /** Values to start from when editing; omit for a blank "new event" form. */
  initialValues?: EventInput;
  submitLabel: string;
  submittingLabel: string;
  submitting: boolean;
  /** Server validation messages keyed by camelCase field name (see `ApiError.fieldErrors`). */
  fieldErrors: Record<string, string>;
  formError: string | null;
}>();

const emit = defineEmits<{ submit: [values: EventInput] }>();

const initialStart = props.initialValues
  ? toDateAndTimeInputs(props.initialValues.startsAt)
  : { date: '', time: '' };
const initialEnd = props.initialValues
  ? toDateAndTimeInputs(props.initialValues.endsAt)
  : { date: '', time: '' };

// Seeded once from initialValues; parents re-mount the form (via :key) to reset it.
const form = reactive({
  title: props.initialValues?.title ?? '',
  description: props.initialValues?.description ?? '',
  startDate: initialStart.date,
  startTime: initialStart.time,
  endDate: initialEnd.date,
  endTime: initialEnd.time,
  maxCapacity: props.initialValues?.maxCapacity ?? 10,
});

const incompleteStart = ref(false);
const startsAtError = computed(() =>
  incompleteStart.value ? 'Choose both a date and a time.' : props.fieldErrors.startsAt,
);
const incompleteEnd = ref(false);
const endsAtError = computed(() =>
  incompleteEnd.value ? 'Choose both a date and a time.' : props.fieldErrors.endsAt,
);
const invalidCapacity = ref(false);
const maxCapacityError = computed(() =>
  invalidCapacity.value ? 'Enter a whole number.' : props.fieldErrors.maxCapacity,
);

function submit() {
  const startsAt = fromDateAndTimeInputs(form.startDate, form.startTime);
  const endsAt = fromDateAndTimeInputs(form.endDate, form.endTime);
  incompleteStart.value = startsAt === null;
  incompleteEnd.value = endsAt === null;
  invalidCapacity.value = !Number.isInteger(form.maxCapacity);

  if (startsAt === null || endsAt === null || invalidCapacity.value) {
    return;
  }

  emit('submit', {
    title: form.title,
    description: form.description || null,
    startsAt,
    endsAt,
    maxCapacity: form.maxCapacity,
  });
}
</script>

<template>
  <form class="form" novalidate @submit.prevent="submit">
    <label>
      Title
      <input v-model="form.title" required maxlength="200" />
      <span v-if="fieldErrors.title" class="field-error">{{ fieldErrors.title }}</span>
    </label>
    <label>
      Description
      <textarea v-model="form.description" rows="2" maxlength="2000" />
      <span v-if="fieldErrors.description" class="field-error">{{ fieldErrors.description }}</span>
    </label>
    <!-- Each legend is announced by screen readers; the visible captions already say start or end. -->
    <fieldset class="field-group">
      <legend class="visually-hidden">Start date and time</legend>
      <div class="split">
        <label>
          Start date
          <input v-model="form.startDate" type="date" required />
        </label>
        <label>
          Start time
          <input v-model="form.startTime" type="time" required />
        </label>
      </div>
      <span v-if="startsAtError" class="field-error">{{ startsAtError }}</span>
    </fieldset>
    <fieldset class="field-group">
      <legend class="visually-hidden">End date and time</legend>
      <div class="split">
        <label>
          End date
          <input v-model="form.endDate" type="date" required />
        </label>
        <label>
          End time
          <input v-model="form.endTime" type="time" required />
        </label>
      </div>
      <span v-if="endsAtError" class="field-error">{{ endsAtError }}</span>
    </fieldset>
    <label>
      Max capacity
      <input v-model.number="form.maxCapacity" type="number" min="1" step="1" required />
      <span v-if="maxCapacityError" class="field-error">{{ maxCapacityError }}</span>
    </label>
    <p v-if="formError" class="error">{{ formError }}</p>
    <div class="actions">
      <button type="submit" :disabled="submitting">
        {{ submitting ? submittingLabel : submitLabel }}
      </button>
      <slot name="actions" />
    </div>
  </form>
</template>
