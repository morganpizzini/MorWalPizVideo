/* BackOffice-owned push worker. It is intentionally registered directly, not shared with public clients. */
self.addEventListener('push', event => {
  if (!event.data) return;
  let payload;
  try { payload = event.data.json(); } catch { payload = { title: 'BackOffice', body: event.data.text() }; }
  if (!payload?.title) return;
  const actions = Array.isArray(payload.actions) ? payload.actions.map(action => ({
    action: action.action,
    title: action.title,
  })) : [];
  event.waitUntil(self.registration.showNotification(payload.title, {
    body: payload.body || '',
    actions,
    data: { url: payload.url || '/', actions: payload.actions || [] },
  }));
});
self.addEventListener('notificationclick', event => {
  event.notification.close();
  const selected = event.notification.data?.actions?.find(action => action.action === event.action);
  const url = new URL(selected?.url || selected?.destination || event.notification.data?.url || '/', self.registration.scope);
  if (url.origin !== self.location.origin) return;
  event.waitUntil(self.clients.openWindow(url.href));
});
