# IT Asset Management System

A full-stack web application for managing IT assets within an organization. The system provides centralized management of hardware, software, licenses, users, departments, and approval workflows, helping organizations efficiently track IT resources and control asset-related operations.

---

## Features

### Asset Management
- Manage Hardware assets
- Manage Software assets
- Manage Software Licenses
- Search, filter, and paginate asset lists
- View detailed asset information

### Request & Approval Workflow
- Create Asset Add Requests
- Create Asset Edit Requests
- Create Asset Delete Requests
- Multi-level approval workflow
- Approve or reject requests with remarks
- Request history tracking

### User & Department Management
- User CRUD operations
- Department CRUD operations
- Assign users to departments
- View department members
- Role & Permission management

### Permission System
Role-based access control including:

- Admin
- Administrator
- Manager
- Department Representative
- General Staff

Permissions include:

- View assets
- Create requests
- Approve requests
- System administration

### Dashboard
- Asset statistics
- Hardware status overview
- Request statistics
- Pending approval summary
- Interactive charts using Chart.js

### Data Import / Export
- Import Hardware data
- Import Software data
- Import License data
- Export data to Excel

### Authentication
- JWT Authentication
- Login
- Change Password
- Authorization based on user permissions

---

# Technology Stack

## Backend

- ASP.NET Core Web API
- C#
- Entity Framework Core
- MySQL
- JWT Authentication
- RESTful API

## Frontend

- HTML5
- CSS3
- JavaScript (Vanilla JS)
- Chart.js

---

# System Architecture

```
Frontend (HTML/CSS/JavaScript)
            │
            │ REST API
            ▼
ASP.NET Core Web API
            │
            ▼
     Entity Framework Core
            │
            ▼
          MySQL
```

---

# Main Modules

- Dashboard
- Hardware Management
- Software Management
- License Management
- User Management
- Department Management
- Request Management
- Approval Workflow
- Permission Management
- Import / Export

---

# Approval Workflow

```
Manager
     │
     ▼
Create Add/Edit/Delete Request
     │
     ▼
Administrator
     │
Approve / Reject
     │
     ▼
Asset Updated
```

---

# Project Structure

```
ITAssetManagement
│
├── Controllers
├── Models
├── DTOs
├── Services
├── Data
├── Middleware
├── Frontend
│   ├── Dashboard
│   ├── Hardware
│   ├── Software
│   ├── License
│   ├── User
│   ├── Department
│   └── Import
│
└── Database
```

---

# Installation

## Clone repository

```bash
git clone https://github.com/yourusername/ITAssetManagement.git
```

## Backend

```bash
cd ITAssetManagement
```

Update the connection string in:

```
appsettings.json
```

Run database migration (or import the SQL database).

Start the API:

```bash
dotnet run
```

---

## Frontend

Open the frontend project or serve it using any local web server.

Example:

```
http://localhost:5500
```

Update API endpoint if necessary:

```javascript
const API = "http://localhost:5288/api";
```

---

# Screenshots

You can add screenshots here.

Example:

```
screenshots/
    dashboard.png
    hardware.png
    software.png
    license.png
    requests.png
```

---

# Future Improvements

- Email notifications
- Audit logs
- QR Code / Barcode support
- Asset assignment history
- Responsive UI
- Docker deployment
- CI/CD pipeline

---

# Author

**Ngoc Le**


---

# License

This project is developed for educational and portfolio purposes.
