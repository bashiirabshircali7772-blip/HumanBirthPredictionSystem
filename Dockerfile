FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY HumanBirthPredictionSystem.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish HumanBirthPredictionSystem.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

RUN apt-get update \
	&& apt-get install -y --no-install-recommends python3 python3-venv \
	&& python3 -m venv /opt/venv \
	&& /opt/venv/bin/pip install --no-cache-dir --upgrade pip \
	&& rm -rf /var/lib/apt/lists/*

COPY python/requirements.txt ./python/requirements.txt
RUN /opt/venv/bin/pip install --no-cache-dir -r ./python/requirements.txt

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV PythonSettings__PythonExecutablePath=/opt/venv/bin/python
EXPOSE 8080

ENTRYPOINT ["sh", "-c", "dotnet HumanBirthPredictionSystem.dll --urls http://0.0.0.0:${PORT:-8080}"]