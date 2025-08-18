import React, { useEffect, useState } from 'react';

export type Column<T> = { key: keyof T | string; header: string; render?: (row: T) => React.ReactNode };
export type FetchParams = { page: number; pageSize: number; search?: string; sort?: string; filters?: Record<string, any> };
export type Fetcher<T> = (params: FetchParams) => Promise<{ items: T[]; total: number }>;

export function DataTable<T>({ columns, fetcher, bulkActions }: { columns: Column<T>[]; fetcher: Fetcher<T>; bulkActions?: React.ReactNode }) {
  const [rows, setRows] = useState<T[]>([]);
  const [selected, setSelected] = useState<Set<number>>(new Set());
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [total, setTotal] = useState(0);

  useEffect(() => {
    fetcher({ page, pageSize }).then(r => { setRows(r.items); setTotal(r.total) });
  }, [page, pageSize]);

  return (
    <div className="space-y-2">
      <div className="flex items-center justify-between">{bulkActions}</div>
      <table className="min-w-full border">
        <thead>
          <tr>{columns.map(c => <th key={String(c.key)} className="text-left p-2 border-b">{c.header}</th>)}</tr>
        </thead>
        <tbody>
          {rows.map((row, i) => (
            <tr key={i} className="border-b">
              {columns.map(c => <td key={String(c.key)} className="p-2">{c.render ? c.render(row) : String((row as any)[c.key])}</td>)}
            </tr>
          ))}
        </tbody>
      </table>
      <div className="flex items-center gap-2">
        <button onClick={() => setPage(p => Math.max(1, p-1))}>Prev</button>
        <span>{page} / {Math.ceil(total / pageSize) || 1}</span>
        <button onClick={() => setPage(p => p+1)}>Next</button>
      </div>
    </div>
  );
}
