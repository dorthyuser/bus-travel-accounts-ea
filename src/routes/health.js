"use strict";

const express = require('express');
const router = express.Router();

// /alive
router.get('/alive', (req, res) => {
  res.status(200).send('UP');
});

// /ready
router.get('/ready', (req, res) => {
  // In a real implementation you would check dependent systems here
  res.status(200).send('UP');
});

module.exports = router;
