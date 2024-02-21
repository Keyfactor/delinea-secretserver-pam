# Copyright 2024 Keyfactor
# Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
# You may obtain a copy of the License at http:#www.apache.org/licenses/LICENSE-2.0
# Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions
# and limitations under the License.

$DEBUG=0 # Set to 1 to enable debug output

# Set the following environment variables to avoid being prompted
#$env:SECRET_SERVER_URL='<your_secret_server_url>:<optional_non_standard_port>/SecretServer'
#$env:SECRET_SERVER_USERNAME='<your_secret_server_service_account_username>'
#$env:SECRET_SERVER_PASSWORD='<your_service_account_password>'
#$env:SECRET_SERVER_SECRET_ID='<your_secret_id>'

function help() {
    Write-Host "Usage: .\test_pwsh.ps1"
    Write-Host "This script prompts for the secret server URL, secret ID, username, and password if they're not set, authenticates and gets a token, uses the token to fetch the secret with the given ID, and then unsets the variables."
    Write-Host "You can set the following environment variables to avoid being prompted:"
    Write-Host "  SECRET_SERVER_URL: The URL of your secret server"
    Write-Host "  SECRET_SERVER_SECRET_ID: The ID of your secret"
    Write-Host "  SECRET_SERVER_USERNAME: Your secret server username"
    Write-Host "  SECRET_SERVER_PASSWORD: Your secret server password"
}

function debug($message) {
  if ($DEBUG) {
    Write-Host $message
  }
}

function writeFile($content, $filePath) {
  Set-Content -Path $filePath -Value $content
}

function writeJsonFile($content, $filePath) {
  $content | ConvertTo-Json | Set-Content -Path $filePath
}

function unsetSecretServerVariables() {
  Remove-Variable -Name SECRET_SERVER_URL, SECRET_SERVER_SECRET_ID, SECRET_SERVER_USERNAME, SECRET_SERVER_PASSWORD, SECRET_SERVER_ACCESS_TOKEN, SECRET_SERVER_ACCESS_TOKEN_RESPONSE, SECRET_RESPONSE -ErrorAction SilentlyContinue
}

if ([string]::IsNullOrEmpty($env:SECRET_SERVER_URL)) {
    $SECRET_SERVER_URL = Read-Host -Prompt "Enter your secret server URL"
} else {
    Write-Host "Using the secret server URL from the environment variable: $env:SECRET_SERVER_URL"
    $SECRET_SERVER_URL = $env:SECRET_SERVER_URL
}

if ([string]::IsNullOrEmpty($env:SECRET_SERVER_SECRET_ID)) {
    $SECRET_SERVER_SECRET_ID = Read-Host -Prompt "Enter your secret ID"
} else {
    Write-Host "Using the secret ID from the environment variable: $env:SECRET_SERVER_SECRET_ID"
    $SECRET_SERVER_SECRET_ID = $env:SECRET_SERVER_SECRET_ID
}

if ([string]::IsNullOrEmpty($env:SECRET_SERVER_USERNAME)) {
    $SECRET_SERVER_USERNAME = Read-Host -Prompt "Enter your secret server username"
} else {
    Write-Host "Using the username from the environment variable: $env:SECRET_SERVER_USERNAME"
    $SECRET_SERVER_USERNAME = $env:SECRET_SERVER_USERNAME
}

if ([string]::IsNullOrEmpty($env:SECRET_SERVER_PASSWORD)) {
    $SECRET_SERVER_PASSWORD = Read-Host -Prompt "Enter your secret server password" -AsSecureString
} else {
    $SECRET_SERVER_PASSWORD = ConvertTo-SecureString -String $env:SECRET_SERVER_PASSWORD -AsPlainText -Force
    Write-Host "Using the password from the environment variable: $SECRET_SERVER_PASSWORD"
    debug "SECRET_SERVER_PASSWORD: $SECRET_SERVER_PASSWORD"
}

if ([string]::IsNullOrEmpty($SECRET_SERVER_URL) -or [string]::IsNullOrEmpty($SECRET_SERVER_SECRET_ID) -or [string]::IsNullOrEmpty($SECRET_SERVER_USERNAME) -or [string]::IsNullOrEmpty($SECRET_SERVER_PASSWORD)) {
  Write-Host "Please provide all the required values. Exiting..."
  help
  return
}

if (!($SECRET_SERVER_URL -match '^https?://')) {
  $SECRET_SERVER_URL = "https://$SECRET_SERVER_URL"
}

$SECRET_SERVER_TOKEN_FILE_OUTPUT_PATH="secret_server_token.txt"
$SECRET_SERVER_SECRET_FILE_OUTPUT_PATH="secret_server_secret_${SECRET_SERVER_SECRET_ID}.json"
$SECRET_SERVER_TOKEN_RESPONSE="secret_server_token.json"

Write-Host "Authenticating and getting a token for $SECRET_SERVER_USERNAME..."
debug "Converting the secure string to plain text..."
$BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($SECRET_SERVER_PASSWORD)
$PlainPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringBSTR($BSTR)
debug "PlainPassword: $PlainPassword"

debug "Fetching the token from $SECRET_SERVER_URL..."
$SECRET_SERVER_ACCESS_TOKEN_RESPONSE = Invoke-RestMethod -Uri "${SECRET_SERVER_URL}/oauth2/token" -Method POST -Body "grant_type=password&username=${SECRET_SERVER_USERNAME}&password=${PlainPassword}&scope=api" -ContentType "application/x-www-form-urlencoded"
debug "SECRET_SERVER_ACCESS_TOKEN_RESPONSE: $SECRET_SERVER_ACCESS_TOKEN_RESPONSE"
writeJsonFile $SECRET_SERVER_ACCESS_TOKEN_RESPONSE $SECRET_SERVER_TOKEN_RESPONSE
debug "Freeing the BSTR..."
[System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($BSTR)

debug "Extracting the token..."
$SECRET_SERVER_ACCESS_TOKEN = $SECRET_SERVER_ACCESS_TOKEN_RESPONSE.access_token
debug "SECRET_SERVER_ACCESS_TOKEN: $SECRET_SERVER_ACCESS"
writeFile $SECRET_SERVER_ACCESS_TOKEN $SECRET_SERVER_TOKEN_FILE_OUTPUT_PATH

Write-Host "Fetching the secret with ID $SECRET_SERVER_SECRET_ID..."
$SECRET_RESPONSE = Invoke-RestMethod -Uri "${SECRET_SERVER_URL}/api/v1/secrets/${SECRET_SERVER_SECRET_ID}" -Method GET -Headers @{"Authorization"="Bearer $SECRET_SERVER_ACCESS_TOKEN"; "Content-Type"="application/json"}
debug "SECRET_RESPONSE: ${SECRET_RESPONSE}"
writeJsonFile $SECRET_RESPONSE $SECRET_SERVER_SECRET_FILE_OUTPUT_PATH
Write-Host $SECRET_RESPONSE | ConvertTo-Json 

unsetSecretServerVariables