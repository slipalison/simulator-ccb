FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY ["Onboarding.slnx", "."]
COPY ["src/Onboarding.Domain/Onboarding.Domain.csproj", "src/Onboarding.Domain/"]
COPY ["src/Onboarding.Application/Onboarding.Application.csproj", "src/Onboarding.Application/"]
COPY ["src/Onboarding.Infrastructure/Onboarding.Infrastructure.csproj", "src/Onboarding.Infrastructure/"]
COPY ["src/Onboarding.API/Onboarding.API.csproj", "src/Onboarding.API/"]

RUN dotnet restore "src/Onboarding.API/Onboarding.API.csproj"

COPY . .

RUN dotnet publish "src/Onboarding.API/Onboarding.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app

RUN addgroup -S app && adduser -S app -G app

ENV DOTNET_GlobalizationMode=Invariant

COPY --from=build /app/publish .

USER app
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:8080/healthz/live || exit 1

ENTRYPOINT ["dotnet", "Onboarding.API.dll"]