import type { ReactNode } from "react";
import { STATUS_LABELS, type FindingStatus, type Severity } from "./types";

export function SeverityBadge({ severity }: { severity: Severity }) {
  return (
    <span className="badge" data-severity={severity}>
      {severity}
    </span>
  );
}

export function StatusLabel({ status }: { status: FindingStatus }) {
  return (
    <span className="status-label" data-status={status}>
      {STATUS_LABELS[status]}
    </span>
  );
}

export function Banner({
  tone,
  children,
}: {
  tone: "ok" | "warn" | "danger" | "info";
  children: ReactNode;
}) {
  // role=alert is announced immediately by screen readers; status politely.
  return (
    <div className="banner" data-tone={tone} role={tone === "danger" ? "alert" : "status"}>
      {children}
    </div>
  );
}

export function Field({
  label,
  errors,
  hint,
  children,
}: {
  label: string;
  errors?: string[];
  hint?: string;
  children: ReactNode;
}) {
  return (
    <label className="field">
      <span className="field-label">{label}</span>
      {children}
      {hint && <span className="field-hint">{hint}</span>}
      {errors?.map((e) => (
        <span key={e} className="field-error">
          {e}
        </span>
      ))}
    </label>
  );
}
