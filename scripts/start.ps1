# ============================================================
#  start.ps1  –  Khởi động hệ thống Load Balancing
#  PRN232 eBay Buyer  |  Nginx + 3 API + 1 Frontend
# ============================================================

param(
    [switch]$Build,      # Force rebuild images
    [switch]$Logs,       # Hiển thị logs sau khi khởi động
    [switch]$Down        # Dừng và xóa containers
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot

Set-Location $ProjectRoot

# ── Màu sắc output ───────────────────────────────────────────────────────────
function Write-Header($msg) { Write-Host "`n=== $msg ===" -ForegroundColor Cyan }
function Write-Ok($msg)     { Write-Host "  [OK] $msg"   -ForegroundColor Green }
function Write-Info($msg)   { Write-Host "  [-] $msg"    -ForegroundColor Yellow }
function Write-Err($msg)    { Write-Host "  [!] $msg"    -ForegroundColor Red }

# ── Kiểm tra Docker ──────────────────────────────────────────────────────────
Write-Header "Kiểm tra môi trường"

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Err "Docker chưa được cài đặt. Tải tại: https://docs.docker.com/desktop/windows/"
    exit 1
}

$dockerVersion = docker --version
Write-Ok "Docker: $dockerVersion"

if (-not (Get-Command "docker" -ErrorAction SilentlyContinue)) {
    Write-Err "Docker Compose v2 chưa khả dụng."
    exit 1
}

# ── Dừng hệ thống ────────────────────────────────────────────────────────────
if ($Down) {
    Write-Header "Dừng và dọn dẹp containers"
    docker compose down --remove-orphans
    Write-Ok "Đã dừng tất cả containers"
    exit 0
}

# ── Build và khởi động ───────────────────────────────────────────────────────
Write-Header "Khởi động hệ thống Load Balancing"
Write-Info "Project root: $ProjectRoot"

$composeArgs = @("compose", "up", "--detach", "--remove-orphans")
if ($Build) {
    $composeArgs += "--build"
    Write-Info "Chế độ: Build + Start"
} else {
    Write-Info "Chế độ: Start (dùng image hiện có, thêm -Build để build lại)"
}

Write-Info "Đang khởi động..."
& docker @composeArgs

if ($LASTEXITCODE -ne 0) {
    Write-Err "Khởi động thất bại! Xem log: docker compose logs"
    exit 1
}

# ── Chờ services healthy ─────────────────────────────────────────────────────
Write-Header "Chờ services khởi động"
Write-Info "Đang chờ health checks... (tối đa 60 giây)"

$timeout = 60
$elapsed = 0
$allHealthy = $false

while ($elapsed -lt $timeout) {
    Start-Sleep -Seconds 5
    $elapsed += 5

    $psOutput = docker compose ps --format json 2>$null
    if ($psOutput) {
        $services = $psOutput | ConvertFrom-Json -ErrorAction SilentlyContinue
        if ($services) {
            $unhealthy = $services | Where-Object { $_.Health -ne "healthy" -and $_.Health -ne "" }
            if ($unhealthy.Count -eq 0) {
                $allHealthy = $true
                break
            }
        }
    }
    Write-Info "  Đang chờ... ($elapsed/$timeout giây)"
}

# ── Hiển thị trạng thái ──────────────────────────────────────────────────────
Write-Header "Trạng thái hệ thống"
docker compose ps

Write-Header "Thông tin truy cập"
Write-Ok "Frontend:     http://localhost"
Write-Ok "API (qua LB): http://localhost/api/health"
Write-Ok "Nginx Status: http://localhost/nginx_status  (chỉ từ Docker network)"

Write-Header "Lệnh hữu ích"
Write-Info "Xem logs tất cả:         docker compose logs -f"
Write-Info "Xem logs Nginx:          docker compose logs -f nginx"
Write-Info "Xem logs API instance 1: docker compose logs -f api1"
Write-Info "Test load balancing:     scripts\test-lb.ps1"
Write-Info "Dừng hệ thống:           scripts\start.ps1 -Down"

if ($Logs) {
    Write-Header "Streaming Logs (Ctrl+C để thoát)"
    docker compose logs -f
}
