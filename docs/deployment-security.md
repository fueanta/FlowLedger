# Deployment Security

## Local Development

`docker-compose.yml` does not contain committed database passwords or JWT signing keys. Provide local values through a gitignored `.env` file or shell environment variables.

Example setup:

```bash
cp .env.example .env
```

Then replace placeholder values in `.env` before running:

```bash
docker compose up --build
```

## Production

Do not deploy with local Compose secrets or development JWT keys.

Production deployment should use:

- Secret storage such as Azure Key Vault, AWS Secrets Manager, GCP Secret Manager, Kubernetes Secrets, Docker secrets, or protected CI/CD environment variables.
- Private networking between API and SQL Server.
- A strong generated SQL credential, rotated regularly, or managed identity where the platform supports it.
- A strong JWT signing key loaded from the secret store.
- Separate credentials for development, staging, and production.
- TLS/encryption enforced for database and public API traffic.

The repository may include placeholders and examples, but production credentials must never be committed.
