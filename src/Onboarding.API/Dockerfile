FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first (for restore caching)
COPY ["Onboarding.slnx", "."]
COPY ["src/Onboarding.Domain/Onboarding.Domain.csproj", "src/Onboarding.Domain/"]
COPY ["src/Onboarding.Application/Onboarding.Application.csproj", "src/Onboarding.Application/"]
COPY ["src/Onboarding.Infrastructure/Onboarding.Infrastructure.csproj", "src/Onboarding.Infrastructure/"]
COPY ["src/Onboarding.API/Onboarding.API.csproj", "src/Onboarding.API/"]

# Restore dependencies
RUN dotnet restore "src/Onboarding.API/Onboarding.API.csproj"

# Copy all source
COPY . .

# Build and Publish
RUN dotnet publish "src/Onboarding.API/Onboarding.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Onboarding.API.dll"]
