// Runs in the browser before each test file. Tests render components directly rather than through
// main.ts, so the global stylesheet is loaded here; without it pages (and failure screenshots) are unstyled.
import './src/assets/main.css';
