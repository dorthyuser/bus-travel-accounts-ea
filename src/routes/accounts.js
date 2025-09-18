"use strict";

const express = require('express');
const router = express.Router();
const accountsService = require('../services/accountsService');

// GET /accounts?email=...
router.get('/', async (req, res, next) => {
  try {
    const email = req.query.email || null;
    const result = await accountsService.getAccounts(req, { email });
    res.status(result.status).send(result.data);
  } catch (err) {
    next(err);
  }
});

// GET /accounts/:id
router.get('/:id', async (req, res, next) => {
  try {
    const id = req.params.id;
    const result = await accountsService.getAccountDetails(req, { id });
    res.status(result.status).send(result.data);
  } catch (err) {
    next(err);
  }
});

// POST /accounts
router.post('/', async (req, res, next) => {
  try {
    const payload = req.body;
    const result = await accountsService.createAccount(req, { payload });
    res.status(result.status).send(result.data);
  } catch (err) {
    next(err);
  }
});

// PUT /accounts/:id
router.put('/:id', async (req, res, next) => {
  try {
    const id = req.params.id;
    const payload = req.body;
    const result = await accountsService.updateAccount(req, { id, payload });
    res.status(result.status).send(result.data);
  } catch (err) {
    next(err);
  }
});

module.exports = router;
