using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using ExitPass.PosServer.Api.FiscalReports;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace ExitPass.PosServer.Api.FiscalDocuments;

public static class SalesInvoiceHeaderProfileAdminAuthorization
{
    public const string AuthenticationScheme = "PosServerAdminApiKey";
    public const string PolicyName = "SalesInvoiceHeaderProfileAdministration";
    public const string PermissionClaimType = "pos_server_permission";
    public const string RequiredPermission = "sales_invoice_header_profile.admin";
    public const string ApiKeyHeaderName = "X-PosServer-Admin-Key";
    public const string PermissionHeaderName = "X-PosServer-Admin-Permission";
    public const string CorrelationHeaderName = "X-Correlation-Id";
    public const string ApiKeyMapConfigurationSection = "PosServer:Admin:ApiKeys";

    public static string? ResolveConfiguredApiKey(IConfiguration configuration) =>
        configuration["POS_SALES_INVOICE_HEADER_PROFILE_ADMIN_API_KEY"] ??
        configuration["PosServer:Admin:SalesInvoiceHeaderProfile:ApiKey"];

    public static PosServerAdminApiKeyConfiguration ResolveConfiguredApiKeys(IConfiguration configuration)
    {
        var registrations = new List<PosServerAdminApiKeyRegistration>();
        var apiKeyChildren = configuration.GetSection(ApiKeyMapConfigurationSection).GetChildren().ToArray();

        foreach (var child in apiKeyChildren)
        {
            var principalName = child["Principal"] ?? child["PrincipalName"] ?? child.Key;
            var secret = child["Key"] ?? child["Secret"];
            var enabled = !string.Equals(child["Enabled"], "false", StringComparison.OrdinalIgnoreCase);
            var permissions = child.GetSection("Permissions")
                .GetChildren()
                .Select(permission => permission.Value)
                .Append(child["Permission"])
                .Where(permission => !string.IsNullOrWhiteSpace(permission))
                .Select(permission => permission!.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            var sitePosServerScopes = ReadScopes(child, "SitePosServerIds");
            var fiscalIdentityScopes = ReadScopes(child, "FiscalIdentityIds");

            if (string.IsNullOrWhiteSpace(principalName) ||
                string.IsNullOrEmpty(secret) ||
                string.IsNullOrWhiteSpace(secret) ||
                permissions.Length == 0 ||
                !ScopesAreValid(sitePosServerScopes) ||
                !ScopesAreValid(fiscalIdentityScopes) ||
                (permissions.Any(permission =>
                        FiscalXReadingAuthorization.IsXReadingPermission(permission) ||
                        FiscalZCloseStateInitializationAuthorization.IsScopedPermission(permission)) &&
                    (sitePosServerScopes.Length == 0 || fiscalIdentityScopes.Length == 0)))
            {
                return PosServerAdminApiKeyConfiguration.Invalid("invalid_admin_api_key_configuration");
            }

            if (registrations.Any(registration =>
                    string.Equals(registration.PrincipalName, principalName, StringComparison.Ordinal) ||
                    SecretEquals(secret, registration.Secret)))
            {
                return PosServerAdminApiKeyConfiguration.Invalid("duplicate_admin_api_key_configuration");
            }

            registrations.Add(new PosServerAdminApiKeyRegistration(
                principalName,
                secret,
                enabled,
                permissions,
                sitePosServerScopes,
                fiscalIdentityScopes));
        }

        if (registrations.Count == 0)
        {
            var legacyKey = ResolveConfiguredApiKey(configuration);
            if (!string.IsNullOrWhiteSpace(legacyKey))
            {
                registrations.Add(new PosServerAdminApiKeyRegistration(
                    "sales-invoice-header-profile-admin",
                    legacyKey,
                    Enabled: true,
                    [RequiredPermission],
                    [],
                    []));
            }
        }

        return registrations.Count == 0
            ? PosServerAdminApiKeyConfiguration.Invalid("missing_admin_api_key_configuration")
            : PosServerAdminApiKeyConfiguration.Valid(registrations);
    }

    public static PosServerAdminApiKeyAuthenticationResult Authenticate(
        IConfiguration configuration,
        StringValues suppliedKeyValues,
        StringValues requestedPermissionValues)
    {
        var configuredApiKeys = ResolveConfiguredApiKeys(configuration);
        if (!configuredApiKeys.IsValid)
        {
            return PosServerAdminApiKeyAuthenticationResult.Failed(PosServerAdminApiKeyAuthenticationStatus.ConfigurationInvalid);
        }

        if (suppliedKeyValues.Count != 1 || string.IsNullOrWhiteSpace(suppliedKeyValues[0]))
        {
            return PosServerAdminApiKeyAuthenticationResult.Failed(PosServerAdminApiKeyAuthenticationStatus.MissingCredential);
        }

        var suppliedKey = suppliedKeyValues[0]!;
        var matchedRegistration = configuredApiKeys.Registrations.FirstOrDefault(registration => SecretEquals(suppliedKey, registration.Secret));
        if (matchedRegistration is null)
        {
            return PosServerAdminApiKeyAuthenticationResult.Failed(PosServerAdminApiKeyAuthenticationStatus.InvalidCredential);
        }

        if (!matchedRegistration.Enabled)
        {
            return PosServerAdminApiKeyAuthenticationResult.Failed(PosServerAdminApiKeyAuthenticationStatus.DisabledCredential);
        }

        return PosServerAdminApiKeyAuthenticationResult.Authenticated(
            matchedRegistration.PrincipalName,
            ResolveServerDerivedPermissions(matchedRegistration, requestedPermissionValues),
            matchedRegistration.SitePosServerScopes,
            matchedRegistration.FiscalIdentityScopes);
    }

    private static string[] ReadScopes(IConfigurationSection registration, string sectionName) =>
        registration.GetSection(sectionName).GetChildren()
            .Select(scope => scope.Value)
            .Append(registration[sectionName.TrimEnd('s')])
            .Where(scope => !string.IsNullOrWhiteSpace(scope))
            .Select(scope => scope!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static bool ScopesAreValid(IEnumerable<string> scopes) =>
        scopes.All(scope => scope == "*" || Guid.TryParse(scope, out var value) && value != Guid.Empty);

    private static IReadOnlyList<string> ResolveServerDerivedPermissions(
        PosServerAdminApiKeyRegistration registration,
        StringValues requestedPermissionValues)
    {
        if (requestedPermissionValues.Count == 0)
        {
            return registration.Permissions;
        }

        if (requestedPermissionValues.Count != 1)
        {
            return [];
        }

        var requestedPermission = requestedPermissionValues[0]?.Trim();
        if (string.IsNullOrWhiteSpace(requestedPermission) ||
            requestedPermission.Contains(',', StringComparison.Ordinal))
        {
            return [];
        }

        return registration.Permissions.Contains(requestedPermission, StringComparer.Ordinal)
            ? [requestedPermission]
            : [];
    }

    private static bool SecretEquals(string suppliedSecret, string configuredSecret)
    {
        var suppliedBytes = Encoding.UTF8.GetBytes(suppliedSecret);
        var configuredBytes = Encoding.UTF8.GetBytes(configuredSecret);

        return suppliedBytes.Length == configuredBytes.Length &&
            CryptographicOperations.FixedTimeEquals(suppliedBytes, configuredBytes);
    }
}

public sealed record PosServerAdminApiKeyRegistration(
    string PrincipalName,
    string Secret,
    bool Enabled,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<string> SitePosServerScopes,
    IReadOnlyList<string> FiscalIdentityScopes);

public sealed record PosServerAdminApiKeyConfiguration(
    bool IsValid,
    IReadOnlyList<PosServerAdminApiKeyRegistration> Registrations,
    string? FailureCode)
{
    public static PosServerAdminApiKeyConfiguration Valid(IReadOnlyList<PosServerAdminApiKeyRegistration> registrations) =>
        new(true, registrations, null);

    public static PosServerAdminApiKeyConfiguration Invalid(string failureCode) =>
        new(false, [], failureCode);
}

public enum PosServerAdminApiKeyAuthenticationStatus
{
    Authenticated,
    MissingCredential,
    InvalidCredential,
    DisabledCredential,
    ConfigurationInvalid
}

public sealed record PosServerAdminApiKeyAuthenticationResult(
    PosServerAdminApiKeyAuthenticationStatus Status,
    string? PrincipalName,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<string> SitePosServerScopes,
    IReadOnlyList<string> FiscalIdentityScopes)
{
    public bool Succeeded => Status == PosServerAdminApiKeyAuthenticationStatus.Authenticated;

    public static PosServerAdminApiKeyAuthenticationResult Authenticated(
        string principalName,
        IReadOnlyList<string> permissions,
        IReadOnlyList<string> sitePosServerScopes,
        IReadOnlyList<string> fiscalIdentityScopes) =>
        new(PosServerAdminApiKeyAuthenticationStatus.Authenticated, principalName, permissions, sitePosServerScopes, fiscalIdentityScopes);

    public static PosServerAdminApiKeyAuthenticationResult Failed(PosServerAdminApiKeyAuthenticationStatus status) =>
        new(status, null, [], [], []);
}

public sealed class PosServerAdminApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
}

public sealed class PosServerAdminApiKeyAuthenticationHandler : AuthenticationHandler<PosServerAdminApiKeyAuthenticationOptions>
{
    private readonly IConfiguration configuration;

