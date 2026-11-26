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
$birthday = Join-Path $root 'data/birthday.png'
$hacLongWalk = Join-Path $root 'data/hac-long-walk.png'
$theme = Join-Path $PSScriptRoot 'PanelTheme.xaml'
$sources = 'NghiLe.cs', 'CountdownUnits.cs', 'HolidayCountdown.cs', 'HolidayScene.cs', 'HolidayArtwork.cs', 'PeachScene.cs', 'HolidayDiorama.cs', 'HolidayDioramaMotion.cs', 'TetDiorama.cs', 'HolidayDioramaModels.cs',
    'PersonalDate.cs', 'PersonalDateArtwork.cs', 'PersonalDatesPage.cs', 'PanelPage.cs', 'SettingsWindow.cs',
    'OccasionReminders.cs', 'PinnedOccasions.cs', 'UpcomingPage.cs', 'MonthCalendarView.cs', 'VietnameseCalendar.cs',
    'PetArtwork.cs', 'TaskbarPet.cs',
    'Music.cs', 'MusicChecks.cs', 'StartupShortcut.cs', 'WidgetPower.cs', 'SettingsBackup.cs', 'CompanionWidgets.cs' | ForEach-Object { Join-Path $PSScriptRoot $_ }
$lunarMonths = Join-Path $root 'data/lunar-months.csv'
$resources = "/resource:$lunarMonths,NghiLe.LunarMonths", "/resource:$data,NghiLe.Calendar", "/resource:$hungKings,NghiLe.HungKings", "/resource:$midAutumn,NghiLe.MidAutumn",
    "/resource:$artwork,NghiLe.Peach", "/resource:$birthday,NghiLe.Birthday", "/resource:$hacLongWalk,NghiLe.HacLongWalk",
    "/resource:$theme,NghiLe.PanelTheme"
$compiler = Join-Path $framework 'csc.exe'
$staging = Join-Path ([IO.Path]::GetTempPath()) ('NghiLe-dioramas-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $staging | Out-Null
try {
    $baker = Join-Path $staging 'BakeDioramas.exe'
    & $compiler /nologo /target:exe /optimize+ /platform:anycpu /codepage:65001 @libs "/out:$baker" `
        /main:NghiLe.BakeDioramas @sources (Join-Path $PSScriptRoot 'BakeDioramas.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Diorama baker build failed.' }

    & $baker $staging
    if ($LASTEXITCODE -ne 0) { throw 'Diorama baking failed.' }
    $dioramas = @(Get-ChildItem -LiteralPath $staging -File -Filter '*.diorama')
    if ($dioramas.Count -ne 10) { throw 'Diorama archives are incomplete.' }
    $dioramaResources = $dioramas | ForEach-Object { "/resource:$($_.FullName),NghiLe.Diorama.$($_.BaseName)" }
    & $compiler /nologo /target:winexe /optimize+ /platform:anycpu /codepage:65001 @libs @resources @dioramaResources `
        "/win32icon:$icon" "/win32manifest:$manifest" "/out:$output" @sources
    if ($LASTEXITCODE -ne 0) { throw 'Portable build failed.' }
}
finally {
    Get-ChildItem -LiteralPath $staging -File | ForEach-Object { Remove-Item -LiteralPath $_.FullName }
    Remove-Item -LiteralPath $staging
}
Get-Item -LiteralPath $output | Select-Object Name, Length
