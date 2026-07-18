using ExitPass.PosServer.Runtime.FiscalDocuments;
using Npgsql;
using NpgsqlTypes;

namespace ExitPass.PosServer.Persistence.Postgres.FiscalDocuments;

public sealed class PostgresSalesInvoiceHeaderProfileRepository : ISalesInvoiceHeaderProfileRepository
{
    private readonly NpgsqlDataSource dataSource;
    private readonly SalesInvoiceHeaderProfileService profileService = new();

    public PostgresSalesInvoiceHeaderProfileRepository(NpgsqlDataSource dataSource)
    {
        this.dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
    }

    public async Task<FiscalIdentityProfile> CreateFiscalIdentityAsync(
        FiscalIdentityProfile identity,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(identity);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new NpgsqlCommand(
            """
            insert into pos.fiscal_identities (
                fiscal_identity_id,
                fiscal_identity_code,
                registered_business_name,
                registered_business_address,
                tin,
                taxpayer_classification,
                fiscal_identity_status,
                created_at,
                updated_at,
                created_by_ref,
                updated_by_ref
            ) values (
                @fiscal_identity_id,
                @fiscal_identity_code,
                @registered_business_name,
                @registered_business_address,
                @tin,
                @taxpayer_classification,
                @fiscal_identity_status,
                @created_at,
                @updated_at,
                @created_by_ref,
                @updated_by_ref
            );
            """,
            connection);
        AddFiscalIdentityParameters(command, identity);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        return identity;
    }

    public async Task<FiscalIdentityProfile?> GetFiscalIdentityAsync(
        Guid fiscalIdentityId,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new NpgsqlCommand(
            """
            select
                fiscal_identity_id,
                registered_business_name,
                registered_business_address,
                tin,
                taxpayer_classification,
                fiscal_identity_status,
                created_at,
                updated_at,
                created_by_ref,
                updated_by_ref
            from pos.fiscal_identities
            where fiscal_identity_id = @fiscal_identity_id;
            """,
            connection);
        command.Parameters.AddWithValue("fiscal_identity_id", fiscalIdentityId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false)
            ? ReadFiscalIdentity(reader)
            : null;
    }

    public async Task<SalesInvoiceHeaderProfile> CreateHeaderProfileAsync(
        SalesInvoiceHeaderProfile profile,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(profile);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var existing = await ReadProfilesForSitePosServerAsync(
            connection,
            profile.SitePosServerId,
            cancellationToken).ConfigureAwait(false);
        profileService.ValidateNoApprovedOverlap(profile, existing);

        await using var command = new NpgsqlCommand(
            """
            insert into pos.sales_invoice_header_profiles (
                sales_invoice_header_profile_id,
                fiscal_identity_id,
                site_id,
                site_pos_server_id,
                profile_version,
                template_version,
                presentation_version,
                pos_serial_number,
                machine_identification_number,
                parking_location_display,
                bir_accreditation_number,
                bir_accreditation_issued_date,
                bir_accreditation_valid_until,
                ptu_number,
                ptu_issued_date,
                sales_invoice_legal_statement,
                customer_service_footer,
                effective_from,
                effective_to,
                lifecycle_status,
                approved_at,
                approved_by_ref,
                retired_at,
                created_at,
                updated_at,
                created_by_ref,
                updated_by_ref
            ) values (
                @sales_invoice_header_profile_id,
                @fiscal_identity_id,
                @site_id,
                @site_pos_server_id,
                @profile_version,
                @template_version,
                @presentation_version,
                @pos_serial_number,
                @machine_identification_number,
                @parking_location_display,
                @bir_accreditation_number,
                @bir_accreditation_issued_date,
                @bir_accreditation_valid_until,
                @ptu_number,
                @ptu_issued_date,
                @sales_invoice_legal_statement,
                @customer_service_footer,
                @effective_from,
                @effective_to,
                @lifecycle_status,
                @approved_at,
                @approved_by_ref,
                @retired_at,
                @created_at,
                @updated_at,
                @created_by_ref,
                @updated_by_ref
            );
            """,
            connection);
        AddHeaderProfileParameters(command, profile);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        return profile;
    }

