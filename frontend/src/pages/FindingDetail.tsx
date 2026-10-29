import { useEffect, useState, type FormEvent } from "react";
import { Link, useParams } from "react-router";
import { useApi, type FieldErrors } from "../api";
import { useSession } from "../session";
import { Banner, Field, SeverityBadge, StatusLabel } from "../ui";
import {
  SEVERITIES,
  STATUSES,
  STATUS_LABELS,
  formatDate,
  isOverdue,
  toDateInput,
  type Finding,
} from "../types";

type Loaded = { id: string; finding: Finding | null; error: string | null; at: number };
type Outcome = { tone: "ok" | "danger"; text: string } | null;

export default function FindingDetail() {
  const { id = "" } = useParams();
  const api = useApi();
  const { username, isAnalystOrPm } = useSession();
  const [loaded, setLoaded] = useState<Loaded | null>(null);

  useEffect(() => {
    let current = true;
    void api<Finding>("GET", `/api/findings/${encodeURIComponent(id)}`).then((r) => {
      if (current) {
        setLoaded({ id, finding: r.ok ? r.data : null, error: r.ok ? null : r.message, at: Date.now() });
      }
    });
    return () => {
      current = false;
    };
  }, [api, id]);

  if (loaded?.id !== id) return <p className="muted">Loading…</p>;

  if (!loaded.finding) {
    return (
      <>
        <Banner tone="danger">{loaded.error}</Banner>
        <Link to="/findings" className="back">
          ← Findings
        </Link>
      </>
    );
  }

  const f = loaded.finding;
  const replace = (finding: Finding) => setLoaded({ ...loaded, finding });

  // UX only: mirrors Finding.IsEditableBy, so we do not offer a form the API
  // will refuse. The API still checks every single request (T-API-03).
  const canEdit = isAnalystOrPm || f.assigneeUsername === username;

  return (
    <article className="detail">
      <Link to="/findings" className="back">
        ← Findings
      </Link>
      <h1 className="page-title">{f.title}</h1>
      <div className="detail-badges">
        <SeverityBadge severity={f.severity} />
        <StatusLabel status={f.status} />
        {isOverdue(f, loaded.at) && (
          <span className="badge" data-tone="danger">
            Overdue
          </span>
        )}
      </div>

      <dl className="meta">
        <dt>Affected asset</dt>
        <dd className="mono">{f.affectedAsset}</dd>
        <dt>Assignee</dt>
        <dd className="mono">{f.assigneeUsername ?? "Unassigned"}</dd>
        <dt>Due</dt>
        <dd>{formatDate(f.dueDate)}</dd>
        <dt>Source</dt>
        <dd>{f.source}</dd>
        <dt>Created</dt>
        <dd>{formatDate(f.createdAt)}</dd>
        <dt>Updated</dt>
        <dd>{formatDate(f.updatedAt)}</dd>
      </dl>

      <h2 className="section-label">Description</h2>
      {/* Plain text, never HTML: descriptions will arrive from the public form,
          written by strangers (T-ER-02). React escapes it; CSS keeps line breaks. */}
      <p className="description">{f.description}</p>

      {canEdit ? (
        <EditForm finding={f} onSaved={replace} />
      ) : (
        <p className="muted">
          Read-only — only the assignee, an Analyst or a PM can edit this finding.
        </p>
      )}

      {isAnalystOrPm && <TriageForm finding={f} onSaved={replace} />}
    </article>
  );
}

