// API URL boleh ditukar ketika run dengan --dart-define.
// Contoh:
// --dart-define=API_BASE_URL=https://nama-api-5000.app.github.dev
const apiBaseUrl = String.fromEnvironment(
  'API_BASE_URL',
  defaultValue: 'http://localhost:5000',
);
