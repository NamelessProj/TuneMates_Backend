#!/bin/bash
set -e

# Generate appsettings.json from environment variables if it doesn't exist
if [ ! -f /app/appsettings.json ]; then
    echo "Generating appsettings.json from environment variables..."
    
    # Validate required environment variables
    if [ -z "$CONNECTION_STRING" ]; then
        echo "ERROR: CONNECTION_STRING environment variable is required when appsettings.json is not provided"
        exit 1
    fi
    
    if [ -z "$JWT_KEY" ]; then
        echo "ERROR: JWT_KEY environment variable is required when appsettings.json is not provided"
        exit 1
    fi
    
    if [ -z "$ENCRYPT_KEY" ]; then
        echo "ERROR: ENCRYPT_KEY environment variable is required when appsettings.json is not provided"
        exit 1
    fi
    
    # Process CORS_ALLOWED_ORIGINS into proper JSON array format
    CORS_ORIGINS_JSON="${CORS_ALLOWED_ORIGINS:-\"http://localhost:5173\"}"
    # If CORS_ALLOWED_ORIGINS doesn't start with a quote, wrap each comma-separated value in quotes
    if [[ ! "$CORS_ORIGINS_JSON" =~ ^\" ]]; then
        CORS_ORIGINS_JSON=$(echo "$CORS_ORIGINS_JSON" | sed 's/,/","/g' | sed 's/^/"/' | sed 's/$/"/')
    fi
    
    cat > /app/appsettings.json <<EOF
{
  "Logging": {
    "LogLevel": {
      "Default": "${LOGGING_LEVEL:-Information}",
      "Microsoft.AspNetCore": "${LOGGING_ASPNETCORE_LEVEL:-Warning}"
    }
  },
  "AllowedHosts": "${ALLOWED_HOSTS:-*}",
  "ConnectionStrings": {
    "DefaultConnection": "${CONNECTION_STRING}"
  },
  "Secrets": {
    "EncryptKey64": "${ENCRYPT_KEY}"
  },
  "Jwt": {
    "Key": "${JWT_KEY}",
    "Issuer": "${JWT_ISSUER:-TuneMates}",
    "Audience": "${JWT_AUDIENCE:-TuneMatesUsers}",
    "ExpiresInMinutes": ${JWT_EXPIRES_MINUTES:-180}
  },
  "CleanupService": {
    "TokenIntervalHours": ${CLEANUP_TOKEN_HOURS:-3},
    "ProposalIntervalHours": ${CLEANUP_PROPOSAL_HOURS:-3},
    "RoomIntervalHours": ${CLEANUP_ROOM_HOURS:-24},
    "SpotifyStateIntervalHours": ${CLEANUP_SPOTIFY_STATE_HOURS:-3}
  },
  "Cors": {
    "AllowedOrigins": [
      ${CORS_ORIGINS_JSON}
    ],
    "AllowCredentials": ${CORS_ALLOW_CREDENTIALS:-true}
  },
  "RateLimiting": {
    "GlobalPerMinute": ${RATE_LIMIT_GLOBAL:-0},
    "SearchPerMinute": ${RATE_LIMIT_SEARCH:-30},
    "MutationPerMinute": ${RATE_LIMIT_MUTATION:-10}
  },
  "Spotify": {
    "ClientId": "${SPOTIFY_CLIENT_ID:-}",
    "ClientSecret": "${SPOTIFY_CLIENT_SECRET:-}",
    "RedirectUri": "${SPOTIFY_REDIRECT_URI:-}"
  }
}
EOF
    
    echo "appsettings.json generated successfully"
else
    echo "Using existing appsettings.json"
fi

# Execute the main application
exec "$@"
