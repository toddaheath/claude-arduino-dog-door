import { BrowserRouter, HashRouter, Routes, Route } from 'react-router-dom';
import Layout from './components/Layout';
import AnimalList from './pages/AnimalList';
import AnimalDetail from './pages/AnimalDetail';
import AccessLog from './pages/AccessLog';
import Settings from './pages/Settings';

const isDemo = import.meta.env.VITE_DEMO_MODE === 'true';
const Router = isDemo ? HashRouter : BrowserRouter;

function App() {
  return (
    <Router basename={isDemo ? undefined : import.meta.env.BASE_URL}>
      <Routes>
        <Route element={<Layout />}>
          <Route path="/" element={<AnimalList />} />
          <Route path="/animals/:id" element={<AnimalDetail />} />
          <Route path="/access-log" element={<AccessLog />} />
          <Route path="/settings" element={<Settings />} />
        </Route>
      </Routes>
    </Router>
  );
}

export default App;
