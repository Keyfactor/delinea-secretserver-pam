#!/usr/bin/env bash
set -e -o pipefail
DEBUG=0


# function that checks if DEBUG is true and prints the output of the argument
function debug() {
  if [ $DEBUG ]; then
    echo $1
  fi
}

# Write API responses to a file
function writeFile() {
  echo $1 > $2
}

function unsetSecretServerVariables() {
  unset SECRET_SERVER_URL
  unset SECRET_SERVER_SECRET_ID
  unset SECRET_SERVER_USERNAME
  unset SECRET_SERVER_PASSWORD
  unset SECRET_SERVER_ACCESS_TOKEN
  unset SECRET_SERVER_ACCESS_TOKEN_RESPONSE
  unset SECRET_RESPONSE
}

# Check if jq is installed and if not install it
if ! [ -x "$(command -v jq)" ]; then
  if [ -x "$(command -v apt-get)" ]; then
    echo "jq is not installed. Installing jq..."
    sudo apt-get update
    sudo apt-get install jq
  else
    echo "jq is not installed. Please install jq and try again."
    exit 1
  fi
fi

# prompt for the secret server URL if it's not set
if [ -z "$SECRET_SERVER_URL" ]; then
  read -rp "Enter your secret server URL: " SECRET_SERVER_URL
fi

# prompt for secret id if it's not set
if [ -z "$SECRET_SERVER_SECRET_ID" ]; then
  read -rp "Enter your secret ID: " SECRET_SERVER_SECRET_ID
fi

# prompt for username if it's not set
if [ -z "$SECRET_SERVER_USERNAME" ]; then
  read -rp "Enter your secret server username: " SECRET_SERVER_USERNAME
fi

# prompt for password if it's not set
if [ -z "$SECRET_SERVER_PASSWORD" ]; then
  read -s -rp "Enter your secret server password" SECRET_SERVER_PASSWORD
  echo
fi

# Check that no values are empty
if [ -z "$SECRET_SERVER_URL" ] || [ -z "$SECRET_SERVER_SECRET_ID" ] || [ -z "$SECRET_SERVER_USERNAME" ] || [ -z "$SECRET_SERVER_PASSWORD" ]; then
  echo "Please provide all the required values. Exiting..."
  exit 1
fi

# Check if URL is prefixed with http or https
if [[ ! $SECRET_SERVER_URL =~ ^https?:// ]]; then
  # If not, prefix with https://
  SECRET_SERVER_URL="https://$SECRET_SERVER_URL"
fi

SECRET_SERVER_TOKEN_FILE_OUTPUT_PATH="secret_server_token.txt"
SECRET_SERVER_SECRET_FILE_OUTPUT_PATH="secret_server_secret_${SECRET_SERVER_SECRET_ID}.json"
SECRET_SERVER_TOKEN_RESPONSE="secret_server_token.json"

# Step 1: Authenticate and get a token
echo "Authenticating and getting a token for $SECRET_SERVER_USERNAME..."

SECRET_SERVER_ACCESS_TOKEN_RESPONSE=$(curl -s -X POST "${SECRET_SERVER_URL}/oauth2/token" \
-H "Content-Type: application/x-www-form-urlencoded" \
-d "grant_type=password&username=${SECRET_SERVER_USERNAME}&password=${SECRET_SERVER_PASSWORD}&scope=api")
debug "SECRET_SERVER_ACCESS_TOKEN_RESPONSE: $SECRET_SERVER_ACCESS_TOKEN_RESPONSE"
writeFile "${SECRET_SERVER_ACCESS_TOKEN_RESPONSE}" "${SECRET_SERVER_TOKEN_RESPONSE}"

SECRET_SERVER_ACCESS_TOKEN=$(echo "${SECRET_SERVER_ACCESS_TOKEN_RESPONSE}" | jq -r '.access_token')
debug "SECRET_SERVER_ACCESS_TOKEN: $SECRET_SERVER_ACCESS"
writeFile "${SECRET_SERVER_ACCESS_TOKEN}" "${SECRET_SERVER_TOKEN_FILE_OUTPUT_PATH}"

# Step 2: Use the token to fetch the secret with ID $SECRET_SERVER_SECRET_ID
echo "Fetching the secret with ID $SECRET_SERVER_SECRET_ID..."
SECRET_RESPONSE=$(curl -s -X GET "${SECRET_SERVER_URL}/api/v1/secrets/${SECRET_SERVER_SECRET_ID}" \
-H "Authorization: Bearer $SECRET_SERVER_ACCESS_TOKEN" \
-H "Content-Type: application/json")
debug "SECRET_RESPONSE: ${SECRET_RESPONSE}"
writeFile "${SECRET_RESPONSE}" "${SECRET_SERVER_SECRET_FILE_OUTPUT_PATH}"

unsetSecretServerVariables