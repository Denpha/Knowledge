# KMS — คู่มือติดตั้งใหม่ (Disaster Recovery)

> ระบบจัดการองค์ความรู้ มทร.อีสาน วิทยาเขตสกลนคร  
> .NET 10 API + React (Vite + TailwindCSS) + PostgreSQL + MinIO

---

## ความต้องการของระบบ

| เครื่องมือ | เวอร์ชัน | ที่ติดตั้ง |
|---|---|---|
| .NET SDK | 10.x | https://dotnet.microsoft.com/download |
| Node.js | 24.x | https://nodejs.org |
| PostgreSQL | 18.x | `apt install postgresql` |
| MinIO | latest | https://min.io/download |

---

## ขั้นตอนติดตั้ง

### 1. Clone โปรเจค

```bash
git clone https://github.com/Denpha/Knowledge.git
cd Knowledge
```

### 2. ตั้งค่า PostgreSQL

```bash
sudo -u postgres psql -c "CREATE DATABASE \"KMS\";"
sudo -u postgres psql -c "ALTER USER postgres PASSWORD '123456';"
```

### 3. ตั้งค่า MinIO

```bash
# สร้าง data directory
mkdir -p ~/minio-data

# สร้าง systemd service สำหรับ MinIO
mkdir -p ~/.config/systemd/user
cat > ~/.config/systemd/user/minio.service << 'EOF'
[Unit]
Description=MinIO Object Storage
After=network.target

[Service]
Type=simple
Environment=MINIO_ROOT_USER=minioadmin
Environment=MINIO_ROOT_PASSWORD=minioadmin123
ExecStart=/usr/local/bin/minio server /home/denpha/minio-data --console-address :9001
Restart=always
RestartSec=5

[Install]
WantedBy=default.target
EOF

systemctl --user daemon-reload
systemctl --user enable --now minio
```

เข้า MinIO Console: http://localhost:9001  
- User: `minioadmin` / Password: `minioadmin123`
- สร้าง bucket: `kms`  
- ตั้ง bucket policy เป็น `public` (สำหรับไฟล์ที่ต้องการ public URL)

### 4. ตั้งค่า API

```bash
# Restore database (ถ้ามี backup)
psql -U postgres KMS < backup/kms_latest.sql

# รัน migration (ถ้าไม่มี backup)
cd src/KMS.Api
dotnet ef database update

# Build และรัน API
cd /path/to/Knowledge
dotnet build src/KMS.Api/KMS.Api.csproj -c Release

# Publish สำหรับ systemd service
dotnet publish src/KMS.Api/KMS.Api.csproj -c Release -o publish/KMS.Api
```

สร้าง systemd user service:

```bash
cat > ~/.config/systemd/user/kms-api.service << 'EOF'
[Unit]
Description=KMS .NET API
After=network.target postgresql.service

[Service]
Type=notify
WorkingDirectory=/home/denpha/Knowledge/publish/KMS.Api
ExecStart=/usr/bin/dotnet /home/denpha/Knowledge/publish/KMS.Api/KMS.Api.dll
Restart=always
RestartSec=5
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://0.0.0.0:5000

[Install]
WantedBy=default.target
EOF

systemctl --user daemon-reload
systemctl --user enable --now kms-api

# Auto-start เมื่อ reboot (ไม่ต้อง login)
loginctl enable-linger denpha
```

### 5. ตั้งค่า Frontend

```bash
cd kms-web
npm install
npm run dev        # development
npm run build      # production build → dist/
```

ตัวแปรสำคัญใน `kms-web/src/services/api.ts`:
- API URL auto-detect จาก `window.location.hostname` → `:5000`

---

## ค่าตั้งต้น (appsettings.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=KMS;Username=postgres;Password=123456"
  },
  "MinIO": {
    "Endpoint": "localhost:9000",
    "AccessKey": "minioadmin",
    "SecretKey": "minioadmin123",
    "Secure": false,
    "Bucket": "kms",
    "PublicUrl": "http://localhost:9000/kms"
  },
  "Jwt": {
    "Issuer": "KMS-API",
    "Audience": "KMS-Client",
    "SecretKey": "<ดูใน appsettings.json จริง>"
  }
}
```

---

## Backup

```bash
# รัน backup ด้วยตนเอง
~/backup-kms.sh

# ดู backup ที่มี
ls -lh ~/kms-backups/
```

Backup จะถูกสร้างอัตโนมัติทุกวันเวลา 02:00 (cron)

---

## การตรวจสอบระบบ

```bash
# ดู status service
systemctl --user status kms-api
systemctl --user status minio

# ดู logs API
journalctl --user -u kms-api -f
# หรือ
tail -f /home/denpha/Knowledge/logs/kms-*.log

# ทดสอบ API
curl http://localhost:5000/api/health

# Hangfire dashboard
# http://localhost:5000/hangfire
```

---

## Port ที่ใช้

| Service | Port |
|---|---|
| KMS API | 5000 |
| MinIO API | 9000 |
| MinIO Console | 9001 |
| Frontend (dev) | 5173 |
