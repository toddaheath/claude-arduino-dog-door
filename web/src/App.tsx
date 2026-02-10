import { BrowserRouter, Routes, Route } from 'react-router-dom';
import Layout from './components/Layout';
import AnimalList from './pages/AnimalList';
import AnimalDetail from './pages/AnimalDetail';
import AccessLog from './pages/AccessLog';
import Settings from './pages/Settings';

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route element={<Layout />}>
          <Route path="/" element={<AnimalList />} />
          <Route path="/animals/:id" element={<AnimalDetail />} />
          <Route path="/access-log" element={<AccessLog />} />
          <Route path="/settings" element={<Settings />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}

export default App;
