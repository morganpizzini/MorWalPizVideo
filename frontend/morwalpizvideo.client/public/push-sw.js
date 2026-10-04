/* eslint-disable no-undef */
/**
 * Web Push handlers for MorWalPiz.
 *
 * Imported into the Workbox-generated service worker via `workbox.importScripts`, so precaching and offline
 * behaviour stay owned by vite-plugin-pwa and only the push surface lives here.
 *
 * Destinations are same-origin relative paths resolved against the registration scope; the body click falls back to
 * the notification-level destination when an action has none.
 */
(function () {
  'use strict';

  var DEFAULT_DESTINATION = '/';
  var ICON = '/android-chrome-192x192.png';
  var BADGE = '/android-chrome-192x192.png';

  function sameOriginUrl(destination) {
    try {
      var resolved = new URL(destination || DEFAULT_DESTINATION, self.registration.scope);
      return resolved.origin === self.location.origin ? resolved.href : self.registration.scope;
    } catch (error) {
      return self.registration.scope;
    }
  }

  function readPayload(event) {
    if (!event.data) return null;
    try {
      return event.data.json();
    } catch (error) {
      return { title: 'MorWalPiz', body: event.data.text() };
    }
  }

  self.addEventListener('push', function (event) {
    var payload = readPayload(event);
    if (!payload || !payload.title) return;

    // Browsers silently drop extra buttons; slicing keeps the visible set deterministic.
    var maxActions = typeof Notification !== 'undefined' && Notification.maxActions ? Notification.maxActions : 0;
    var actions = Array.isArray(payload.actions) ? payload.actions.slice(0, maxActions) : [];
    var destinations = {};
    actions.forEach(function (action) {
      destinations[action.action] = action.url || payload.url || DEFAULT_DESTINATION;
    });

    event.waitUntil(
      self.registration.showNotification(payload.title, {
        body: payload.body || '',
        icon: payload.icon || ICON,
        badge: payload.badge || BADGE,
        tag: payload.tag,
        renotify: Boolean(payload.tag),
        data: {
          url: payload.url || DEFAULT_DESTINATION,
          actionDestinations: destinations
        },
        actions: actions.map(function (action) {
          return { action: action.action, title: action.title };
        })
      })
    );
  });

  self.addEventListener('notificationclick', function (event) {
    event.notification.close();
    var data = event.notification.data || {};
    var destinations = data.actionDestinations || {};
    var target = sameOriginUrl(
      event.action && destinations[event.action] ? destinations[event.action] : data.url
    );

    event.waitUntil(
      self.clients.matchAll({ type: 'window', includeUncontrolled: true }).then(function (clientList) {
        for (var index = 0; index < clientList.length; index += 1) {
          var client = clientList[index];
          if (client.url === target && 'focus' in client) return client.focus();
        }
        if (self.clients.openWindow) return self.clients.openWindow(target);
        return undefined;
      })
    );
  });
})();
