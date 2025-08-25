#!/bin/bash
echo "=== BARQ Preview Health Check Validation ==="
echo "Testing preview endpoints..."

# Test API health endpoints
echo "1. Testing API ready endpoint..."
if curl -f -s https://api.barq-preview.tetco.sa/health/ready; then
    echo "✅ API /health/ready - PASS"
else
    echo "❌ API /health/ready - FAIL"
fi

echo "2. Testing API live endpoint..."
if curl -f -s https://api.barq-preview.tetco.sa/health/live; then
    echo "✅ API /health/live - PASS"
else
    echo "❌ API /health/live - FAIL"
fi

echo "3. Testing Frontend endpoint..."
if curl -f -s https://barq-preview.tetco.sa/; then
    echo "✅ Frontend / - PASS"
else
    echo "❌ Frontend / - FAIL"
fi

echo "=== Health check validation complete ==="
