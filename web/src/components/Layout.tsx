import { useState } from 'react';
import { Link, Outlet, useLocation, useNavigate } from 'react-router-dom';
import DemoBanner from './DemoBanner';
import ToastContainer from './Toast';
import { useAuth } from '../contexts/AuthContext';

const isDemo = import.meta.env.VITE_DEMO_MODE === 'true';

const authedNavItems = [
  { path: '/dashboard', label: 'Dashboard' },
  { path: '/animals', label: 'Animals' },
  { path: '/access-log', label: 'Access Log' },
  { path: '/settings', label: 'Settings' },
  { path: '/notifications', label: 'Notifications' },
  { path: '/collars', label: 'Collars' },
  { path: '/geofences', label: 'Geofences' },
];

export default function Layout() {
  const location = useLocation();
  const navigate = useNavigate();
  const { currentUser, logout } = useAuth();
  const [showUserMenu, setShowUserMenu] = useState(false);

  async function handleLogout() {
    setShowUserMenu(false);
    await logout();
    navigate('/');
  }

  return (
    <div style={{ minHeight: '100vh', display: 'flex', flexDirection: 'column' }}>
      {isDemo && <DemoBanner />}
      <header className="app-header">
        <Link to="/" className="app-header__brand">
          Smart Dog Door
        </Link>
        <nav className="app-header__nav">
          {currentUser && authedNavItems.map(item => (
            <Link
              key={item.path}
              to={item.path}
              className={`nav-link${location.pathname.startsWith(item.path) ? ' nav-link--active' : ''}`}
            >
              {item.label}
            </Link>
          ))}
        </nav>
        <div className="app-header__actions">
          {currentUser ? (
            <div style={{ position: 'relative' }}>
              <button
                onClick={() => setShowUserMenu(v => !v)}
                style={{ background: 'none', border: '1px solid #444', color: '#ccc', padding: '4px 12px', borderRadius: 6, cursor: 'pointer', fontSize: 14 }}
              >
                {currentUser.firstName ?? currentUser.email}
              </button>
              {showUserMenu && (
                <div style={{
                  position: 'absolute', right: 0, top: '100%', marginTop: 4,
                  background: '#1e1e3a', border: '1px solid #444', borderRadius: 8,
                  boxShadow: '0 4px 12px rgba(0,0,0,0.4)', minWidth: 160, zIndex: 100,
                }}>
                  <Link
                    to="/profile"
                    onClick={() => setShowUserMenu(false)}
                    style={{ display: 'block', padding: '10px 16px', color: '#ccc', textDecoration: 'none', fontSize: 14 }}
                  >
                    Profile
                  </Link>
                  <Link
                    to="/profile/guests"
                    onClick={() => setShowUserMenu(false)}
                    style={{ display: 'block', padding: '10px 16px', color: '#ccc', textDecoration: 'none', fontSize: 14 }}
                  >
                    Guest Access
                  </Link>
                  <hr style={{ margin: 0, border: 'none', borderTop: '1px solid #333' }} />
                  <button
                    onClick={handleLogout}
                    style={{ display: 'block', width: '100%', padding: '10px 16px', color: '#ef5350', background: 'none', border: 'none', textAlign: 'left', cursor: 'pointer', fontSize: 14 }}
                  >
                    Sign Out
                  </button>
                </div>
              )}
            </div>
          ) : (
            <>
              <Link to="/login" style={{ color: '#ccc', textDecoration: 'none', fontSize: 14 }}>Sign In</Link>
              <Link
                to="/register"
                style={{ background: '#4fc3f7', color: '#1a1a2e', padding: '4px 12px', borderRadius: 6, textDecoration: 'none', fontSize: 14, fontWeight: 600 }}
              >
                Register
              </Link>
            </>
          )}
        </div>
      </header>
      <main style={{ flex: 1, padding: 24, maxWidth: 1200, margin: '0 auto', width: '100%' }}>
        <Outlet />
      </main>
      <ToastContainer />
    </div>
  );
}
