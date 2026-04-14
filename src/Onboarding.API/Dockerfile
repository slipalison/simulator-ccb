FROM mcr.microsoft.com/dotnet/sdk:10.0 AS dev
WORKDIR /app

# Copy only .NET files — skip node_modules, .git, etc.
COPY Onboarding.slnx .
COPY src/Onboarding.Domain/Onboarding.Domain.csproj src/Onboarding.Domain/
COPY src/Onboarding.Application/Onboarding.Application.csproj src/Onboarding.Application/
COPY src/Onboarding.Infrastructure/Onboarding.Infrastructure.csproj src/Onboarding.Infrastructure/
COPY src/Onboarding.API/Onboarding.API.csproj src/Onboarding.API/
COPY tests/Onboarding.Domain.Tests/Onboarding.Domain.Tests.csproj tests/Onboarding.Domain.Tests/
COPY tests/Onboarding.API.Tests/Onboarding.API.Tests.csproj tests/Onboarding.API.Tests/
COPY tests/Onboarding.Integration.Tests/Onboarding.Integration.Tests.csproj tests/Onboarding.Integration.Tests/
RUN dotnet build

# Copy source
COPY src/ ./src/
COPY tests/ ./tests/
COPY entrypoint.sh /app/entrypoint.sh

# Fix permissions for non-root user — only .NET dirs, skip node_modules
RUN find /app -name node_modules -prune -o -exec chown $APP_UID:$APP_UID {} + 2>/dev/null || true

RUN chmod +x /app/entrypoint.sh

WORKDIR /app/src/Onboarding.API
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "run", "--no-launch-profile", "--urls", "http://0.0.0.0:8080"]
