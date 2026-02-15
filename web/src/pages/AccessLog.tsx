import { useState } from 'react';
import { getAccessLogs } from '../api/client';
import { useApi } from '../hooks/useApi';

const eventTypeColors: Record<string, string> = {
  AccessGranted: '#2e7d32',
  AccessDenied: '#c62828',
  UnknownAnimal: '#f57c00',
  DoorOpened: '#1565c0',
  DoorClosed: '#1565c0',
  ManualOverride: '#7b1fa2',
  ExitGranted: '#2e7d32',
  ExitDenied: '#c62828',
  EntryGranted: '#2e7d32',
  EntryDenied: '#c62828',
};

const directionBadge = (direction: string | null) => {
  if (!direction) return null;
  const isEntering = direction === 'Entering';
  return (
    <span style={{
      padding: '2px 8px',
      borderRadius: 12,
      fontSize: 12,
      fontWeight: 600,
      background: isEntering ? '#1565c0' : '#e65100',
      color: '#fff',
    }}>
      {isEntering ? 'IN' : 'OUT'}
    </span>
  );
};

export default function AccessLog() {
  const [page, setPage] = useState(1);
  const [filter, setFilter] = useState('');
  const [directionFilter, setDirectionFilter] = useState('');

  const { data: logs, loading, error } = useApi(
    () => getAccessLogs(page, 20, filter || undefined, directionFilter || undefined),
    [page, filter, directionFilter],
  );

  const enteringCount = logs?.filter(l => l.direction === 'Entering').length ?? 0;
  const exitingCount = logs?.filter(l => l.direction === 'Exiting').length ?? 0;

  return (
    <div>
      <h2>Access Log</h2>

      <div style={{ display: 'flex', gap: 16, marginBottom: 16, flexWrap: 'wrap', alignItems: 'center' }}>
        <div style={{
          padding: '6px 14px',
          borderRadius: 8,
          background: '#1a237e',
          color: '#90caf9',
          fontSize: 13,
          fontWeight: 600,
        }}>
          Entries: {enteringCount}
        </div>
        <div style={{
          padding: '6px 14px',
          borderRadius: 8,
          background: '#bf360c',
          color: '#ffcc80',
          fontSize: 13,
          fontWeight: 600,
        }}>
          Exits: {exitingCount}
        </div>
      </div>

      <div style={{ display: 'flex', gap: 12, marginBottom: 16, flexWrap: 'wrap' }}>
        <select
          value={filter}
          onChange={e => { setFilter(e.target.value); setPage(1); }}
          style={{ padding: 8, borderRadius: 4, border: '1px solid #444', background: '#1a1a2e', color: '#fff' }}
        >
          <option value="">All Events</option>
          <option value="AccessGranted">Access Granted</option>
          <option value="AccessDenied">Access Denied</option>
          <option value="ExitGranted">Exit Granted</option>
          <option value="ExitDenied">Exit Denied</option>
          <option value="EntryGranted">Entry Granted</option>
          <option value="EntryDenied">Entry Denied</option>
          <option value="UnknownAnimal">Unknown Animal</option>
          <option value="DoorOpened">Door Opened</option>
          <option value="DoorClosed">Door Closed</option>
          <option value="ManualOverride">Manual Override</option>
        </select>

        <select
          value={directionFilter}
          onChange={e => { setDirectionFilter(e.target.value); setPage(1); }}
          style={{ padding: 8, borderRadius: 4, border: '1px solid #444', background: '#1a1a2e', color: '#fff' }}
        >
          <option value="">All Directions</option>
          <option value="Entering">Entering</option>
          <option value="Exiting">Exiting</option>
        </select>
      </div>

      {loading && <p>Loading...</p>}
      {error && <p style={{ color: 'red' }}>Error: {error}</p>}

      <table style={{ width: '100%', borderCollapse: 'collapse' }}>
        <thead>
          <tr style={{ borderBottom: '2px solid #333', textAlign: 'left' }}>
            <th style={{ padding: 8 }}>Time</th>
            <th style={{ padding: 8 }}>Event</th>
            <th style={{ padding: 8 }}>Direction</th>
            <th style={{ padding: 8 }}>Animal</th>
            <th style={{ padding: 8 }}>Confidence</th>
            <th style={{ padding: 8 }}>Notes</th>
          </tr>
        </thead>
        <tbody>
          {logs?.map(log => (
            <tr key={log.id} style={{ borderBottom: '1px solid #222' }}>
              <td style={{ padding: 8, fontSize: 13 }}>
                {new Date(log.timestamp).toLocaleString()}
              </td>
              <td style={{ padding: 8 }}>
                <span style={{
                  padding: '2px 8px',
                  borderRadius: 12,
                  fontSize: 12,
                  background: eventTypeColors[log.eventType] || '#555',
                  color: '#fff',
                }}>
                  {log.eventType}
                </span>
              </td>
              <td style={{ padding: 8 }}>
                {directionBadge(log.direction)}
              </td>
              <td style={{ padding: 8 }}>{log.animalName || '-'}</td>
              <td style={{ padding: 8 }}>
                {log.confidenceScore != null ? `${(log.confidenceScore * 100).toFixed(0)}%` : '-'}
              </td>
              <td style={{ padding: 8, color: '#999', fontSize: 13 }}>{log.notes || '-'}</td>
            </tr>
          ))}
          {logs?.length === 0 && (
            <tr>
              <td colSpan={6} style={{ padding: 24, textAlign: 'center', color: '#666' }}>
                No events found.
              </td>
            </tr>
          )}
        </tbody>
      </table>

      <div style={{ display: 'flex', gap: 8, marginTop: 16, justifyContent: 'center' }}>
        <button disabled={page <= 1} onClick={() => setPage(p => p - 1)}>Previous</button>
        <span style={{ padding: '8px 16px' }}>Page {page}</span>
        <button disabled={!logs || logs.length < 20} onClick={() => setPage(p => p + 1)}>Next</button>
      </div>
    </div>
  );
}
