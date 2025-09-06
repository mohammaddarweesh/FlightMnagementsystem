# Flight Booking Management System - Documentation

Welcome to the comprehensive documentation for the Flight Booking Management System. This documentation provides everything you need to understand, deploy, and work with the system.

## 📋 Table of Contents

### 🚀 Getting Started
- [**Developer README**](./README.md) - Complete setup and quickstart guide
- [Installation Guide](./installation.md) - Detailed installation instructions
- [Configuration Guide](./configuration.md) - System configuration options

### 📊 API Documentation
- [**OpenAPI Specification**](./openapi.json) - Complete API specification
- [Analytics API Guide](./analytics-api.md) - Detailed analytics endpoints documentation
- [Authentication Guide](./authentication.md) - JWT authentication and authorization

### 🧪 Testing & Examples
- [**Postman Collection**](./postman/) - Ready-to-use API testing collection
  - [Analytics API Collection](./postman/FlightBooking-Analytics-API.postman_collection.json)
  - [Development Environment](./postman/FlightBooking-Development.postman_environment.json)
  - [Production Environment](./postman/FlightBooking-Production.postman_environment.json)
- [**cURL Examples**](./curl/) - Command-line testing examples
  - [Complete Test Suite](./curl/analytics-api-examples.sh)
  - [Quick Examples](./curl/quick-examples.md)

### 🏗️ Architecture & Design
- [System Architecture](./architecture.md) - High-level system design
- [Database Schema](./database-schema.md) - Database design and relationships
- [Analytics Architecture](./analytics-architecture.md) - Analytics system design

### 🚀 Deployment
- [Deployment Guide](./deployment.md) - Production deployment instructions
- [Docker Guide](./docker.md) - Containerization and Docker Compose
- [Environment Configuration](./environment.md) - Environment-specific settings

### 🔧 Development
- [Development Setup](./development-setup.md) - Local development environment
- [Contributing Guidelines](./contributing.md) - How to contribute to the project
- [Code Standards](./code-standards.md) - Coding conventions and best practices

## 🎯 Quick Links

### For Developers
- **Start Here**: [Developer README](./README.md)
- **API Testing**: [Postman Collection](./postman/FlightBooking-Analytics-API.postman_collection.json)
- **Quick Examples**: [cURL Examples](./curl/quick-examples.md)

### For DevOps/Infrastructure
- **Deployment**: [Deployment Guide](./deployment.md)
- **Configuration**: [Environment Configuration](./environment.md)
- **Monitoring**: [Monitoring Guide](./monitoring.md)

### For API Consumers
- **API Specification**: [OpenAPI JSON](./openapi.json)
- **Authentication**: [Authentication Guide](./authentication.md)
- **Rate Limiting**: [Rate Limiting Guide](./rate-limiting.md)

## 🔑 Key Features

### Analytics & Reporting
- **Revenue Analytics**: Track revenue by route, fare class, airline, and time period
- **Booking Analytics**: Monitor booking statuses, conversion rates, and trends
- **Demographics**: Analyze passenger demographics and travel patterns
- **Route Performance**: Evaluate route profitability and operational metrics
- **Data Export**: Export analytics data in CSV, Excel, JSON, and PDF formats
- **Real-time Refresh**: Update analytics data on-demand or via scheduled jobs

### Flight Management
- **Flight Search**: Advanced search with filters and sorting
- **Route Management**: Manage flight routes and schedules
- **Pricing Engine**: Dynamic pricing with multiple strategies
- **Inventory Management**: Seat availability and booking limits

### Booking System
- **Reservation Management**: Create, modify, and cancel bookings
- **Payment Processing**: Secure payment handling
- **Passenger Management**: Manage passenger information and preferences
- **Notification System**: Email and SMS notifications

### System Features
- **Authentication & Authorization**: JWT-based security with role-based access
- **Caching**: Redis-based caching for performance
- **Background Jobs**: Hangfire for scheduled tasks
- **API Documentation**: Comprehensive OpenAPI/Swagger documentation
- **Monitoring**: Health checks and application monitoring

## 🛠️ Technology Stack

### Backend
- **.NET 8**: Modern C# framework
- **ASP.NET Core**: Web API framework
- **Entity Framework Core**: ORM for database access
- **PostgreSQL**: Primary database
- **Redis**: Caching and session storage

### Infrastructure
- **Docker**: Containerization
- **Hangfire**: Background job processing
- **Serilog**: Structured logging
- **Swagger/OpenAPI**: API documentation

### Development Tools
- **Postman**: API testing
- **cURL**: Command-line testing
- **Entity Framework Migrations**: Database versioning

## 📞 Support & Community

### Getting Help
- **Issues**: Report bugs and request features on [GitHub Issues](https://github.com/your-org/flight-booking-system/issues)
- **Discussions**: Join community discussions on [GitHub Discussions](https://github.com/your-org/flight-booking-system/discussions)
- **Documentation**: Check this documentation for answers

### Contributing
- **Code Contributions**: See [Contributing Guidelines](./contributing.md)
- **Documentation**: Help improve documentation
- **Testing**: Report bugs and test new features

### Resources
- **Changelog**: [View release notes](./changelog.md)
- **Roadmap**: [View project roadmap](./roadmap.md)
- **FAQ**: [Frequently asked questions](./faq.md)

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](../LICENSE) file for details.

---

**Last Updated**: January 2024  
**Version**: 1.0.0  
**Maintained by**: Flight Booking Development Team
