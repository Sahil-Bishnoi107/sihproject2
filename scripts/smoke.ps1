<#
  End-to-end demo of BidGuard AI against a running stack (docker compose up -d --build).
  Officer: tender PDF -> AI rules -> approve -> publish. Bidder: official bid + pre-bid simulation.
  Officer: report -> decision. Finally the audit chain is verified.
#>
param(
    [string]$BaseUrl = "http://localhost:8080",
    [string]$ConfigPath = "$PSScriptRoot/../backend/src/BidGuard.API/appsettings.Development.json"
)
$ErrorActionPreference = "Stop"
$samples = Resolve-Path "$PSScriptRoot/../samples"
$seed = (Get-Content $ConfigPath -Raw | ConvertFrom-Json).Seed

function Api($method, $path, $token, $body) {
    $headers = @{}
    if ($token) { $headers.Authorization = "Bearer $token" }
    $json = if ($null -ne $body) { $body | ConvertTo-Json -Depth 10 } else { $null }
    Invoke-RestMethod "$BaseUrl$path" -Method $method -Headers $headers -ContentType "application/json" -Body $json
}

# Multipart upload via HttpClient (works on Windows PowerShell 5.1, which lacks Invoke-RestMethod -Form).
Add-Type -AssemblyName System.Net.Http
function Upload($path, $token, $file, $docType) {
    $client = New-Object System.Net.Http.HttpClient
    try {
        $client.DefaultRequestHeaders.Authorization = New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", $token)
        $content = New-Object System.Net.Http.MultipartFormDataContent
        $bytes = New-Object System.Net.Http.ByteArrayContent(, [System.IO.File]::ReadAllBytes($file))
        $bytes.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::Parse("application/pdf")
        $content.Add($bytes, "file", [System.IO.Path]::GetFileName($file))
        if ($docType) { $content.Add((New-Object System.Net.Http.StringContent($docType)), "docType") }
        $response = $client.PostAsync("$BaseUrl$path", $content).Result
        $body = $response.Content.ReadAsStringAsync().Result
        if (-not $response.IsSuccessStatusCode) { throw "Upload failed ($([int]$response.StatusCode)): $body" }
        return $body | ConvertFrom-Json
    } finally { $client.Dispose() }
}

function WaitFor($what, [scriptblock]$probe, [scriptblock]$done, $timeoutSec = 120) {
    $deadline = (Get-Date).AddSeconds($timeoutSec)
    while ((Get-Date) -lt $deadline) {
        $value = & $probe
        if (& $done $value) { return $value }
        Start-Sleep -Seconds 2
    }
    throw "Timed out waiting for $what"
}

function Step($text) { Write-Host "`n== $text" -ForegroundColor Cyan }

Step "Health"
Invoke-RestMethod "$BaseUrl/health" | Out-Null
Write-Host "API is up"

Step "Officer logs in and creates a tender"
$officer = (Api Post "/api/auth/login" $null @{ email = "officer@bidguard.local"; password = $seed.OfficerPassword }).accessToken
$gem = "GEM/2026/B/" + (Get-Date -Format "MMddHHmmss")
$tender = Api Post "/api/tenders" $officer @{ gemBidNumber = $gem; title = "Supply of 200 laptops"; category = "IT Hardware";
    estimatedValue = 12000000; emdAmount = 240000; bidEndDate = (Get-Date).ToUniversalTime().AddDays(20).ToString("o") }
Write-Host "Tender $($tender.gemBidNumber) created"

Step "Upload tender PDF - AI drafts the rules"
Upload "/api/tenders/$($tender.id)/document" $officer "$samples/tender_sample.pdf" | Out-Null
WaitFor "rule extraction" { Api Get "/api/tenders/$($tender.id)" $officer } { param($t) $t.status -eq "RulesExtracted" } | Out-Null
$rules = Api Get "/api/tenders/$($tender.id)/rules" $officer
$rules | ForEach-Object { Write-Host (" {0,-15} {1,-9} {2}" -f $_.code, $_.status, $_.title) }

Step "Officer approves all rules and publishes"
Api Post "/api/tenders/$($tender.id)/rules/approve-all" $officer | Out-Null
Api Post "/api/tenders/$($tender.id)/publish" $officer | Out-Null
Write-Host "Published with $($rules.Count) rules"

Step "Bidder runs a pre-bid simulation with weak documents"
$bidder = (Api Post "/api/auth/login" $null @{ email = "bidder@shakti.local"; password = $seed.BidderPassword }).accessToken
$sim = Api Post "/api/tenders/$($tender.id)/simulate" $bidder
Upload "/api/bids/$($sim.id)/documents" $bidder "$samples/bid_noncompliant.pdf" "financials" | Out-Null
Api Post "/api/bids/$($sim.id)/submit" $bidder | Out-Null
WaitFor "simulation" { Api Get "/api/bids/$($sim.id)" $bidder } { param($b) $b.status -in "UnderReview", "Failed" } | Out-Null
$simReport = Api Get "/api/bids/$($sim.id)/report" $bidder
Write-Host "Simulation: score $($simReport.bid.overallScore), suggested $($simReport.bid.suggestedDecision)"
Write-Host "Fix list:"
$simReport.fixes | ForEach-Object { Write-Host (" +{0,5} pts  {1}" -f $_.scoreGain, $_.action) }

Step "Bidder submits the official bid with full documents"
$bid = Api Post "/api/tenders/$($tender.id)/bids" $bidder
Upload "/api/bids/$($bid.id)/documents" $bidder "$samples/bid_compliant.pdf" "financials" | Out-Null
Api Post "/api/bids/$($bid.id)/submit" $bidder | Out-Null
WaitFor "evaluation" { Api Get "/api/bids/$($bid.id)" $bidder } { param($b) $b.status -in "UnderReview", "Failed" } | Out-Null

Step "Officer reviews the explainable report"
$report = Api Get "/api/bids/$($bid.id)/report" $officer
Write-Host "Score $($report.bid.overallScore), suggested $($report.bid.suggestedDecision)"
$report.evaluations | ForEach-Object { Write-Host (" {0,-15} {1,-9} {2}" -f $_.ruleCode, $_.effectiveOutcome, $_.explanation) }
$report.verifications | ForEach-Object { Write-Host (" registry {0,-6} {1,-22} {2}" -f $_.source, $_.identifier, $_.status) }

Step "Officer records the final decision"
$decided = Api Post "/api/bids/$($bid.id)/decision" $officer @{ decision = $report.bid.suggestedDecision; remarks = "Verified by the evaluation committee" }
Write-Host "Decision: $($decided.decision)"

Step "Officers cannot see the simulation"
try { Api Get "/api/bids/$($sim.id)" $officer | Out-Null; throw "simulation was visible" } catch { Write-Host "Hidden (404) as expected" }

Step "Audit chain"
$audit = Api Get "/api/audit/verify" $officer
Write-Host "Intact: $($audit.isIntact) ($($audit.entriesChecked) entries)"
if (-not $audit.isIntact) { throw "Audit chain broken" }

Write-Host "`nSmoke test passed." -ForegroundColor Green
