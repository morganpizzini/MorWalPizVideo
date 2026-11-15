// Workshop's service worker is intentionally lifecycle-only.
// It does not intercept requests or cache authenticated application data.
self.addEventListener("install", () => {
  void self.skipWaiting();
});

self.addEventListener("activate", event => {
  event.waitUntil(self.clients.claim());
});