function EditForm({ finding, onSaved }: { finding: Finding; onSaved: (f: Finding) => void }) {
  const api = useApi();
  const [errors, setErrors] = useState<FieldErrors>({});
  const [outcome, setOutcome] = useState<Outcome>(null);
  const [saving, setSaving] = useState(false);

  const submit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const form = new FormData(e.currentTarget);
    setSaving(true);

    // Exactly the four fields UpdateFindingDto accepts. Severity and assignee
    // are not sent - and would be ignored if they were (T-API-04).
    const r = await api<Finding>("PUT", `/api/findings/${finding.id}`, {
      title: form.get("title"),
      description: form.get("description"),
      affectedAsset: form.get("affectedAsset"),
      status: form.get("status"),
    });

    setSaving(false);
    if (r.ok) {
      setErrors({});
      setOutcome({ tone: "ok", text: "Saved." });
      onSaved(r.data);
    } else {
      setErrors(r.fieldErrors);
      setOutcome({ tone: "danger", text: r.message });
    }
  };

  // maxLength/required are for convenience. The API enforces the real limits.
  return (
    <form className="panel form" onSubmit={(e) => void submit(e)}>
      <h2 className="section-label">Edit</h2>
      <Field label="Title" errors={errors.title}>
        <input name="title" defaultValue={finding.title} required maxLength={200} />
      </Field>
      <Field label="Description" errors={errors.description}>
        <textarea name="description" defaultValue={finding.description} required maxLength={10000} rows={6} />
      </Field>
      <Field label="Affected asset" errors={errors.affectedAsset}>
        <input name="affectedAsset" defaultValue={finding.affectedAsset} required maxLength={200} />
      </Field>
      <Field label="Status" errors={errors.status}>
        <select name="status" defaultValue={finding.status}>
          {STATUSES.map((s) => (
            <option key={s} value={s}>
              {STATUS_LABELS[s]}
            </option>
          ))}
        </select>
      </Field>
      {outcome && <Banner tone={outcome.tone}>{outcome.text}</Banner>}
      <button className="btn-primary" disabled={saving}>
        {saving ? "Saving…" : "Save changes"}
      </button>
    </form>
  );
}

function TriageForm({ finding, onSaved }: { finding: Finding; onSaved: (f: Finding) => void }) {
  const api = useApi();
  const [errors, setErrors] = useState<FieldErrors>({});
  const [outcome, setOutcome] = useState<Outcome>(null);
  const [saving, setSaving] = useState(false);

  const submit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const form = new FormData(e.currentTarget);
    const assignee = String(form.get("assigneeUsername") ?? "").trim();
    const due = String(form.get("dueDate") ?? "");
    setSaving(true);

    const r = await api<Finding>("POST", `/api/findings/${finding.id}/triage`, {
      severity: form.get("severity"),
      assigneeUsername: assignee || null,
      // A date input gives "2026-11-30". Send the END of that day, local time,
      // so "due today" is still in the future for FutureDateAttribute.
      dueDate: due ? new Date(`${due}T23:59:59`).toISOString() : null,
    });

    setSaving(false);
    if (r.ok) {
      setErrors({});
      setOutcome({ tone: "ok", text: "Triage saved." });
      onSaved(r.data);
    } else {
      setErrors(r.fieldErrors);
      setOutcome({ tone: "danger", text: r.message });
    }
  };

  return (
    <form className="panel form" onSubmit={(e) => void submit(e)}>
      <h2 className="section-label">Triage — Analyst / PM</h2>
      <Field label="Severity" errors={errors.severity}>
        <select name="severity" defaultValue={finding.severity}>
          {SEVERITIES.map((s) => (
            <option key={s} value={s}>
              {s}
            </option>
          ))}
        </select>
      </Field>
      <Field
        label="Assignee"
        errors={errors.assigneeUsername}
        hint="Keycloak username, e.g. dev.test. Leave empty to unassign."
      >
        <input name="assigneeUsername" defaultValue={finding.assigneeUsername ?? ""} maxLength={100} />
      </Field>
      <Field label="Due date (SLA)" errors={errors.dueDate}>
        <input name="dueDate" type="date" defaultValue={toDateInput(finding.dueDate)} />
      </Field>
      {outcome && <Banner tone={outcome.tone}>{outcome.text}</Banner>}
      <button className="btn-primary" disabled={saving}>
        {saving ? "Saving…" : "Save triage"}
      </button>
    </form>
  );
}
