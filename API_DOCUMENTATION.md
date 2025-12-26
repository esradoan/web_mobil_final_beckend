# Smart Campus API Documentation

Base URL: `/api/v1`

## 🔐 Authentication
All requests require `Authorization: Bearer <token>` unless marked [Public].

### Auth Module
- `POST /auth/register`: Create a new student account.
- `POST /auth/login`: [Public] Authenticate and get tokens.
- `POST /auth/refresh-token`: Get new access token using refresh token.

## 📚 Academic Module

### Courses
- `GET /courses`: List all courses.
- `GET /courses/{id}`: Get course details.
- `POST /courses`: (Admin) Create course.
- `PUT /courses/{id}`: (Admin) Update course.

### Sections
- `GET /sections`: List open sections.
- `GET /sections/{id}`: Get section details.
- `GET /sections/{id}/students`: (Faculty) List enrolled students.

### Enrollments
- `POST /enrollments`: Enroll in a section.
- `DELETE /enrollments/{id}`: Drop a course.
- `GET /enrollments/my-courses`: List student's current courses.
- `GET /enrollments/transcript`: Get full academic transcript.

### Grades & Grading
- `POST /enrollments/grade`: (Faculty) Enter/Update grades.
- `GET /enrollments/my-grades`: (Student) View detailed grades.

## 🕒 Attendance Module

### Sessions
- `GET /attendance/sessions`: List sessions for a section.
- `POST /attendance/sessions`: (Faculty) Create access session (QR).
- `PUT /attendance/sessions/{id}/terminate`: End a session.

### Records
- `POST /attendance/qr-code`: (Student) Check-in via QR Code (Geofenced).
- `GET /attendance/sessions/{id}/records`: (Faculty) View attendance list.

## 🔔 Notification Module (Part 4)

- `GET /notifications`: List all notifications (Paged).
- `GET /notifications/unread-count`: Get unread count.
- `PUT /notifications/{id}/read`: Mark specific notification read.
- `PUT /notifications/mark-all-read`: Mark all read.
- `DELETE /notifications/{id}`: Delete notification.

**SignalR Hub**: `/hubs/notifications`
- Events: `ReceiveNotification`

## 📊 Analytics Module (Part 4)

- `GET /analytics/campus`: Campus-wide stats (Admin).
- `GET /analytics/sections/{id}`: Section stats.
- `GET /analytics/sections/{id}/export/pdf`: **Download Attendance Report (PDF)**.
- `GET /analytics/sections/{id}/export/excel`: **Download Attendance Report (Excel)**.

## 📡 IoT Module (Part 4)

- `GET /sensors`: List all campus sensors.
- `GET /sensors/{id}`: Get sensor details.
- `GET /sensors/{id}/history`: Get historical data points.
- `POST /sensors/simulate`: Trigger data simulation.

**SignalR Hub**: `/hubs/sensors`
- Events: `ReceiveSensorData`

## 🍔 Campus Services

### Meals
- `GET /meals/menu`: Get daily/weekly menu.
- `POST /meals/reservations`: Reserve a meal.
- `GET /meals/my-reservations`: List user reservations.

### Events
- `GET /events`: List upcoming events.
- `POST /events/register`: Register for an event.
- `GET /events/my-registrations`: List registered events.

### Wallet
- `GET /wallet`: Get balance.
- `POST /wallet/add`: Add simulated funds.
- `GET /wallet/transactions`: View transaction history.
