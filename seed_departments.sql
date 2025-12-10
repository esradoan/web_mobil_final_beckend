-- Seed Departments for Smart Campus
-- Run this in MySQL after stopping the API server or directly in phpMyAdmin/MySQL Workbench

INSERT INTO Departments (Id, Name, Code, FacultyName, CreatedAt, IsDeleted) VALUES 
(1, 'Bilgisayar Mühendisliği', 'CENG', 'Mühendislik Fakültesi', '2024-01-01', 0),
(2, 'Elektrik-Elektronik Mühendisliği', 'EE', 'Mühendislik Fakültesi', '2024-01-01', 0),
(3, 'Yazılım Mühendisliği', 'SE', 'Mühendislik Fakültesi', '2024-01-01', 0),
(4, 'İşletme', 'BA', 'İktisadi ve İdari Bilimler Fakültesi', '2024-01-01', 0),
(5, 'Psikoloji', 'PSY', 'Fen-Edebiyat Fakültesi', '2024-01-01', 0);
