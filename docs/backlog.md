# Backlog

## Regular Priority

- Add admin-driven active session revocation.
  - Recommended approach: add a user session version/security stamp, include it as a JWT claim, validate it for protected requests, and let an admin invalidate active sessions by changing that value.
  - Reason: current signed JWTs remain valid until expiry unless the signing key rotates or the user is disabled.
- Add rate limiting for sensitive and expensive actions.
  - Recommended approach: use ASP.NET Core rate limiting policies per endpoint group, backed by distributed storage if the app later runs on multiple nodes.
  - Target actions: login, registration, enrollment approval/rejection, billing approval/rejection, CSV export, and PDF invoice export.
  - Reason: protects authentication, workflow mutation, and export endpoints from brute force, accidental repeat submissions, and expensive request bursts.
