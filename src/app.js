"use strict";

const express = require('express');
const cors = require('cors');
const bodyParser = require('express').json;
const accountsRouter = require('./routes/accounts');
const healthRouter = require('./routes/health');
const logger = require('./middleware/logger');
const errorHandler = require('./middleware/errorHandler');

const app = express();

app.use(cors());
app.use(bodyParser({ limit: '5mb' }));
app.use(logger.start); // START logger per request

app.use('/accounts', accountsRouter);
app.use('/', healthRouter);

app.use(logger.end); // END logger per request
app.use(errorHandler); // global error handler

module.exports = app;
