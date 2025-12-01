import { defineConfig, globalIgnores } from "eslint/config";

export default defineConfig([
  globalIgnores(["wwwroot/lib/", "obj/", "bin/"]),

  {
    languageOptions: {
      ecmaVersion: "latest",
      sourceType: "module",
      globals: {
        window: "readonly",
        document: "readonly",
        console: "readonly",
      },
    },
    rules: {},
  },
]);
