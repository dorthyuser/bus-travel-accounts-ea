# bus-travel-accounts-ea (Node.js migration)

This project is a migration of the MuleSoft API to Node.js (Express). It exposes the same endpoints and behavior:

- GET /accounts?email=...        -> proxied to system API /accounts
- GET /accounts/:id              -> proxied to system API /accounts/:id
- POST /accounts                 -> forwarded to system API POST /accounts
- PUT /accounts/:id              -> forwarded to system API PUT /accounts/:id
- GET /alive                     -> health check (UP)
- GET /ready                     -> readiness check (UP)

It includes JSON structured logging and error handling. On certain errors it saves a record to S3 (similar to original Mule flows).

Setup:
1. Copy .env.example to .env and set environment variables.
2. npm install
3. npm start

