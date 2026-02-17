import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';

export default function Register() {
  const { register } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      await register(email, password, firstName || undefined, lastName || undefined);
      navigate('/animals');
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Registration failed');
    } finally {
      setLoading(false);
    }
  }

  const inputStyle = {
    width: '100%', padding: '8px 12px', border: '1px solid #ccc',
    borderRadius: 6, fontSize: 14, boxSizing: 'border-box' as const
  };

  return (
    <div style={{ maxWidth: 400, margin: '48px auto', padding: '0 24px' }}>
      <h2 style={{ marginBottom: 24 }}>Create Account</h2>
      {error && (
        <div style={{ background: '#fee', border: '1px solid #faa', padding: 12, borderRadius: 6, marginBottom: 16, color: '#c00' }}>
          {error}
        </div>
      )}
      <form onSubmit={handleSubmit}>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16, marginBottom: 16 }}>
          <div>
            <label style={{ display: 'block', marginBottom: 4, fontWeight: 500 }}>First Name</label>
            <input type="text" value={firstName} onChange={e => setFirstName(e.target.value)} style={inputStyle} />
          </div>
          <div>
            <label style={{ display: 'block', marginBottom: 4, fontWeight: 500 }}>Last Name</label>
            <input type="text" value={lastName} onChange={e => setLastName(e.target.value)} style={inputStyle} />
          </div>
        </div>
        <div style={{ marginBottom: 16 }}>
          <label style={{ display: 'block', marginBottom: 4, fontWeight: 500 }}>Email *</label>
          <input type="email" value={email} onChange={e => setEmail(e.target.value)} required style={inputStyle} />
        </div>
        <div style={{ marginBottom: 24 }}>
          <label style={{ display: 'block', marginBottom: 4, fontWeight: 500 }}>Password *</label>
          <input type="password" value={password} onChange={e => setPassword(e.target.value)} required style={inputStyle} />
        </div>
        <button
          type="submit"
          disabled={loading}
          style={{ width: '100%', padding: '10px', background: '#1a1a2e', color: '#fff', border: 'none', borderRadius: 6, fontWeight: 600, cursor: 'pointer' }}
        >
          {loading ? 'Creating account…' : 'Create Account'}
        </button>
      </form>
      <div style={{ marginTop: 16, fontSize: 14 }}>
        Already have an account? <Link to="/login" style={{ color: '#1a1a2e' }}>Sign in</Link>
      </div>
    </div>
  );
}
