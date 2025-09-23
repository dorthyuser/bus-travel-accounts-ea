const logger = require('../utils/logger');

module.exports = (err, req, res, next) => {
  // err may be an axios error with response
  let status = 500;
  let payload = null;

  if (err && err.response && err.response.status) {
    status = err.response.status;
    payload = err.response.data;
  } else if (err && err.status) {
    status = err.status;
    payload = err.message || {};
  } else {
    payload = { error: err && err.message ? err.message : 'Internal Server Error' };
  }

  // Create error payload neutral to client
  const clientPayload = {
    error: {
      errorCode: status,
      errorDateTime: new Date().toISOString(),
      errorMessage: (payload && payload.error && payload.error.errorMessage) || (payload && payload.message) || 'ERROR',
      errorDescription: payload
    }
  };

  logger.error('Error', { correlationId: req.correlationId, error: clientPayload });

  res.status(status).json(clientPayload);
};
