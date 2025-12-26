# Deployment Guide

## 🐳 Docker Deployment

### Prerequisites
- Docker & Docker Compose installed on the host.

### 1. Build and Run
We provide a `docker-compose.yml` for easy setup.

```yaml
version: '3.8'
services:
  smartcampus-api:
    build: 
      context: .
      dockerfile: Dockerfile
    ports:
      - "5000:8080"
    environment:
      - ConnectionStrings__DefaultConnection=Server=db;Database=smart_campus_db;User=root;Password=example;
      - JWT__Key=YourSuperSecretKeyThere
    depends_on:
      - db

  db:
    image: mysql:8.0
    environment:
      MYSQL_ROOT_PASSWORD: example
      MYSQL_DATABASE: smart_campus_db
```

Run:
```bash
docker-compose up --build -d
```

## ☁️ Manual / Cloud Deployment (IIS / Azure / AWS)

### Environment Variables
Configure these in your production environment:

| Variable | Description | Example |
| -- | -- | -- |
| `ASPNETCORE_ENVIRONMENT` | Runtime Env | `Production` |
| `ConnectionStrings__DefaultConnection` | DB Connection | `Server=...` |
| `JWT__Key` | Signing Key | `LongRandomString` |
| `JWT__Issuer` | Token Issuer | `SmartCampusAPI` |
| `Email__SmtpHost` | SMTP Server | `smtp.gmail.com` |
| `Email__SmtpUser` | SMTP User | `admin@smartcampus.edu` |
| `SKIP_MIGRATIONS` | Skip DB checks | `false` |

### Database Migrations
On production, the application will attempt to apply migrations automatically. Ensure the DB user has `DDL` permissions (CREATE, ALTER TABLE).
Or, generate a SQL script to run manually:

```bash
dotnet ef migrations script -o deploy.sql
```
