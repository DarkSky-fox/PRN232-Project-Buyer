# Hướng dẫn Load Balancing – PRN232 eBay Buyer

## Kiến trúc tổng quan

```
                         ┌─────────────────────────────────────┐
  Browser Client ──────► │       Nginx  (Port 80)              │
                         │       Reverse Proxy + Load Balancer │
                         └────────────────┬────────────────────┘
                                          │
               ┌──────────────────────────┼──────────────────────────┐
               │                          │                          │
               ▼                          ▼                          ▼
   ┌───────────────────┐      ┌───────────────────┐      ┌───────────────────┐
   │  API Instance 1   │      │  API Instance 2   │      │  API Instance 3   │
   │  (api1:8080)      │      │  (api2:8080)      │      │  (api3:8080)      │
   └────────┬──────────┘      └────────┬──────────┘      └────────┬──────────┘
            │                          │                           │
            └──────────────────────────▼───────────────────────────┘
                              ┌─────────────────┐
                              │   SQL Server    │
                              │ (host machine)  │
                              └─────────────────┘
               ┌──────────────────────────┐
               │    Frontend (5000→8080)  │
               │  Razor Pages + Cookies   │
               └──────────────────────────┘
```

## Yêu cầu

- **Docker Desktop** cho Windows: https://docs.docker.com/desktop/windows/
- **SQL Server** đang chạy trên máy host (port 1433)
- **Port 80** chưa bị chiếm dụng

## Khởi động nhanh

```powershell
# 1. Build images và khởi động toàn bộ hệ thống
.\scripts\start.ps1 -Build

# 2. Kiểm tra trạng thái
docker compose ps

# 3. Test load balancing
.\scripts\test-lb.ps1

# 4. Dừng hệ thống
.\scripts\start.ps1 -Down
```

## Cấu hình Nginx chi tiết

### Upstream Pool (Load Balancing)

File: `nginx/nginx.conf`

```nginx
upstream api_backend {
    least_conn;   # Ưu tiên server ít kết nối nhất

    server api1:8080 weight=1 max_fails=3 fail_timeout=30s;
    server api2:8080 weight=1 max_fails=3 fail_timeout=30s;
    server api3:8080 weight=1 max_fails=3 fail_timeout=30s;

    keepalive 32;   # Connection pool
}
```

### Thuật toán Load Balancing

| Thuật toán | Config | Mô tả |
|---|---|---|
| **Round Robin** | (default, bỏ `least_conn`) | Phân phối lần lượt |
| **Least Connections** | `least_conn;` | Ưu tiên server ít bận ✅ |
| **IP Hash** | `ip_hash;` | Cùng IP → cùng server (sticky) |
| **Weighted** | `weight=2` | Phân theo trọng số |

### Passive Health Check

Nginx tự động đánh dấu server `down` khi:
- Lỗi **3 lần liên tiếp** (`max_fails=3`)
- Bị đánh dấu down trong **30 giây** (`fail_timeout=30s`)
- Sau 30 giây, Nginx thử lại server đó

### Rate Limiting

```nginx
limit_req_zone $binary_remote_addr zone=api_limit:10m rate=20r/s;
```
- Mỗi IP: tối đa **20 request/giây**
- `burst=40`: cho phép burst tạm thời lên 40 req
- Bảo vệ khỏi DDoS và abuse

## Monitoring

### Xem phân phối traffic

```powershell
# Xem access log (hiển thị upstream nào xử lý từng request)
docker compose exec nginx tail -f /var/log/nginx/access.log
```

Output mẫu:
```
172.20.0.1 - - [22/Jul/2026:15:00:01] "GET /api/health" 200 upstream="172.20.0.3:8080" rt=0.003
172.20.0.1 - - [22/Jul/2026:15:00:02] "GET /api/health" 200 upstream="172.20.0.4:8080" rt=0.002
172.20.0.1 - - [22/Jul/2026:15:00:03] "GET /api/health" 200 upstream="172.20.0.5:8080" rt=0.004
```

### Response headers

Mỗi response từ API có header:
```
X-Instance-Id: api-instance-2      ← Instance nào xử lý
X-Handled-By: 172.20.0.4:8080     ← IP:Port của upstream
```

Kiểm tra bằng `curl -I http://localhost/api/health`

### Nginx stub status

```
http://localhost/nginx_status  (chỉ truy cập từ Docker network)
```

## Test Failover

```powershell
# 1. Chạy test baseline (30 requests)
.\scripts\test-lb.ps1 -Requests 30

# 2. Tắt 1 instance
docker compose stop api2

# 3. Test lại - hệ thống vẫn hoạt động với 2 instance
.\scripts\test-lb.ps1 -Requests 20

# 4. Khởi động lại instance
docker compose start api2
```

## Lệnh Docker hữu ích

```powershell
# Xem trạng thái tất cả containers
docker compose ps

# Xem logs theo service
docker compose logs -f nginx
docker compose logs -f api1
docker compose logs -f api2
docker compose logs -f api3
docker compose logs -f frontend

# Restart 1 service
docker compose restart api1

# Scale (nếu muốn thêm instance sau - chú ý phải cập nhật nginx.conf)
# Không dùng docker compose scale vì upstream đã hardcode

# Xem resource usage
docker stats

# Vào container
docker compose exec api1 sh
docker compose exec nginx sh
```

## Troubleshooting

### Lỗi: "502 Bad Gateway"
```powershell
# Kiểm tra API health check
docker compose exec api1 curl http://localhost:8080/health
docker compose logs api1 --tail=50
```

### Lỗi: Không kết nối được SQL Server
```
# Đảm bảo SQL Server cho phép kết nối từ Docker
# SQL Server Configuration Manager → TCP/IP → Enable
# Firewall → Allow port 1433
```

### Lỗi: Port 80 bị chiếm
```powershell
# Kiểm tra ai đang dùng port 80
netstat -ano | findstr :80

# Thay đổi port trong docker-compose.yml:
# ports:
#   - "8080:80"   ← dùng port 8080 thay vì 80
```

### Xem logs chi tiết Nginx
```powershell
docker compose exec nginx cat /var/log/nginx/error.log
```
