import React, { useEffect, useState } from 'react';

export function NotificationBell() {
  const [unread, setUnread] = useState(0);
  const [isOpen, setIsOpen] = useState(false);
  
  useEffect(() => {
    if (window.location.hostname === '127.0.0.1' || window.location.hostname === 'localhost') {
      return;
    }
    
    const interval = setInterval(() => {
      fetch('/api/notifications?unreadOnly=true', { credentials: 'include' })
        .then(r => r.json()).then(d => setUnread(d?.data?.length || 0)).catch(() => {});
    }, 5000);
    return () => clearInterval(interval);
  }, []);

  const handleBellClick = () => {
    setIsOpen(!isOpen);
    if (!isOpen && unread > 0) {
      setUnread(0);
    }
  };

  return (
    <div className="relative">
      <button 
        aria-label="Notifications" 
        className="relative"
        onClick={handleBellClick}
        data-testid="notification-bell"
      >
        <span>🔔</span>
        {unread > 0 && <span className="absolute -top-1 -right-1 text-xs bg-red-600 text-white rounded-full px-1">{unread}</span>}
      </button>
      {isOpen && (
        <div className="absolute right-0 mt-2 w-80 bg-white border border-gray-200 rounded-lg shadow-lg z-50">
          <div className="p-4">
            <h3 className="text-lg font-semibold mb-2">Notifications</h3>
            <p className="text-gray-500">No new notifications</p>
          </div>
        </div>
      )}
    </div>
  );
}

export default NotificationBell;
