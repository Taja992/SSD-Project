import type { AuthProviderProps } from "react-oidc-context";
import { WebStorageStateStore } from "oidc-client-ts";

// Fail loudly at startup rather than producing a broken login later.
function required(name: string): string {
  const value = import.meta.env[name];
  if (!value) throw new Error(`Missing ${name} — copy .env.example to .env.local`);
  return value;
}

export const apiUrl = required("VITE_API_URL");

export const oidcConfig: AuthProviderProps = {
  authority: required("VITE_KEYCLOAK_AUTHORITY"),
  client_id: required("VITE_KEYCLOAK_CLIENT_ID"),
  redirect_uri: window.location.origin,
  post_logout_redirect_uri: window.location.origin,
  response_type: "code",              // Authorization Code flow
  scope: "openid profile email",

  // T-SPA-01: the ACCESS TOKEN stays in memory only. This store holds just the
  // transient login state (PKCE verifier, nonce) which is useless after login.
  stateStore: new WebStorageStateStore({ store: window.sessionStorage }),
  userStore: undefined,

  // Clean ?code= and ?state= out of the URL after login so tokens and codes
  // never sit in browser history or get copy-pasted into a bug report.
  onSigninCallback: () => {
    window.history.replaceState({}, document.title, window.location.pathname);
  },
};
