# ============================================================
#  test-lb.ps1  –  Kiểm tra Load Balancing hoạt động
#  Gửi N requests và thống kê phân phối giữa các instance
# ============================================================

param(
    [int]$Requests   = 30,                     # Số lượng request gửi
    [string]$BaseUrl = "http://localhost",      # URL Nginx
    [switch]$Verbose                           # Hiển thị từng response
)

$ErrorActionPreference = "Continue"

function Write-Header($msg) { Write-Host "`n=== $msg ===" -ForegroundColor Cyan }
function Write-Ok($msg)     { Write-Host "  [OK] $msg"   -ForegroundColor Green }
function Write-Err($msg)    { Write-Host "  [!] $msg"    -ForegroundColor Red }

Write-Header "Load Balancing Test"
Write-Host "  URL:      $BaseUrl/api/health" -ForegroundColor White
Write-Host "  Requests: $Requests" -ForegroundColor White

$results    = @{}
$errors     = 0
$totalMs    = 0
$stopwatch  = [System.Diagnostics.Stopwatch]::StartNew()

for ($i = 1; $i -le $Requests; $i++) {
    try {
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        $response = Invoke-RestMethod -Uri "$BaseUrl/api/health" -Method GET -TimeoutSec 10
        $sw.Stop()

        $instance = $response.instance
        $ms       = $sw.ElapsedMilliseconds
        $totalMs += $ms

        if ($results.ContainsKey($instance)) {
            $results[$instance]++
        } else {
            $results[$instance] = 1
        }

        if ($Verbose) {
            Write-Host "  [$i] → $instance  (${ms}ms)" -ForegroundColor DarkGray
        }
    } catch {
        $errors++
        if ($Verbose) {
            Write-Err "  [$i] → ERROR: $_"
        }
    }
}

$stopwatch.Stop()

# ── Kết quả ──────────────────────────────────────────────────────────────────
Write-Header "Kết quả phân phối"

$success = $Requests - $errors
$avgMs   = if ($success -gt 0) { [math]::Round($totalMs / $success) } else { 0 }

foreach ($instance in $results.Keys | Sort-Object) {
    $count   = $results[$instance]
    $percent = [math]::Round($count / $Requests * 100, 1)
    $bar     = "#" * [math]::Round($percent / 2)
    Write-Host ("  {0,-20} {1,3} requests  ({2,5}%)  {3}" -f $instance, $count, $percent, $bar) -ForegroundColor Green
}

Write-Host ""
Write-Host "  Total requests : $Requests" -ForegroundColor White
Write-Host "  Successful     : $success"  -ForegroundColor Green
Write-Host "  Errors         : $errors"   -ForegroundColor $(if ($errors -gt 0) { "Red" } else { "Green" })
Write-Host "  Avg response   : ${avgMs}ms" -ForegroundColor White
Write-Host "  Total time     : $($stopwatch.ElapsedMilliseconds)ms" -ForegroundColor White

# ── Kiểm tra phân phối đều ───────────────────────────────────────────────────
Write-Header "Đánh giá"
$instanceCount = $results.Count
if ($instanceCount -eq 0) {
    Write-Err "Không có instance nào phản hồi!"
} elseif ($errors -gt 0) {
    Write-Host "  [WARNING] Có $errors request lỗi. Kiểm tra logs: docker compose logs" -ForegroundColor Yellow
} else {
    $expected = [math]::Round($Requests / $instanceCount)
    $balanced = $true
    foreach ($count in $results.Values) {
        if ([math]::Abs($count - $expected) -gt ($expected * 0.3)) {
            $balanced = $false
        }
    }

    if ($balanced) {
        Write-Ok "Load balancing hoạt động tốt! Phân phối đồng đều giữa $instanceCount instances."
    } else {
        Write-Host "  [INFO] Phân phối không hoàn toàn đều (bình thường với số request nhỏ)." -ForegroundColor Yellow
    }
}

# ── Test failover (tắt 1 instance) ───────────────────────────────────────────
Write-Host ""
Write-Host "Để test failover:" -ForegroundColor Cyan
Write-Host "  docker compose stop api1" -ForegroundColor DarkGray
Write-Host "  .\scripts\test-lb.ps1 -Requests 15" -ForegroundColor DarkGray
Write-Host "  docker compose start api1" -ForegroundColor DarkGray
