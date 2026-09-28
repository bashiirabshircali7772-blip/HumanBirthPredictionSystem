FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY HumanBirthPredictionSystem.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish HumanBirthPredictionSystem.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS dotnet-runtime

FROM python:3.12-slim AS runtime
WORKDIR /app

COPY --from=dotnet-runtime /usr/share/dotnet /usr/share/dotnet
COPY --from=dotnet-runtime /usr/bin/dotnet /usr/bin/dotnet

COPY python/requirements.txt ./python/requirements.txt
RUN pip install --no-cache-dir -r ./python/requirements.txt

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["sh", "-c", "dotnet HumanBirthPredictionSystem.dll --urls http://0.0.0.0:${PORT:-8080}"]