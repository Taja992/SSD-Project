import { useCallback } from "react";
import { useAuth } from "react-oidc-context";
import { apiUrl } from "./auth";

/** Form field name (camelCase) -> messages from the API's validation. */
export type FieldErrors = Record<string, string[]>;

export type ApiResult<T> =
  | { ok: true; status: number; data: T }
  | { ok: false; status: number; message: string; fieldErrors: FieldErrors };

const MESSAGES: Record<number, string> = {
  0: `No response from ${apiUrl} — is the API running?`,
  400: "Some fields are not valid.",
  401: "Your session has expired. Sign out and sign in again.",
  403: "Not allowed — the API refused this action.",
  404: "Not found.",
};

/**
 * The one place the app talks to the API. Every call RETURNS a result instead
 * of throwing, so no page can forget a failure - including status 0, when
 * fetch throws because there was no response at all (API down, CORS).
 */
export function useApi() {
  const auth = useAuth();
  const token = auth.user?.access_token;

  return useCallback(
    async <T,>(method: string, path: string, body?: unknown): Promise<ApiResult<T>> => {
      let res: Response;
      try {
        res = await fetch(`${apiUrl}${path}`, {
          method,
          headers: {
            Authorization: `Bearer ${token}`,
            ...(body !== undefined ? { "Content-Type": "application/json" } : {}),
          },
          body: body !== undefined ? JSON.stringify(body) : undefined,
        });
      } catch {
        return { ok: false, status: 0, message: MESSAGES[0], fieldErrors: {} };
      }

      if (res.ok) {
        const text = await res.text();
        return { ok: true, status: res.status, data: (text ? JSON.parse(text) : null) as T };
      }

      return {
        ok: false,
        status: res.status,
        message: MESSAGES[res.status] ?? `Request failed (${res.status}).`,
        fieldErrors: res.status === 400 ? await readFieldErrors(res) : {},
      };
    },
    [token]
  );
}

/** ASP.NET sends {"errors": {"Title": ["..."]}} - re-key to the form's camelCase names. */
async function readFieldErrors(res: Response): Promise<FieldErrors> {
  try {
    const problem = (await res.json()) as { errors?: Record<string, string[]> };
    const out: FieldErrors = {};
    for (const [key, messages] of Object.entries(problem.errors ?? {})) {
      const name = key.replace(/^\$\./, "");
      out[name.charAt(0).toLowerCase() + name.slice(1)] = messages;
    }
    return out;
  } catch {
    return {};
  }
}
