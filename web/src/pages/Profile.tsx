import { useState, useEffect } from 'react';
import { useAuth } from '../contexts/AuthContext';
import { usersApi } from '../api/users';
import type { UserProfile } from '../types';
import { Link } from 'react-router-dom';
import { Skeleton } from '../components/Skeleton';
import { useToast } from '../contexts/ToastContext';

export default function Profile() {
  const { accessToken } = useAuth();
  const { addToast } = useToast();
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [loadError, setLoadError] = useState('');
  const [saving, setSaving] = useState(false);
  const [changingPassword, setChangingPassword] = useState(false);
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');

  useEffect(() => {
    if (!accessToken) return;
    usersApi.getProfile(accessToken)
      .then(setProfile)
      .catch(() => setLoadError('Failed to load profile'));
  }, [accessToken]);

  async function handleProfileSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (!accessToken || !profile) return;
    setSaving(true);
    const form = e.currentTarget;
    const data = Object.fromEntries(
      ['firstName', 'lastName', 'phone', 'mobilePhone', 'addressLine1', 'addressLine2', 'city', 'state', 'postalCode', 'country']
        .map(k => [k, (form.elements.namedItem(k) as HTMLInputElement)?.value || null])
    );
    try {
      const updated = await usersApi.updateProfile(accessToken, data);
      setProfile(updated);
      addToast('Profile updated', 'success');
    } catch (err: unknown) {
      addToast(err instanceof Error ? err.message : 'Update failed', 'error');
    } finally {
      setSaving(false);
    }
  }

  async function handlePasswordSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!accessToken) return;
    setChangingPassword(true);
    try {
      await usersApi.changePassword(accessToken, currentPassword, newPassword);
      addToast('Password changed', 'success');
      setCurrentPassword('');
      setNewPassword('');
    } catch (err: unknown) {
      addToast(err instanceof Error ? err.message : 'Password change failed', 'error');
    } finally {
      setChangingPassword(false);
    }
  }

  if (!profile && !loadError) {
    return (
      <div style={{ maxWidth: 600 }}>
        <h2>Profile</h2>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
          {[...Array(6)].map((_, i) => <Skeleton key={i} height={36} />)}
        </div>
      </div>
    );
  }

  if (loadError) {
    return <div className="alert alert--error">{loadError}</div>;
  }

  return (
    <div style={{ maxWidth: 600 }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 }}>
        <h2 style={{ margin: 0 }}>Profile</h2>
        <Link to="/profile/guests" style={{ color: 'var(--color-accent)', fontSize: 14, textDecoration: 'none' }}>Manage Guests</Link>
      </div>

      {profile && (
        <form onSubmit={handleProfileSubmit}>
          <div className="form-field">
            <label className="form-label">Email</label>
            <input type="email" value={profile.email} disabled className="form-input" />
          </div>

          <div className="form-row">
            <div className="form-field">
              <label className="form-label">First Name</label>
              <input name="firstName" type="text" defaultValue={profile.firstName ?? ''} className="form-input" />
            </div>
            <div className="form-field">
              <label className="form-label">Last Name</label>
              <input name="lastName" type="text" defaultValue={profile.lastName ?? ''} className="form-input" />
            </div>
          </div>

          <div className="form-row">
            <div className="form-field">
              <label className="form-label">Phone</label>
              <input name="phone" type="tel" defaultValue={profile.phone ?? ''} className="form-input" />
            </div>
            <div className="form-field">
              <label className="form-label">Mobile Phone</label>
              <input name="mobilePhone" type="tel" defaultValue={profile.mobilePhone ?? ''} className="form-input" />
            </div>
          </div>

          <div className="form-field">
            <label className="form-label">Address Line 1</label>
            <input name="addressLine1" type="text" defaultValue={profile.addressLine1 ?? ''} className="form-input" />
          </div>
          <div className="form-field">
            <label className="form-label">Address Line 2</label>
            <input name="addressLine2" type="text" defaultValue={profile.addressLine2 ?? ''} className="form-input" />
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '2fr 1fr 1fr', gap: 16, marginBottom: 16 }}>
            <div className="form-field" style={{ marginBottom: 0 }}>
              <label className="form-label">City</label>
              <input name="city" type="text" defaultValue={profile.city ?? ''} className="form-input" />
            </div>
            <div className="form-field" style={{ marginBottom: 0 }}>
              <label className="form-label">State</label>
              <input name="state" type="text" defaultValue={profile.state ?? ''} className="form-input" />
            </div>
            <div className="form-field" style={{ marginBottom: 0 }}>
              <label className="form-label">Postal Code</label>
              <input name="postalCode" type="text" defaultValue={profile.postalCode ?? ''} className="form-input" />
            </div>
          </div>

          <div className="form-field">
            <label className="form-label">Country</label>
            <input name="country" type="text" defaultValue={profile.country ?? ''} className="form-input" />
          </div>

          <button
            type="submit"
            disabled={saving}
            style={{ padding: '10px 24px', background: 'var(--color-surface)', color: '#fff', border: '1px solid #444', borderRadius: 6, fontWeight: 600, cursor: 'pointer', marginBottom: 40 }}
          >
            {saving ? 'Saving…' : 'Save Profile'}
          </button>
        </form>
      )}

      <hr style={{ margin: '0 0 32px', border: 'none', borderTop: '1px solid var(--color-border)' }} />

      <h3 style={{ marginBottom: 16 }}>Change Password</h3>
      <form onSubmit={handlePasswordSubmit} style={{ maxWidth: 400 }}>
        <div className="form-field">
          <label className="form-label">Current Password</label>
          <input
            type="password"
            value={currentPassword}
            onChange={e => setCurrentPassword(e.target.value)}
            required
            className="form-input"
          />
        </div>
        <div className="form-field">
          <label className="form-label">New Password</label>
          <input
            type="password"
            value={newPassword}
            onChange={e => setNewPassword(e.target.value)}
            required
            minLength={8}
            className="form-input"
          />
        </div>
        <button
          type="submit"
          disabled={changingPassword}
          style={{ padding: '10px 24px', background: 'var(--color-surface)', color: '#fff', border: '1px solid #444', borderRadius: 6, fontWeight: 600, cursor: 'pointer' }}
        >
          {changingPassword ? 'Updating…' : 'Change Password'}
        </button>
      </form>
    </div>
  );
}
