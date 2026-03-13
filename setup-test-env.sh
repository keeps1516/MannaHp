#!/bin/bash
set -e

echo "=== Setting up PostgreSQL test user ==="
sudo -u postgres psql -c "CREATE USER app WITH PASSWORD 'devpassword123' CREATEDB;" 2>/dev/null || \
  sudo -u postgres psql -c "ALTER USER app WITH PASSWORD 'devpassword123' CREATEDB;"
echo "PostgreSQL 'app' user ready."

echo ""
echo "=== Installing next-client dependencies ==="
cd "$(dirname "$0")/src/next-client"
npm install
echo "node_modules installed."

echo ""
echo "=== Done! You can now run all tests. ==="