    public async Task<SalesInvoiceHeaderProfile?> GetHeaderProfileAsync(
        Guid salesInvoiceHeaderProfileId,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        return await ReadHeaderProfileAsync(connection, salesInvoiceHeaderProfileId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<SalesInvoiceHeaderProfile> ApproveHeaderProfileAsync(
        Guid salesInvoiceHeaderProfileId,
        DateTimeOffset approvedAt,
        string approvedByRef,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(approvedByRef))
        {
            throw new ArgumentException("Approval actor reference is required.", nameof(approvedByRef));
        }

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var profile = await ReadHeaderProfileAsync(connection, salesInvoiceHeaderProfileId, cancellationToken, transaction).ConfigureAwait(false) ??
            throw new InvalidOperationException("Sales Invoice header profile was not found.");
        var approved = profile with
        {
            LifecycleStatus = SalesInvoiceHeaderProfileLifecycle.Approved,
            ApprovedAt = approvedAt,
            ApprovedByRef = approvedByRef.Trim(),
            UpdatedAt = approvedAt,
            UpdatedByRef = approvedByRef.Trim()
        };
        var existing = await ReadProfilesForSitePosServerAsync(connection, approved.SitePosServerId, cancellationToken, transaction).ConfigureAwait(false);
        profileService.ValidateNoApprovedOverlap(approved, existing);

        await using var command = new NpgsqlCommand(
            """
            update pos.sales_invoice_header_profiles
            set lifecycle_status = 'APPROVED',
                approved_at = @approved_at,
                approved_by_ref = @approved_by_ref,
                updated_at = @approved_at,
                updated_by_ref = @approved_by_ref
            where sales_invoice_header_profile_id = @sales_invoice_header_profile_id;
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("sales_invoice_header_profile_id", salesInvoiceHeaderProfileId);
        command.Parameters.AddWithValue("approved_at", approvedAt);
        command.Parameters.AddWithValue("approved_by_ref", approvedByRef.Trim());
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return approved;
    }

    public async Task<SalesInvoiceHeaderProfile> RetireHeaderProfileAsync(
        Guid salesInvoiceHeaderProfileId,
        DateTimeOffset retiredAt,
        string retiredByRef,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(retiredByRef))
        {
            throw new ArgumentException("Retirement actor reference is required.", nameof(retiredByRef));
        }

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var existing = await ReadHeaderProfileAsync(connection, salesInvoiceHeaderProfileId, cancellationToken).ConfigureAwait(false) ??
            throw new InvalidOperationException("Sales Invoice header profile was not found.");

        await using var command = new NpgsqlCommand(
            """
            update pos.sales_invoice_header_profiles
            set lifecycle_status = 'RETIRED',
                retired_at = @retired_at,
                updated_at = @retired_at,
                updated_by_ref = @retired_by_ref
            where sales_invoice_header_profile_id = @sales_invoice_header_profile_id;
            """,
            connection);
        command.Parameters.AddWithValue("sales_invoice_header_profile_id", salesInvoiceHeaderProfileId);
        command.Parameters.AddWithValue("retired_at", retiredAt);
        command.Parameters.AddWithValue("retired_by_ref", retiredByRef.Trim());
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        return existing with
        {
            LifecycleStatus = SalesInvoiceHeaderProfileLifecycle.Retired,
            RetiredAt = retiredAt,
            UpdatedAt = retiredAt,
            UpdatedByRef = retiredByRef.Trim()
        };
    }

    public async Task<SalesInvoiceHeaderProfileResolutionResult> ResolveEffectiveSalesInvoiceHeaderProfileAsync(
        Guid siteId,
        Guid sitePosServerId,
        DateTimeOffset effectiveAt,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var profiles = await ReadProfilesForSitePosServerAsync(connection, sitePosServerId, cancellationToken).ConfigureAwait(false);
        return profileService.ResolveEffective(profiles, siteId, sitePosServerId, effectiveAt);
    }

    private static void AddFiscalIdentityParameters(NpgsqlCommand command, FiscalIdentityProfile identity)
    {
        command.Parameters.AddWithValue("fiscal_identity_id", identity.FiscalIdentityId);
        command.Parameters.AddWithValue("fiscal_identity_code", identity.FiscalIdentityId.ToString("D"));
        command.Parameters.AddWithValue("registered_business_name", identity.RegisteredBusinessName);
        command.Parameters.AddWithValue("registered_business_address", identity.RegisteredBusinessAddress);
        command.Parameters.AddWithValue("tin", identity.Tin);
        command.Parameters.AddWithValue("taxpayer_classification", (object?)identity.TaxpayerClassification ?? DBNull.Value);
        command.Parameters.AddWithValue("fiscal_identity_status", identity.Status);
        command.Parameters.AddWithValue("created_at", identity.CreatedAt);
        command.Parameters.AddWithValue("updated_at", identity.UpdatedAt);
        command.Parameters.AddWithValue("created_by_ref", (object?)identity.CreatedByRef ?? DBNull.Value);
        command.Parameters.AddWithValue("updated_by_ref", (object?)identity.UpdatedByRef ?? DBNull.Value);
    }

    private static void AddHeaderProfileParameters(NpgsqlCommand command, SalesInvoiceHeaderProfile profile)
    {
        command.Parameters.AddWithValue("sales_invoice_header_profile_id", profile.SalesInvoiceHeaderProfileId);
        command.Parameters.AddWithValue("fiscal_identity_id", profile.FiscalIdentityId);
        command.Parameters.AddWithValue("site_id", profile.SiteId);
        command.Parameters.AddWithValue("site_pos_server_id", profile.SitePosServerId);
        command.Parameters.AddWithValue("profile_version", profile.ProfileVersion);
        command.Parameters.AddWithValue("template_version", profile.TemplateVersion);
        command.Parameters.AddWithValue("presentation_version", profile.PresentationVersion);
        AddText(command, "pos_serial_number", profile.PosSerialNumber);
        AddText(command, "machine_identification_number", profile.MachineIdentificationNumber);
        AddText(command, "parking_location_display", profile.ParkingLocationDisplay);
        AddText(command, "bir_accreditation_number", profile.BirAccreditationNumber);
        AddDate(command, "bir_accreditation_issued_date", profile.BirAccreditationIssuedDate);
        AddDate(command, "bir_accreditation_valid_until", profile.BirAccreditationValidUntil);
        AddText(command, "ptu_number", profile.PtuNumber);
        AddDate(command, "ptu_issued_date", profile.PtuIssuedDate);
        AddText(command, "sales_invoice_legal_statement", profile.SalesInvoiceLegalStatement);
        AddText(command, "customer_service_footer", profile.CustomerServiceFooter);
        command.Parameters.AddWithValue("effective_from", profile.EffectiveFrom);
        command.Parameters.AddWithValue("effective_to", (object?)profile.EffectiveTo ?? DBNull.Value);
        command.Parameters.AddWithValue("lifecycle_status", profile.LifecycleStatus);
        command.Parameters.AddWithValue("approved_at", (object?)profile.ApprovedAt ?? DBNull.Value);
        command.Parameters.AddWithValue("approved_by_ref", (object?)profile.ApprovedByRef ?? DBNull.Value);
        command.Parameters.AddWithValue("retired_at", (object?)profile.RetiredAt ?? DBNull.Value);
        command.Parameters.AddWithValue("created_at", profile.CreatedAt);
        command.Parameters.AddWithValue("updated_at", profile.UpdatedAt);
        command.Parameters.AddWithValue("created_by_ref", (object?)profile.CreatedByRef ?? DBNull.Value);
        command.Parameters.AddWithValue("updated_by_ref", (object?)profile.UpdatedByRef ?? DBNull.Value);
    }

    private static void AddText(NpgsqlCommand command, string name, string? value) =>
        command.Parameters.Add(name, NpgsqlDbType.Text).Value =
            string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();

    private static void AddDate(NpgsqlCommand command, string name, DateOnly? value) =>
        command.Parameters.Add(name, NpgsqlDbType.Date).Value = value is null ? DBNull.Value : value.Value;

    private static async Task<SalesInvoiceHeaderProfile?> ReadHeaderProfileAsync(
        NpgsqlConnection connection,
        Guid salesInvoiceHeaderProfileId,
        CancellationToken cancellationToken,
        NpgsqlTransaction? transaction = null)
    {
        await using var command = new NpgsqlCommand(HeaderProfileSelectSql + " where profile.sales_invoice_header_profile_id = @sales_invoice_header_profile_id;", connection, transaction);
        command.Parameters.AddWithValue("sales_invoice_header_profile_id", salesInvoiceHeaderProfileId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false)
            ? ReadHeaderProfile(reader)
            : null;
    }

    private static async Task<IReadOnlyList<SalesInvoiceHeaderProfile>> ReadProfilesForSitePosServerAsync(
        NpgsqlConnection connection,
        Guid sitePosServerId,
        CancellationToken cancellationToken,
        NpgsqlTransaction? transaction = null)
    {
        await using var command = new NpgsqlCommand(HeaderProfileSelectSql + " where profile.site_pos_server_id = @site_pos_server_id;", connection, transaction);
        command.Parameters.AddWithValue("site_pos_server_id", sitePosServerId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        var results = new List<SalesInvoiceHeaderProfile>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            results.Add(ReadHeaderProfile(reader));
        }

        return results;
    }

    private static FiscalIdentityProfile ReadFiscalIdentity(NpgsqlDataReader reader) =>
        new(
            reader.GetGuid(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.IsDBNull(4) ? null : reader.GetString(4),
            reader.GetString(5),
            ReadDateTimeOffset(reader, 6),
            ReadDateTimeOffset(reader, 7),
            reader.IsDBNull(8) ? null : reader.GetString(8),
            reader.IsDBNull(9) ? null : reader.GetString(9));

    private static SalesInvoiceHeaderProfile ReadHeaderProfile(NpgsqlDataReader reader)
    {
        var identity = new FiscalIdentityProfile(
            reader.GetGuid(1),
            reader.GetString(27),
            reader.GetString(28),
            reader.GetString(29),
            reader.IsDBNull(30) ? null : reader.GetString(30),
            reader.GetString(31),
            ReadDateTimeOffset(reader, 32),
            ReadDateTimeOffset(reader, 33),
            reader.IsDBNull(34) ? null : reader.GetString(34),
            reader.IsDBNull(35) ? null : reader.GetString(35));

        return new SalesInvoiceHeaderProfile(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.GetGuid(2),
            reader.GetGuid(3),
            reader.GetString(4),
            reader.GetString(5),
            reader.GetString(6),
            reader.IsDBNull(7) ? null : reader.GetString(7),
            reader.IsDBNull(8) ? null : reader.GetString(8),
            reader.IsDBNull(9) ? null : reader.GetString(9),
            reader.IsDBNull(10) ? null : reader.GetString(10),
            ReadDateOnly(reader, 11),
            ReadDateOnly(reader, 12),
            reader.IsDBNull(13) ? null : reader.GetString(13),
            ReadDateOnly(reader, 14),
            reader.IsDBNull(15) ? null : reader.GetString(15),
            reader.IsDBNull(16) ? null : reader.GetString(16),
            ReadDateTimeOffset(reader, 17),
            reader.IsDBNull(18) ? null : ReadDateTimeOffset(reader, 18),
            reader.GetString(19),
            reader.IsDBNull(20) ? null : ReadDateTimeOffset(reader, 20),
            reader.IsDBNull(21) ? null : reader.GetString(21),
            reader.IsDBNull(22) ? null : ReadDateTimeOffset(reader, 22),
            ReadDateTimeOffset(reader, 23),
            ReadDateTimeOffset(reader, 24),
            reader.IsDBNull(25) ? null : reader.GetString(25),
            reader.IsDBNull(26) ? null : reader.GetString(26),
            identity,
            reader.GetBoolean(36));
    }

    private static DateTimeOffset ReadDateTimeOffset(NpgsqlDataReader reader, int ordinal)
    {
        var value = reader.GetValue(ordinal);
        return value switch
        {
            DateTimeOffset dateTimeOffset => dateTimeOffset,
            DateTime dateTime => new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)),
            _ => throw new InvalidOperationException("PostgreSQL timestamp value could not be read.")
        };
    }

    private static DateOnly? ReadDateOnly(NpgsqlDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return null;
        }

        var value = reader.GetValue(ordinal);
        return value switch
        {
            DateOnly dateOnly => dateOnly,
            DateTime dateTime => DateOnly.FromDateTime(dateTime),
            _ => throw new InvalidOperationException("PostgreSQL date value could not be read.")
        };
    }

    private const string HeaderProfileSelectSql = """
        select
            profile.sales_invoice_header_profile_id,
            profile.fiscal_identity_id,
            profile.site_id,
            profile.site_pos_server_id,
            profile.profile_version,
            profile.template_version,
            profile.presentation_version,
            profile.pos_serial_number,
            profile.machine_identification_number,
            profile.parking_location_display,
            profile.bir_accreditation_number,
            profile.bir_accreditation_issued_date,
            profile.bir_accreditation_valid_until,
            profile.ptu_number,
            profile.ptu_issued_date,
            profile.sales_invoice_legal_statement,
            profile.customer_service_footer,
            profile.effective_from,
            profile.effective_to,
            profile.lifecycle_status,
            profile.approved_at,
            profile.approved_by_ref,
            profile.retired_at,
            profile.created_at,
            profile.updated_at,
            profile.created_by_ref,
            profile.updated_by_ref,
            identity.registered_business_name,
            identity.registered_business_address,
            identity.tin,
            identity.taxpayer_classification,
            identity.fiscal_identity_status,
            identity.created_at,
            identity.updated_at,
            identity.created_by_ref,
            identity.updated_by_ref,
            exists (
                select 1
                from pos.fiscal_document_header_snapshots snapshot
                where snapshot.sales_invoice_header_profile_id = profile.sales_invoice_header_profile_id
            )
        from pos.sales_invoice_header_profiles profile
        inner join pos.fiscal_identities identity
            on identity.fiscal_identity_id = profile.fiscal_identity_id
        """;
}
