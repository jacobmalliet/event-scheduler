import 'vue-router';

declare module 'vue-router' {
  interface RouteMeta {
    /** Use the wider page container (e.g. for side-by-side layouts). */
    wide?: boolean;
  }
}
