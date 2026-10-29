import { useAuth } from "react-oidc-context";
import { useState } from "react";
import { apiUrl } from "../auth";
import { useSession } from "../session";

// The test harness from Steps 2 and 3, kept as its own page. It exists to
// PROVE the controls: it predicts the status the API should return and says
// whether the API agreed. Not product UI - the real pages are under /findings.

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

type Finding = {
  id: string;
  title: string;
  severity: string;
  status: string;
  assigneeUsername: string | null;
};

type Toast = {
  id: string;
  tone: "ok" | "warn" | "danger";
  status: number;
  message: string;
};

export default function Probes() {
  const auth = useAuth();
  const [result, setResult] = useState<Result | null>(null);
  const [pending, setPending] = useState<string | null>(null);
  const [findings, setFindings] = useState<Finding[]>([]);
  const [toasts, setToasts] = useState<Toast[]>([]);

  /**
   * A toast says whether the CONTROL behaved correctly, which is not the same
   * as whether the request succeeded. A 403 where we expected one is a pass.
   */
  const toast = (status: number, expected: number, message: string) => {
    const id = crypto.randomUUID();
    const tone: Toast["tone"] =
      status === expected ? "ok" : status === 403 ? "warn" : "danger";
    setToasts((t) => [...t, { id, tone, status, message }]);
    setTimeout(() => setToasts((t) => t.filter((x) => x.id !== id)), 6000);
  };

  // Display only - see session.ts. Used here to PREDICT the API status.
  const { roles, username, isAnalystOrPm } = useSession();

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

  /** Generic request helper for the findings panel. */
  const send = async (
    method: string,
    path: string,
    label: string,
    expected: number,
    body?: unknown,
    quiet = false
  ) => {
    setPending(label);
    try {
      const res = await fetch(`${apiUrl}${path}`, {
        method,
        headers: {
          Authorization: `Bearer ${auth.user?.access_token}`,
          ...(body ? { "Content-Type": "application/json" } : {}),
        },
        body: body ? JSON.stringify(body) : undefined,
      });
      const raw = await res.text();
      let shown = raw;
      try {
        shown = JSON.stringify(JSON.parse(raw), null, 2);
      } catch {
        /* 403 bodies are often empty */
      }
      if (!quiet) {
        setResult({
          path: `${method} ${path}`,
          status: res.status,
          body: shown || "(empty response body)",
          expected,
        });
      }
      return { status: res.status, raw: res.ok ? raw : null };
    } catch (e) {
      // fetch THROWS when the network fails — backend down, CORS blocked, DNS.
      // Without this the throw escapes the handler and nothing reports it:
      // the button un-disables and the page looks like nothing happened.
      // Status 0 means "no HTTP response at all", distinct from any real code.
      const reason = e instanceof Error ? e.message : "Network error";
      if (!quiet) {
        setResult({
          path: `${method} ${path}`,
          status: 0,
          body: `${reason}\n\nIs the API running on ${apiUrl}?`,
          expected,
        });
      }
      toast(0, expected, `No response from ${apiUrl} — is the API running?`);
      return { status: 0, raw: null };
    } finally {
      setPending(null);
    }
  };

  /**
   * Reloads the list. `quiet` skips the result panel so a create/triage/escalate
   * outcome is not immediately overwritten by the list response.
   */
  const loadFindings = async (quiet = false) => {
    const { status, raw } = await send("GET", "/api/findings", "list", 200, undefined, quiet);
    if (raw) setFindings((JSON.parse(raw) as { items: Finding[] }).items);
    if (!quiet && status !== 0) toast(status, 200, "Loaded findings");
  };

  const createFinding = async (n: number) => {
    const expected = isAnalystOrPm ? 201 : 403;
    const { status } = await send("POST", "/api/findings", "create", expected, {
      title: `Test finding ${n}`,
      description: `Created from the UI to exercise Step 3 §14. Number ${n}.`,
      affectedAsset: `service-${n}`,
      severity: n === 1 ? "Medium" : "High",
    });
    // status 0 means send() already raised a network toast.
    if (status !== 0) {
      toast(
        status,
        expected,
        status === 201
          ? `Created "Test finding ${n}"`
          : status === 403
            ? "Refused — creating requires Analyst or PM"
            : `Create failed (${status})`
      );
    }
    await loadFindings(true);
  };

  const triageToDev = async (id: string) => {
    // A week out. FutureDateAttribute rejects anything already past.
    const due = new Date();
    due.setDate(due.getDate() + 7);
    // Triage is Analyst/PM only — a Developer SHOULD be refused here.
    const expected = isAnalystOrPm ? 200 : 403;
    const { status } = await send("POST", `/api/findings/${id}/triage`, "triage", expected, {
      severity: "High",
      assigneeUsername: "dev.test",
      dueDate: due.toISOString(),
    });
    if (status !== 0) {
      toast(
        status,
        expected,
        status === 200
          ? "Triaged → dev.test, severity High, due in 7 days"
          : status === 403
            ? "Refused — triage requires Analyst or PM"
            : `Triage failed (${status})`
      );
    }
    await loadFindings(true);
  };

  /** Exit criterion 3: severity must be IGNORED, not honoured. */
  const tryEscalate = async (f: Finding) => {
    // T-API-03: Analyst/PM may edit anything; a Developer only their own.
    const mayEdit = isAnalystOrPm || f.assigneeUsername === username;
    const expected = mayEdit ? 200 : 403;
    const { status, raw } = await send("PUT", `/api/findings/${f.id}`, "escalate", expected, {
      title: f.title,
      description: "Attempting to set severity through the update endpoint.",
      affectedAsset: "escalation-attempt",
      status: f.status,
      severity: "Critical", // has no matching DTO property — must be dropped
    });

    if (status === 403) {
      // Exit criterion 2: not the assignee, and not Analyst/PM.
      toast(403, 403, "Refused — you are not the assignee (T-API-03)");
    } else if (status === 200 && raw) {
      // Exit criterion 3: the request SUCCEEDS but severity must not move.
      const after = JSON.parse(raw) as Finding;
      const held = after.severity === f.severity;
      toast(
        held ? 200 : 500, // a changed severity is a real failure, colour it so
        200,
        held
          ? `Edited. Severity still ${after.severity} — the field was ignored`
          : `SEVERITY CHANGED to ${after.severity} — mass assignment is possible`
      );
    } else if (status !== 0) {
      toast(status, 200, `Update failed (${status})`);
    }
    await loadFindings(true);
  };

  const correct = result !== null && result.status === result.expected;

  return (
    <section className="probes-page">
      {/* Live region so a screen reader announces the outcome too. */}
      <div className="toasts" role="status" aria-live="polite">
        {toasts.map((t) => (
          <div key={t.id} className="toast" data-tone={t.tone}>
            <span className="toast-status">{t.status}</span>
            <span className="toast-msg">{t.message}</span>
          </div>
        ))}
      </div>

      <h1 className="page-title">Probes</h1>

      <section className="identity">
        <div className="identity-top">
          <div>
            <span className="who-label">authenticated as</span>
            <p className="who">{username}</p>
          </div>
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

      <p className="section-label">findings — step 3 §14</p>
      <p className="section-note">
        Create as <code>analyst.test</code>, triage to <code>dev.test</code>,
        then sign in as <code>dev.test</code> and try to edit or escalate.
      </p>

      <div className="probes">
        <button
          className="probe"
          disabled={pending !== null}
          onClick={() => void createFinding(1)}
        >
          <span className="probe-method">POST</span>
          <span className="probe-path">create finding 1</span>
          <span className="probe-expect">
            {roles.includes("Analyst") || roles.includes("PM")
              ? "expect 201"
              : "expect 403 · not Analyst/PM"}
          </span>
        </button>

        <button
          className="probe"
          disabled={pending !== null}
          onClick={() => void createFinding(2)}
        >
          <span className="probe-method">POST</span>
          <span className="probe-path">create finding 2</span>
          <span className="probe-expect">
            {roles.includes("Analyst") || roles.includes("PM")
              ? "expect 201"
              : "expect 403 · not Analyst/PM"}
          </span>
        </button>

        <button
          className="probe"
          disabled={pending !== null}
          onClick={() => void loadFindings()}
        >
          <span className="probe-method">GET</span>
          <span className="probe-path">/api/findings</span>
          <span className="probe-expect">expect 200 · any staff role</span>
        </button>
      </div>

      {findings.length > 0 && (
        <div className="probes">
          {findings.map((f) => (
            <div key={f.id} className="probe" style={{ cursor: "default" }}>
              <span className="probe-path">
                {f.title} · {f.severity} · {f.status}
                {f.assigneeUsername ? ` · → ${f.assigneeUsername}` : " · unassigned"}
              </span>
              <button
                className="btn-quiet"
                disabled={pending !== null}
                onClick={() => void triageToDev(f.id)}
                title="Analyst/PM only — assigns to dev.test"
              >
                triage → dev.test · {isAnalystOrPm ? "200" : "403"}
              </button>
              <button
                className="btn-quiet"
                disabled={pending !== null}
                onClick={() => void tryEscalate(f)}
                title="Sends severity:Critical in the body. It must be IGNORED."
              >
                try escalate ·{" "}
                {isAnalystOrPm || f.assigneeUsername === username ? "200" : "403"}
              </button>
            </div>
          ))}
        </div>
      )}

      {result && (
        <div className="result">
          <div className="result-head">
            <span
              className="status"
              data-tone={
                // Same rule as the toast: green when the status MATCHES what
                // was expected — 201 on a create is a pass, not a failure.
                // Amber for a refusal we predicted; red for anything else.
                result.status === result.expected
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
    </section>
  );
}
