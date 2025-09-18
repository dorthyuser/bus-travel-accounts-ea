"use strict";

const uuid = require('uuid');
const jsonLoggerModule = require('../utils/jsonLoggerModule');

// START logger middleware
function start(req, res, next) {
  // initialize basicDetails similar to Mule flow
  const correlationId = req.headers['x_correlation_id'] || uuid.v4();
  const customerCorrelationId = (req.headers['customer_correlation_id'] || 'CUSTOMER_CORRELATION_ID_NOT_FOUND');

  req.locals = req.locals || {};
  req.locals.basicDetails = {
    CUSTOMER_CORRELATION_ID: customerCorrelationId,
    X_CORRELATION_ID: correlationId,
    client_id: req.headers['client_id'] || null,
    httpMethod: req.method,
    relativePath: req.path
  };

  // Start log
  const payloadToLog = (req.method !== 'GET' && req.body) ? jsonLoggerModule.stringifyNonJSON(req.body) : null;
  console.info(JSON.stringify({ message: 'START - Request received', correlationId: correlationId, basicDetails: req.locals.basicDetails, payload: payloadToLog }));

  // make basicDetails available to other middlewares
  res.locals.basicDetails = req.locals.basicDetails;

  next();
}

// END logger middleware
function end(req, res, next) {
  // We want to capture response end; hook into finish
  res.on('finish', () => {
    const basicDetails = res.locals.basicDetails || (req.locals && req.locals.basicDetails) || {};
    const correlationId = basicDetails.X_CORRELATION_ID || 'MULE-CORRELATIONID-NOTFOUND';
    let payload = null;
    // We don't have raw response body here; just log status and basicDetails
    console.info(JSON.stringify({ message: 'END - Request processing completed', correlationId: correlationId, httpStatus: res.statusCode, basicDetails }));
  });
  next();
}

module.exports = { start, end };
