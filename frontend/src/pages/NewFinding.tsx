import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router";
import { useApi, type FieldErrors } from "../api";
import { useSession } from "../session";
import { Banner, Field } from "../ui";
import { SEVERITIES, type Finding } from "../types";

export default function NewFinding() {
  const api = useApi();
  const navigate = useNavigate();
  const { isAnalystOrPm } = useSession();
  const [errors, setErrors] = useState<FieldErrors>({});
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  // UX only - the API refuses anyone else with 403 (FIND-01).
  if (!isAnalystOrPm) {
    return <Banner tone="warn">Only Analysts and PMs can create findings.</Banner>;
  }

  const submit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const form = new FormData(e.currentTarget);
    setSaving(true);

    // Status, source and creator are set by the server - never sent from here.
    const r = await api<Finding>("POST", "/api/findings", {
      title: form.get("title"),
      description: form.get("description"),
      affectedAsset: form.get("affectedAsset"),
      severity: form.get("severity"),
    });

    setSaving(false);
    if (r.ok) {
      navigate(`/findings/${r.data.id}`);
    } else {
      setErrors(r.fieldErrors);
      setError(r.message);
    }
  };

  return (
    <section className="detail">
      <h1 className="page-title">New finding</h1>
      <form className="panel form" onSubmit={(e) => void submit(e)}>
        <Field label="Title" errors={errors.title} hint="Short and generic — the detail goes in the description.">
          <input name="title" required maxLength={200} autoFocus />
        </Field>
        <Field label="Description" errors={errors.description}>
          <textarea name="description" required maxLength={10000} rows={8} />
        </Field>
        <Field label="Affected asset" errors={errors.affectedAsset} hint="Service, component or URL.">
          <input name="affectedAsset" required maxLength={200} />
        </Field>
        <Field label="Severity" errors={errors.severity}>
          <select name="severity" defaultValue="Medium">
            {SEVERITIES.map((s) => (
              <option key={s} value={s}>
                {s}
              </option>
            ))}
          </select>
        </Field>
        {error && <Banner tone="danger">{error}</Banner>}
        <button className="btn-primary" disabled={saving}>
          {saving ? "Creating…" : "Create finding"}
        </button>
      </form>
    </section>
  );
}
