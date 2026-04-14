FROM mcr.microsoft.com/dotnet/sdk:10.0 AS dev
WORKDIR /app

# Copy solution and project files for layer caching
COPY . .
COPY Onboarding.slnx .
COPY src/Onboarding.Domain/Onboarding.Domain.csproj src/Onboarding.Domain/
COPY src/Onboarding.Application/Onboarding.Application.csproj src/Onboarding.Application/
COPY src/Onboarding.Infrastructure/Onboarding.Infrastructure.csproj src/Onboarding.Infrastructure/
COPY src/Onboarding.API/Onboarding.API.csproj src/Onboarding.API/
RUN dotnet build

# Copy source
COPY src/ ./src/
COPY entrypoint.sh /app/entrypoint.sh

# Fix permissions for non-root user
RUN chown -R $APP_UID:$APP_UID /app

RUN chmod +x /app/entrypoint.sh

WORKDIR /app/src/Onboarding.API
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "run", "--no-launch-profile", "--urls", "http://0.0.0.0:8080"]
