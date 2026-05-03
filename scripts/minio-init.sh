#!/bin/sh
# Idempotent: safe on every docker compose up (new or existing MinIO volume).
# Creates the bucket if missing and allows anonymous s3:GetObject (public download).
set -e

BUCKET="${MINIO_BUCKET:-images}"
ENDPOINT="${MINIO_ENDPOINT:-minio:9000}"
PROTOCOL="${MINIO_PROTOCOL:-http}"

echo "minio-init: configuring MinIO at ${PROTOCOL}://${ENDPOINT}, bucket='${BUCKET}'"

mc alias set local "${PROTOCOL}://${ENDPOINT}" "${MINIO_ROOT_USER}" "${MINIO_ROOT_PASSWORD}"
mc mb "local/${BUCKET}" --ignore-existing
mc anonymous set download "local/${BUCKET}"

echo "minio-init: done."
