import { Link } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';

export default function Landing() {
  const { currentUser } = useAuth();

  return (
    <div style={{ maxWidth: 700, margin: '0 auto', textAlign: 'center', padding: '48px 24px' }}>
      <h1 style={{ fontSize: 40, fontWeight: 800, marginBottom: 16 }}>Smart Dog Door</h1>
      <p style={{ fontSize: 18, color: '#555', marginBottom: 32 }}>
        Automatically lets your dogs in and out using AI-powered recognition.
        Know who came and went, when, and from which direction.
      </p>
      <div style={{ display: 'flex', gap: 16, justifyContent: 'center', flexWrap: 'wrap' }}>
        {currentUser ? (
          <Link
            to="/animals"
            style={{
              background: '#1a1a2e',
              color: '#fff',
              padding: '12px 32px',
              borderRadius: 8,
              textDecoration: 'none',
              fontWeight: 600,
            }}
          >
            Go to Dashboard
          </Link>
        ) : (
          <>
            <Link
              to="/register"
              style={{
                background: '#1a1a2e',
                color: '#fff',
                padding: '12px 32px',
                borderRadius: 8,
                textDecoration: 'none',
                fontWeight: 600,
              }}
            >
              Get Started
            </Link>
            <Link
              to="/login"
              style={{
                border: '2px solid #1a1a2e',
                color: '#1a1a2e',
                padding: '12px 32px',
                borderRadius: 8,
                textDecoration: 'none',
                fontWeight: 600,
              }}
            >
              Sign In
            </Link>
          </>
        )}
      </div>

      <div style={{ marginTop: 64, display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 24 }}>
        {[
          { title: 'AI Recognition', desc: 'On-device dog detection with cloud-based identity matching.' },
          { title: 'Access Logs', desc: 'Full history of who entered and exited, with timestamps.' },
          { title: 'Guest Sharing', desc: 'Share view-only access with family, dog walkers, and vets.' },
        ].map(f => (
          <div
            key={f.title}
            style={{ padding: 24, background: '#f5f5f5', borderRadius: 12, textAlign: 'left' }}
          >
            <h3 style={{ marginBottom: 8 }}>{f.title}</h3>
            <p style={{ color: '#666', fontSize: 14, margin: 0 }}>{f.desc}</p>
          </div>
        ))}
      </div>
    </div>
  );
}
