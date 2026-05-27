import { useState, useEffect } from 'react';
import { Button, Spinner } from 'react-bootstrap';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faBell, faBellSlash } from '@fortawesome/free-solid-svg-icons';

const VAPID_PUBLIC_KEY = import.meta.env.VITE_VAPID_PUBLIC_KEY ?? '';
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000';

function urlBase64ToUint8Array(base64String: string): Uint8Array {
  const padding = '='.repeat((4 - (base64String.length % 4)) % 4);
  const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
  const rawData = atob(base64);
  const output = new Uint8Array(rawData.length);
  for (let i = 0; i < rawData.length; ++i) {
    output[i] = rawData.charCodeAt(i);
  }
  return output;
}

export function PushSubscriptionButton() {
  const [subscribed, setSubscribed] = useState(false);
  const [loading, setLoading] = useState(false);
  const [supported, setSupported] = useState(false);

  useEffect(() => {
    const isSupported =
      'serviceWorker' in navigator &&
      'PushManager' in window &&
      Notification.permission !== 'denied' &&
      !!VAPID_PUBLIC_KEY;
    setSupported(isSupported);

    if (isSupported) {
      navigator.serviceWorker.ready.then((reg) =>
        reg.pushManager.getSubscription().then((sub) => setSubscribed(!!sub))
      );
    }
  }, []);

  if (!supported) return null;

  const handleToggle = async () => {
    setLoading(true);
    try {
      const reg = await navigator.serviceWorker.ready;
      if (subscribed) {
        const sub = await reg.pushManager.getSubscription();
        if (sub) {
          await fetch(`${API_BASE_URL}/api/push/unsubscribe`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ endpoint: sub.endpoint }),
          });
          await sub.unsubscribe();
        }
        setSubscribed(false);
      } else {
        const permission = await Notification.requestPermission();
        if (permission !== 'granted') return;

        const sub = await reg.pushManager.subscribe({
          userVisibleOnly: true,
          applicationServerKey: urlBase64ToUint8Array(VAPID_PUBLIC_KEY),
        });
        await fetch(`${API_BASE_URL}/api/push/subscribe`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(sub),
        });
        setSubscribed(true);
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <Button
      variant={subscribed ? 'outline-secondary' : 'outline-primary'}
      size="sm"
      onClick={handleToggle}
      disabled={loading}
      title={subscribed ? 'Disattiva notifiche' : 'Attiva notifiche'}
    >
      {loading ? (
        <Spinner animation="border" size="sm" />
      ) : (
        <FontAwesomeIcon icon={subscribed ? faBellSlash : faBell} />
      )}
      <span className="ms-2">{subscribed ? 'Notifiche attive' : 'Attiva notifiche'}</span>
    </Button>
  );
}
