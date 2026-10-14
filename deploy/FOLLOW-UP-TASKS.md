# Deployment follow-up tasks

## Required: upgrade ExitPass POS Server from .NET 8 to .NET 10

This task is intentionally outside `feature/docker-deployment-assets`. The current branch must remain on .NET 8 so deployment packaging changes can be reviewed independently from an application-runtime migration.

Before production use beyond the .NET 8 support date, create a separate branch and review that:

1. changes the application target framework and build/runtime images to supported .NET 10 releases;
2. verifies Npgsql and every direct and transitive dependency against .NET 10;
3. introduces reviewed NuGet dependency locking and repeatable restore behavior;
4. runs the complete unit, integration, PostgreSQL, API authorization, fiscal, Electronic Journal, X/Z Reading, BIR reporting, and Annex E-1 regression suites;
5. rebuilds and scans the API image, records immutable image digests and provenance, and repeats the isolated restore drill;
6. records rollback compatibility for the database schema, configuration, persisted Annex E-1 artifacts, and API clients.

The runtime upgrade must not be combined with or inferred from approval of the Docker deployment assets.
