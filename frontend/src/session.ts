import { useAuth } from "react-oidc-context";

/** The four application roles. Anything else in the token is a Keycloak default. */
export const APP_ROLES = ["Analyst", "Developer", "PM", "Auditor"] as const;

/**
 * Who is signed in - for DISPLAY ONLY. Roles are decoded from the access token
 * WITHOUT checking its signature; the API checks it and decides (T-SPA-04).
 * Use these to hide buttons the API would refuse, never to grant anything.
 */
export function useSession() {
  const auth = useAuth();
  const roles = decodeRoles(auth.user?.access_token);

  return {
    username: auth.user?.profile.preferred_username ?? "unknown",
    roles,
    isAnalystOrPm: roles.includes("Analyst") || roles.includes("PM"),
    signOut: () => void auth.removeUser(),
  };
}

// Roles live in the ACCESS token, not the ID token (auth.user.profile).
function decodeRoles(token: string | undefined): string[] {
  if (!token) return [];
  try {
    const payload = JSON.parse(
      atob(token.split(".")[1].replace(/-/g, "+").replace(/_/g, "/"))
    ) as { realm_access?: { roles?: string[] } };
    return payload.realm_access?.roles ?? [];
  } catch {
    return [];
  }
}
