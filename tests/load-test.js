// Sivar.Os Load Test
// Tests app under concurrent load

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate } from 'k6/metrics';

const BASE_URL = __ENV.BASE_URL || 'http://127.0.0.1:5001';
const errorRate = new Rate('errors');

export const options = {
  stages: [
    { duration: '30s', target: 10 },  // Ramp up to 10 users
    { duration: '1m', target: 10 },   // Stay at 10 users
    { duration: '30s', target: 50 },  // Ramp up to 50 users
    { duration: '1m', target: 50 },   // Stay at 50 users
    { duration: '30s', target: 0 },   // Ramp down to 0
  ],
  thresholds: {
    http_req_duration: ['p(95)<2000'], // 95% of requests under 2s
    errors: ['rate<0.1'],              // Error rate under 10%
  },
};

export default function () {
  // Test 1: Health endpoint
  let healthRes = http.get(`${BASE_URL}/api/Health`);
  check(healthRes, {
    'health status is 200': (r) => r.status === 200,
    'health response time OK': (r) => r.timings.duration < 500,
  }) || errorRate.add(1);

  sleep(1);

  // Test 2: Landing page
  let landingRes = http.get(`${BASE_URL}/`);
  check(landingRes, {
    'landing status is 200': (r) => r.status === 200,
    'landing response time OK': (r) => r.timings.duration < 2000,
  }) || errorRate.add(1);

  sleep(2);

  // Test 3: Dev auth status
  let authRes = http.get(`${BASE_URL}/api/DevAuth/status`);
  check(authRes, {
    'auth status is 200': (r) => r.status === 200,
  }) || errorRate.add(1);

  sleep(1);
}

export function handleSummary(data) {
  return {
    'stdout': textSummary(data, { indent: ' ', enableColors: true }),
    'load-test-results.json': JSON.stringify(data),
  };
}

function textSummary(data, options) {
  const indent = options.indent || '';
  const enableColors = options.enableColors || false;
  
  let summary = `
${indent}=== Load Test Summary ===
${indent}
${indent}Scenarios: ${data.root_group.checks.length}
${indent}Total Requests: ${data.metrics.http_reqs.values.count}
${indent}Failed Requests: ${data.metrics.http_req_failed.values.passes}
${indent}
${indent}Response Time:
${indent}  avg: ${data.metrics.http_req_duration.values.avg.toFixed(2)}ms
${indent}  p95: ${data.metrics.http_req_duration.values['p(95)'].toFixed(2)}ms
${indent}  max: ${data.metrics.http_req_duration.values.max.toFixed(2)}ms
${indent}
${indent}Error Rate: ${(data.metrics.errors.values.rate * 100).toFixed(2)}%
${indent}
  `;
  
  return summary;
}
