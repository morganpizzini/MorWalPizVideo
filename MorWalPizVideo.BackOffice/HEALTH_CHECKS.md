# Health Checks and Readiness Configuration

This document describes the comprehensive health check and readiness probe implementation for the MorWalPizVideo BackOffice application.

## Overview

The application now includes sophisticated health monitoring with multiple endpoints for different types of health checks:

- **Liveness probes**: Basic application responsiveness
- **Readiness probes**: External dependency availability
- **Startup probes**: Critical startup dependency validation

## Health Check Endpoints

### Available Endpoints

| Endpoint | Purpose | Tags | Description |
|----------|---------|------|-------------|
| `/health/live` | Liveness | `live` | Basic application responsiveness check |
| `/health/ready` | Readiness | `ready` | External dependencies readiness |
| `/health/startup` | Startup | `startup` | Critical startup dependencies |
| `/health` | All checks | All | Complete health status |
| `/alive` | Legacy | `live` | Development-only backward compatibility |

### Response Format

All endpoints return detailed JSON responses:

```json
{
  "status": "Healthy|Degraded|Unhealthy",
  "totalDuration": 123.45,
  "checks": [
    {
      "name": "mongodb",
      "status": "Healthy",
      "description": "MongoDB connection check",
      "duration": 45.67,
      "tags": ["ready", "database"],
      "data": null,
      "exception": null
    }
  ]
}
```

## Implemented Health Checks

### 1. Basic Application Health
- **Name**: `self`
- **Tags**: `live`
- **Purpose**: Ensures the application is running and responsive

### 2. MongoDB Health Check (Production Mode)
- **Name**: `mongodb`
- **Tags**: `ready`, `database`
- **Purpose**: Validates MongoDB connectivity
- **Timeout**: 10 seconds
- **Failure Status**: Unhealthy

### 3. Azure OpenAI Health Check (Production Mode)
- **Name**: `azure-openai`
- **Tags**: `ready`, `external`
- **Purpose**: Checks Azure OpenAI service availability
- **Timeout**: 15 seconds
- **Failure Status**: Degraded

### 4. YouTube API Health Check (Production Mode)
- **Name**: `youtube-api`
- **Tags**: `ready`, `external`
- **Purpose**: Validates YouTube API connectivity
- **Timeout**: 10 seconds
- **Failure Status**: Degraded

### 5. MorWalPiz API Health Check (Production Mode)
- **Name**: `morwalpiz-api`
- **Tags**: `ready`, `internal`
- **Purpose**: Checks internal API connectivity
- **Timeout**: 10 seconds
- **Failure Status**: Degraded

### 6. Blob Storage Readiness (When Configured)

- **Name**: `blob-storage`
- **Tags**: `ready`, `storage`
- **Purpose**: Performs an authorized `GetProperties` operation against the configured public preview container
- **Failure Status**: Unhealthy
- **Diagnostics**: Reports only the service authority, authentication mode on success, or exception type on failure; credentials and private Blob URLs are never returned
- **Registration**: Production/non-mock mode only when an endpoint or connection-string fallback is configured

This container-scoped operation validates the least-privilege role used by each host without requiring account-wide service-property permissions. It is readiness-only and does not alter anonymous preview access.

### 7. Hangfire Health Checks (When Enabled)

Hangfire is disabled in checked-in configuration. While disabled, neither probe below is registered and readiness has no dependency on a Hangfire store or worker. When enabled, startup first requires `ConnectionStrings:HangfireConnection`; the application does not provision the SQL database or run migrations.

#### SQL Server Health Check
- **Name**: `hangfire-sqlserver`
- **Tags**: `ready`, `hangfire`, `database`
- **Purpose**: Validates Hangfire SQL Server database
- **Timeout**: 10 seconds
- **Failure Status**: Unhealthy

#### Hangfire Processing Health Check
- **Name**: `hangfire-processing`
- **Tags**: `ready`, `hangfire`
- **Purpose**: Ensures background job processing is working
- **Timeout**: 5 seconds
- **Failure Status**: Degraded

