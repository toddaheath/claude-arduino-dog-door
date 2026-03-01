import { useState } from 'react';
import { getGeofences, createGeofence, deleteGeofence, getGeofenceEvents } from '../api/collars';
import { useApi } from '../hooks/useApi';
import { SkeletonCard } from '../components/Skeleton';
import EmptyState from '../components/EmptyState';
import { useToast } from '../contexts/ToastContext';

const FENCE_TYPES = [
  { value: 'polygon', label: 'Polygon (Yard boundary)' },
  { value: 'circle', label: 'Circle (Exclusion zone)' },
  { value: 'corridor', label: 'Corridor (Path)' },
];

const RULES = [
  { value: 'allow', label: 'Allow (dog can be here)' },
  { value: 'deny', label: 'Deny (dog should not be here)' },
];

const BUZZER_PATTERNS = [
  { value: 0, label: 'Silent (monitoring only)' },
  { value: 1, label: 'Short beep (gentle)' },
  { value: 2, label: 'Long beep (urgent)' },
  { value: 3, label: 'Continuous (dangerous area)' },
];

export default function GeofenceList() {
  const { data: fences, loading, error, reload } = useApi(getGeofences);
  const { data: events } = useApi(() => getGeofenceEvents());
  const { addToast } = useToast();
  const [showForm, setShowForm] = useState(false);
  const [name, setName] = useState('');
  const [fenceType, setFenceType] = useState('polygon');
  const [rule, setRule] = useState('allow');
  const [buzzerPattern, setBuzzerPattern] = useState(1);

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!name.trim()) return;
    try {
      await createGeofence({
        name: name.trim(),
        fenceType,
        rule,
        boundaryJson: '{}',  // Placeholder — satellite map editor in Phase 4
        buzzerPattern,
      });
      setName('');
      setShowForm(false);
      reload();
      addToast(`${name.trim()} created`, 'success');
    } catch (err) {
      addToast(err instanceof Error ? err.message : 'Failed to create geofence', 'error');
    }
  };

  const handleDelete = async (id: number, fenceName: string) => {
    if (!confirm(`Delete ${fenceName}?`)) return;
    try {
      await deleteGeofence(id);
      reload();
      addToast(`${fenceName} deleted`, 'success');
    } catch (err) {
      addToast(err instanceof Error ? err.message : 'Failed to delete', 'error');
    }
  };

  const ruleColor = (r: string) => r === 'allow' ? 'var(--success)' : 'var(--danger)';

  return (
    <div>
      <div className="page-header">
        <h2 className="page-title">Virtual Geofences</h2>
        <button className="btn btn-primary" onClick={() => setShowForm(true)}>
          + Create Geofence
        </button>
      </div>

      {showForm && (
        <div className="card" style={{ marginBottom: '1rem' }}>
          <form onSubmit={handleCreate}>
            <div className="form-group">
              <label>Fence Name</label>
              <input
                type="text"
                value={name}
                onChange={e => setName(e.target.value)}
                placeholder="e.g. Front Yard, Pool Area"
                required
              />
            </div>
            <div className="grid grid-cols-3">
              <div className="form-group">
                <label>Type</label>
                <select value={fenceType} onChange={e => setFenceType(e.target.value)}>
                  {FENCE_TYPES.map(t => <option key={t.value} value={t.value}>{t.label}</option>)}
                </select>
              </div>
              <div className="form-group">
                <label>Rule</label>
                <select value={rule} onChange={e => setRule(e.target.value)}>
                  {RULES.map(r => <option key={r.value} value={r.value}>{r.label}</option>)}
                </select>
              </div>
              <div className="form-group">
                <label>Buzzer Pattern</label>
                <select value={buzzerPattern} onChange={e => setBuzzerPattern(Number(e.target.value))}>
                  {BUZZER_PATTERNS.map(b => <option key={b.value} value={b.value}>{b.label}</option>)}
                </select>
              </div>
            </div>
            <p style={{ color: 'var(--text-muted)', fontSize: '0.85rem', marginBottom: '0.75rem' }}>
              Boundary drawing on satellite map will be available in a future update. For now, boundaries can be configured via the API.
            </p>
            <div style={{ display: 'flex', gap: '0.5rem' }}>
              <button type="submit" className="btn btn-primary">Create</button>
              <button type="button" className="btn" onClick={() => setShowForm(false)}>Cancel</button>
            </div>
          </form>
        </div>
      )}

      {loading && (
        <div className="grid grid-cols-3">
          {[1, 2, 3].map(i => <SkeletonCard key={i} />)}
        </div>
      )}

      {error && <div className="alert alert-error">{error}</div>}

      {!loading && fences?.length === 0 && !showForm && (
        <EmptyState
          title="No geofences defined"
          message="Create virtual boundaries using satellite imagery to define where your dog is allowed or restricted."
          action={<button className="btn btn-primary" onClick={() => setShowForm(true)}>Create Geofence</button>}
        />
      )}

      {fences && fences.length > 0 && (
        <div className="grid grid-cols-3">
          {fences.map(fence => (
            <div key={fence.id} className="card">
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                <h3 style={{ margin: 0 }}>{fence.name}</h3>
                <span style={{
                  padding: '2px 8px',
                  borderRadius: 12,
                  fontSize: '0.75rem',
                  color: ruleColor(fence.rule),
                  border: `1px solid ${ruleColor(fence.rule)}`,
                }}>
                  {fence.rule}
                </span>
              </div>

              <div style={{ marginTop: '0.5rem', fontSize: '0.85rem', color: 'var(--text-muted)' }}>
                <div>Type: {fence.fenceType}</div>
                <div>Buzzer: {BUZZER_PATTERNS.find(b => b.value === fence.buzzerPattern)?.label || 'Unknown'}</div>
                <div>Version: {fence.version}</div>
                <div>Status: {fence.isActive ? 'Active' : 'Inactive'}</div>
              </div>

              <div style={{ marginTop: '0.75rem' }}>
                <button
                  className="btn btn-sm btn-danger"
                  onClick={() => handleDelete(fence.id, fence.name)}
                >
                  Delete
                </button>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Recent Geofence Events */}
      {events && events.length > 0 && (
        <div className="card" style={{ marginTop: '1rem' }}>
          <h3>Recent Geofence Events</h3>
          <div style={{ maxHeight: 300, overflow: 'auto' }}>
            <table style={{ width: '100%', fontSize: '0.85rem' }}>
              <thead>
                <tr>
                  <th>Time</th>
                  <th>Fence</th>
                  <th>Collar</th>
                  <th>Event</th>
                  <th>Location</th>
                </tr>
              </thead>
              <tbody>
                {events.map(ev => (
                  <tr key={ev.id}>
                    <td>{new Date(ev.timestamp).toLocaleString()}</td>
                    <td>{ev.geofenceName}</td>
                    <td>{ev.collarName}</td>
                    <td style={{
                      color: ev.eventType === 'breach' ? 'var(--danger)' :
                             ev.eventType === 'entered' ? 'var(--success)' : 'var(--text-muted)'
                    }}>
                      {ev.eventType}
                    </td>
                    <td style={{ fontFamily: 'monospace', fontSize: '0.75rem' }}>
                      {ev.latitude.toFixed(4)}, {ev.longitude.toFixed(4)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}
