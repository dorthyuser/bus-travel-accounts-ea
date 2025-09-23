require('dotenv').config();
const CronJob = require('cron').CronJob;
const logger = require('../../src/utils/logger');
const saClient = require('../../src/services/saClient');

// CRON expression from env: SCHEDULE_CRON (six field cron: sec min hour day month weekday)
const cronExpr = process.env.SCHEDULE_CRON || '0 */5 * * * *'; // default every 5 minutes

logger.info('Scheduled job starting', { schedule: cronExpr });

const job = new CronJob(cronExpr, async () => {
  const correlationId = require('uuid').v4();
  logger.info('Scheduled run - start', { correlationId });
  try {
    // Example scheduled task: call SA /accounts without email to fetch list (or health-check). If SA requires query, adapt accordingly.
    const headers = {
      CUSTOMER_CORRELATION_ID: correlationId,
      X_CORRELATION_ID: correlationId,
      client_secret: process.env.SA_CLIENT_SECRET || null,
      client_id: process.env.SA_CLIENT_ID || null
    };
    const response = await saClient.getAccounts(null, headers);
    logger.info('Scheduled run - completed', { correlationId, status: response.status });
  } catch (err) {
    logger.error('Scheduled run - error', { correlationId, error: (err && err.message) || err });
  }
}, null, true, process.env.TZ || 'UTC');

// start immediately
job.start();

// keep process alive
process.stdin.resume();
