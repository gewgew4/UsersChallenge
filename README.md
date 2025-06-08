# UsersChallenge

A comprehensive .NET 9 Web Api for managing user permissions,
built following Clean Architecture principles with CQRS pattern implementation.
This project demonstrates development practices including containerization,
comprehensive testing and integration with external services.

## Overview

This Api provides functionality to request, modify and retrieve user permissions while maintaining
audit trails through ElasticSearch indexing and Kafka messaging.
The solution is designed with scalability, maintainability and testability as core principles.

## Architecture

The project follows Clean Architecture with clear separation of concerns across multiple layers:

- **Api**: Web Api controllers, middleware and configuration
- **Application**: Business logic, CQRS handlers, validation and Dtos
- **Infrastructure**: Data access, external service integrations (ElasticSearch, Kafka)
- **Domain**: Core entities
- **Common**: Shared constants, exceptions and Dtos
- **Tests**: Unit and integration tests

## Key technologies

- **.NET 9** - Latest (non-preview) framework
- **Entity Framework Core** - ORM with SQL Server provider
- **MediatR** - CQRS and mediator pattern implementation
- **AutoMapper** - Object mapping
- **FluentValidation** - Input validation with fluent syntax
- **ElasticSearch** - Document indexing and search capabilities
- **Apache Kafka** - Event streaming and messaging
- **Serilog** - Structured logging
- **Docker** - Containerization and orchestration
- **xUnit + Testcontainers** - Testing framework with real dependencies

## Api endpoints

### Request permission
```http
POST /api/permissions
Content-Type: application/json

{
  "employeeForename": "John",
  "employeeSurname": "Doe",
  "permissionTypeId": 1,
  "permissionDate": "2025-12-25"
}
```

### Modify permission
```http
PUT /api/permissions/{id}
Content-Type: application/json

{
  "employeeForename": "Jane",
  "employeeSurname": "Smith",
  "permissionTypeId": 2,
  "permissionDate": "2025-12-30"
}
```

### Get all permissions
```http
GET /api/permissions
```

### Get permission by Id
```http
GET /api/permissions/{id}
```

### Health check
```http
GET /health
```

## Database schema

### Permissions table
- `Id` (int, PK, Identity)
- `EmployeeForename` (nvarchar(50), required)
- `EmployeeSurname` (nvarchar(50), required)
- `PermissionTypeId` (int, FK, required)
- `PermissionDate` (date, required)

### PermissionTypes table
- `Id` (int, PK, Identity)
- `Description` (nvarchar(100), required, indexed)

## Getting Started

### Prerequisites
- Docker (for desktop may help)
- .NET 9 SDK (for development)
- Git

### Running with Docker compose

```bash
# Clone the repository
git clone https://github.com/gewgew4/UsersChallenge.git
cd UsersChallenge

# Start all services
docker-compose up -d

# Check service health
curl http://localhost:8080/health
```

## Accessing services and UIs

After running `docker-compose up -d`, the following services and user interfaces will be available:

### Api and documentation
- **OpenApi documentation (Scalar)**: http://localhost:8080/scalar/v1
  - Interactive Api documentation and testing interface
  - Allows you to test all endpoints directly from the browser
- **Health Check**: http://localhost:8080/health

### Monitoring and management UIs

#### Kafka UI
- **URL**: http://localhost:8082/ui/clusters/local/all-topics
- **Purpose**: Monitor Kafka topics, messages and broker health
- **Features**: View messages in `permissions-operations` topic, consumer groups, topic management

#### Kibana (ElasticSearch UI)
- **URL**: http://localhost:5601/app/dev_tools#/console
- **Purpose**: ElasticSearch data exploration and index management
- **Features**: Query permissions index, monitor cluster health, data visualization

#### Database access
- **SQL Server**: localhost:1433
- **Tools**: Use SQL Server Management Studio or any SQL client

## Configuration

### Environment Variables

| Variable | Description |
|----------|-------------|
| `ConnectionStrings__DefaultConnection` | SQL Server connection string |
| `ElasticSearch__Uri` | ElasticSearch endpoint |
| `ElasticSearch__IndexName` | Index name for permissions |
| `Kafka__BootstrapServers` | Kafka broker addresses |
| `Kafka__TopicName` | Topic for operation messages |

## Services and infrastructure

### ElasticSearch
- **Purpose**: Document indexing for enhanced search capabilities
- **Index**: `permissions` with mappings for all permission fields
- **Operations**: Index on create, update on modify, search by employee names

### Apache Kafka
- **Purpose**: Event streaming for operation audit trails
- **Topic**: `permissions-operations`
- **Message Format**: `{ "Id": "guid", "NameOperation": "get|modify|request" }`

### SQL Server
- **Purpose**: Primary data storage with Entity Framework
- **Features**: Entity relationships, constraints, indexing, migrations

## Monitoring and observability

### Health checks
- Database connectivity
- ElasticSearch cluster health
- Kafka broker availability
- Overall application health at `/health`

### Logging
- Structured logging with Serilog
- Request/response logging middleware
- Operation-specific log entries
- Error tracking and correlation

## Testing strategy

### Unit tests
- Command and query handlers with mocked dependencies
- Repository pattern implementations
- Validation logic verification
- Business rule enforcement

### Integration tests
- End-to-end Api testing with Testcontainers
- Real database, ElasticSearch and Kafka instances
- Error handling and edge case scenarios
- Performance and concurrency testing

## Assumptions and observations

### Service resilience
Both ElasticSearch and Kafka are considered **non-critical dependencies**.
If either service is unavailable, the core Api functionality continues to
operate normally. The application gracefully handles failures by:
- Logging errors when ElasticSearch indexing fails
- Continuing operation when Kafka message production fails
- Not throwing exceptions that would break the main workflow

### Permission types management
Permission types are **pre-seeded and considered static**. The system assumes:
- Initial permission types are created through database seeding
- New types would be added via database migrations rather than Api endpoints
- No runtime CRUD operations for permission types are expected
- The current implementation seeds 4 permission types (First type, Second type, Third type, Fourth type)

### Data validation assumptions
- Employee names contain only valid characters
- Permission dates cannot be in the past
- Unicode characters are supported for international names
- Maximum field lengths align with database constraints

### Security considerations
- Api endpoints are not secured with authentication or authorization
	- This is a development convenience for testing purposes
	- Cognito or similar authentication services may be added later
- Hardcoded unsafe passwords like "YourStrong@Passw0rd" should not be used in production
	- Some secrets management solution should be implemented
- Ports are default and may need to be changed for production