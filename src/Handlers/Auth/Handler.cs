using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Alejof.Notes.Storage;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.WindowsAzure.Storage.Table;

namespace Alejof.Notes.Handlers.Auth
{
    public class Identity
    {        
        public string TenantId { get; set; }
        public string Nickname { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
    }

    public class Request : IRequest<(Identity, string)>
    {
        public HttpRequest HttpRequest { get; set; }
    }

    public class Handler : IRequestHandler<Request, (Identity, string)>
    {
        private static readonly Dictionary<string, Auth0TokenValidator> TenantAuthenticators = new Dictionary<string, Auth0TokenValidator>();
        private const string TenantIdHeaderName = "Notes-Tenant-Id";

        protected readonly CloudTable _tenantMappingTable;


        public Handler(
            CloudTableClient tableClient)
        {
            this._tenantMappingTable = tableClient.GetTableReference(TenantEntity.TableName);
        }

        // https://liftcodeplay.com/2017/11/25/validating-auth0-jwt-tokens-in-azure-functions-aka-how-to-use-auth0-with-azure-functions/
        public async Task<(Identity, string)> Handle(Request request, CancellationToken cancellationToken)
        {
            // Get TenantId and Bearer token
            string tenantId = request.HttpRequest.Headers[TenantIdHeaderName];
            if (string.IsNullOrEmpty(tenantId))
                return (null, "TenantId header not present");

            string token = request.HttpRequest.Headers["Authorization"];
            if (token?.StartsWith("Bearer") != true)
                return (null, "Authorization header not present or not using valid scheme");

            token = token.Substring("Bearer".Length).Trim();

            // Get/Build authenticator for Tenant
            if (!TenantAuthenticators.TryGetValue(tenantId, out var validator))
            {
                validator = await BuildTokenValidator(tenantId);
                if (validator == null)
                    return (null, "Tenant mapping not found");

                TenantAuthenticators.Add(tenantId, validator);
            }

            // Validate token using authenticator
            var (principal, error) = await validator.ValidateTokenAsync(token);
            if (principal == null)
                return (null, "Token not valid. " + error);

            // Build AuthData
            var identity = BuildIdentity(tenantId, principal);
            return (identity, null);
        }

        private async Task<Auth0TokenValidator> BuildTokenValidator(string tenantId)
        {
            // find tenantId mapping in Storage
            await _tenantMappingTable.CreateIfNotExistsAsync();
            var tenant = await _tenantMappingTable.RetrieveAsync<TenantEntity>(TenantEntity.DefaultKey, tenantId);

            if (tenant != null)
                return new Auth0TokenValidator(tenant.Domain, tenant.ClientId);

            return null;
        }

        private Identity BuildIdentity(string tenantId, ClaimsPrincipal principal)
        {
            // local function
            string findClaim(ClaimsPrincipal p, string type) => p.Claims
                .FirstOrDefault(c => string.Equals(c.Type, type, StringComparison.OrdinalIgnoreCase))
                ?.Value;

            return new Identity
            {
                TenantId = tenantId,
                Nickname = findClaim(principal, "nickname"),
                FullName = findClaim(principal, "name"),
                Email = findClaim(principal, "email"),
            };
        }
    }

    ///
    /// <summary>PartitionKey: "tenant". RowKey: tenantId</summary>
    ///
    public class TenantEntity : TableEntity
    {
        public const string TableName = "NoteAppTenants";
        public const string DefaultKey = "tenant";

        public string ClientId { get; set; }
        public string Domain { get; set; }
    }
}
