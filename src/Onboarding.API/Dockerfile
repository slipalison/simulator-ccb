FROM mcr.microsoft.com/dotnet/sdk:10.0 AS dev
WORKDIR /app

# Copy solution and project files for layer caching
COPY Onboarding.slnx .
COPY src/Onboarding.Domain/Onboarding.Domain.csproj src/Onboarding.Domain/
COPY src/Onboarding.Application/Onboarding.Application.csproj src/Onboarding.Application/
COPY src/Onboarding.Infrastructure/Onboarding.Infrastructure.csproj src/Onboarding.Infrastructure/
COPY src/Onboarding.API/Onboarding.API.csproj src/Onboarding.API/
COPY tests/Onboarding.Domain.Tests/Onboarding.Domain.Tests.csproj tests/Onboarding.Domain.Tests/
COPY tests/Onboarding.API.Tests/Onboarding.API.Tests.csproj tests/Onboarding.API.Tests/
COPY tests/Onboarding.Integration.Tests/Onboarding.Integration.Tests.csproj tests/Onboarding.Integration.Tests/

# Restore only (no build yet — source not copied)
RUN dotnet restore

# Copy all source code
COPY --chown=$APP_UID:$APP_UID src/ ./src/
COPY --chown=$APP_UID:$APP_UID tests/ ./tests/
COPY --chown=$APP_UID:$APP_UID entrypoint.sh /app/entrypoint.sh

# Build with source code present
RUN dotnet build --no-restore

RUN chmod +x /app/entrypoint.sh

WORKDIR /app/src/Onboarding.API
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "run", "--no-launch-profile", "--urls", "http://0.0.0.0:8080"]
