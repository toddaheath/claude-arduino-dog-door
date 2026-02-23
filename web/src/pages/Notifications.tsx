import { useState, useEffect } from 'react';
import { usersApi } from '../api/users';
import { useAuth } from '../contexts/AuthContext';
import { useToast } from '../contexts/ToastContext';
import { Skeleton } from '../components/Skeleton';
import type { NotificationPreferences } from '../types';

const ALL_FALSE: NotificationPreferences = {
  emailEnabled: false,
  smsEnabled: false,
  animalApproachInside: false,
  animalApproachOutside: false,
  unknownAnimalInside: false,
  unknownAnimalOutside: false,
  doorOpened: false,
  doorClosed: false,
  doorFailedOpen: false,
  doorFailedClose: false,
  powerDisconnected: false,
  powerRestored: false,
  batteryLow: false,
  batteryCharged: false,
};

const ALL_TRUE: NotificationPreferences = {
  emailEnabled: true,
  smsEnabled: true,
  animalApproachInside: true,
  animalApproachOutside: true,
  unknownAnimalInside: true,
  unknownAnimalOutside: true,
  doorOpened: true,
  doorClosed: true,
  doorFailedOpen: true,
  doorFailedClose: true,
  powerDisconnected: true,
  powerRestored: true,
  batteryLow: true,
  batteryCharged: true,
};

type BoolField = keyof NotificationPreferences;

function Toggle({ label, checked, onChange }: { label: string; checked: boolean; onChange: (v: boolean) => void }) {
  return (
    <label style={{ display: 'flex', alignItems: 'center', gap: 10, cursor: 'pointer' }}>
      <input type="checkbox" checked={checked} onChange={e => onChange(e.target.checked)} />
      <span style={{ fontSize: 14 }}>{label}</span>
    </label>
  );
}

const groups: { title: string; fields: { key: BoolField; label: string }[] }[] = [
  {
    title: 'Channels',
    fields: [
      { key: 'emailEnabled', label: 'Email notifications' },
      { key: 'smsEnabled', label: 'SMS notifications' },
    ],
  },
  {
    title: 'Animal Approach',
    fields: [
      { key: 'animalApproachOutside', label: 'Known dog entered (outside sensor)' },
      { key: 'animalApproachInside', label: 'Known dog exited (inside sensor)' },
      { key: 'unknownAnimalOutside', label: 'Unknown animal at outside sensor' },
      { key: 'unknownAnimalInside', label: 'Unknown animal at inside sensor' },
    ],
  },
  {
    title: 'Door Events',
    fields: [
      { key: 'doorOpened', label: 'Door opened' },
      { key: 'doorClosed', label: 'Door closed' },
      { key: 'doorFailedOpen', label: 'Door failed to open' },
      { key: 'doorFailedClose', label: 'Door failed to close' },
    ],
  },
  {
    title: 'Power & Battery',
    fields: [
      { key: 'powerDisconnected', label: 'Main power disconnected' },
      { key: 'powerRestored', label: 'Main power restored' },
      { key: 'batteryLow', label: 'Battery low' },
      { key: 'batteryCharged', label: 'Battery charged' },
    ],
  },
];

export default function Notifications() {
  const { accessToken } = useAuth();
  const { addToast } = useToast();
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [prefs, setPrefs] = useState<NotificationPreferences>(ALL_FALSE);

  useEffect(() => {
    if (!accessToken) return;
    usersApi.getNotificationPreferences(accessToken)
      .then(setPrefs)
      .catch(() => addToast('Failed to load notification preferences', 'error'))
      .finally(() => setLoading(false));
  }, [accessToken]); // eslint-disable-line react-hooks/exhaustive-deps

  function set(key: BoolField, value: boolean) {
    setPrefs(p => ({ ...p, [key]: value }));
  }

  async function handleSave(e: React.FormEvent) {
    e.preventDefault();
    if (!accessToken) return;
    setSaving(true);
    try {
      const saved = await usersApi.updateNotificationPreferences(accessToken, prefs);
      setPrefs(saved);
      addToast('Notification preferences saved', 'success');
    } catch (err) {
      addToast(err instanceof Error ? err.message : 'Save failed', 'error');
    } finally {
      setSaving(false);
    }
  }

  if (loading) {
    return (
      <div>
        <h2>Notifications</h2>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 12, maxWidth: 500 }}>
          {[...Array(6)].map((_, i) => <Skeleton key={i} height={32} />)}
        </div>
      </div>
    );
  }

  return (
    <div>
      <h2>Notifications</h2>
      <div style={{ display: 'flex', gap: 8, marginBottom: 24 }}>
        <button type="button" onClick={() => setPrefs(ALL_TRUE)} style={{ fontSize: 13, padding: '4px 12px' }}>
          Enable All
        </button>
        <button type="button" onClick={() => setPrefs(ALL_FALSE)} style={{ fontSize: 13, padding: '4px 12px' }}>
          Disable All
        </button>
      </div>
      <form onSubmit={handleSave} style={{ display: 'flex', flexDirection: 'column', gap: 24, maxWidth: 500 }}>
        {groups.map(group => (
          <div
            key={group.title}
            style={{ padding: 16, background: 'var(--color-surface)', borderRadius: 8, border: '1px solid var(--color-border)' }}
          >
            <h3 style={{ margin: '0 0 12px 0', fontSize: 15 }}>{group.title}</h3>
            <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
              {group.fields.map(({ key, label }) => (
                <Toggle key={key} label={label} checked={prefs[key]} onChange={v => set(key, v)} />
              ))}
            </div>
          </div>
        ))}
        <button type="submit" disabled={saving} style={{ alignSelf: 'flex-start' }}>
          {saving ? 'Saving...' : 'Save Preferences'}
        </button>
      </form>
    </div>
  );
}
