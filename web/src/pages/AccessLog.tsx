import { useState } from 'react';
import { getAccessLogs } from '../api/client';
import { useApi } from '../hooks/useApi';
import { SkeletonRow } from '../components/Skeleton';
import EmptyState from '../components/EmptyState';

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
    <span className={`badge ${isEntering ? 'badge--info' : 'badge--unknown'}`}>
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
      <div className="page-header">
        <h2 className="page-title">Access Log</h2>
      </div>

      <div className="stat-bar">
        <div className="stat-chip" style={{ background: '#1a237e', color: '#90caf9' }}>
          Entries: {enteringCount}
        </div>
        <div className="stat-chip" style={{ background: '#bf360c', color: '#ffcc80' }}>
          Exits: {exitingCount}
        </div>
      </div>

      <div style={{ display: 'flex', gap: 12, marginBottom: 16, flexWrap: 'wrap' }}>
        <select
          value={filter}
          onChange={e => { setFilter(e.target.value); setPage(1); }}
          className="form-input"
          style={{ width: 'auto' }}
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
          className="form-input"
          style={{ width: 'auto' }}
        >
          <option value="">All Directions</option>
          <option value="Entering">Entering</option>
          <option value="Exiting">Exiting</option>
        </select>
      </div>

      {error && <div className="alert alert--error">{error}</div>}

      <table className="table">
        <thead>
          <tr>
            <th>Time</th>
            <th>Event</th>
            <th>Direction</th>
            <th>Animal</th>
            <th>Confidence</th>
            <th>Notes</th>
          </tr>
        </thead>
        <tbody>
          {loading ? (
            [...Array(10)].map((_, i) => <SkeletonRow key={i} />)
          ) : logs && logs.length === 0 ? (
            <tr>
              <td colSpan={6}>
                <EmptyState icon="📋" title="No events yet" message="Door events will appear here once your system is active." />
              </td>
            </tr>
          ) : (
            logs?.map(log => (
              <tr key={log.id}>
                <td style={{ fontSize: 13 }}>
                  {new Date(log.timestamp).toLocaleString()}
                </td>
                <td>
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
                <td>{directionBadge(log.direction)}</td>
                <td>{log.animalName || '-'}</td>
                <td>
                  {log.confidenceScore != null ? `${(log.confidenceScore * 100).toFixed(0)}%` : '-'}
                </td>
                <td style={{ color: 'var(--color-muted)', fontSize: 13 }}>{log.notes || '-'}</td>
              </tr>
            ))
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
