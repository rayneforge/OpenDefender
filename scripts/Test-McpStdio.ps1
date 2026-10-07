param(
    [Parameter(Mandatory)][string]$PublishDirectory,
    [string]$PackageDirectory,
    [string]$PackageVersion
)
$ErrorActionPreference = 'Stop'
$published = (Resolve-Path -LiteralPath $PublishDirectory).Path
$executable = if ($IsWindows) { 'Service.exe' } else { 'Service' }
$testRoot = Join-Path (Split-Path $published -Parent) ('smoke-' + [guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $testRoot
if ($PackageDirectory) {
    if (-not $PackageVersion) { throw 'PackageVersion is required for package execution' }
    $packageSource = (Resolve-Path -LiteralPath $PackageDirectory).Path
} else {
    # Only the executable is copied: no config or native DLL sidecars.
    Copy-Item -LiteralPath (Join-Path $published $executable) -Destination $testRoot
    if (-not $IsWindows) { & chmod +x (Join-Path $testRoot $executable) }
}
function Read-Response($Process, [int]$Id) {
    $read = $Process.StandardOutput.ReadLineAsync()
    if (-not $read.Wait(30000)) { throw "Request $Id timed out" }
    if ($null -eq $read.Result) { throw "Server closed stdout during request $Id" }
    $response = $read.Result | ConvertFrom-Json
    if ($response.jsonrpc -ne '2.0' -or $response.id -ne $Id -or $response.error) {
        throw "Unexpected response: $($read.Result)"
    }
    return $response
}
foreach ($mode in @('default', 'stdio', 'legacy-urls')) {
    $psi = [System.Diagnostics.ProcessStartInfo]::new()
    if ($PackageDirectory) {
        $psi.FileName = 'dotnet'
        foreach ($argument in @('tool', 'exec', "Rayneforge.OpenDefender@$PackageVersion", '--source', $packageSource)) {
            $psi.ArgumentList.Add($argument)
        }
    } else { $psi.FileName = Join-Path $testRoot $executable }
    $psi.WorkingDirectory = (Get-Location).Path
    $psi.UseShellExecute = $false
    $psi.CreateNoWindow = $true
    $psi.RedirectStandardInput = $true
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.Environment.Remove('Service__TransportType') | Out-Null
    if ($mode -eq 'stdio') { $psi.Environment['Service__TransportType'] = 'stdio' }
    if ($mode -eq 'legacy-urls') { $psi.Environment['ASPNETCORE_URLS'] = 'http://0.0.0.0:5297'; $psi.ArgumentList.Add('--urls=http://0.0.0.0:5297') }
    $dataDirectory = Join-Path $testRoot "data-$mode"
    $psi.Environment['OPENDEFENDER_DATA_DIR'] = $dataDirectory
    $process = [System.Diagnostics.Process]::new()
    $process.StartInfo = $psi
    $stderr = $null
    try {
        $null = $process.Start()
        $stderr = $process.StandardError.ReadToEndAsync()
        $process.StandardInput.WriteLine('{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-11-25","capabilities":{},"clientInfo":{"name":"stdio-smoke","version":"1"}}}')
        $initialize = Read-Response $process 1
        if ($IsWindows) {
            $listeners = @(Get-NetTCPConnection -State Listen -ErrorAction SilentlyContinue | Where-Object OwningProcess -eq $process.Id)
            if ($listeners.Count) { throw 'Stdio process opened a TCP listener' }
        }
        if (-not $initialize.result.capabilities.tools) { throw 'Missing tools capability' }
        $process.StandardInput.WriteLine('{"jsonrpc":"2.0","method":"notifications/initialized"}')
        $process.StandardInput.WriteLine('{"jsonrpc":"2.0","id":2,"method":"tools/list"}')
        $tools = Read-Response $process 2
        if ('query_resource_metrics' -notin $tools.result.tools.name) { throw 'Resource tool missing' }
        foreach ($tool in $tools.result.tools) {
            if ($tool.annotations.readOnlyHint -ne $true -or $tool.annotations.destructiveHint -ne $false -or $tool.annotations.openWorldHint -ne $false) {
                throw "Missing read-only/local annotations: $($tool.name)"
            }
        }
        $process.StandardInput.WriteLine('{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"query_resource_metrics","arguments":{"request":{"top":3}}}}')
        $call = Read-Response $process 3
        if ($call.result.isError -or -not $call.result.content) { throw 'Resource query failed' }
        $null = $call.result.content[0].text | ConvertFrom-Json
        $process.StandardInput.WriteLine('{"jsonrpc":"2.0","id":4,"method":"prompts/list"}')
        $prompts = Read-Response $process 4
        if ('infrastructure-health-check' -notin $prompts.result.prompts.name) { throw 'Prompt missing' }
        $process.StandardInput.WriteLine('{"jsonrpc":"2.0","id":5,"method":"tools/call","params":{"name":"query_network_connections","arguments":{"request":{"top":3}}}}')
        $connections = Read-Response $process 5
        if ($connections.result.isError) { throw 'Live TCP query failed' }
        $null = $connections.result.content[0].text | ConvertFrom-Json
        $process.StandardInput.Close()
        if (-not $process.WaitForExit(10000)) { throw 'Server did not exit within 10 seconds of stdin EOF' }
        if ($process.ExitCode -ne 0) { throw "Server exited with code $($process.ExitCode)" }
        if ($process.StandardOutput.ReadToEnd().Trim()) { throw 'Unexpected trailing stdout' }
        if ($stderr.Result -match 'Now listening on:') { throw 'Stdio started an HTTP listener' }
        foreach ($database in @('diagnostic_reports.db', 'analytics_reports.db')) {
            if (-not (Test-Path (Join-Path $dataDirectory $database))) { throw "Missing database: $database" }
        }
        Write-Output "PASS $mode`: initialize, read-only annotations, queries, live TCP snapshot, prompts, stdin EOF"
    } catch {
        if (-not $process.HasExited) { $process.Kill($true); $process.WaitForExit() }
        if ($stderr) { Write-Output ($stderr.Result -split "`n" | Select-Object -Last 15) }
        throw
    } finally {
        if (-not $process.HasExited) { $process.Kill($true); $process.WaitForExit() }
        $process.Dispose()
    }
}

# Both historical configuration entry points must fail before starting a host.
foreach ($source in @('environment', 'command-line')) {
    $psi.Environment.Remove('ASPNETCORE_URLS') | Out-Null
    $psi.Environment['Service__TransportType'] = if ($source -eq 'environment') { 'Http' } else { 'Stdio' }
    if ($source -eq 'command-line') { $psi.ArgumentList.Add('--Service:TransportType=Http') }
    $process = [System.Diagnostics.Process]::Start($psi)
    $stderr = $process.StandardError.ReadToEndAsync()
    $stdout = $process.StandardOutput.ReadToEndAsync()
    try {
        $process.StandardInput.Close()
        if (-not $process.WaitForExit(10000)) { throw 'Legacy HTTP setting did not exit promptly' }
        if ($process.ExitCode -eq 0 -or $stderr.Result -notmatch 'supports only Stdio') { throw 'Legacy HTTP setting was not rejected' }
        if ($stdout.Result.Trim()) { throw 'Unexpected stdout when rejecting HTTP' }
        Write-Output "PASS HTTP $source setting rejected"
    } finally {
        if (-not $process.HasExited) { $process.Kill($true); $process.WaitForExit() }
        $process.Dispose()
    }
}
