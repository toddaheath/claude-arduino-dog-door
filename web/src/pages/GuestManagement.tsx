import { useState, useEffect } from 'react';
import { useAuth } from '../contexts/AuthContext';
import { usersApi } from '../api/users';
import type { Guest, Invitation } from '../types';

export default function GuestManagement() {
  const { accessToken } = useAuth();
  const [guests, setGuests] = useState<Guest[]>([]);
  const [invitations, setInvitations] = useState<Invitation[]>([]);
  const [inviteEmail, setInviteEmail] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [inviteError, setInviteError] = useState('');
  const [inviteSuccess, setInviteSuccess] = useState('');
  const [inviting, setInviting] = useState(false);

  useEffect(() => {
    if (!accessToken) return;
    Promise.all([
      usersApi.getGuests(accessToken),
      usersApi.getInvitations(accessToken),
    ]).then(([g, i]) => {
      setGuests(g);
      setInvitations(i);
    }).catch(() => setError('Failed to load guest data')).finally(() => setLoading(false));
  }, [accessToken]);

  async function handleInvite(e: React.FormEvent) {
    e.preventDefault();
    if (!accessToken) return;
    setInviteError('');
    setInviteSuccess('');
    setInviting(true);
    try {
      await usersApi.inviteGuest(accessToken, inviteEmail);
      setInviteSuccess(`Invitation sent to ${inviteEmail}`);
      setInviteEmail('');
      const updated = await usersApi.getInvitations(accessToken);
      setInvitations(updated);
    } catch (err: unknown) {
      setInviteError(err instanceof Error ? err.message : 'Invite failed');
    } finally {
      setInviting(false);
    }
  }

  async function handleRemove(guestId: number) {
    if (!accessToken) return;
    try {
      await usersApi.removeGuest(accessToken, guestId);
      setGuests(g => g.filter(x => x.userId !== guestId));
    } catch {
      setError('Failed to remove guest');
    }
  }

  if (loading) return <div style={{ padding: 24 }}>Loading…</div>;

  return (
    <div style={{ maxWidth: 600 }}>
      <h2 style={{ marginBottom: 24 }}>Guest Access</h2>

      {error && (
        <div style={{ background: '#fee', border: '1px solid #faa', padding: 12, borderRadius: 6, marginBottom: 16, color: '#c00' }}>
          {error}
        </div>
      )}

      <h3 style={{ marginBottom: 12 }}>Invite a Guest</h3>
      <p style={{ color: '#666', fontSize: 14, marginBottom: 16 }}>
        Guests can view your animals and access logs in read-only mode.
      </p>
      <form onSubmit={handleInvite} style={{ display: 'flex', gap: 12, marginBottom: 8 }}>
        <input
          type="email"
          value={inviteEmail}
          onChange={e => setInviteEmail(e.target.value)}
          placeholder="guest@example.com"
          required
          style={{ flex: 1, padding: '8px 12px', border: '1px solid #ccc', borderRadius: 6, fontSize: 14 }}
        />
        <button
          type="submit"
          disabled={inviting}
          style={{ padding: '8px 20px', background: '#1a1a2e', color: '#fff', border: 'none', borderRadius: 6, fontWeight: 600, cursor: 'pointer', whiteSpace: 'nowrap' }}
        >
          {inviting ? 'Sending…' : 'Send Invite'}
        </button>
      </form>
      {inviteError && <p style={{ color: '#c00', fontSize: 14, margin: '4px 0 0' }}>{inviteError}</p>}
      {inviteSuccess && <p style={{ color: '#060', fontSize: 14, margin: '4px 0 0' }}>{inviteSuccess}</p>}

      <hr style={{ margin: '32px 0', border: 'none', borderTop: '1px solid #eee' }} />

      <h3 style={{ marginBottom: 12 }}>Active Guests</h3>
      {guests.length === 0 ? (
        <p style={{ color: '#888', fontSize: 14 }}>No active guests.</p>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
          {guests.map(g => (
            <div
              key={g.userId}
              style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '12px 16px', background: '#f9f9f9', borderRadius: 8 }}
            >
              <div>
                <div style={{ fontWeight: 500 }}>
                  {g.firstName || g.lastName ? `${g.firstName ?? ''} ${g.lastName ?? ''}`.trim() : g.email}
                </div>
                <div style={{ fontSize: 13, color: '#666' }}>{g.email}</div>
                <div style={{ fontSize: 12, color: '#999' }}>
                  Accepted {new Date(g.acceptedAt!).toLocaleDateString()}
                </div>
              </div>
              <button
                onClick={() => handleRemove(g.userId)}
                style={{ padding: '6px 14px', background: '#fff', border: '1px solid #ddd', borderRadius: 6, cursor: 'pointer', fontSize: 13, color: '#c00' }}
              >
                Remove
              </button>
            </div>
          ))}
        </div>
      )}

      <hr style={{ margin: '32px 0', border: 'none', borderTop: '1px solid #eee' }} />

      <h3 style={{ marginBottom: 12 }}>Pending Invitations</h3>
      {invitations.filter(i => !i.isAccepted).length === 0 ? (
        <p style={{ color: '#888', fontSize: 14 }}>No pending invitations.</p>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
          {invitations.filter(i => !i.isAccepted).map(inv => (
            <div
              key={inv.id}
              style={{ padding: '12px 16px', background: '#f9f9f9', borderRadius: 8 }}
            >
              <div style={{ fontWeight: 500 }}>{inv.inviteeEmail}</div>
              <div style={{ fontSize: 12, color: '#999' }}>
                Sent {new Date(inv.createdAt).toLocaleDateString()} ·{' '}
                Expires {new Date(inv.expiresAt).toLocaleDateString()}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
