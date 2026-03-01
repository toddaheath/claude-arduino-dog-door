import { Link } from 'react-router-dom';
import { getDoorStatus, getAccessLogs, getAnimals } from '../api/client';
import { getCollars, getGeofences } from '../api/collars';
import { useApi } from '../hooks/useApi';

export default function Dashboard() {
  const { data: config } = useApi(getDoorStatus);
  const { data: logs } = useApi(() => getAccessLogs(1, 5));
  const { data: animals } = useApi(getAnimals);
  const { data: collars } = useApi(getCollars);
  const { data: geofences } = useApi(getGeofences);

  const allowedCount = animals?.filter(a => a.isAllowed).length ?? 0;
  const deniedCount = animals?.filter(a => !a.isAllowed).length ?? 0;
  const activeCollars = collars?.filter(c => c.isActive).length ?? 0;
  const lowBatteryCollars = collars?.filter(c => c.batteryPercent != null && c.batteryPercent <= 15).length ?? 0;

  return (
    <div>
      <div className="page-header">
        <h2 className="page-title">Dashboard</h2>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(320px, 1fr))', gap: 24 }}>
        {/* Door Status Widget */}
        <div className="card">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <h3 style={{ margin: 0, fontSize: 16, fontWeight: 600 }}>Door Status</h3>
            <Link to="/settings" style={{ fontSize: 12, color: 'var(--color-accent)', textDecoration: 'none' }}>Settings →</Link>
          </div>
          {config ? (
            <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8, marginTop: 4 }}>
              <span className={`badge ${config.isEnabled ? 'badge--allowed' : 'badge--denied'}`}>
                {config.isEnabled ? 'Enabled' : 'Disabled'}
              </span>
              {config.autoCloseEnabled && (
                <span className="badge badge--info">Auto Close {config.autoCloseDelaySeconds}s</span>
              )}
              {config.nightModeEnabled && (
                <span className="badge badge--warning">Night Mode</span>
              )}
            </div>
          ) : (
            <div style={{ display: 'flex', flexDirection: 'column', gap: 6, marginTop: 4 }}>
              <div className="skeleton skeleton--text" style={{ width: '40%' }} />
            </div>
          )}
        </div>

        {/* Recent Activity Widget */}
        <div className="card">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <h3 style={{ margin: 0, fontSize: 16, fontWeight: 600 }}>Recent Activity</h3>
            <Link to="/access-log" style={{ fontSize: 12, color: 'var(--color-accent)', textDecoration: 'none' }}>View all →</Link>
          </div>
          {logs ? (
            logs.length === 0 ? (
              <p style={{ color: 'var(--color-muted)', fontSize: 14, margin: '4px 0 0' }}>No events yet.</p>
            ) : (
              <div style={{ display: 'flex', flexDirection: 'column', gap: 6, marginTop: 4 }}>
                {logs.slice(0, 5).map(log => (
                  <div key={log.id} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', fontSize: 13 }}>
                    <span style={{ color: 'var(--color-text)' }}>
                      {log.animalName ? `${log.animalName} — ` : ''}{log.eventType}
                    </span>
                    <span style={{ color: 'var(--color-muted)', fontSize: 12, whiteSpace: 'nowrap', marginLeft: 8 }}>
                      {new Date(log.timestamp).toLocaleTimeString()}
                    </span>
                  </div>
                ))}
              </div>
            )
          ) : (
            <div style={{ display: 'flex', flexDirection: 'column', gap: 6, marginTop: 4 }}>
              {[...Array(4)].map((_, i) => (
                <div key={i} className="skeleton skeleton--text" />
              ))}
            </div>
          )}
        </div>

        {/* Animals Widget */}
        <div className="card">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <h3 style={{ margin: 0, fontSize: 16, fontWeight: 600 }}>Animals</h3>
            <Link to="/animals" style={{ fontSize: 12, color: 'var(--color-accent)', textDecoration: 'none' }}>Manage →</Link>
          </div>
          {animals ? (
            <div style={{ display: 'flex', gap: 24, marginTop: 8 }}>
              <div style={{ textAlign: 'center' }}>
                <div style={{ fontSize: 32, fontWeight: 700, color: '#4caf50', lineHeight: 1 }}>{allowedCount}</div>
                <div style={{ fontSize: 12, color: 'var(--color-muted)', marginTop: 4 }}>Allowed</div>
              </div>
              <div style={{ textAlign: 'center' }}>
                <div style={{ fontSize: 32, fontWeight: 700, color: '#ef5350', lineHeight: 1 }}>{deniedCount}</div>
                <div style={{ fontSize: 12, color: 'var(--color-muted)', marginTop: 4 }}>Denied</div>
              </div>
              <div style={{ textAlign: 'center' }}>
                <div style={{ fontSize: 32, fontWeight: 700, lineHeight: 1 }}>{animals.length}</div>
                <div style={{ fontSize: 12, color: 'var(--color-muted)', marginTop: 4 }}>Total</div>
              </div>
            </div>
          ) : (
            <div className="skeleton skeleton--text" style={{ marginTop: 8, width: '50%' }} />
          )}
        </div>

        {/* Collar Devices Widget */}
        <div className="card">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <h3 style={{ margin: 0, fontSize: 16, fontWeight: 600 }}>Collar Devices</h3>
            <Link to="/collars" style={{ fontSize: 12, color: 'var(--color-accent)', textDecoration: 'none' }}>Manage →</Link>
          </div>
          {collars ? (
            collars.length === 0 ? (
              <p style={{ color: 'var(--color-muted)', fontSize: 14, margin: '4px 0 0' }}>No collars registered.</p>
            ) : (
              <div>
                <div style={{ display: 'flex', gap: 24, marginTop: 8 }}>
                  <div style={{ textAlign: 'center' }}>
                    <div style={{ fontSize: 32, fontWeight: 700, color: '#4caf50', lineHeight: 1 }}>{activeCollars}</div>
                    <div style={{ fontSize: 12, color: 'var(--color-muted)', marginTop: 4 }}>Active</div>
                  </div>
                  <div style={{ textAlign: 'center' }}>
                    <div style={{ fontSize: 32, fontWeight: 700, lineHeight: 1 }}>{collars.length}</div>
                    <div style={{ fontSize: 12, color: 'var(--color-muted)', marginTop: 4 }}>Total</div>
                  </div>
                  {lowBatteryCollars > 0 && (
                    <div style={{ textAlign: 'center' }}>
                      <div style={{ fontSize: 32, fontWeight: 700, color: '#ef5350', lineHeight: 1 }}>{lowBatteryCollars}</div>
                      <div style={{ fontSize: 12, color: 'var(--color-muted)', marginTop: 4 }}>Low Battery</div>
                    </div>
                  )}
                </div>
                <div style={{ display: 'flex', flexDirection: 'column', gap: 4, marginTop: 8 }}>
                  {collars.slice(0, 3).map(c => (
                    <div key={c.id} style={{ display: 'flex', justifyContent: 'space-between', fontSize: 13 }}>
                      <Link to={`/collars/${c.id}`} style={{ color: 'var(--color-text)', textDecoration: 'none' }}>{c.name}</Link>
                      <span style={{
                        color: c.batteryPercent == null ? 'var(--color-muted)' :
                               c.batteryPercent > 50 ? '#4caf50' :
                               c.batteryPercent > 15 ? '#ff9800' : '#ef5350',
                        fontSize: 12,
                      }}>
                        {c.batteryPercent != null ? `${Math.round(c.batteryPercent)}%` : '--'}
                      </span>
                    </div>
                  ))}
                </div>
              </div>
            )
          ) : (
            <div className="skeleton skeleton--text" style={{ marginTop: 8, width: '50%' }} />
          )}
        </div>

        {/* Geofences Widget */}
        <div className="card">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <h3 style={{ margin: 0, fontSize: 16, fontWeight: 600 }}>Geofences</h3>
            <Link to="/geofences" style={{ fontSize: 12, color: 'var(--color-accent)', textDecoration: 'none' }}>Manage →</Link>
          </div>
          {geofences ? (
            geofences.length === 0 ? (
              <p style={{ color: 'var(--color-muted)', fontSize: 14, margin: '4px 0 0' }}>No geofences defined.</p>
            ) : (
              <div style={{ display: 'flex', gap: 24, marginTop: 8 }}>
                <div style={{ textAlign: 'center' }}>
                  <div style={{ fontSize: 32, fontWeight: 700, color: '#4caf50', lineHeight: 1 }}>
                    {geofences.filter(f => f.rule === 'allow').length}
                  </div>
                  <div style={{ fontSize: 12, color: 'var(--color-muted)', marginTop: 4 }}>Allow Zones</div>
                </div>
                <div style={{ textAlign: 'center' }}>
                  <div style={{ fontSize: 32, fontWeight: 700, color: '#ef5350', lineHeight: 1 }}>
                    {geofences.filter(f => f.rule === 'deny').length}
                  </div>
                  <div style={{ fontSize: 12, color: 'var(--color-muted)', marginTop: 4 }}>Deny Zones</div>
                </div>
                <div style={{ textAlign: 'center' }}>
                  <div style={{ fontSize: 32, fontWeight: 700, lineHeight: 1 }}>
                    {geofences.filter(f => f.isActive).length}
                  </div>
                  <div style={{ fontSize: 12, color: 'var(--color-muted)', marginTop: 4 }}>Active</div>
                </div>
              </div>
            )
          ) : (
            <div className="skeleton skeleton--text" style={{ marginTop: 8, width: '50%' }} />
          )}
        </div>
      </div>
    </div>
  );
}
