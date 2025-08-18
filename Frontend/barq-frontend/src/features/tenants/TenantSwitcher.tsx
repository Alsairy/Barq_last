import React, { useEffect, useState } from 'react';

type Tenant = { id: string; name: string };
export default function TenantSwitcher() {
  const [tenants, setTenants] = useState<Tenant[]>([]);
  const [current, setCurrent] = useState('');

  useEffect(() => {
    fetch('/api/tenants', { credentials: 'include' })
      .then(r => r.json()).then(d => setTenants(d?.data?.items || [])).catch(()=>{});
  }, []);

  const select = (id: string) => {
    setCurrent(id);
    // set the header for subsequent requests (X-Tenant-Id)
    localStorage.setItem('x-tenant-id', id);
  };

  return (
    <select value={current} onChange={e => select(e.target.value)}>
      <option value="">Select tenant</option>
      {tenants.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}
    </select>
  );
}
