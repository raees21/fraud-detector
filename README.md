# Fraud Detector

Fraud Detector is a machine-to-machine fraud evaluation API. It accepts transaction data from partner systems such as payment processors or banks, evaluates the request against seeded fraud rules, stores the transaction and evaluation result, and exposes a small set of authenticated read APIs for operations and audit workflows.

The project is built with ASP.NET Core, PostgreSQL, Redis, MediatR, and [Microsoft RulesEngine](https://github.com/microsoft/RulesEngine) for configurable fraud rule execution. For local evaluation, the fastest path is Docker Compose.

## What This Project Does

- Accepts a transaction payload and evaluates it for fraud.
- Returns a fraud decision immediately: `ALLOW`, `REVIEW`, or `BLOCK`.
- Stores transaction history and evaluation history in PostgreSQL.
- Uses Redis for short-window velocity checks.
- Protects partner-facing endpoints with API key authentication.
- Applies rate limiting per authenticated client.

## Seeded Fraud Rules

On startup, the application seeds or refreshes the default fraud rules stored in PostgreSQL.

Current decision thresholds:

- `ALLOW` for total score under `40`
- `REVIEW` for total score from `40` to `69`
- `BLOCK` for total score `70` or higher

Current seeded rules:

| Rule | Condition | Score |
| --- | --- | --- |
| `HIGH_AMOUNT_RULE` | `amount > 10000` | `35` |
| `VELOCITY_RULE` | more than `3` transactions for the same `accountId` within `60` seconds | `40` |
| `SUSPICIOUS_HOUR_RULE` | current UTC hour is between `01:00` and `04:59` | `15` |
| `HIGH_RISK_MERCHANT_RULE` | `merchantCategory` is `GAMBLING`, `CRYPTO`, or `ADULT` | `25` |
| `NEW_ACCOUNT_RULE` | `accountAgeDays < 7` | `20` |
| `DUPLICATE_TRANSACTION_RULE` | same `accountId`, `amount`, and `merchantName` seen within the last `5` minutes | `35` |
| `RECENT_LOCATION_CHANGE_RULE` | same `accountId` has recent activity from a different mapped country within `24` hours | `40` |
| `FOREIGN_CURRENCY_HIGH_AMOUNT_RULE` | `currency != "ZAR"` and `amount > 5000` | `20` |

Notes:

- velocity is tracked in Redis per `accountId`
- duplicate detection is checked against stored transactions in PostgreSQL
- the location-change rule uses configured IP-to-country mappings, not a live third-party lookup
- suspicious hour is based on the server's current UTC time at evaluation, not the submitted transaction timestamp

Local demo IP mappings:

- `203.0.113.0/24` maps to `ZA` (South Africa)
- `198.51.100.0/24` maps to `US` (United States)
- `192.0.2.0/24` maps to `GB` (United Kingdom)

## Quick Start

### Prerequisites

- Docker
- Docker Compose

### 1. Start the full stack

```sh
docker compose up --build
```

This starts:

- `api` on `http://localhost:5050`
- `postgres` inside Docker
- `redis` inside Docker

Container hardening in the default Docker setup:

- the API container runs as a non-root user
- the API container filesystem is read-only except for `/tmp`
- Docker Compose applies conservative CPU, memory, and PID limits to the API, PostgreSQL, and Redis services

No `.env` file is required for local evaluation. The demo database, Redis, and API auth values are already wired into:

- `docker-compose.yml`
- `FraudEngine.API/appsettings.json`

### 2. Confirm the API is healthy

```sh
curl http://localhost:5050/api/v1/health
```

Expected response:

```json
{
  "status": "Healthy"
}
```

## Local Demo Credentials

For evaluator convenience, the app ships with a demo operations client in `FraudEngine.API/appsettings.json`.

Use these headers for authenticated requests:

- `X-Client-Id: fraud-ops`
- `X-Api-Key: fraud-ops-dev-local-2026`

Important:

- `X-Api-Key` must be the raw secret.
- The API stores only the SHA-256 hash internally.
- Do not send the hash value from config as the request header.

## Authentication Model

All API endpoints require authentication except health:

- `GET /api/v1/health` is anonymous
- all other `/api/v1/*` routes require:
  - `X-Client-Id`
  - `X-Api-Key`

The `fraud-ops` demo client has access to:

- `transactions:submit`
- `transactions:read`
- `evaluations:read`
- `rules:read`
- `rules:write`

## Core Endpoints

### Health

```http
GET /api/v1/health
```

No auth required.

### Evaluate a Transaction

```http
POST /api/v1/transactions
X-Client-Id: fraud-ops
X-Api-Key: fraud-ops-dev-local-2026
Content-Type: application/json
```

Example request body:

```json
{
  "accountId": "ACC-10001",
  "amount": 149.99,
  "currency": "ZAR",
  "merchantName": "Contoso",
  "merchantCategory": "RETAIL",
  "ipAddress": "203.0.113.10",
  "deviceId": "DEVICE-001",
  "accountAgeDays": 365,
  "timestamp": "2026-03-08T12:00:00Z"
}
```

Example response:

```json
{
  "transactionId": "2f5de68a-879d-4268-ab30-cba1a7a4a353",
  "decision": "ALLOW",
  "evaluatedAt": "2026-03-08T12:00:00.1234567+00:00"
}
```

### Get Transaction History

```http
GET /api/v1/transactions?page=1&pageSize=20
X-Client-Id: fraud-ops
X-Api-Key: fraud-ops-dev-local-2026
```

If `page` and `pageSize` are omitted, the API defaults to:

- `page=1`
- `pageSize=20`

Optional filters:

- `decision`
- `accountId`
- `from`
- `to`
- `page`
- `pageSize`

Example using filters:

```http
GET /api/v1/transactions?decision=REVIEW&accountId=ACC-10001&from=2026-03-01T00:00:00Z&to=2026-03-08T23:59:59Z
X-Client-Id: fraud-ops
X-Api-Key: fraud-ops-dev-local-2026
```

Example response:

```json
{
  "data": [
    {
      "transactionId": "2f5de68a-879d-4268-ab30-cba1a7a4a353",
      "maskedAccountId": "*****0001",
      "amount": 149.99,
      "currency": "USD",
      "merchantName": "Contoso",
      "merchantCategory": "RETAIL",
      "timestamp": "2026-03-08T12:00:00+00:00",
      "createdAt": "2026-03-08T12:00:00.1000000+00:00"
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 20
}
```

### Get a Transaction by ID

```http
GET /api/v1/transactions/{transactionId}
X-Client-Id: fraud-ops
X-Api-Key: fraud-ops-dev-local-2026
```

### Get Evaluation History

```http
GET /api/v1/evaluations?page=1&pageSize=20
X-Client-Id: fraud-ops
X-Api-Key: fraud-ops-dev-local-2026
```

If `page` and `pageSize` are omitted, the API defaults to:

- `page=1`
- `pageSize=20`

Optional filters:

- `decision`
- `minScore`
- `page`
- `pageSize`

Example using filters:

```http
GET /api/v1/evaluations?decision=BLOCK&minScore=70
X-Client-Id: fraud-ops
X-Api-Key: fraud-ops-dev-local-2026
```

Example response:

```json
{
  "data": [
    {
      "transactionId": "2f5de68a-879d-4268-ab30-cba1a7a4a353",
      "decision": "ALLOW",
      "evaluatedAt": "2026-03-08T12:00:00.1234567+00:00"
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 20
}
```

### Get Rule Definitions

```http
GET /api/v1/rules
X-Client-Id: fraud-ops
X-Api-Key: fraud-ops-dev-local-2026
```

Example response:

```json
[
  {
    "id": "cd0d490c-474e-43cc-b8bd-67994af18da4",
    "ruleName": "HIGH_AMOUNT_RULE",
    "description": "Transaction exceeds high-value threshold",
    "isActive": true
  }
]
```

### Toggle a Rule

```http
PATCH /api/v1/rules/{ruleId}/toggle
X-Client-Id: fraud-ops
X-Api-Key: fraud-ops-dev-local-2026
```

Example response:

```json
{
  "isActive": false
}
```

## Copy/Paste Curl Examples

### Evaluate a transaction

```sh
curl -X POST http://localhost:5050/api/v1/transactions \
  -H "Content-Type: application/json" \
  -H "X-Client-Id: fraud-ops" \
  -H "X-Api-Key: fraud-ops-dev-local-2026" \
  -d '{
    "accountId": "ACC-10001",
    "amount": 149.99,
    "currency": "ZAR",
    "merchantName": "Contoso",
    "merchantCategory": "RETAIL",
    "ipAddress": "203.0.113.10",
    "deviceId": "DEVICE-001",
    "accountAgeDays": 365,
    "timestamp": "2026-03-08T12:00:00Z"
  }'
```

### Read rules

```sh
curl http://localhost:5050/api/v1/rules \
  -H "X-Client-Id: fraud-ops" \
  -H "X-Api-Key: fraud-ops-dev-local-2026"
```

### Read evaluations

```sh
curl "http://localhost:5050/api/v1/evaluations?page=1&pageSize=20" \
  -H "X-Client-Id: fraud-ops" \
  -H "X-Api-Key: fraud-ops-dev-local-2026"
```

### Read transactions without paging filters

```sh
curl "http://localhost:5050/api/v1/transactions" \
  -H "X-Client-Id: fraud-ops" \
  -H "X-Api-Key: fraud-ops-dev-local-2026"
```

This returns the first page with 20 results by default.

## Validation Rules

The API rejects malformed requests before evaluation. Examples:

- `currency` must be a 3-letter uppercase ISO code such as `USD`
- `ipAddress` is required and must be a valid IPv4 or IPv6 address
- `deviceId` is required
- `amount` must be greater than `0`
- `page` must be greater than `0`
- `pageSize` must be between `1` and `100`
- `from` must be earlier than or equal to `to`

Validation failures return `400 Bad Request`.

## Rate Limiting

Rate limiting is applied per authenticated client.

Current local defaults:

- global limiter: token bucket with burst `2000`, refill `250` requests per second
- transaction submissions: token bucket with burst `1000`, refill `100` requests per second

If the limit is exceeded, the API returns `429 Too Many Requests`.

## Project Structure

```text
FraudEngine.API              HTTP API, auth, middleware, controllers
FraudEngine.Application      use cases, DTOs, MediatR handlers, validation
FraudEngine.Domain           core entities and result types
FraudEngine.Infrastructure   EF Core, Redis, rules engine, persistence
FraudEngine.UnitTests        unit tests
FraudEngine.IntegrationTests integration test scaffolding
```

## Running Tests

```sh
dotnet test FraudEngine.sln
```

## Useful Files

- [README.md](/Users/raees/Documents/fraud-detector/README.md)
- [FraudEngine.API/FraudEngine.API.http](/Users/raees/Documents/fraud-detector/FraudEngine.API/FraudEngine.API.http)
- [docker-compose.yml](/Users/raees/Documents/fraud-detector/docker-compose.yml)
- [FraudEngine.API/appsettings.json](/Users/raees/Documents/fraud-detector/FraudEngine.API/appsettings.json)

## Troubleshooting

### `401 Unauthorized`

Check these first:

- you included both `X-Client-Id` and `X-Api-Key`
- `X-Client-Id` is `fraud-ops`
- `X-Api-Key` is `fraud-ops-dev-local-2026`
- you sent the raw secret, not the SHA-256 hash from config
- if you changed config, you rebuilt the Docker container with `docker compose up --build`

### `429 Too Many Requests`

You hit the per-client rate limit. Slow down the request rate or adjust the limiter configuration in `FraudEngine.API/Program.cs`.

### `400 Bad Request`

The request body or query parameters failed validation. Check the response error message for the specific field and constraint.

## Notes for Evaluators

- Docker Compose runs the API in `Production`, so Swagger is not exposed there.
- If you want Swagger locally, run the API directly in Development with `dotnet run --project FraudEngine.API`.
- The checked-in database, Redis, and API credentials are demo-only and exist purely to make local evaluation easy.
