const express = require('express');
const dotenv = require('dotenv');

// Ortam değişkenlerini yükle
dotenv.config();

const app = express();
const PORT = process.env.PORT || 3000;

// Basit bir test route'u
app.get('/', (req, res) => {
    res.json({
        message: 'Smart Campus Backend API Çalışıyor!',
        timestamp: new Date()
    });
});

app.listen(PORT, () => {
    console.log(`Sunucu çalışıyor: http://localhost:${PORT}`);
});
