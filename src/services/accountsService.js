"use strict";

const axios = require('axios');
const uuid = require('uuid');
const jsonLoggerModule = require('../utils/jsonLoggerModule');

const SYST_API_BASE = (process.env.HTTPS_REQUESTER_SA_ACCOUNTS_BASEPATH || '/');
const SYST_API_HOST = process.env.HTTPS_REQUESTER_SA_ACCOUNTS_HOST;
const SYST_API_PORT = process.env.HTTPS_REQUESTER_SA_ACCOUNTS_PORT || 443;
const TIMEOUT = parseInt(process.env.HTTPS_REQUESTER_SA_ACCOUNTS_RESPONSE_TIMEOUT || '30000', 10);

const CLIENT_ID = process.env.HTTPS_REQUESTER_SA_ACCOUNTS_CLIENT_ID;
const CLIENT_SECRET = process.env.HTTPS_REQUESTER_SA_ACCOUNTS_CLIENT_SECRET;

const instance = axios.create({
  baseURL: `${process.env.HTTPS_REQUESTER_SA_ACCOUNTS_HOST ? (process.env.HTTPS_REQUESTER_SA_ACCOUNTS_HOST.indexOf('http') === 0 ? process.env.HTTPS_REQUESTER_SA_ACCOUNTS_HOST : `https://${process.env.HTTPS_REQUESTER_SA_ACCOUNTS_HOST}`) : ''}${SYST_API_BASE}`,
  timeout: TIMEOUT
});

function buildHeaders(req) {
  const basicDetails = req.locals && req.locals.basicDetails ? req.locals.basicDetails : {};
  return {
    CUSTOMER_CORRELATION_ID: basicDetails.CUSTOMER_CORRELATION_ID || 'CUSTOMER_CORRELATION_ID_NOT_FOUND',
    X_CORRELATION_ID: basicDetails.X_CORRELATION_ID || uuid.v4(),
    client_id: CLIENT_ID,
    client_secret: CLIENT_SECRET
  };
}

async function getAccounts(req, opts) {
  const headers = buildHeaders(req);
  // Log before request
  req.app && req.app.emit('log:before', { action: 'get-accounts', personEmail: opts.email, correlationId: headers.X_CORRELATION_ID });
  try {
    const resp = await instance.get('/accounts', { params: { email: opts.email }, headers });
    // Log after request
    req.app && req.app.emit('log:after', { action: 'get-accounts', httpStatus: resp.status, payload: jsonLoggerModule.stringifyNonJSON(resp.data), correlationId: headers.X_CORRELATION_ID });
    return { status: resp.status, data: resp.data };
  } catch (err) {
    // Map to HTTP-like response shape
    const status = err.response ? err.response.status : 500;
    const payload = err.response ? err.response.data : { error: err.message };
    err.meta = { httpStatus: status, payload };
    throw err;
  }
}

async function getAccountDetails(req, opts) {
  const headers = buildHeaders(req);
  req.app && req.app.emit('log:before', { action: 'get-account-details', AccountId: opts.id, correlationId: headers.X_CORRELATION_ID });
  try {
    const resp = await instance.get(`/accounts/${encodeURIComponent(opts.id)}`, { headers });
    req.app && req.app.emit('log:after', { action: 'get-account-details', httpStatus: resp.status, payload: jsonLoggerModule.stringifyNonJSON(resp.data), correlationId: headers.X_CORRELATION_ID });
    return { status: resp.status, data: resp.data };
  } catch (err) {
    const status = err.response ? err.response.status : 500;
    const payload = err.response ? err.response.data : { error: err.message };
    err.meta = { httpStatus: status, payload };
    throw err;
  }
}

async function createAccount(req, opts) {
  const headers = buildHeaders(req);
  req.app && req.app.emit('log:before', { action: 'create-account', correlationId: headers.X_CORRELATION_ID });
  try {
    const resp = await instance.post('/accounts', opts.payload, { headers });
    req.app && req.app.emit('log:after', { action: 'create-account', httpStatus: resp.status, payload: jsonLoggerModule.stringifyNonJSON(resp.data), correlationId: headers.X_CORRELATION_ID });
    return { status: resp.status, data: resp.data };
  } catch (err) {
    const status = err.response ? err.response.status : 500;
    const payload = err.response ? err.response.data : { error: err.message };
    err.meta = { httpStatus: status, payload };
    throw err;
  }
}

async function updateAccount(req, opts) {
  const headers = buildHeaders(req);
  req.app && req.app.emit('log:before', { action: 'update-account', AccountId: opts.id, payload: jsonLoggerModule.stringifyNonJSON(opts.payload), correlationId: headers.X_CORRELATION_ID });
  try {
    const resp = await instance.put(`/accounts/${encodeURIComponent(opts.id)}`, opts.payload, { headers });
    req.app && req.app.emit('log:after', { action: 'update-account', httpStatus: resp.status, payload: jsonLoggerModule.stringifyNonJSON(resp.data), correlationId: headers.X_CORRELATION_ID });
    return { status: resp.status, data: resp.data };
  } catch (err) {
    const status = err.response ? err.response.status : 500;
    const payload = err.response ? err.response.data : { error: err.message };
    err.meta = { httpStatus: status, payload };
    throw err;
  }
}

module.exports = {
  getAccounts,
  getAccountDetails,
  createAccount,
  updateAccount
};
