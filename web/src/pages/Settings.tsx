import { useState, useEffect } from 'react';
import { getDoorStatus, updateDoorConfig } from '../api/client';
import { useApi } from '../hooks/useApi';

export default function Settings() {
  const { data: config, loading, error, reload } = useApi(getDoorStatus);

  const [isEnabled, setIsEnabled] = useState(true);
  const [autoClose, setAutoClose] = useState(true);
  const [autoCloseDelay, setAutoCloseDelay] = useState(10);
  const [confidence, setConfidence] = useState(0.7);
  const [nightMode, setNightMode] = useState(false);
  const [nightStart, setNightStart] = useState('22:00');
  const [nightEnd, setNightEnd] = useState('06:00');
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (config) {
      setIsEnabled(config.isEnabled);
      setAutoClose(config.autoCloseEnabled);
      setAutoCloseDelay(config.autoCloseDelaySeconds);
      setConfidence(config.minConfidenceThreshold);
      setNightMode(config.nightModeEnabled);
      if (config.nightModeStart) setNightStart(config.nightModeStart);
      if (config.nightModeEnd) setNightEnd(config.nightModeEnd);
    }
  }, [config]);

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    try {
      await updateDoorConfig({
        isEnabled,
        autoCloseEnabled: autoClose,
        autoCloseDelaySeconds: autoCloseDelay,
        minConfidenceThreshold: confidence,
        nightModeEnabled: nightMode,
        nightModeStart: nightMode ? nightStart : undefined,
        nightModeEnd: nightMode ? nightEnd : undefined,
      });
      reload();
    } catch (err) {
      alert(err instanceof Error ? err.message : 'Save failed');
    } finally {
      setSaving(false);
    }
  };

  if (loading) return <p>Loading...</p>;
  if (error) return <p style={{ color: 'red' }}>Error: {error}</p>;

  const fieldStyle = { padding: 8, borderRadius: 4, border: '1px solid #444', background: '#1a1a2e', color: '#fff', width: 200 };

  return (
    <div>
      <h2>Door Settings</h2>
      <form onSubmit={handleSave} style={{ display: 'flex', flexDirection: 'column', gap: 16, maxWidth: 500 }}>
        <label style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
          <input type="checkbox" checked={isEnabled} onChange={e => setIsEnabled(e.target.checked)} />
          Door Enabled
        </label>

        <label style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
          <input type="checkbox" checked={autoClose} onChange={e => setAutoClose(e.target.checked)} />
          Auto Close
        </label>

        <label style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
          Auto Close Delay (seconds)
          <input
            type="number"
            min={1}
            max={120}
            value={autoCloseDelay}
            onChange={e => setAutoCloseDelay(Number(e.target.value))}
            style={fieldStyle}
          />
        </label>

        <label style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
          Minimum Confidence Threshold ({(confidence * 100).toFixed(0)}%)
          <input
            type="range"
            min={0}
            max={1}
            step={0.05}
            value={confidence}
            onChange={e => setConfidence(Number(e.target.value))}
            style={{ width: 200 }}
          />
        </label>

        <label style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
          <input type="checkbox" checked={nightMode} onChange={e => setNightMode(e.target.checked)} />
          Night Mode (lock door during set hours)
        </label>

        {nightMode && (
          <div style={{ display: 'flex', gap: 16 }}>
            <label style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
              Start
              <input type="time" value={nightStart} onChange={e => setNightStart(e.target.value)} style={fieldStyle} />
            </label>
            <label style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
              End
              <input type="time" value={nightEnd} onChange={e => setNightEnd(e.target.value)} style={fieldStyle} />
            </label>
          </div>
        )}

        <button type="submit" disabled={saving} style={{ alignSelf: 'flex-start', marginTop: 8 }}>
          {saving ? 'Saving...' : 'Save Settings'}
        </button>
      </form>

      <div style={{ marginTop: 32, padding: 16, background: '#1a1a2e', borderRadius: 8, maxWidth: 500 }}>
        <h3 style={{ margin: '0 0 12px 0', fontSize: 16 }}>System Info</h3>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 8, fontSize: 14, color: '#ccc' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between' }}>
            <span>Dual-Sided Detection</span>
            <span style={{ color: '#4fc3f7', fontWeight: 600 }}>Supported</span>
          </div>
          <div style={{ display: 'flex', justifyContent: 'space-between' }}>
            <span>Direction Tracking</span>
            <span style={{ color: '#4fc3f7', fontWeight: 600 }}>Entering / Exiting</span>
          </div>
          <div style={{ display: 'flex', justifyContent: 'space-between' }}>
            <span>Camera Sides</span>
            <span style={{ color: '#999' }}>Inside + Outside</span>
          </div>
        </div>
      </div>
    </div>
  );
}
