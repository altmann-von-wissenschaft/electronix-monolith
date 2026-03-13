#!/bin/bash

DOMAIN="localhost"
OUTDIR="./certs"

mkdir -p "$OUTDIR"

cat > "$OUTDIR/openssl.cnf" <<EOF
[req]
default_bits       = 2048
prompt             = no
default_md         = sha256
distinguished_name = dn
req_extensions     = req_ext

[dn]
C  = RU
ST = Penza District
L  = Penza
O  = pnzgu
OU = fvt
CN = $DOMAIN

[req_ext]
subjectAltName = @alt_names

[alt_names]
DNS.1 = $DOMAIN
DNS.2 = www.$DOMAIN
IP.1 = 127.0.0.1
EOF

# --------------------------
# Генерация ключа и сертификата
# --------------------------
openssl req \
  -x509 \
  -nodes \
  -days 365 \
  -newkey rsa:2048 \
  -keyout "$OUTDIR/private.pem" \
  -out "$OUTDIR/public.pem" \
  -config "$OUTDIR/openssl.cnf"

openssl pkcs12 -export -in "$OUTDIR/public.pem" -inkey "$OUTDIR/private.pem" -out "$OUTDIR/cert.p12"

echo "Готово! Сертификаты в $OUTDIR:"
echo "  privkey.pem"
echo "  fullchain.pem"