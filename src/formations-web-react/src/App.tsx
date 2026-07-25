import { Routes, Route } from 'react-router-dom';
import { Layout } from './components/Layout';
import { ProtectedRoute } from './components/ProtectedRoute';
import { Home } from './pages/Home';
import { FormationsList } from './pages/FormationsList';
import { FormationDetail } from './pages/FormationDetail';
import { MesFormations } from './pages/MesFormations';
import { Login } from './pages/Login';
import { Register } from './pages/Register';
import { AvatarSession } from './pages/AvatarSession';
import { ModuleLearn } from './pages/ModuleLearn';
import { AdminDashboard } from './pages/AdminDashboard';
import { AdminUsers } from './pages/AdminUsers';
import { AdminLogs } from './pages/AdminLogs';
import { GestionFormations } from './pages/GestionFormations';
import { GestionModules } from './pages/GestionModules';
import { NotFound } from './pages/NotFound';

export default function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route path="/" element={<Home />} />
        <Route path="/formations" element={<FormationsList />} />
        <Route path="/formations/:id" element={<FormationDetail />} />
        <Route path="/connexion" element={<Login />} />
        <Route path="/inscription" element={<Register />} />

        <Route
          path="/mes-formations"
          element={
            <ProtectedRoute>
              <MesFormations />
            </ProtectedRoute>
          }
        />
        <Route
          path="/modules/:moduleId/apprendre"
          element={
            <ProtectedRoute>
              <ModuleLearn />
            </ProtectedRoute>
          }
        />
        <Route
          path="/modules/:moduleId/avatar"
          element={
            <ProtectedRoute>
              <AvatarSession />
            </ProtectedRoute>
          }
        />
        <Route
          path="/gestion/formations"
          element={
            <ProtectedRoute roles={['Admin', 'Formateur']}>
              <GestionFormations />
            </ProtectedRoute>
          }
        />
        <Route
          path="/gestion/formations/:id/modules"
          element={
            <ProtectedRoute roles={['Admin', 'Formateur']}>
              <GestionModules />
            </ProtectedRoute>
          }
        />
        <Route
          path="/admin/dashboard"
          element={
            <ProtectedRoute role="Admin">
              <AdminDashboard />
            </ProtectedRoute>
          }
        />
        <Route
          path="/admin/utilisateurs"
          element={
            <ProtectedRoute role="Admin">
              <AdminUsers />
            </ProtectedRoute>
          }
        />
        <Route
          path="/admin/logs"
          element={
            <ProtectedRoute role="Admin">
              <AdminLogs />
            </ProtectedRoute>
          }
        />

        <Route path="*" element={<NotFound />} />
      </Route>
    </Routes>
  );
}
