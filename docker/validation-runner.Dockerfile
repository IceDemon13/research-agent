FROM mcr.microsoft.com/dotnet/sdk:9.0

RUN apt-get update \
    && apt-get install -y --no-install-recommends python3 ca-certificates \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY scripts/validation_runner_server.py /app/scripts/validation_runner_server.py

ENV PYTHONUNBUFFERED=1
ENV VALIDATION_RUNNER_PORT=8091

CMD ["python3", "/app/scripts/validation_runner_server.py"]
