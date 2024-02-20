$DEBUG=0

function debug($message) {
  if ($DEBUG) {
    Write-Host $message
  }
}

function writeFile($content, $filePath) {
  Set-Content -Path $filePath -Value $content
}

function unsetSecretServerVariables() {
  Remove-Variable -Name SECRET_SERVER_URL, SECRET_SERVER_SECRET_ID, SECRET_SERVER_USERNAME, SECRET_SERVER_PASSWORD, SECRET_SERVER_ACCESS_TOKEN, SECRET_SERVER_ACCESS_TOKEN_RESPONSE, SECRET_RESPONSE -ErrorAction SilentlyContinue
}

if (![string]::IsNullOrEmpty($env:SECRET_SERVER_URL)) {
  $SECRET_SERVER_URL = Read-Host -Prompt "Enter your secret server URL"
}

if (![string]::IsNullOrEmpty($env:SECRET_SERVER_SECRET_ID)) {
  $SECRET_SERVER_SECRET_ID = Read-Host -Prompt "Enter your secret ID"
}

if (![string]::IsNullOrEmpty($env:SECRET_SERVER_USERNAME)) {
  $SECRET_SERVER_USERNAME = Read-Host -Prompt "Enter your secret server username"
}

if (![string]::IsNullOrEmpty($env:SECRET_SERVER_PASSWORD)) {
  $SECRET_SERVER_PASSWORD = Read-Host -Prompt "Enter your secret server password"
}

if ([string]::IsNullOrEmpty($SECRET_SERVER_URL) -or [string]::IsNullOrEmpty($SECRET_SERVER_SECRET_ID) -or [string]::IsNullOrEmpty($SECRET_SERVER_USERNAME) -or [string]::IsNullOrEmpty($SECRET_SERVER_PASSWORD)) {
  Write-Host "Please provide all the required values. Exiting..."
  exit
}

if (!($SECRET_SERVER_URL -match '^https?://')) {
  $SECRET_SERVER_URL = "https://$SECRET_SERVER_URL"
}

$SECRET_SERVER_TOKEN_FILE_OUTPUT_PATH="secret_server_token.txt"
$SECRET_SERVER_SECRET_FILE_OUTPUT_PATH="secret_server_secret_${SECRET_SERVER_SECRET_ID}.json"
$SECRET_SERVER_TOKEN_RESPONSE="secret_server_token.json"

Write-Host "Authenticating and getting a token for $SECRET_SERVER_USERNAME..."

$SECRET_SERVER_ACCESS_TOKEN_RESPONSE = Invoke-RestMethod -Uri "${SECRET_SERVER_URL}/oauth2/token" -Method POST -Body "grant_type=password&username=${SECRET_SERVER_USERNAME}&password=${SECRET_SERVER_PASSWORD}&scope=api" -ContentType "application/x-www-form-urlencoded"
debug "SECRET_SERVER_ACCESS_TOKEN_RESPONSE: $SECRET_SERVER_ACCESS_TOKEN_RESPONSE"
writeFile $SECRET_SERVER_ACCESS_TOKEN_RESPONSE $SECRET_SERVER_TOKEN_RESPONSE

$SECRET_SERVER_ACCESS_TOKEN = $SECRET_SERVER_ACCESS_TOKEN_RESPONSE.access_token
debug "SECRET_SERVER_ACCESS_TOKEN: $SECRET_SERVER_ACCESS"
writeFile $SECRET_SERVER_ACCESS_TOKEN $SECRET_SERVER_TOKEN_FILE_OUTPUT_PATH

Write-Host "Fetching the secret with ID $SECRET_SERVER_SECRET_ID..."
$SECRET_RESPONSE = Invoke-RestMethod -Uri "${SECRET_SERVER_URL}/api/v1/secrets/${SECRET_SERVER_SECRET_ID}" -Method GET -Headers @{"Authorization"="Bearer $SECRET_SERVER_ACCESS_TOKEN"; "Content-Type"="application/json"}
debug "SECRET_RESPONSE: ${SECRET_RESPONSE}"
writeFile "${SECRET_RESPONSE}" $SECRET_SERVER_SECRET_FILE_OUTPUT_PATH

unsetSecretServerVariables