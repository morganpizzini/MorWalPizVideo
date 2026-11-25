const SHELL_CACHE = "workshop-shell-v1";

self.addEventListener("install", () => {
  self.skipWaiting();
});

self.addEventListener("activate", event => {
  event.waitUntil((async () => {
    const keys = await caches.keys();
    await Promise.all(keys
      .filter(key => key.startsWith("workshop-shell-") && key !== SHELL_CACHE)
      .map(key => caches.delete(key)));
    await self.clients.claim();
  })());
});

self.addEventListener("install", event => {
  event.waitUntil((async () => {
    const cache = await caches.open(SHELL_CACHE);
    const response = await fetch("/", { cache: "reload" });
    if (!response.ok) throw new Error(`Workshop shell returned ${response.status}.`);
    await cache.put("/", response.clone());
    await cache.put("/index.html", response.clone());
    const html = await response.text();
    const paths = [...html.matchAll(/(?:src|href)=["']([^"']+\.(?:js|css))["']/g)]
      .map(match => match[1])
      .filter(path => path.startsWith("/") || path.startsWith(location.origin))
      .map(path => new URL(path, location.origin).href);
    await Promise.all(paths.map(async path => {
      try {
        const asset = await fetch(path, { cache: "reload" });
        if (asset.ok) await cache.put(path, asset);
      } catch (error) {
        console.warn("Workshop app asset was not precached.", path, error);
      }
    }));
  })());
});

self.addEventListener("fetch", event => {
  const request = event.request;
  const url = new URL(request.url);
  if (request.method !== "GET" || url.origin !== self.location.origin ||
      url.pathname === "/api" || url.pathname.startsWith("/api/")) return;

  if (request.mode === "navigate") {
    event.respondWith((async () => {
      try {
        return await fetch(request);
      } catch {
        return (await caches.match("/index.html")) || Response.error();
      }
    })());
    return;
  }

  event.respondWith((async () => {
    const cached = await caches.match(request);
    if (cached) return cached;
    const response = await fetch(request);
    if (response.ok && response.type === "basic") {
      const cache = await caches.open(SHELL_CACHE);
      await cache.put(request, response.clone());
    }
    return response;
  })());
});
