FROM mcr.microsoft.com/dotnet/sdk:10.0 AS dev
WORKDIR /app

# Copy solution file
COPY Onboarding.slnx .

# Copy project files first (for restore caching)
COPY src/Onboarding.Domain/Onboarding.Domain.csproj src/Onboarding.Domain/
COPY src/Onboarding.Application/Onboarding.Application.csproj src/Onboarding.Application/
COPY src/Onboarding.Infrastructure/Onboarding.Infrastructure.csproj src/Onboarding.Infrastructure/
COPY src/Onboarding.API/Onboarding.API.csproj src/Onboarding.API/
COPY tests/Onboarding.Domain.Tests/Onboarding.Domain.Tests.csproj tests/Onboarding.Domain.Tests/
COPY tests/Onboarding.API.Tests/Onboarding.API.Tests.csproj tests/Onboarding.API.Tests/
COPY tests/Onboarding.Integration.Tests/Onboarding.Integration.Tests.csproj tests/Onboarding.Integration.Tests/

# Restore dependencies
RUN dotnet restore

# Copy all source (obj/bin excluded via .dockerignore)
COPY src/ src/
COPY tests/ tests/

# Build
RUN dotnet build --no-restore

WORKDIR /app/src/Onboarding.API
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "bin/Debug/net10.0/Onboarding.API.dll", "--no-launch-profile", "--urls", "http://0.0.0.0:8080"]
