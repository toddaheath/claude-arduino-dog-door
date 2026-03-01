import { useState } from 'react';
import { Link } from 'react-router-dom';
import { getCollars, registerCollar, deleteCollar } from '../api/collars';
import { getAnimals } from '../api/client';
import { useApi } from '../hooks/useApi';
import { SkeletonCard } from '../components/Skeleton';
import EmptyState from '../components/EmptyState';
import { useToast } from '../contexts/ToastContext';
import CollarProvision from '../components/CollarProvision';
import CollarOverviewMap from '../components/CollarOverviewMap';
import type { CollarPairingResult } from '../types';

export default function CollarList() {
  const { data: collars, loading, error, reload } = useApi(getCollars);
  const { data: animals } = useApi(getAnimals);
  const { addToast } = useToast();
  const [showForm, setShowForm] = useState(false);
  const [name, setName] = useState('');
  const [animalId, setAnimalId] = useState<string>('');
  const [pairingResult, setPairingResult] = useState<CollarPairingResult | null>(null);
  const [showProvision, setShowProvision] = useState(false);

  const handleRegister = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!name.trim()) return;
    try {
      const result = await registerCollar({
        name: name.trim(),
        animalId: animalId ? Number(animalId) : undefined,
      });
      setPairingResult(result);
      setName('');
      setAnimalId('');
      setShowForm(false);
      reload();
      addToast(`${name.trim()} registered`, 'success');
    } catch (err) {
      addToast(err instanceof Error ? err.message : 'Failed to register collar', 'error');
    }
  };

  const handleDelete = async (id: number, collarName: string) => {
    if (!confirm(`Delete ${collarName}?`)) return;
    try {
      await deleteCollar(id);
      reload();
      addToast(`${collarName} deleted`, 'success');
    } catch (err) {
      addToast(err instanceof Error ? err.message : 'Failed to delete collar', 'error');
    }
  };

  const formatBattery = (pct: number | null) =>
    pct == null ? '--' : `${Math.round(pct)}%`;

  const batteryColor = (pct: number | null) => {
    if (pct == null) return 'var(--text-muted)';
    if (pct > 50) return 'var(--success)';
    if (pct > 15) return 'var(--warning)';
    return 'var(--danger)';
  };

  return (
    <div>
      <div className="page-header">
        <h2 className="page-title">Collar Devices</h2>
        <button className="btn btn-primary" onClick={() => setShowForm(true)}>
          + Register Collar
        </button>
      </div>

      {pairingResult && !showProvision && (
        <div className="card" style={{ marginBottom: '1rem', border: '2px solid var(--success)' }}>
          <h3>Pairing Credentials</h3>
          <p>Flash these values to your collar firmware. <strong>The shared secret is shown only once.</strong></p>
          <div style={{ fontFamily: 'monospace', fontSize: '0.85rem', background: 'var(--bg-muted)', padding: '0.75rem', borderRadius: 4 }}>
            <div><strong>Collar ID:</strong> {pairingResult.collarId}</div>
            <div><strong>Shared Secret:</strong> {pairingResult.sharedSecret}</div>
          </div>
          <div style={{ display: 'flex', gap: '0.5rem', marginTop: '0.5rem' }}>
            <button className="btn btn-primary" onClick={() => setShowProvision(true)}>
              Setup via Bluetooth
            </button>
            <button className="btn" onClick={() => setPairingResult(null)}>
              Dismiss
            </button>
          </div>
        </div>
      )}

      {pairingResult && showProvision && (
        <CollarProvision
          collarId={pairingResult.collarId}
          sharedSecret={pairingResult.sharedSecret}
          onComplete={() => {
            setShowProvision(false);
            setPairingResult(null);
          }}
        />
      )}

      {showForm && (
        <div className="card" style={{ marginBottom: '1rem' }}>
          <form onSubmit={handleRegister}>
            <div className="form-group">
              <label>Collar Name</label>
              <input
                type="text"
                value={name}
                onChange={e => setName(e.target.value)}
                placeholder="e.g. Buddy's Collar"
                required
              />
            </div>
            <div className="form-group">
              <label>Assign to Animal (optional)</label>
              <select value={animalId} onChange={e => setAnimalId(e.target.value)}>
                <option value="">None</option>
                {animals?.map(a => (
                  <option key={a.id} value={a.id}>{a.name}</option>
                ))}
              </select>
            </div>
            <div style={{ display: 'flex', gap: '0.5rem' }}>
              <button type="submit" className="btn btn-primary">Register</button>
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

      {!loading && collars?.length === 0 && !showForm && (
        <EmptyState
          title="No collars registered"
          message="Register a collar device to enable GPS tracking, NFC identification, and virtual geofencing."
          action={<button className="btn btn-primary" onClick={() => setShowForm(true)}>Register Collar</button>}
        />
      )}

      {collars && collars.length > 0 && (
        <>
          {/* Overview Map */}
          {collars.some(c => c.lastLatitude != null) && (
            <div className="card" style={{ marginBottom: '1rem' }}>
              <h3 style={{ margin: '0 0 0.5rem' }}>All Collar Locations</h3>
              <CollarOverviewMap collars={collars} />
            </div>
          )}

          <div className="grid grid-cols-3">
            {collars.map(collar => (
              <div key={collar.id} className="card">
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                  <div>
                    <Link to={`/collars/${collar.id}`} style={{ textDecoration: 'none' }}>
                      <h3 style={{ margin: 0 }}>{collar.name}</h3>
                    </Link>
                    {collar.animalName && (
                      <span style={{ color: 'var(--text-muted)', fontSize: '0.85rem' }}>
                        {collar.animalName}
                      </span>
                    )}
                  </div>
                  <span style={{
                    padding: '2px 8px',
                    borderRadius: 12,
                    fontSize: '0.75rem',
                    background: collar.isActive ? 'var(--success-bg)' : 'var(--bg-muted)',
                    color: collar.isActive ? 'var(--success)' : 'var(--text-muted)',
                  }}>
                    {collar.isActive ? 'Active' : 'Inactive'}
                  </span>
                </div>

                <div style={{ marginTop: '0.75rem', fontSize: '0.85rem', color: 'var(--text-muted)' }}>
                  <div>
                    Battery: <span style={{ color: batteryColor(collar.batteryPercent) }}>
                      {formatBattery(collar.batteryPercent)}
                    </span>
                  </div>
                  <div>Last seen: {collar.lastSeenAt ? new Date(collar.lastSeenAt).toLocaleString() : 'Never'}</div>
                  {collar.firmwareVersion && <div>Firmware: v{collar.firmwareVersion}</div>}
                </div>

                <div style={{ marginTop: '0.75rem', display: 'flex', gap: '0.5rem' }}>
                  <Link to={`/collars/${collar.id}`} className="btn btn-sm">Details</Link>
                  <button
                    className="btn btn-sm btn-danger"
                    onClick={() => handleDelete(collar.id, collar.name)}
                  >
                    Delete
                  </button>
                </div>
              </div>
            ))}
          </div>
        </>
      )}
    </div>
  );
}
