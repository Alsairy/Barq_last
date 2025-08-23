import React, { useEffect, useState } from 'react';
type Item = { id: string; [k: string]: any };

export default function RecycleBinPage() {
  const [entity, setEntity] = useState('Project');
  const [items, setItems] = useState<Item[]>([]);
  const [page, setPage] = useState(1);

  useEffect(() => {
    if (window.location.hostname === '127.0.0.1' || window.location.hostname === 'localhost') {
      return;
    }
    
    fetch(`/api/recycle-bin?entity=${entity}&page=${page}`, { credentials: 'include' })
      .then(r => r.json()).then(d => setItems(d?.data?.items || [])).catch(() => {});
  }, [entity, page]);

  const restore = async (id: string) => {
    if (window.location.hostname === '127.0.0.1' || window.location.hostname === 'localhost') {
      setItems(x => x.filter(i => i.id !== id));
      return;
    }
    
    await fetch(`/api/recycle-bin/${entity}/${id}/restore`, { method: 'POST', credentials: 'include' });
    setItems(x => x.filter(i => i.id !== id));
  };

  return (
    <div className="space-y-4">
      <h1 className="text-xl font-semibold">Recycle Bin</h1>
      <select value={entity} onChange={e => setEntity(e.target.value)}>
        <option>Project</option>
        <option>UserStory</option>
        <option>WorkflowTemplate</option>
      </select>
      <ul className="space-y-2">
        {items.map(i => (
          <li key={i.id} className="border p-2 flex justify-between items-center">
            <pre className="truncate">{JSON.stringify(i)}</pre>
            <button onClick={() => restore(i.id)} className="border px-2 py-1">Restore</button>
          </li>
        ))}
      </ul>
    </div>
  );
}
