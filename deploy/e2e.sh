#!/usr/bin/env bash
# End-to-end check through the gateway: place orders and watch the saga complete over RabbitMQ.
# Usage: deploy/e2e.sh [gateway-url]   (default http://localhost:5100)
set -euo pipefail
GW=${1:-http://localhost:5100}

json() { curl -sf -H 'Content-Type: application/json' "$@"; }

wait_for() {
  for _ in $(seq 1 60); do curl -sf "$1" >/dev/null && return 0; sleep 2; done
  echo "Timed out waiting for $1"; exit 1
}

wait_for_decision() {
  local status
  for _ in $(seq 1 30); do
    status=$(json "$GW/ordering/api/app/order/$1" | jq -r .status)
    [ "$status" != "Pending" ] && { json "$GW/ordering/api/app/order/$1"; return 0; }
    sleep 1
  done
  echo "Order $1 still Pending after 30s"; exit 1
}

wait_for "$GW/catalog/health"
wait_for "$GW/ordering/health"

echo "== Create a product with 5 in stock"
product=$(json -X POST "$GW/catalog/api/app/product" -d '{"name":"Mechanical keyboard","price":25.50,"stockCount":5}')
echo "$product"
pid=$(echo "$product" | jq -r .id)

echo "== Order 2: should be confirmed at the catalog price"
order=$(json -X POST "$GW/ordering/api/app/order" -d "{\"lines\":[{\"productId\":\"$pid\",\"quantity\":2}]}")
oid=$(echo "$order" | jq -r .id)
result=$(wait_for_decision "$oid")
echo "$result"
[ "$(echo "$result" | jq -r .status)" = "Confirmed" ]
[ "$(echo "$result" | jq -r .total)" = "51" ] || [ "$(echo "$result" | jq -r .total)" = "51.00" ] || [ "$(echo "$result" | jq -r .total)" = "51.0" ]

echo "== Stock went from 5 to 3"
[ "$(json "$GW/catalog/api/app/product/$pid" | jq -r .stockCount)" = "3" ]

echo "== Order 10: more than in stock, should be rejected and reserve nothing"
order=$(json -X POST "$GW/ordering/api/app/order" -d "{\"lines\":[{\"productId\":\"$pid\",\"quantity\":10}]}")
result=$(wait_for_decision "$(echo "$order" | jq -r .id)")
echo "$result"
[ "$(echo "$result" | jq -r .status)" = "Rejected" ]
[ "$(json "$GW/catalog/api/app/product/$pid" | jq -r .stockCount)" = "3" ]

echo "== Order an unknown product: rejected"
order=$(json -X POST "$GW/ordering/api/app/order" -d '{"lines":[{"productId":"00000000-0000-0000-0000-000000000001","quantity":1}]}')
result=$(wait_for_decision "$(echo "$order" | jq -r .id)")
[ "$(echo "$result" | jq -r .status)" = "Rejected" ]

echo "All end-to-end checks passed."
