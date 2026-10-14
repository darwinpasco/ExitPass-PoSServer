ARG POSTGRES_BASE_IMAGE=postgres@sha256:0ea6700a3b4f0ae6ce746519073558aed4d88a79d8d07622a9a644946c7319c4
FROM ${POSTGRES_BASE_IMAGE}

ARG GOOGLE_CLOUD_CLI_VERSION=588.0.0-0

USER root
RUN apt-get update \
    && apt-get install --yes --no-install-recommends age ca-certificates curl gnupg jq \
    && mkdir -p /etc/apt/keyrings \
    && curl --fail --silent --show-error --location https://packages.cloud.google.com/apt/doc/apt-key.gpg \
       | gpg --dearmor --yes --output /etc/apt/keyrings/cloud.google.gpg \
    && printf '%s\n' 'deb [signed-by=/etc/apt/keyrings/cloud.google.gpg] https://packages.cloud.google.com/apt cloud-sdk main' \
       > /etc/apt/sources.list.d/google-cloud-sdk.list \
    && apt-get update \
    && test -n "$GOOGLE_CLOUD_CLI_VERSION" \
    && apt-get install --yes --no-install-recommends "google-cloud-cli=$GOOGLE_CLOUD_CLI_VERSION" \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /opt/exitpass-backup
RUN mkdir -p /var/lib/exitpass-backup/staging /var/lib/exitpass-backup/status \
    && chown -R postgres:postgres /opt/exitpass-backup /var/lib/exitpass-backup

COPY --chown=postgres:postgres --chmod=0555 backup-loop.sh run-backup.sh healthcheck.sh restore-from-gcs.sh ./

USER postgres
ENTRYPOINT ["/opt/exitpass-backup/backup-loop.sh"]
