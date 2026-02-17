import { useState } from 'react';
import { Link, useSearchParams, useNavigate } from 'react-router-dom';
import { authApi } from '../api/auth';

export default function ResetPassword() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const token = searchParams.get('token') ?? '';
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  if (!token) {
    return (
      <div style={{ maxWidth: 400, margin: '48px auto', padding: '0 24px' }}>
        <h2 style={{ marginBottom: 16 }}>Invalid Link</h2>
        <p style={{ color: '#555', marginBottom: 16 }}>This password reset link is invalid or has expired.</p>
        <Link to="/forgot-password" style={{ color: '#1a1a2e' }}>Request a new link</Link>
      </div>
    );
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      await authApi.resetPassword(token, password);
      navigate('/login');
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Reset failed. The link may have expired.');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div style={{ maxWidth: 400, margin: '48px auto', padding: '0 24px' }}>
      <h2 style={{ marginBottom: 8 }}>Set New Password</h2>
      <p style={{ color: '#555', marginBottom: 24, fontSize: 14 }}>Enter your new password below.</p>
      {error && (
        <div style={{ background: '#fee', border: '1px solid #faa', padding: 12, borderRadius: 6, marginBottom: 16, color: '#c00' }}>
          {error}
        </div>
      )}
      <form onSubmit={handleSubmit}>
        <div style={{ marginBottom: 24 }}>
          <label style={{ display: 'block', marginBottom: 4, fontWeight: 500 }}>New Password</label>
          <input
            type="password"
            value={password}
            onChange={e => setPassword(e.target.value)}
            required
            minLength={8}
            style={{ width: '100%', padding: '8px 12px', border: '1px solid #ccc', borderRadius: 6, fontSize: 14, boxSizing: 'border-box' }}
          />
        </div>
        <button
          type="submit"
          disabled={loading}
          style={{ width: '100%', padding: '10px', background: '#1a1a2e', color: '#fff', border: 'none', borderRadius: 6, fontWeight: 600, cursor: 'pointer' }}
        >
          {loading ? 'Updating…' : 'Update Password'}
        </button>
      </form>
      <div style={{ marginTop: 16, fontSize: 14 }}>
        <Link to="/login" style={{ color: '#1a1a2e' }}>Back to sign in</Link>
      </div>
    </div>
  );
}
