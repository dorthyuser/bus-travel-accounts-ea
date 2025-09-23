const axios = require('axios');
const logger = require('../utils/logger');

const SA_BASE = process.env.SA_BASE_URL || process.env.SA_BASE || 'http://localhost:8080';

const instance = axios.create({
  baseURL: SA_BASE,
  timeout: 15000
});

instance.interceptors.request.use((config) => {
  // ensure JSON content-type unless otherwise set
  config.headers['Content-Type'] = config.headers['Content-Type'] || 'application/json';
  return config;
});

instance.interceptors.response.use((resp) => resp, (error) => {
  // normalize axios error
  if (error.response) return Promise.reject(error);
  return Promise.reject({ response: { status: 500, data: { error: 'SA request failed', details: error.message } } });
});

module.exports = {
  getAccounts: async (email, headers = {}) => {
    const params = {};
    if (email) params.email = email;
    logger.debug('SA GET /accounts', { params, headers });
    const resp = await instance.get('/accounts', { params, headers });
    return resp;
  },
  getAccountDetails: async (id, headers = {}) => {
    logger.debug(`SA GET /accounts/${id}`, { headers });
    const resp = await instance.get(`/accounts/${encodeURIComponent(id)}`, { headers });
    return resp;
  },
  createAccount: async (payload, headers = {}) => {
    logger.debug('SA POST /accounts', { headers, payload });
    const resp = await instance.post('/accounts', payload, { headers });
    return resp;
  },
  updateAccount: async (id, payload, headers = {}) => {
    logger.debug(`SA PUT /accounts/${id}`, { headers, payload });
    const resp = await instance.put(`/accounts/${encodeURIComponent(id)}`, payload, { headers });
    return resp;
  }
};
