// Shapes the API sends and accepts. Enum values match the C# enums exactly -
// the API rejects anything else (JsonStringEnumConverter + [EnumDataType]).

export const SEVERITIES = ["Critical", "High", "Medium", "Low"] as const;
export type Severity = (typeof SEVERITIES)[number];

export const STATUSES = ["New", "Triaged", "InProgress", "Remediated", "AcceptedRisk"] as const;
export type FindingStatus = (typeof STATUSES)[number];

export const STATUS_LABELS: Record<FindingStatus, string> = {
  New: "New",
  Triaged: "Triaged",
  InProgress: "In progress",
  Remediated: "Remediated",
  AcceptedRisk: "Accepted risk",
};

/** Mirrors FindingDto. ReporterEmail is absent on purpose - the API never sends it. */
export type Finding = {
  id: string;
  title: string;
  description: string;
  affectedAsset: string;
  severity: Severity;
  status: FindingStatus;
  assigneeUsername: string | null;
  dueDate: string | null;
  source: string;
  createdAt: string;
  updatedAt: string;
};

export type PagedResult<T> = {
  total: number;
  page: number;
  pageSize: number;
  items: T[];
};

/**
 * Past its due date and still open. Closed findings are never overdue.
 * `now` is passed in rather than read here, so rendering stays pure.
 */
export const isOverdue = (f: Finding, now: number) =>
  f.dueDate !== null &&
  f.status !== "Remediated" &&
  f.status !== "AcceptedRisk" &&
  new Date(f.dueDate).getTime() < now;

export const formatDate = (iso: string | null) =>
  iso ? new Date(iso).toLocaleDateString() : "—";

/** ISO timestamp -> "yyyy-mm-dd" in LOCAL time, for <input type="date">. */
export const toDateInput = (iso: string | null) => {
  if (!iso) return "";
  const d = new Date(iso);
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
};
