import { useState, useEffect } from 'react';
import { useAuth } from '../contexts/AuthContext';
import { usersApi } from '../api/users';
import type { Guest, Invitation } from '../types';
import { Skeleton } from '../components/Skeleton';
import EmptyState from '../components/EmptyState';
import { useToast } from '../contexts/ToastContext';

function daysUntil(dateStr: string): number {
  const now = new Date();
  const target = new Date(dateStr);
  return Math.ceil((target.getTime() - now.getTime()) / (1000 * 60 * 60 * 24));
}

export default function GuestManagement() {
  const { accessToken } = useAuth();
  const { addToast } = useToast();
  const [guests, setGuests] = useState<Guest[]>([]);
  const [invitations, setInvitations] = useState<Invitation[]>([]);
  const [inviteEmail, setInviteEmail] = useState('');
  const [loading, setLoading] = useState(true);
  const [inviting, setInviting] = useState(false);

  useEffect(() => {
    if (!accessToken) return;
    Promise.all([
      usersApi.getGuests(accessToken),
      usersApi.getInvitations(accessToken),
    ]).then(([g, i]) => {
      setGuests(g);
      setInvitations(i);
    }).catch(() => addToast('Failed to load guest data', 'error')).finally(() => setLoading(false));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [accessToken]);

  async function handleInvite(e: React.FormEvent) {
    e.preventDefault();
    if (!accessToken) return;
    setInviting(true);
    try {
      await usersApi.inviteGuest(accessToken, inviteEmail);
      addToast(`Invitation sent to ${inviteEmail}`, 'success');
      setInviteEmail('');
      const updated = await usersApi.getInvitations(accessToken);
      setInvitations(updated);
    } catch (err: unknown) {
      addToast(err instanceof Error ? err.message : 'Invite failed', 'error');
    } finally {
      setInviting(false);
    }
  }

  async function handleRemove(guestId: number, guestEmail: string) {
    if (!accessToken) return;
    try {
      await usersApi.removeGuest(accessToken, guestId);
      setGuests(g => g.filter(x => x.userId !== guestId));
      addToast(`${guestEmail} removed`, 'success');
    } catch {
      addToast('Failed to remove guest', 'error');
    }
  }

  if (loading) {
    return (
      <div style={{ maxWidth: 600 }}>
        <h2>Guest Access</h2>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
          {[...Array(3)].map((_, i) => <Skeleton key={i} height={64} />)}
        </div>
      </div>
    );
  }

  const pendingInvitations = invitations.filter(i => !i.isAccepted);

  return (
    <div style={{ maxWidth: 600 }}>
      <h2 style={{ marginBottom: 24 }}>Guest Access</h2>

      <h3 style={{ marginBottom: 8 }}>Invite a Guest</h3>
      <p style={{ color: 'var(--color-muted)', fontSize: 14, marginBottom: 16 }}>
        Guests can view your animals and access logs in read-only mode.
      </p>
      <form onSubmit={handleInvite} style={{ display: 'flex', gap: 12, marginBottom: 8 }}>
        <input
          type="email"
          value={inviteEmail}
          onChange={e => setInviteEmail(e.target.value)}
          placeholder="guest@example.com"
          required
          className="form-input"
          style={{ flex: 1 }}
        />
        <button
          type="submit"
          disabled={inviting}
          style={{ padding: '8px 20px', background: 'var(--color-surface)', color: '#fff', border: '1px solid #444', borderRadius: 6, fontWeight: 600, cursor: 'pointer', whiteSpace: 'nowrap' }}
        >
          {inviting ? 'Sending…' : 'Send Invite'}
        </button>
      </form>

      <hr style={{ margin: '32px 0', border: 'none', borderTop: '1px solid var(--color-border)' }} />

      <h3 style={{ marginBottom: 12 }}>Active Guests</h3>
      {guests.length === 0 ? (
        <EmptyState icon="👤" title="No active guests" message="Invite someone to give them read-only access." />
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
          {guests.map(g => (
            <div
              key={g.userId}
              style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '12px 16px', background: 'var(--color-surface)', borderRadius: 8, border: '1px solid var(--color-border)' }}
            >
              <div>
                <div style={{ fontWeight: 500 }}>
                  {g.firstName || g.lastName ? `${g.firstName ?? ''} ${g.lastName ?? ''}`.trim() : g.email}
                </div>
                <div style={{ fontSize: 13, color: 'var(--color-muted)' }}>{g.email}</div>
                <div style={{ fontSize: 12, color: 'var(--color-muted)' }}>
                  Accepted {new Date(g.acceptedAt!).toLocaleDateString()}
                </div>
              </div>
              <button
                onClick={() => handleRemove(g.userId, g.email)}
                className="btn btn--danger"
                style={{ fontSize: 13, padding: '6px 14px' }}
              >
                Remove
              </button>
            </div>
          ))}
        </div>
      )}

      <hr style={{ margin: '32px 0', border: 'none', borderTop: '1px solid var(--color-border)' }} />

      <h3 style={{ marginBottom: 12 }}>Pending Invitations</h3>
      {pendingInvitations.length === 0 ? (
        <EmptyState icon="✉️" title="No pending invitations" message="Invited guests will appear here until they accept." />
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
          {pendingInvitations.map(inv => {
            const days = daysUntil(inv.expiresAt);
            return (
              <div
                key={inv.id}
                style={{ padding: '12px 16px', background: 'var(--color-surface)', borderRadius: 8, border: '1px solid var(--color-border)', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}
              >
                <div>
                  <div style={{ fontWeight: 500 }}>{inv.inviteeEmail}</div>
                  <div style={{ fontSize: 12, color: 'var(--color-muted)', marginTop: 4 }}>
                    Sent {new Date(inv.createdAt).toLocaleDateString()} · Expires {new Date(inv.expiresAt).toLocaleDateString()}
                  </div>
                </div>
                {days < 3 && days >= 0 && (
                  <span className="badge badge--warning">{days}d left</span>
                )}
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
