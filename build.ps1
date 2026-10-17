# Builds dist\GlitchChop.exe (native window, single file) and dist\index.html (standalone page).
# Needs only Windows: it uses the C# compiler that ships with .NET Framework 4.x and fetches the
# WebView2 SDK from nuget.org on first run.
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$wv2Version = '1.0.4191.47'
$build = Join-Path $root '.build'
$dist = Join-Path $root 'dist'
New-Item -ItemType Directory -Force $build, $dist | Out-Null

# 1. Standalone page: the app source is an HTML fragment (it's also published as a Claude
#    artifact, which adds its own skeleton), so wrap it in a full document here.
$src = [IO.File]::ReadAllText((Join-Path $root 'glitch-chop.html'))
$split = $src.IndexOf('<div class="wrap">')
if ($split -lt 0) { throw 'glitch-chop.html: couldn''t find <div class="wrap">' }
$page = "<!doctype html>`n<html lang=""en"">`n<head>`n<meta charset=""utf-8"">`n<meta name=""viewport"" content=""width=device-width,initial-scale=1,viewport-fit=cover"">`n" +
        $src.Substring(0, $split).Trim() + "`n</head>`n<body>`n" + $src.Substring($split).Trim() + "`n</body>`n</html>`n"
$indexPath = Join-Path $dist 'index.html'
[IO.File]::WriteAllText($indexPath, $page, (New-Object Text.UTF8Encoding $false))

# 2. WebView2 SDK (Microsoft-signed DLLs from the official NuGet package)
$pkgDir = Join-Path $build "webview2-$wv2Version"
if (-not (Test-Path (Join-Path $pkgDir 'lib'))) {
    New-Item -ItemType Directory -Force $pkgDir | Out-Null
    $zip = Join-Path $build "webview2-$wv2Version.zip"
    Write-Host "Downloading WebView2 SDK $wv2Version from nuget.org..."
    Invoke-WebRequest "https://api.nuget.org/v3-flatcontainer/microsoft.web.webview2/$wv2Version/microsoft.web.webview2.$wv2Version.nupkg" -OutFile $zip -UseBasicParsing
    Expand-Archive $zip $pkgDir -Force
    Remove-Item $zip
}
$core = Join-Path $pkgDir 'lib\net462\Microsoft.Web.WebView2.Core.dll'
if (-not (Test-Path $core)) { $core = Join-Path $pkgDir 'lib\net45\Microsoft.Web.WebView2.Core.dll' }
$winforms = Join-Path (Split-Path $core) 'Microsoft.Web.WebView2.WinForms.dll'
$loaders = @{ x64 = 'runtimes\win-x64\native\WebView2Loader.dll'; x86 = 'runtimes\win-x86\native\WebView2Loader.dll'; arm64 = 'runtimes\win-arm64\native\WebView2Loader.dll' }
foreach ($f in @($core, $winforms) + ($loaders.Values | ForEach-Object { Join-Path $pkgDir $_ })) {
    $sig = Get-AuthenticodeSignature $f
    if ($sig.Status -ne 'Valid' -or $sig.SignerCertificate.Subject -notmatch 'O=Microsoft Corporation') {
        throw "Signature check failed for $f ($($sig.Status))"
    }
}

# 3. Compile
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) { $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
$exe = Join-Path $dist 'GlitchChop.exe'
$ico = Join-Path $root 'desktop\app.ico'
$cscArgs = @(
    '/nologo', '/target:winexe', '/platform:anycpu', '/optimize+',
    "/out:$exe", "/win32icon:$ico",
    "/reference:$core", "/reference:$winforms",
    '/reference:System.Windows.Forms.dll', '/reference:System.Drawing.dll',
    "/resource:$indexPath,index.html", "/resource:$ico,app.ico",
    "/resource:$core,Microsoft.Web.WebView2.Core.dll", "/resource:$winforms,Microsoft.Web.WebView2.WinForms.dll"
)
foreach ($a in $loaders.Keys) { $cscArgs += "/resource:$(Join-Path $pkgDir $loaders[$a]),WebView2Loader.$a.dll" }
$cscArgs += (Join-Path $root 'desktop\GlitchChop.cs')
& $csc @cscArgs
if ($LASTEXITCODE -ne 0) { throw "csc failed ($LASTEXITCODE)" }
Write-Host ("Built {0} ({1:N0} KB)" -f $exe, ((Get-Item $exe).Length / 1KB))
