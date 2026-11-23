<script setup lang="ts">
import { RouterLink, RouterView, useRoute } from 'vue-router';

const route = useRoute();
</script>

<template>
  <header class="site-header">
    <RouterLink to="/" class="brand">Event Scheduler</RouterLink>
  </header>
  <!-- Routes with meta.wide (e.g. the side-by-side events page) get a wider container. -->
  <main class="container" :class="{ wide: route.meta.wide }">
    <RouterView />
  </main>
</template>
