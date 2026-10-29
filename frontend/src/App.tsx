import { useAuth } from "react-oidc-context";
import { Navigate, NavLink, Route, Routes } from "react-router";
import { APP_ROLES, useSession } from "./session";
import FindingDetail from "./pages/FindingDetail";
import FindingsList from "./pages/FindingsList";
import NewFinding from "./pages/NewFinding";
import Probes from "./pages/Probes";
import "./App.css";

export default function App() {
  const auth = useAuth();

  if (auth.isLoading) {
    return <div className="state">authenticating…</div>;
  }

  if (auth.error) {
    return (
      <div className="state" data-tone="danger">
        {auth.error.message}
      </div>
    );
  }

  // Every page needs a signed-in user. The public report form (Step 9) will be
  // the one exception, routed BEFORE this check.
  if (!auth.isAuthenticated) {
    return (
      <div className="gate">
        <div className="gate-card">
          <h1>Vulnerability Management</h1>
          <p>Sign in to continue.</p>
          <button className="btn-primary" onClick={() => void auth.signinRedirect()}>
            Sign in with Keycloak
          </button>
          <div className="gate-foot">
            This application never sees your password.
            <br />
            Authentication is delegated to Keycloak.
          </div>
        </div>
      </div>
    );
  }

  return <Layout />;
}

function Layout() {
  const { username, roles, isAnalystOrPm, signOut } = useSession();
  const appRoles = roles.filter((r) => (APP_ROLES as readonly string[]).includes(r));

  return (
    <div className="app">
      <header className="topbar">
        <span className="wordmark">
          vms<span>/</span>tracker
        </span>
        <nav className="nav">
          <NavLink to="/findings" end>
            Findings
          </NavLink>
          {/* Hidden for UX only - the API refuses non-Analyst/PM creates with 403. */}
          {isAnalystOrPm && <NavLink to="/findings/new">New finding</NavLink>}
          <NavLink to="/probes">Probes</NavLink>
        </nav>
        <div className="whoami">
          <span>{username}</span>
          {appRoles.map((r) => (
            <span key={r} className="role" data-app="true">
              {r}
            </span>
          ))}
          <button className="btn-quiet" onClick={signOut}>
            sign out
          </button>
        </div>
      </header>

      <main>
        <Routes>
          <Route path="/" element={<Navigate to="/findings" replace />} />
          <Route path="/findings" element={<FindingsList />} />
          <Route path="/findings/new" element={<NewFinding />} />
          <Route path="/findings/:id" element={<FindingDetail />} />
          <Route path="/probes" element={<Probes />} />
          <Route path="*" element={<p className="empty">Page not found.</p>} />
        </Routes>
      </main>
    </div>
  );
}
