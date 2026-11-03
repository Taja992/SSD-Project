# syntax=docker/dockerfile:1

# =====================================================================
# Frontend image — used for the pen-test environment.
#
# Production hosting is Firebase (static files, no container). This
# image exists so the whole stack can be handed to someone as images
# rather than a repo they have to build.
#
# VITE_* values are inlined into the JavaScript at BUILD time, so they
# ship to every visitor and must never hold a secret. An authority URL
# and a public client id are public by design.
# =====================================================================

# Node 26, matching the version CI builds on. Node 24's bundled corepack
# cannot run pnpm 12 - it prepares the package then fails to find pnpm.cjs.
FROM node:26-alpine@sha256:c6ad9e9c1d80f6eb607c26b4b0a132ff7a2344fad1c78ea3baa2a7d4c8de6d94 AS build
WORKDIR /app

# Lockfile first so this layer caches until dependencies actually change.
COPY package.json pnpm-lock.yaml ./

# Node 26 no longer bundles corepack, so install it explicitly - pinned,
# like everything else. corepack then reads "packageManager" from
# package.json and verifies the pnpm binary against its SHA-512 hash
# before running it.
#
# All in ONE layer on purpose: corepack prepares pnpm under /root/.cache,
# and a separate RUN starts from a fresh layer where that cache is not
# visible — "Cannot find module .../pnpm.cjs".
#
# --frozen-lockfile FAILS if package.json and the lockfile disagree.
# Plain `pnpm install` would quietly resolve new versions (T-SC-01).
RUN npm install --global corepack@0.36.0  && corepack enable  && corepack install  && pnpm install --frozen-lockfile

COPY . .

ARG VITE_KEYCLOAK_AUTHORITY
ARG VITE_KEYCLOAK_CLIENT_ID
ARG VITE_API_URL
RUN pnpm run build

# =====================================================================
# Runtime — nginx serving the static bundle
# =====================================================================
FROM nginx:1.27-alpine@sha256:b1f7437a6d0398a47a5d74a1e178ea6fff3ea692c9e41d19c2b3f7ce52cdb371 AS runtime

COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Non-root. Binding :80 needs root, so serve on :5173 — which also matches
# the redirect URIs already registered in the Keycloak realm.
RUN touch /var/run/nginx.pid \
 && chown -R nginx:nginx /var/run/nginx.pid /var/cache/nginx /usr/share/nginx/html
USER nginx

EXPOSE 5173
