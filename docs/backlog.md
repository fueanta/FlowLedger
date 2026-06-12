# Backlog

## Regular Priority

- Add admin-driven active session revocation.
  - Recommended approach: add a user session version/security stamp, include it as a JWT claim, validate it for protected requests, and let an admin invalidate active sessions by changing that value.
  - Reason: current signed JWTs remain valid until expiry unless the signing key rotates or the user is disabled.
