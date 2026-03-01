import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { getCollar, updateCollar, deleteCollar, getCurrentLocation, getLocationHistory, getActivitySummary, getGeofences, getFirmwareReleases, uploadFirmware } from '../api/collars';
import { getAnimals } from '../api/client';
import { useApi } from '../hooks/useApi';
import { useToast } from '../contexts/ToastContext';
import CollarMap from '../components/CollarMap';

export default function CollarDetail() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { addToast } = useToast();
  const collarId = Number(id);

  const { data: collar, loading, error, reload } = useApi(() => getCollar(collarId), [collarId]);
  const { data: animals } = useApi(getAnimals);
  const { data: currentLoc } = useApi(() => getCurrentLocation(collarId).catch(() => null), [collarId]);
  const { data: history } = useApi(() => getLocationHistory(collarId), [collarId]);
  const { data: geofences } = useApi(getGeofences);
  const { data: activity } = useApi(() => getActivitySummary(collarId).catch(() => null), [collarId]);
  const { data: firmwareReleases, reload: reloadFirmware } = useApi(getFirmwareReleases);

  const [editing, setEditing] = useState(false);
  const [editName, setEditName] = useState('');
  const [editAnimalId, setEditAnimalId] = useState<string>('');
  const [showFwUpload, setShowFwUpload] = useState(false);
  const [fwVersion, setFwVersion] = useState('');
  const [fwNotes, setFwNotes] = useState('');
  const [fwFile, setFwFile] = useState<File | null>(null);

  const startEdit = () => {
    if (!collar) return;
    setEditName(collar.name);
    setEditAnimalId(collar.animalId?.toString() || '');
    setEditing(true);
  };

  const handleSave = async () => {
    try {
      await updateCollar(collarId, {
        name: editName.trim() || undefined,
        animalId: editAnimalId ? Number(editAnimalId) : undefined,
      });
      setEditing(false);
      reload();
      addToast('Collar updated', 'success');
    } catch (err) {
      addToast(err instanceof Error ? err.message : 'Failed to update', 'error');
    }
  };

  const handleFwUpload = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!fwVersion.trim() || !fwFile) return;
    try {
      await uploadFirmware(fwVersion.trim(), fwNotes.trim(), fwFile);
      addToast(`Firmware v${fwVersion.trim()} uploaded`, 'success');
      setShowFwUpload(false);
      setFwVersion('');
      setFwNotes('');
      setFwFile(null);
      reloadFirmware();
    } catch (err) {
      addToast(err instanceof Error ? err.message : 'Failed to upload firmware', 'error');
    }
  };

  const handleDelete = async () => {
    if (!collar || !confirm(`Delete ${collar.name}?`)) return;
    try {
      await deleteCollar(collarId);
      addToast(`${collar.name} deleted`, 'success');
      navigate('/collars');
    } catch (err) {
      addToast(err instanceof Error ? err.message : 'Failed to delete', 'error');
    }
  };

  const batteryColor = (pct: number | null) => {
    if (pct == null) return 'var(--text-muted)';
    if (pct > 50) return 'var(--success)';
    if (pct > 15) return 'var(--warning)';
    return 'var(--danger)';
  };

  if (loading) return <div className="skeleton" style={{ height: 300 }} />;
  if (error) return <div className="alert alert-error">{error}</div>;
  if (!collar) return <div className="alert alert-error">Collar not found</div>;

  return (
    <div>
      <div className="page-header">
        <h2 className="page-title">{collar.name}</h2>
        <div style={{ display: 'flex', gap: '0.5rem' }}>
          <button className="btn" onClick={startEdit}>Edit</button>
          <button className="btn btn-danger" onClick={handleDelete}>Delete</button>
        </div>
      </div>

      {editing && (
        <div className="card" style={{ marginBottom: '1rem' }}>
          <div className="form-group">
            <label>Name</label>
            <input value={editName} onChange={e => setEditName(e.target.value)} />
          </div>
          <div className="form-group">
            <label>Animal</label>
            <select value={editAnimalId} onChange={e => setEditAnimalId(e.target.value)}>
              <option value="">None</option>
              {animals?.map(a => <option key={a.id} value={a.id}>{a.name}</option>)}
            </select>
          </div>
          <div style={{ display: 'flex', gap: '0.5rem' }}>
            <button className="btn btn-primary" onClick={handleSave}>Save</button>
            <button className="btn" onClick={() => setEditing(false)}>Cancel</button>
          </div>
        </div>
      )}

      {/* Map */}
      {(currentLoc || (history && history.length > 0)) && (
        <div className="card" style={{ marginBottom: '1rem' }}>
          <h3>Location Map</h3>
          <CollarMap currentLocation={currentLoc} history={history ?? []} geofences={geofences ?? []} />
        </div>
      )}

      <div className="grid grid-cols-2" style={{ marginBottom: '1rem' }}>
        {/* Status Card */}
        <div className="card">
          <h3>Status</h3>
          <table style={{ width: '100%' }}>
            <tbody>
              <tr>
                <td style={{ color: 'var(--text-muted)' }}>Collar ID</td>
                <td style={{ fontFamily: 'monospace' }}>{collar.collarId}</td>
              </tr>
              <tr>
                <td style={{ color: 'var(--text-muted)' }}>Animal</td>
                <td>{collar.animalName || 'Not assigned'}</td>
              </tr>
              <tr>
                <td style={{ color: 'var(--text-muted)' }}>Battery</td>
                <td style={{ color: batteryColor(collar.batteryPercent) }}>
                  {collar.batteryPercent != null ? `${Math.round(collar.batteryPercent)}%` : '--'}
                  {collar.batteryVoltage != null && ` (${collar.batteryVoltage.toFixed(2)}V)`}
                </td>
              </tr>
              <tr>
                <td style={{ color: 'var(--text-muted)' }}>Firmware</td>
                <td>{collar.firmwareVersion || '--'}</td>
              </tr>
              <tr>
                <td style={{ color: 'var(--text-muted)' }}>Last Seen</td>
                <td>{collar.lastSeenAt ? new Date(collar.lastSeenAt).toLocaleString() : 'Never'}</td>
              </tr>
              <tr>
                <td style={{ color: 'var(--text-muted)' }}>Status</td>
                <td>{collar.isActive ? 'Active' : 'Inactive'}</td>
              </tr>
            </tbody>
          </table>
        </div>

        {/* Current Location Card */}
        <div className="card">
          <h3>Current Location</h3>
          {currentLoc ? (
            <div style={{ fontSize: '0.85rem' }}>
              <div>Lat: {currentLoc.latitude.toFixed(6)}</div>
              <div>Lng: {currentLoc.longitude.toFixed(6)}</div>
              {currentLoc.accuracy && <div>Accuracy: {currentLoc.accuracy.toFixed(1)}m</div>}
              {currentLoc.speed != null && <div>Speed: {currentLoc.speed.toFixed(1)} m/s</div>}
              {currentLoc.activityState && (
                <div>Activity: <span style={{
                  textTransform: 'capitalize',
                  color: currentLoc.activityState === 'running' ? 'var(--success)' :
                         currentLoc.activityState === 'walking' ? 'var(--warning)' : 'var(--text-muted)',
                }}>{currentLoc.activityState}</span></div>
              )}
              <div>Updated: {new Date(currentLoc.timestamp).toLocaleString()}</div>
            </div>
          ) : (
            <div style={{ color: 'var(--text-muted)' }}>No location data available</div>
          )}
        </div>
      </div>

      {/* Activity Summary */}
      <div className="card" style={{ marginBottom: '1rem' }}>
        <h3>Activity (Last 24h)</h3>
        {activity ? (
          <div className="grid grid-cols-2" style={{ gap: '1rem' }}>
            <div>
              <div style={{ color: 'var(--text-muted)', fontSize: '0.75rem' }}>Distance</div>
              <div style={{ fontSize: '1.25rem', fontWeight: 600 }}>
                {activity.totalDistanceMeters >= 1000
                  ? `${(activity.totalDistanceMeters / 1000).toFixed(1)} km`
                  : `${Math.round(activity.totalDistanceMeters)} m`}
              </div>
            </div>
            <div>
              <div style={{ color: 'var(--text-muted)', fontSize: '0.75rem' }}>Active Time</div>
              <div style={{ fontSize: '1.25rem', fontWeight: 600 }}>
                {activity.activeMinutes >= 60
                  ? `${Math.floor(activity.activeMinutes / 60)}h ${activity.activeMinutes % 60}m`
                  : `${activity.activeMinutes} min`}
              </div>
            </div>
            <div>
              <div style={{ color: 'var(--text-muted)', fontSize: '0.75rem' }}>Max Speed</div>
              <div style={{ fontSize: '1.25rem', fontWeight: 600 }}>{activity.maxSpeedMps.toFixed(1)} m/s</div>
            </div>
            <div>
              <div style={{ color: 'var(--text-muted)', fontSize: '0.75rem' }}>GPS Points</div>
              <div style={{ fontSize: '1.25rem', fontWeight: 600 }}>{activity.locationPointCount}</div>
            </div>
          </div>
        ) : (
          <div style={{ color: 'var(--text-muted)' }}>No activity data available</div>
        )}
      </div>

      {/* Location History */}
      <div className="card">
        <h3>Location History (Last 24h)</h3>
        {history && history.length > 0 ? (
          <div style={{ maxHeight: 300, overflow: 'auto' }}>
            <table style={{ width: '100%', fontSize: '0.8rem' }}>
              <thead>
                <tr>
                  <th>Time</th>
                  <th>Lat</th>
                  <th>Lng</th>
                  <th>Accuracy</th>
                  <th>Speed</th>
                  <th>Satellites</th>
                </tr>
              </thead>
              <tbody>
                {history.map((pt, i) => (
                  <tr key={i}>
                    <td>{new Date(pt.timestamp).toLocaleTimeString()}</td>
                    <td>{pt.latitude.toFixed(6)}</td>
                    <td>{pt.longitude.toFixed(6)}</td>
                    <td>{pt.accuracy?.toFixed(1) ?? '--'}m</td>
                    <td>{pt.speed?.toFixed(1) ?? '--'} m/s</td>
                    <td>{pt.satellites ?? '--'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <div style={{ color: 'var(--text-muted)' }}>No location history available</div>
        )}
      </div>

      {/* Firmware Management */}
      <div className="card" style={{ marginTop: '1rem' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <h3 style={{ margin: 0 }}>Firmware</h3>
          <button className="btn btn-sm" onClick={() => setShowFwUpload(!showFwUpload)}>
            Upload New Version
          </button>
        </div>

        {showFwUpload && (
          <form onSubmit={handleFwUpload} style={{ marginTop: '0.75rem', padding: '0.75rem', background: 'var(--bg-muted)', borderRadius: 8 }}>
            <div className="grid grid-cols-2" style={{ gap: '0.5rem' }}>
              <div className="form-group" style={{ margin: 0 }}>
                <label>Version</label>
                <input
                  type="text"
                  value={fwVersion}
                  onChange={e => setFwVersion(e.target.value)}
                  placeholder="e.g. 1.1.0"
                  required
                />
              </div>
              <div className="form-group" style={{ margin: 0 }}>
                <label>Binary (.bin)</label>
                <input
                  type="file"
                  accept=".bin"
                  onChange={e => setFwFile(e.target.files?.[0] ?? null)}
                  required
                />
              </div>
            </div>
            <div className="form-group" style={{ margin: '0.5rem 0 0' }}>
              <label>Release Notes</label>
              <input
                type="text"
                value={fwNotes}
                onChange={e => setFwNotes(e.target.value)}
                placeholder="e.g. Bug fixes and battery improvements"
              />
            </div>
            <div style={{ display: 'flex', gap: '0.5rem', marginTop: '0.5rem' }}>
              <button type="submit" className="btn btn-primary btn-sm">Upload</button>
              <button type="button" className="btn btn-sm" onClick={() => setShowFwUpload(false)}>Cancel</button>
            </div>
          </form>
        )}

        {firmwareReleases && firmwareReleases.length > 0 ? (
          <div style={{ maxHeight: 200, overflow: 'auto', marginTop: '0.75rem' }}>
            <table style={{ width: '100%', fontSize: '0.8rem' }}>
              <thead>
                <tr>
                  <th>Version</th>
                  <th>Size</th>
                  <th>SHA-256</th>
                  <th>Notes</th>
                  <th>Released</th>
                </tr>
              </thead>
              <tbody>
                {firmwareReleases.map(fw => (
                  <tr key={fw.id} style={{
                    background: collar.firmwareVersion === fw.version ? 'var(--success-bg, rgba(76,175,80,0.1))' : undefined
                  }}>
                    <td style={{ fontFamily: 'monospace' }}>
                      {fw.version}
                      {collar.firmwareVersion === fw.version && (
                        <span style={{ marginLeft: 4, fontSize: '0.7rem', color: 'var(--success)' }}>current</span>
                      )}
                    </td>
                    <td>{(fw.fileSize / 1024).toFixed(0)} KB</td>
                    <td style={{ fontFamily: 'monospace', fontSize: '0.7rem' }}>{fw.sha256Hash?.slice(0, 12)}...</td>
                    <td>{fw.releaseNotes || '--'}</td>
                    <td>{new Date(fw.createdAt).toLocaleDateString()}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <div style={{ color: 'var(--text-muted)', marginTop: '0.5rem', fontSize: '0.85rem' }}>
            No firmware releases yet. Upload a .bin to enable OTA updates.
          </div>
        )}
      </div>
    </div>
  );
}
