param([string]$OutputPath)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$framework = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
if (!(Test-Path -LiteralPath (Join-Path $framework 'csc.exe'))) {
    $framework = Join-Path $env:WINDIR 'Microsoft.NET/Framework/v4.0.30319'
}
$libs = @('System.Drawing.dll', 'System.Windows.Forms.dll', 'System.Web.Extensions.dll', 'System.Xaml.dll',
    'WPF/PresentationFramework.dll', 'WPF/PresentationCore.dll', 'WPF/WindowsBase.dll') |
    ForEach-Object { '/r:' + (Join-Path $framework $_) }
$output = if ($OutputPath) { $OutputPath } else { Join-Path $root 'NghiLe.exe' }
$icon = Join-Path $root 'src-tauri/icons/icon.ico'
$manifest = Join-Path $PSScriptRoot 'NghiLe.manifest'
$data = Join-Path $root 'data/tet.csv'
$hungKings = Join-Path $root 'data/hung-kings.csv'
$midAutumn = Join-Path $root 'data/mid-autumn.csv'
$artwork = Join-Path $root 'data/peach.json'
$theme = Join-Path $PSScriptRoot 'PanelTheme.xaml'
$sources = 'NghiLe.cs', 'HolidayCountdown.cs', 'HolidayScene.cs', 'HolidayArtwork.cs', 'PeachScene.cs', 'PanelPage.cs', 'SettingsWindow.cs', 'Music.cs', 'MusicChecks.cs', 'StartupShortcut.cs' | ForEach-Object { Join-Path $PSScriptRoot $_ }
& (Join-Path $framework 'csc.exe') /nologo /target:winexe /optimize+ /platform:anycpu /codepage:65001 @libs "/win32icon:$icon" "/win32manifest:$manifest" "/resource:$data,NghiLe.Calendar" "/resource:$hungKings,NghiLe.HungKings" "/resource:$midAutumn,NghiLe.MidAutumn" "/resource:$artwork,NghiLe.Peach" "/resource:$theme,NghiLe.PanelTheme" "/out:$output" @sources
if ($LASTEXITCODE -ne 0) { throw 'Portable build failed.' }
Get-Item -LiteralPath $output | Select-Object Name, Length
