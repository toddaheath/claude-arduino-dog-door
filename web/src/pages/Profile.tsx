import { useState, useEffect } from 'react';
import { useAuth } from '../contexts/AuthContext';
import { usersApi } from '../api/users';
import type { UserProfile } from '../types';
import { Link } from 'react-router-dom';

const inputStyle = {
  width: '100%', padding: '8px 12px', border: '1px solid #ccc',
  borderRadius: 6, fontSize: 14, boxSizing: 'border-box' as const,
};

export default function Profile() {
  const { accessToken } = useAuth();
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [profileError, setProfileError] = useState('');
  const [profileSuccess, setProfileSuccess] = useState('');
  const [saving, setSaving] = useState(false);

  const [passwordError, setPasswordError] = useState('');
  const [passwordSuccess, setPasswordSuccess] = useState('');
  const [changingPassword, setChangingPassword] = useState(false);
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');

  useEffect(() => {
    if (!accessToken) return;
    usersApi.getProfile(accessToken)
      .then(setProfile)
      .catch(() => setProfileError('Failed to load profile'));
  }, [accessToken]);

  async function handleProfileSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (!accessToken || !profile) return;
    setProfileError('');
    setProfileSuccess('');
    setSaving(true);
    const form = e.currentTarget;
    const data = Object.fromEntries(
      ['firstName', 'lastName', 'phone', 'mobilePhone', 'addressLine1', 'addressLine2', 'city', 'state', 'postalCode', 'country']
        .map(k => [k, (form.elements.namedItem(k) as HTMLInputElement)?.value || null])
    );
    try {
      const updated = await usersApi.updateProfile(accessToken, data);
      setProfile(updated);
      setProfileSuccess('Profile updated successfully.');
    } catch (err: unknown) {
      setProfileError(err instanceof Error ? err.message : 'Update failed');
    } finally {
      setSaving(false);
    }
  }

  async function handlePasswordSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!accessToken) return;
    setPasswordError('');
    setPasswordSuccess('');
    setChangingPassword(true);
    try {
      await usersApi.changePassword(accessToken, currentPassword, newPassword);
      setPasswordSuccess('Password changed successfully.');
      setCurrentPassword('');
      setNewPassword('');
    } catch (err: unknown) {
      setPasswordError(err instanceof Error ? err.message : 'Password change failed');
    } finally {
      setChangingPassword(false);
    }
  }

  if (!profile && !profileError) {
    return <div style={{ padding: 24 }}>Loading…</div>;
  }

  return (
    <div style={{ maxWidth: 600 }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 }}>
        <h2 style={{ margin: 0 }}>Profile</h2>
        <Link to="/profile/guests" style={{ color: '#1a1a2e', fontSize: 14 }}>Manage Guests</Link>
      </div>

      {profileError && !profile && (
        <div style={{ background: '#fee', border: '1px solid #faa', padding: 12, borderRadius: 6, marginBottom: 16, color: '#c00' }}>
          {profileError}
        </div>
      )}

      {profile && (
        <form onSubmit={handleProfileSubmit}>
          {profileError && (
            <div style={{ background: '#fee', border: '1px solid #faa', padding: 12, borderRadius: 6, marginBottom: 16, color: '#c00' }}>
              {profileError}
            </div>
          )}
          {profileSuccess && (
            <div style={{ background: '#efe', border: '1px solid #afa', padding: 12, borderRadius: 6, marginBottom: 16, color: '#060' }}>
              {profileSuccess}
            </div>
          )}

          <div style={{ marginBottom: 16 }}>
            <label style={{ display: 'block', marginBottom: 4, fontWeight: 500 }}>Email</label>
            <input type="email" value={profile.email} disabled style={{ ...inputStyle, background: '#f5f5f5', color: '#888' }} />
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16, marginBottom: 16 }}>
            <div>
              <label style={{ display: 'block', marginBottom: 4, fontWeight: 500 }}>First Name</label>
              <input name="firstName" type="text" defaultValue={profile.firstName ?? ''} style={inputStyle} />
            </div>
            <div>
              <label style={{ display: 'block', marginBottom: 4, fontWeight: 500 }}>Last Name</label>
              <input name="lastName" type="text" defaultValue={profile.lastName ?? ''} style={inputStyle} />
            </div>
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16, marginBottom: 16 }}>
            <div>
              <label style={{ display: 'block', marginBottom: 4, fontWeight: 500 }}>Phone</label>
              <input name="phone" type="tel" defaultValue={profile.phone ?? ''} style={inputStyle} />
            </div>
            <div>
              <label style={{ display: 'block', marginBottom: 4, fontWeight: 500 }}>Mobile Phone</label>
              <input name="mobilePhone" type="tel" defaultValue={profile.mobilePhone ?? ''} style={inputStyle} />
            </div>
          </div>

          <div style={{ marginBottom: 16 }}>
            <label style={{ display: 'block', marginBottom: 4, fontWeight: 500 }}>Address Line 1</label>
            <input name="addressLine1" type="text" defaultValue={profile.addressLine1 ?? ''} style={inputStyle} />
          </div>
          <div style={{ marginBottom: 16 }}>
            <label style={{ display: 'block', marginBottom: 4, fontWeight: 500 }}>Address Line 2</label>
            <input name="addressLine2" type="text" defaultValue={profile.addressLine2 ?? ''} style={inputStyle} />
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '2fr 1fr 1fr', gap: 16, marginBottom: 24 }}>
            <div>
              <label style={{ display: 'block', marginBottom: 4, fontWeight: 500 }}>City</label>
              <input name="city" type="text" defaultValue={profile.city ?? ''} style={inputStyle} />
            </div>
            <div>
              <label style={{ display: 'block', marginBottom: 4, fontWeight: 500 }}>State</label>
              <input name="state" type="text" defaultValue={profile.state ?? ''} style={inputStyle} />
            </div>
            <div>
              <label style={{ display: 'block', marginBottom: 4, fontWeight: 500 }}>Postal Code</label>
              <input name="postalCode" type="text" defaultValue={profile.postalCode ?? ''} style={inputStyle} />
            </div>
          </div>

          <div style={{ marginBottom: 24 }}>
            <label style={{ display: 'block', marginBottom: 4, fontWeight: 500 }}>Country</label>
            <input name="country" type="text" defaultValue={profile.country ?? ''} style={inputStyle} />
          </div>

          <button
            type="submit"
            disabled={saving}
            style={{ padding: '10px 24px', background: '#1a1a2e', color: '#fff', border: 'none', borderRadius: 6, fontWeight: 600, cursor: 'pointer' }}
          >
            {saving ? 'Saving…' : 'Save Profile'}
          </button>
        </form>
      )}

      <hr style={{ margin: '40px 0', border: 'none', borderTop: '1px solid #eee' }} />

      <h3 style={{ marginBottom: 16 }}>Change Password</h3>
      <form onSubmit={handlePasswordSubmit} style={{ maxWidth: 400 }}>
        {passwordError && (
          <div style={{ background: '#fee', border: '1px solid #faa', padding: 12, borderRadius: 6, marginBottom: 16, color: '#c00' }}>
            {passwordError}
          </div>
        )}
        {passwordSuccess && (
          <div style={{ background: '#efe', border: '1px solid #afa', padding: 12, borderRadius: 6, marginBottom: 16, color: '#060' }}>
            {passwordSuccess}
          </div>
        )}
        <div style={{ marginBottom: 16 }}>
          <label style={{ display: 'block', marginBottom: 4, fontWeight: 500 }}>Current Password</label>
          <input
            type="password"
            value={currentPassword}
            onChange={e => setCurrentPassword(e.target.value)}
            required
            style={inputStyle}
          />
        </div>
        <div style={{ marginBottom: 24 }}>
          <label style={{ display: 'block', marginBottom: 4, fontWeight: 500 }}>New Password</label>
          <input
            type="password"
            value={newPassword}
            onChange={e => setNewPassword(e.target.value)}
            required
            minLength={8}
            style={inputStyle}
          />
        </div>
        <button
          type="submit"
          disabled={changingPassword}
          style={{ padding: '10px 24px', background: '#1a1a2e', color: '#fff', border: 'none', borderRadius: 6, fontWeight: 600, cursor: 'pointer' }}
        >
          {changingPassword ? 'Updating…' : 'Change Password'}
        </button>
      </form>
    </div>
  );
}
