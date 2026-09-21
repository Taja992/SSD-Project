import { useAuth } from "react-oidc-context";
import { useState } from "react";
import { apiUrl } from "./auth";
import "./App.css";

/** Roles Keycloak hands everyone. Not evidence that authorization works. */
const INHERITED_ROLES = new Set([
  "offline_access",
  "uma_authorization",
  "default-roles-vms",
]);

type Probe = {
  path: string;
  label: string;
  /** What a correctly-configured API returns for THIS user. */
  expect: (roles: string[]) => number;
};

const PROBES: Probe[] = [
  {
    path: "/api/me",
    label: "any authenticated staff",
    expect: () => 200,
  },
  {
    path: "/api/admin/ping",
    label: "PM only",
    // A Developer being refused here is the proof, not a failure.
    expect: (roles) => (roles.includes("PM") ? 200 : 403),
  },
];

type Result = {
  path: string;
  status: number;
  body: string;
  expected: number;
};

export default function App() {
  const auth = useAuth();
  const [result, setResult] = useState<Result | null>(null);
  const [pending, setPending] = useState<string | null>(null);

  const roles: string[] =
    (auth.user?.profile as { realm_access?: { roles?: string[] } })
      ?.realm_access?.roles ?? [];

  const call = async (probe: Probe) => {
    setPending(probe.path);
    try {
      const res = await fetch(`${apiUrl}${probe.path}`, {
        headers: { Authorization: `Bearer ${auth.user?.access_token}` },
      });
      const raw = await res.text();
      let body = raw;
      try {
        body = JSON.stringify(JSON.parse(raw), null, 2);
      } catch {
        // Not JSON (403 bodies are often empty) — show it verbatim.
      }
      setResult({
        path: probe.path,
        status: res.status,
        body: body || "(empty response body)",
        expected: probe.expect(roles),
      });
    } catch (e) {
      setResult({
        path: probe.path,
        status: 0,
        body:
          e instanceof Error ? e.message : "Network error — is the API running?",
        expected: probe.expect(roles),
      });
    } finally {
      setPending(null);
    }
  };

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

  if (!auth.isAuthenticated) {
    return (
      <div className="gate">
        <div className="gate-card">
          <h1>Vulnerability Management</h1>
          <p>Sign in to continue.</p>
          <button
            className="btn-primary"
            onClick={() => void auth.signinRedirect()}
          >
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

  const username = auth.user?.profile.preferred_username ?? "unknown";
  const correct = result !== null && result.status === result.expected;

  return (
    <div className="shell">
      <header className="masthead">
        <h1 className="wordmark">
          vms<span>/</span>identity
        </h1>
        <span className="phase">step 2 · identity spine</span>
      </header>

      <section className="identity">
        <div className="identity-top">
          <div>
            <span className="who-label">authenticated as</span>
            <p className="who">{username}</p>
          </div>
          <button className="btn-quiet" onClick={() => void auth.removeUser()}>
            sign out
          </button>
        </div>

        <div className="roles">
          {roles.length === 0 && <span className="role">no roles</span>}
          {roles.map((r) => (
            <span
              key={r}
              className="role"
              data-app={!INHERITED_ROLES.has(r)}
              title={
                INHERITED_ROLES.has(r)
                  ? "Inherited from Keycloak defaults — grants nothing in this application"
                  : "Application role — enforced by the API"
              }
            >
              {r}
            </span>
          ))}
        </div>
      </section>

      <p className="section-label">authorization probes</p>
      <p className="section-note">
        The API decides. This page only asks — hiding a button is not a
        security control.
      </p>

      <div className="probes">
        {PROBES.map((p) => (
          <button
            key={p.path}
            className="probe"
            disabled={pending !== null}
            onClick={() => void call(p)}
          >
            <span className="probe-method">GET</span>
            <span className="probe-path">{p.path}</span>
            <span className="probe-expect">
              {pending === p.path ? "…" : `expect ${p.expect(roles)} · ${p.label}`}
            </span>
          </button>
        ))}
      </div>

      {result && (
        <div className="result">
          <div className="result-head">
            <span
              className="status"
              data-tone={
                result.status === 200
                  ? "ok"
                  : result.status === 403
                    ? "warn"
                    : "danger"
              }
            >
              {result.status || "ERR"}
            </span>
            <span className="verdict">
              {correct ? (
                <>
                  <strong>as expected</strong> — the control behaved correctly
                </>
              ) : (
                <>
                  <strong>unexpected</strong> — expected {result.expected}
                </>
              )}
            </span>
          </div>
          {/* Rendered as text, never dangerouslySetInnerHTML — T-ER-02 */}
          <pre className="result-body">{result.body}</pre>
        </div>
      )}
    </div>
  );
}