    public PosServerAdminApiKeyAuthenticationHandler(
        IOptionsMonitor<PosServerAdminApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration)
        : base(options, logger, encoder)
    {
        this.configuration = configuration;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        Request.Headers.TryGetValue(SalesInvoiceHeaderProfileAdminAuthorization.ApiKeyHeaderName, out var suppliedKey);
        Request.Headers.TryGetValue(SalesInvoiceHeaderProfileAdminAuthorization.PermissionHeaderName, out var requestedPermission);
        var authentication = SalesInvoiceHeaderProfileAdminAuthorization.Authenticate(
            configuration,
            suppliedKey,
            requestedPermission);

        if (authentication.Status == PosServerAdminApiKeyAuthenticationStatus.MissingCredential)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (!authentication.Succeeded)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid POS Server profile administration API credential."));
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, authentication.PrincipalName!)
        };
        claims.AddRange(authentication.Permissions.Select(permission =>
            new Claim(SalesInvoiceHeaderProfileAdminAuthorization.PermissionClaimType, permission)));
        claims.AddRange(authentication.SitePosServerScopes.Select(scope =>
            new Claim(FiscalXReadingAuthorization.SitePosServerScopeClaimType, scope)));
        claims.AddRange(authentication.FiscalIdentityScopes.Select(scope =>
            new Claim(FiscalXReadingAuthorization.FiscalIdentityScopeClaimType, scope)));

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}
