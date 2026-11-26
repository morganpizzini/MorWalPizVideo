const STATE_CACHE = "workshop-worker-state";
const ACTIVE_VERSION_KEY = "/active-version";

async function readActiveCacheName() {
  const state = await caches.open(STATE_CACHE);
  const response = await state.match(ACTIVE_VERSION_KEY);
  if (!response) return null;
  try {
    const stateValue = await response.json();
    return typeof stateValue.cacheName === "string" ? stateValue.cacheName : null;
  } catch {
    return null;
  }
}

self.addEventListener("install", event => {
  event.waitUntil((async () => {
    const manifestResponse = await fetch("/asset-manifest.json", { cache: "reload" });
    if (!manifestResponse.ok) throw new Error(`Workshop asset manifest returned ${manifestResponse.status}.`);
    const manifest = await manifestResponse.clone().json();
    if (!manifest || typeof manifest.version !== "string" ||
        !Array.isArray(manifest.assets) || manifest.assets.length === 0) {
      throw new Error("Workshop asset manifest is invalid.");
    }

    const assets = manifest.assets.filter(path =>
      typeof path === "string" &&
      /^\/assets\/[^/]+\.(?:js|css)$/.test(path));
    if (assets.length !== manifest.assets.length) {
      throw new Error("Workshop asset manifest contains an invalid path.");
    }

    const shellResponse = await fetch("/", { cache: "reload" });
    if (!shellResponse.ok) throw new Error(`Workshop shell returned ${shellResponse.status}.`);
    const contentType = shellResponse.headers.get("content-type") || "";
    if (!contentType.includes("text/html")) {
      throw new Error("Workshop shell response was not HTML.");
    }

    const cacheName = `workshop-shell-${manifest.version}`;
    const cache = await caches.open(cacheName);
    await cache.put("/index.html", shellResponse.clone());
    await cache.put("/", shellResponse.clone());
    await cache.put("/asset-manifest.json", manifestResponse.clone());

    await Promise.all(assets.map(async path => {
      const assetResponse = await fetch(path, { cache: "reload" });
      if (!assetResponse.ok) {
        throw new Error(`Workshop asset ${path} returned ${assetResponse.status}.`);
      }
      const assetType = assetResponse.headers.get("content-type") || "";
      const isScript = path.endsWith(".js") &&
        (assetType.includes("javascript") || assetType.includes("ecmascript"));
      const isStyle = path.endsWith(".css") && assetType.includes("text/css");
      if (!isScript && !isStyle) {
        throw new Error(`Workshop asset ${path} returned invalid content type ${assetType}.`);
      }
      await cache.put(path, assetResponse.clone());
    }));

    const state = await caches.open(STATE_CACHE);
    await state.put(ACTIVE_VERSION_KEY, new Response(JSON.stringify({ cacheName }), {
      headers: { "content-type": "application/json" },
    }));
    self.skipWaiting();
  })());
});

self.addEventListener("activate", event => {
  event.waitUntil((async () => {
    const activeCacheName = await readActiveCacheName();
    if (activeCacheName) {
      const keys = await caches.keys();
      await Promise.all(keys
        .filter(key => key.startsWith("workshop-shell-") && key !== activeCacheName)
        .map(key => caches.delete(key)));
    }
    await self.clients.claim();
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
        const activeCacheName = await readActiveCacheName();
        if (activeCacheName) {
          const activeCache = await caches.open(activeCacheName);
          return (await activeCache.match("/index.html")) || Response.error();
        }
        return (await caches.match("/index.html")) || Response.error();
      }
    })());
    return;
  }

  event.respondWith((async () => {
    const activeCacheName = await readActiveCacheName();
    const activeCache = activeCacheName ? await caches.open(activeCacheName) : null;
    const cached = activeCache ? await activeCache.match(request) : await caches.match(request);
    if (cached) return cached;
    return fetch(request);
  })());
});
