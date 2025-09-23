require('dotenv').config();
const express = require('express');
const helmet = require('helmet');
require('express-async-errors');
const logger = require('./utils/logger');
const accountsRouter = require('./routes/accounts');
const healthRouter = require('./routes/health');
const errorHandler = require('./middleware/errorHandler');

const app = express();
const PORT = process.env.PORT || 3000;

app.use(helmet());
app.use(express.json({ limit: '1mb' }));

// Attach a simple request logger and correlation id
app.use((req, res, next) => {
  // correlation id precedence: header 'CUSTOMER_CORRELATION_ID' else generated UUID
  const { v4: uuidv4 } = require('uuid');
  req.correlationId = (req.headers['customer_correlation_id'] || uuidv4()).toString();
  // client_id forwarded from incoming headers if present
  req.client_id = req.headers['client_id'] || null;
  logger.info('START - Request received', { correlationId: req.correlationId, method: req.method, path: req.originalUrl });
  // after response finished, log END
  res.on('finish', () => {
    logger.info('END - Request processing completed', {
      correlationId: req.correlationId,
      statusCode: res.statusCode
    });
  });
  next();
});

app.use('/accounts', accountsRouter);
app.use('/', healthRouter);

// global error handler
app.use(errorHandler);

app.listen(PORT, () => {
  logger.info(`bus-travel-accounts-ea listening on port ${PORT}`);
});
