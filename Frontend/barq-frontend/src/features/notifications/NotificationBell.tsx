import React, { useEffect, useState } from 'react';

export default function NotificationBell() {
  const [unread, setUnread] = useState(0);
  useEffect(() => {
    const interval = setInterval(() => {
      fetch('/api/notifications?unreadOnly=true', { credentials: 'include' })
        .then(r => r.json()).then(d => setUnread(d?.data?.length || 0)).catch(() => {});
    }, 5000);
    return () => clearInterval(interval);
  }, []);
  return (
    <button aria-label="Notifications" className="relative">
      <span>🔔</span>
      {unread > 0 && <span className="absolute -top-1 -right-1 text-xs bg-red-600 text-white rounded-full px-1">{unread}</span>}
    </button>
  );
}
