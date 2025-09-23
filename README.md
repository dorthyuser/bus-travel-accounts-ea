# bus-travel-accounts-ea (Node.js migrated)

This repository contains a Node.js migration of the MuleSoft project "bus-travel-accounts-ea".

Structure:
- src/: main Express API implementing the same endpoints as the MuleSoft app
- scheduled-job/: a separate small project that runs a configurable scheduled job

Environment variables (see .env.example):
- PORT - port to run the HTTP API
- SA_BASE_URL - base URL for the downstream System API (SA)
- SA_CLIENT_ID - client id for SA requests
- SA_CLIENT_SECRET - client secret for SA requests
- SCHEDULE_CRON - cron expression for the scheduled job (scheduled-job uses this). Default: every 5 minutes if not set

Run:
- npm install
- npm start (starts HTTP API)
- npm run start:job (runs scheduled job)

Health endpoints:
- GET /alive
- GET /ready

API endpoints:
- GET /accounts?email=... -> proxied to SA GET /accounts
- GET /accounts/:id -> proxied to SA GET /accounts/{id}
- POST /accounts -> proxied to SA POST /accounts
- PUT /accounts/:id -> proxied to SA PUT /accounts/{id}

Logging: JSON structured logs (winston). Correlation ID support via incoming header CUSTOMER_CORRELATION_ID or generated UUID.
