import { useEffect, useState, type FormEvent } from "react";
import { Link, useSearchParams } from "react-router";
import { useApi } from "../api";
import { Banner, SeverityBadge, StatusLabel } from "../ui";
import {
  SEVERITIES,
  STATUSES,
  STATUS_LABELS,
  formatDate,
  isOverdue,
  type Finding,
  type PagedResult,
} from "../types";

const PAGE_SIZE = 25;

/** What was loaded, and for which filters - so a stale answer is never shown as current. */
type Loaded = {
  query: string;
  data: PagedResult<Finding> | null;
  error: string | null;
  at: number;
};

export default function FindingsList() {
  const api = useApi();
  const [params, setParams] = useSearchParams();
  const query = params.toString();
  const [loaded, setLoaded] = useState<Loaded | null>(null);

  // The URL is the single source of truth for the filters: a filtered list can
  // be bookmarked, shared and refreshed, and the back button works.
  useEffect(() => {
    let current = true;
    const search = new URLSearchParams(query);
    search.set("pageSize", String(PAGE_SIZE));

    void api<PagedResult<Finding>>("GET", `/api/findings?${search}`).then((r) => {
      if (!current) return; // the filters changed meanwhile - drop this answer
      setLoaded({
        query,
        data: r.ok ? r.data : null,
        error: r.ok ? null : r.message,
        at: Date.now(),
      });
    });

    return () => {
      current = false;
    };
  }, [api, query]);

  const loading = loaded?.query !== query;
  const data = loaded?.data;
  const page = Number(params.get("page") ?? "1");
  const pages = data ? Math.max(1, Math.ceil(data.total / data.pageSize)) : 1;

  const applyFilters = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const next = new URLSearchParams();
    for (const [key, value] of new FormData(e.currentTarget)) {
      const v = String(value).trim();
      if (v) next.set(key, v);
    }
    setParams(next); // no "page" - a new filter starts on page 1
  };

  const goTo = (p: number) => {
    const next = new URLSearchParams(params);
    next.set("page", String(p));
    setParams(next);
  };

  return (
    <section>
      <div className="page-head">
        <h1 className="page-title">Findings</h1>
        {data && <span className="muted">{data.total} total</span>}
      </div>

      {/* key={query}: when the URL changes (back button, Clear), the inputs reset to it. */}
      <form className="filters" onSubmit={applyFilters} key={query}>
        <select name="status" defaultValue={params.get("status") ?? ""} aria-label="Status">
          <option value="">Any status</option>
          {STATUSES.map((s) => (
            <option key={s} value={s}>
              {STATUS_LABELS[s]}
            </option>
          ))}
        </select>
        <select name="severity" defaultValue={params.get("severity") ?? ""} aria-label="Severity">
          <option value="">Any severity</option>
          {SEVERITIES.map((s) => (
            <option key={s} value={s}>
              {s}
            </option>
          ))}
        </select>
        <input
          name="assignee"
          placeholder="Assignee (username)"
          maxLength={100}
          defaultValue={params.get("assignee") ?? ""}
          aria-label="Assignee"
        />
        <input
          name="asset"
          placeholder="Affected asset contains…"
          maxLength={200}
          defaultValue={params.get("asset") ?? ""}
          aria-label="Affected asset"
        />
        <button className="btn-quiet" type="submit">
          Filter
        </button>
        <button className="btn-quiet" type="button" onClick={() => setParams(new URLSearchParams())}>
          Clear
        </button>
      </form>

      {loaded?.error && <Banner tone="danger">{loaded.error}</Banner>}
      {loading && <p className="muted">Loading…</p>}

      {!loading && data?.items.length === 0 && (
        <p className="empty">No findings match these filters.</p>
      )}

      {data && data.items.length > 0 && (
        <table className="table" aria-busy={loading}>
          <thead>
            <tr>
              <th>Title</th>
              <th>Severity</th>
              <th>Status</th>
              <th>Affected asset</th>
              <th>Assignee</th>
              <th>Due</th>
              <th>Updated</th>
            </tr>
          </thead>
          <tbody>
            {data.items.map((f) => (
              <tr key={f.id}>
                <td>
                  <Link to={`/findings/${f.id}`}>{f.title}</Link>
                </td>
                <td>
                  <SeverityBadge severity={f.severity} />
                </td>
                <td>
                  <StatusLabel status={f.status} />
                </td>
                <td className="mono">{f.affectedAsset}</td>
                <td className="mono">{f.assigneeUsername ?? "—"}</td>
                <td className={loaded && isOverdue(f, loaded.at) ? "overdue" : undefined}>
                  {formatDate(f.dueDate)}
                </td>
                <td>{formatDate(f.updatedAt)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {data && pages > 1 && (
        <div className="pager">
          <button className="btn-quiet" disabled={page <= 1} onClick={() => goTo(page - 1)}>
            ← Previous
          </button>
          <span className="muted">
            Page {page} of {pages}
          </span>
          <button className="btn-quiet" disabled={page >= pages} onClick={() => goTo(page + 1)}>
            Next →
          </button>
        </div>
      )}
    </section>
  );
}
