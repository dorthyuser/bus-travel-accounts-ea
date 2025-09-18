"use strict";

const aws = require('aws-sdk');
const jsonLoggerModule = require('../utils/jsonLoggerModule');

const s3 = new aws.S3({
  region: process.env.AWS_REGION || process.env.AWS_DEFAULT_REGION
});

function mapErrorToResponse(err) {
  // Map some common scenarios similar to Mule global error handler
  if (err && err.meta && err.meta.httpStatus) {
    const s = err.meta.httpStatus;
    const body = err.meta.payload || { error: err.message };
    return { status: s, body };
  }
  // generic
  return { status: 500, body: { error: err.message || 'Internal Server Error' } };
}

async function sendErrorToS3(basicDetails, httpStatus, payload) {
  try {
    const bucket = process.env.AMAZON_S3_BUCKET;
    if (!bucket) return;
    const key = `${process.env.npm_package_name || 'bus-travel-accounts-ea'}-${new Date().toISOString()}`;
    const s3payload = JSON.stringify({
      errorCode: payload && payload.error && payload.error.errorCode ? payload.error.errorCode : httpStatus,
      httpMethod: basicDetails ? basicDetails.httpMethod : null,
      httpErrorCode: httpStatus,
      errorMessage: payload && payload.error && payload.error.errorMessage ? payload.error.errorMessage : (payload && payload.error) || payload,
      errorDescription: (payload && payload.error && payload.error.errorDescription) || payload,
      timestamp: new Date().toISOString(),
      apiName: process.env.npm_package_name || 'bus-travel-accounts-ea',
      endpoint: basicDetails ? basicDetails.relativePath : null
    });

    await s3.putObject({ Bucket: bucket, Key: key, Body: s3payload }).promise();
    console.error(JSON.stringify({ message: 'Created error in S3 bucket.', key, bucket }));
  } catch (e) {
    console.error('Failed to save error to S3', e.message);
  }
}

module.exports = async function (err, req, res, next) {
  try {
    const basicDetails = (req.locals && req.locals.basicDetails) || res.locals.basicDetails || {};
    const mapped = mapErrorToResponse(err);

    // Log error
    console.error(JSON.stringify({ message: 'Error', correlationId: basicDetails.X_CORRELATION_ID || 'MULE-CORRELATIONID-NOTFOUND', payload: jsonLoggerModule.stringifyNonJSON(mapped.body), details: basicDetails }));

    // Send custom metric - in this migration we only log metric emission
    console.error(JSON.stringify({ metric: 'GBR_EXCEPTIONS', dimensions: { HTTP_METHOD: basicDetails.httpMethod, GBR_EXCEPTION: (mapped.body && mapped.body.error && mapped.body.error.errorMessage) ? (mapped.body.error.errorMessage.replace(/\s+/g, '_')) : 'UNKNOWN' }, facts: { GBR_EXCEPTION_COUNT: 1 } }));

    // Send object to S3 (fire-and-forget)
    await sendErrorToS3(basicDetails, mapped.status, mapped.body);

    // Return payload as JSON
    res.status(mapped.status).json(mapped.body);
  } catch (e) {
    console.error('Error in global error handler', e);
    res.status(500).json({ error: 'Internal Server Error' });
  }
};
