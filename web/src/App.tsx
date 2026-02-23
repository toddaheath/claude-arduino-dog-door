import { BrowserRouter, HashRouter, Routes, Route } from 'react-router-dom';
import { AuthProvider } from './contexts/AuthContext';
import { ToastProvider } from './contexts/ToastContext';
import Layout from './components/Layout';
import ProtectedRoute from './components/ProtectedRoute';
import Landing from './pages/Landing';
import Login from './pages/Login';
import Register from './pages/Register';
import ForgotPassword from './pages/ForgotPassword';
import ResetPassword from './pages/ResetPassword';
import ForgotUsername from './pages/ForgotUsername';
import Dashboard from './pages/Dashboard';
import AnimalList from './pages/AnimalList';
import AnimalDetail from './pages/AnimalDetail';
import AccessLog from './pages/AccessLog';
import Settings from './pages/Settings';
import Profile from './pages/Profile';
import GuestManagement from './pages/GuestManagement';
import Notifications from './pages/Notifications';

const isDemo = import.meta.env.VITE_DEMO_MODE === 'true';
const Router = isDemo ? HashRouter : BrowserRouter;

function App() {
  return (
    <Router basename={isDemo ? undefined : import.meta.env.BASE_URL}>
      <AuthProvider>
        <ToastProvider>
          <Routes>
            <Route element={<Layout />}>
              {/* Public routes */}
              <Route path="/" element={<Landing />} />
              <Route path="/login" element={<Login />} />
              <Route path="/register" element={<Register />} />
              <Route path="/forgot-password" element={<ForgotPassword />} />
              <Route path="/reset-password" element={<ResetPassword />} />
              <Route path="/forgot-username" element={<ForgotUsername />} />

              {/* Protected routes */}
              <Route path="/dashboard" element={<ProtectedRoute><Dashboard /></ProtectedRoute>} />
              <Route path="/animals" element={<ProtectedRoute><AnimalList /></ProtectedRoute>} />
              <Route path="/animals/:id" element={<ProtectedRoute><AnimalDetail /></ProtectedRoute>} />
              <Route path="/access-log" element={<ProtectedRoute><AccessLog /></ProtectedRoute>} />
              <Route path="/settings" element={<ProtectedRoute><Settings /></ProtectedRoute>} />
              <Route path="/profile" element={<ProtectedRoute><Profile /></ProtectedRoute>} />
              <Route path="/profile/guests" element={<ProtectedRoute><GuestManagement /></ProtectedRoute>} />
              <Route path="/notifications" element={<ProtectedRoute><Notifications /></ProtectedRoute>} />
            </Route>
          </Routes>
        </ToastProvider>
      </AuthProvider>
    </Router>
  );
}

export default App;
