const express = require('express');
const router = express.Router();

router.get('/alive', (req, res) => {
  res.status(200).send('UP');
});

router.get('/ready', (req, res) => {
  // Here we could check dependencies, e.g. SA connectivity. For now return UP.
  res.status(200).send('UP');
});

module.exports = router;
