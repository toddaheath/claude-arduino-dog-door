import { useState } from 'react';
import { Link } from 'react-router-dom';
import { authApi } from '../api/auth';

export default function ForgotPassword() {
  const [email, setEmail] = useState('');
  const [submitted, setSubmitted] = useState(false);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      await authApi.forgotPassword(email);
      setSubmitted(true);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Request failed');
    } finally {
      setLoading(false);
    }
  }

  if (submitted) {
    return (
      <div style={{ maxWidth: 400, margin: '48px auto', padding: '0 24px' }}>
        <h2 style={{ marginBottom: 16 }}>Check Your Email</h2>
        <p style={{ color: '#555', marginBottom: 24 }}>
          If an account exists for <strong>{email}</strong>, a password reset link has been sent.
        </p>
        <Link to="/login" style={{ color: '#1a1a2e' }}>Back to sign in</Link>
      </div>
    );
  }

  return (
    <div style={{ maxWidth: 400, margin: '48px auto', padding: '0 24px' }}>
      <h2 style={{ marginBottom: 8 }}>Reset Password</h2>
      <p style={{ color: '#555', marginBottom: 24, fontSize: 14 }}>
        Enter your email address and we&apos;ll send you a link to reset your password.
      </p>
      {error && (
        <div style={{ background: '#fee', border: '1px solid #faa', padding: 12, borderRadius: 6, marginBottom: 16, color: '#c00' }}>
          {error}
        </div>
      )}
      <form onSubmit={handleSubmit}>
        <div style={{ marginBottom: 24 }}>
          <label style={{ display: 'block', marginBottom: 4, fontWeight: 500 }}>Email</label>
          <input
            type="email"
            value={email}
            onChange={e => setEmail(e.target.value)}
            required
            style={{ width: '100%', padding: '8px 12px', border: '1px solid #ccc', borderRadius: 6, fontSize: 14, boxSizing: 'border-box' }}
          />
        </div>
        <button
          type="submit"
          disabled={loading}
          style={{ width: '100%', padding: '10px', background: '#1a1a2e', color: '#fff', border: 'none', borderRadius: 6, fontWeight: 600, cursor: 'pointer' }}
        >
          {loading ? 'Sending…' : 'Send Reset Link'}
        </button>
      </form>
      <div style={{ marginTop: 16, fontSize: 14 }}>
        <Link to="/login" style={{ color: '#1a1a2e' }}>Back to sign in</Link>
      </div>
    </div>
  );
}
