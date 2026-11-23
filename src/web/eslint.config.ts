import { globalIgnores } from 'eslint/config';
import { defineConfigWithVueTs, vueTsConfigs } from '@vue/eslint-config-typescript';
import pluginVue from 'eslint-plugin-vue';
import skipFormatting from '@vue/eslint-config-prettier/skip-formatting';

// ESLint handles code quality; Prettier handles formatting (see .prettierrc.json).
// skipFormatting turns off every ESLint rule that would conflict with Prettier.
export default defineConfigWithVueTs(
  {
    name: 'app/files-to-lint',
    files: ['**/*.{vue,ts,mts,tsx}'],
  },

  globalIgnores(['**/dist/**', '**/dist-ssr/**', '**/coverage/**', '**/bin/**', '**/obj/**']),

  pluginVue.configs['flat/recommended'],
  vueTsConfigs.recommended,
  skipFormatting,
);
