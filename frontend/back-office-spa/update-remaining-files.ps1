# PowerShell script to update remaining API call files
# This script updates all files listed in update-api-calls.md

Write-Host "Starting API calls update process..." -ForegroundColor Green

# Calendar Events - Remaining files
$calendarEventsFiles = @(
    "src/routes/calendarEvents/create/action.ts",
    "src/routes/calendarEvents/edit/loader.ts",
    "src/routes/calendarEvents/edit/action.ts",
    "src/routes/calendarEvents/detail/loader.ts",
    "src/routes/calendarEvents/detail/action.ts"
)

# Custom Forms
$customFormsFiles = @(
    "src/routes/customForms/index/loader.ts",
    "src/routes/customForms/index/action.ts",
    "src/routes/customForms/form/loader.ts",
    "src/routes/customForms/detail/loader.ts"
)

# Categories
$categoriesFiles = @(
    "src/routes/categories/index/action.ts",
    "src/routes/categories/create/action.ts",
    "src/routes/categories/edit/action.ts"
)

# Channels
$channelsFiles = @(
    "src/routes/channels/index/loader.ts",
    "src/routes/channels/index/action.ts",
    "src/routes/channels/create/action.ts",
    "src/routes/channels/edit/action.ts",
    "src/routes/channels/detail/loader.ts"
)

# Configurations
$configurationsFiles = @(
    "src/routes/morwalpizconfigurations/index/loader.ts",
    "src/routes/morwalpizconfigurations/index/action.ts",
    "src/routes/morwalpizconfigurations/create/action.ts",
    "src/routes/morwalpizconfigurations/edit/action.ts",
    "src/routes/morwalpizconfigurations/detail/loader.ts"
)

# Query Links
$queryLinksFiles = @(
    "src/routes/queryLinks/index/loader.ts",
    "src/routes/queryLinks/index/action.ts",
    "src/routes/queryLinks/create/action.ts"
)

# Short Links
$shortLinksFiles = @(
    "src/routes/shortLinks/index/action.ts",
    "src/routes/shortLinks/create/action.ts",
    "src/routes/shortLinks/edit/action.ts",
    "src/routes/shortLinks/detail/loader.ts"
)

# Videos
$videosFiles = @(
    "src/routes/videos/detail/loader.ts",
    "src/routes/videos/edit/action.ts",
    "src/routes/videos/import/loader.ts",
    "src/routes/videos/import/action.ts",
    "src/routes/videos/create-root/loader.ts",
    "src/routes/videos/create-root/action.ts",
    "src/routes/videos/create-sub-video/loader.ts",
    "src/routes/videos/create-sub-video/action.ts",
    "src/routes/videos/convert-to-root/loader.ts",
    "src/routes/videos/convert-to-root/action.ts",
    "src/routes/videos/swap-thumbnail/loader.ts",
    "src/routes/videos/swap-thumbnail/action.ts",
    "src/routes/videos/translate/action.ts"
)

# Images  
$imagesFiles = @(
    "src/routes/images/upload/loader.ts",
    "src/routes/images/upload/action.ts",
    "src/routes/images/upload-multiple/loader.ts",
    "src/routes/images/upload-multiple/action.ts"
)

$allFiles = $calendarEventsFiles + $customFormsFiles + $categoriesFiles + $channelsFiles + `
            $configurationsFiles + $queryLinksFiles + $shortLinksFiles + $videosFiles + $imagesFiles

Write-Host "Total files to check: $($allFiles.Count)" -ForegroundColor Cyan

$filesNeedingUpdate = @()

foreach ($file in $allFiles) {
    if (Test-Path $file) {
        $content = Get-Content $file -Raw
        if ($content -match 'fetch\s*\(') {
            $filesNeedingUpdate += $file
            Write-Host "  - $file" -ForegroundColor Yellow
        }
    }
}

Write-Host "`nFiles needing update: $($filesNeedingUpdate.Count)" -ForegroundColor Cyan
Write-Host "`nPlease update these files manually following the pattern in update-api-calls.md" -ForegroundColor Green