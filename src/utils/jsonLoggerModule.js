"use strict";

function stringifyNonJSON(payload) {
  try {
    if (payload === null || payload === undefined) return null;
    if (typeof payload === 'string') return payload;
    if (typeof payload === 'object') return JSON.stringify(payload);
    // fallback
    return String(payload);
  } catch (e) {
    return String(payload);
  }
}

module.exports = { stringifyNonJSON };
