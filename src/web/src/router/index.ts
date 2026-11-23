import { createRouter, createWebHistory } from 'vue-router';
import EventsView from '../views/EventsView.vue';

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: '/', name: 'events', component: EventsView, meta: { wide: true } },
    {
      path: '/events/:id/registrations',
      name: 'event-registrations',
      component: () => import('../views/EventRegistrationsView.vue'),
      props: true,
    },
    {
      path: '/events/:id/edit',
      name: 'edit-event',
      component: () => import('../views/EditEventView.vue'),
      props: true,
    },
  ],
});

export default router;