### 8. Feature Management Health Check
- **Name**: `feature-management`
- **Tags**: `startup`, `configuration`
- **Purpose**: Validates feature flag configuration
- **Failure Status**: Unhealthy

### 9. Mock Services Health Check (Mock Mode)
- **Name**: `mock-services`
- **Tags**: `ready`, `mock`
- **Purpose**: Always healthy in mock mode
- **Status**: Always Healthy

## Configuration

### Feature Flag Integration

Health checks automatically adapt based on feature flags:

- `EnableMock`: Switches between real and mock health checks
- `EnableHangFire`: Adds both Hangfire SQL storage and processing checks; when disabled, adds neither
- `EnableSwagger`: Used for feature management validation

### Environment-Specific Behavior

- **Development**: All endpoints available, including legacy `/alive`
- **Production**: Full health check suite with appropriate timeouts

## Usage Examples

### Kubernetes Deployment

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: morwalpiz-backoffice
spec:
  template:
    spec:
      containers:
      - name: backoffice
        image: morwalpiz-backoffice:latest
        ports:
        - containerPort: 8080
        livenessProbe:
          httpGet:
            path: /health/live
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10
          timeoutSeconds: 5
          failureThreshold: 3
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 5
          timeoutSeconds: 10
          failureThreshold: 3
        startupProbe:
          httpGet:
            path: /health/startup
            port: 8080
          initialDelaySeconds: 10
          periodSeconds: 5
          timeoutSeconds: 5
          failureThreshold: 30
```

### Docker Compose

```yaml
version: '3.8'
services:
  backoffice:
    image: morwalpiz-backoffice:latest
    ports:
      - "8080:8080"
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health/live"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
```

### Monitoring Integration

#### Prometheus Metrics
The health check endpoints can be scraped by Prometheus for monitoring:

```yaml
# prometheus.yml
scrape_configs:
  - job_name: 'morwalpiz-backoffice-health'
    static_configs:
      - targets: ['localhost:8080']
    metrics_path: '/health'
    scrape_interval: 30s
```

#### Custom Monitoring Scripts

```bash
#!/bin/bash
# health-monitor.sh

HEALTH_URL="http://localhost:8080/health/ready"
RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" $HEALTH_URL)

if [ $RESPONSE -eq 200 ]; then
    echo "✅ Application is healthy"
    exit 0
else
    echo "❌ Application is unhealthy (HTTP $RESPONSE)"
    exit 1
fi
```

## Troubleshooting

### Common Issues

1. **MongoDB Connection Failures**
   - Check connection string in configuration
   - Verify MongoDB server availability
   - Check network connectivity and firewall rules

2. **Azure OpenAI Service Unavailable**
   - Verify API key and endpoint configuration
   - Check Azure service status
   - Review request quotas and limits

3. **Hangfire Health Check Failures**
  - Keep `EnableHangFire` disabled until an approved durable SQL store exists
  - Verify the externally provisioned SQL store and `ConnectionStrings:HangfireConnection`
   - Check Hangfire dashboard for job processing status
  - Do not treat local probe success as restart-durability, retry/idempotency, or production readiness evidence

### Health Check Debugging

Enable detailed logging to troubleshoot health check issues:

```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.Extensions.Diagnostics.HealthChecks": "Debug"
    }
  }
}
```

## Security Considerations

- Health check endpoints expose system status information
- In production, consider restricting access to health endpoints
- Use authentication/authorization if sensitive data is exposed
- Monitor for potential information disclosure

## Performance Impact

- Health checks run with appropriate timeouts to prevent blocking
- External service checks are marked as `Degraded` rather than `Unhealthy` to avoid cascading failures
- Database checks use connection pooling for efficiency

## Extension Points

To add custom health checks:

1. Create a new health check class implementing `IHealthCheck`
2. Register it in the `HealthCheckService.ConfigureHealthChecks` method
3. Add appropriate tags for endpoint filtering

```csharp
// Example custom health check
public class CustomServiceHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        // Custom health check logic
        return HealthCheckResult.Healthy("Custom service is operational");
    }
}
