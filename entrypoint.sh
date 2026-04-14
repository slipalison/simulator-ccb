#!/bin/sh
# Fix permissions on obj/bin directories so dotnet watch can rebuild
for dir in $(find /app -type d \( -name "obj" -o -name "bin" \) 2>/dev/null); do
    chown -R $APP_UID:$APP_UID "$dir" 2>/dev/null || true
done
exec dotnet watch run --no-launch-profile --urls http://0.0.0.0:8080
