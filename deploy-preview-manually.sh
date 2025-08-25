#!/bin/bash


TAG="v1.0.0"
REGISTRY="ghcr.io"
IMAGE_NAME_API="alsairy/barq-api"
IMAGE_NAME_FRONTEND="alsairy/barq-frontend"

echo "=== Manual BARQ Preview Deployment ==="
echo "Deploying images:"
echo "- API: ${REGISTRY}/${IMAGE_NAME_API}:${TAG}"
echo "- Frontend: ${REGISTRY}/${IMAGE_NAME_FRONTEND}:${TAG}"
echo ""

kubectl apply -f k8s/preview/namespace.yaml || echo "Namespace may already exist"

kubectl apply -f k8s/preview/api-config.yaml || echo "Config may already exist"
kubectl apply -f k8s/preview/api-secrets.yaml || echo "Secrets may already exist"

kubectl apply -f k8s/preview/api-deployment.yaml || echo "API deployment may already exist"
kubectl apply -f k8s/preview/app-deployment.yaml || echo "App deployment may already exist"

kubectl set image deployment/barq-api api=${REGISTRY}/${IMAGE_NAME_API}:${TAG} -n barq-preview || echo "Failed to update API image"
kubectl set image deployment/barq-frontend app=${REGISTRY}/${IMAGE_NAME_FRONTEND}:${TAG} -n barq-preview || echo "Failed to update Frontend image"

kubectl apply -f k8s/preview/api-ingress.yaml || echo "API ingress may already exist"
kubectl apply -f k8s/preview/app-ingress.yaml || echo "App ingress may already exist"

kubectl rollout status deployment/barq-api -n barq-preview --timeout=600s || echo "API rollout may have timed out"
kubectl rollout status deployment/barq-frontend -n barq-preview --timeout=600s || echo "Frontend rollout may have timed out"

echo ""
echo "=== Deployment completed ==="
echo "Run ./validate-preview-health.sh to verify the deployment"
