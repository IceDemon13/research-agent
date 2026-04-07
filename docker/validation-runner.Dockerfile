FROM mcr.microsoft.com/dotnet/runtime:8.0 AS dotnet8runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS aspnet8runtime

FROM mcr.microsoft.com/dotnet/sdk:9.0

COPY --from=dotnet8runtime /usr/share/dotnet/shared/Microsoft.NETCore.App /usr/share/dotnet/shared/Microsoft.NETCore.App
COPY --from=aspnet8runtime /usr/share/dotnet/shared/Microsoft.AspNetCore.App /usr/share/dotnet/shared/Microsoft.AspNetCore.App

RUN apt-get update \
    && apt-get install -y --no-install-recommends python3 ca-certificates \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY scripts/validation_runner_server.py /app/scripts/validation_runner_server.py

ENV PYTHONUNBUFFERED=1
ENV VALIDATION_RUNNER_PORT=8091

CMD ["python3", "/app/scripts/validation_runner_server.py"]
