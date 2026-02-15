import { Link, Outlet, useLocation } from 'react-router-dom';
import DemoBanner from './DemoBanner';

const isDemo = import.meta.env.VITE_DEMO_MODE === 'true';

const navItems = [
  { path: '/', label: 'Animals' },
  { path: '/access-log', label: 'Access Log' },
  { path: '/settings', label: 'Settings' },
];

export default function Layout() {
  const location = useLocation();

  return (
    <div style={{ minHeight: '100vh', display: 'flex', flexDirection: 'column' }}>
      {isDemo && <DemoBanner />}
      <header style={{
        background: '#1a1a2e',
        color: '#fff',
        padding: '0 24px',
        display: 'flex',
        alignItems: 'center',
        height: 56,
        gap: 32,
      }}>
        <h1 style={{ fontSize: 18, margin: 0, fontWeight: 700 }}>Smart Dog Door</h1>
        <nav style={{ display: 'flex', gap: 16 }}>
          {navItems.map(item => (
            <Link
              key={item.path}
              to={item.path}
              style={{
                color: location.pathname === item.path ? '#4fc3f7' : '#ccc',
                textDecoration: 'none',
                fontWeight: location.pathname === item.path ? 600 : 400,
                fontSize: 14,
              }}
            >
              {item.label}
            </Link>
          ))}
        </nav>
      </header>
      <main style={{ flex: 1, padding: 24, maxWidth: 1200, margin: '0 auto', width: '100%' }}>
        <Outlet />
      </main>
    </div>
  );
}
