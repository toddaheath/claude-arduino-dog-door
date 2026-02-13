# Deployment Guide

## Local Development

Use Docker Compose to run all services locally:

```bash
# Start everything
docker-compose up --build

# Start just the database
docker-compose up -d postgres

# Stop all services
docker-compose down
```

## Container Images

Container images are automatically built and pushed to GitHub Container Registry (GHCR) when a version tag is pushed.

| Image | Registry |
|-------|----------|
| API | `ghcr.io/toddaheath/dogdoor-api` |
| Web | `ghcr.io/toddaheath/dogdoor-web` |

Images are tagged with:
- Semver version (e.g., `1.0.0`, `1.0`)
- `latest` (most recent release)

## Creating a Release

1. Ensure all CI checks pass on `main`.
2. Create and push a version tag:
   ```bash
   git tag v1.0.0
   git push origin v1.0.0
   ```
3. The [Release workflow](../.github/workflows/release.yml) will automatically:
   - Build and push Docker images to GHCR
   - Package the Helm chart and attach it to the GitHub Release

## Kubernetes Deployment

### Prerequisites

- A Kubernetes cluster with `kubectl` access
- Helm 3 installed
- GHCR image pull access configured (images are public, or configure `imagePullSecrets`)

### Manual Deployment with Helm

```bash
# Staging
helm upgrade --install dog-door helm/dog-door \
  --namespace dog-door-staging \
  --create-namespace \
  --values helm/dog-door/values-staging.yaml \
  --set api.image.tag=1.0.0 \
  --set web.image.tag=1.0.0

# Production
helm upgrade --install dog-door helm/dog-door \
  --namespace dog-door-production \
  --create-namespace \
  --values helm/dog-door/values-production.yaml \
  --set api.image.tag=1.0.0 \
  --set web.image.tag=1.0.0
```

### Automated Deployment via GitHub Actions

Use the **Deploy** workflow (`workflow_dispatch`):

1. Go to **Actions** > **Deploy** in GitHub.
2. Select the target environment (`staging` or `production`).
3. Enter the image tag to deploy (e.g., `1.0.0`).
4. Click **Run workflow**.

The workflow uses GitHub Environments for protection rules. Configure required reviewers on the `production` environment to enforce approval before deploying.

## Environment Configuration

| Values File | Purpose |
|-------------|---------|
| `values.yaml` | Base defaults |
| `values-staging.yaml` | Staging overrides (1 replica, staging ingress) |
| `values-production.yaml` | Production overrides (2 replicas, TLS, higher resources) |

## Secrets Management

The following secrets must be configured:

### GitHub Repository Secrets
- **`KUBE_CONFIG`** — Base64-encoded kubeconfig for the target cluster. Add this as a secret in each GitHub Environment (`staging`, `production`).

### Kubernetes Secrets (managed by Helm)
- PostgreSQL credentials are set in `values.yaml` under `secret.*`. Override these per environment:
  ```bash
  helm upgrade --install dog-door helm/dog-door \
    --set secret.postgresPassword=<secure-password>
  ```

## Database

- PostgreSQL 16 runs as a StatefulSet with persistent storage.
- EF Core handles migrations automatically on API startup.
- The `storage.size` value controls PVC size (default: 5Gi).
- For production, ensure `storage.storageClassName` is set to a provisioner available in your cluster.

## Verifying a Deployment

```bash
# Check Helm release status
helm status dog-door --namespace dog-door-staging

# Check pod status
kubectl get pods --namespace dog-door-staging

# View API logs
kubectl logs -l app=dog-door-api --namespace dog-door-staging

# Port-forward for local access
kubectl port-forward svc/dog-door-api 5000:5000 --namespace dog-door-staging
```
