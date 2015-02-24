using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using Thinktecture.IdentityServer.Core.Models;
using Thinktecture.IdentityServer.EntityFramework;
using Thinktecture.IdentityServer.EntityFramework.Entities;

namespace IdentityServer3.Contrib.PowerShell.EntityFramework
{
    [Cmdlet("Get", "IdSrvClient")]
    public class GetIdSrvClient : Cmdlet
    {
        [Parameter(Position = 0, Mandatory = true, HelpMessage = "Connection string to the database")]
        public string ConnectionString { get; set; }

        [Parameter(Position = 1, Mandatory = false, HelpMessage = "Schema of the database")]
        public string Schema { get; set; }


        [Parameter(Position = 2, Mandatory = false, HelpMessage = "Scope name to retrieve")]
        public string ClientId { get; set; }

        protected override void ProcessRecord()
        {
            var db = new ClientConfigurationDbContext(this.ConnectionString, this.Schema);

            if (!string.IsNullOrEmpty(this.ClientId))
            {
                var client = db.Clients.FirstOrDefault(s => s.ClientId == this.ClientId);
                WriteObject(client.ToModel());
            }
            else
            {
                var dbclients = db.Clients.ToList();
                var clients = from c in dbclients select c.ToModel();
                WriteObject(clients);
            }
        }

    }

    [Cmdlet("Set", "IdSrvClient")]
    public class SetIdSrvClient : Cmdlet
    {
        [Parameter(Position = 0, Mandatory = true, HelpMessage = "Connection string to the database")]
        public string ConnectionString { get; set; }

        [Parameter(Position = 1, Mandatory = false, HelpMessage = "Schema of the database")]
        public string Schema { get; set; }


        [Parameter(Position = 2, Mandatory = false, HelpMessage = "Client object to update")]
        public Thinktecture.IdentityServer.Core.Models.Client Client { get; set; }

        protected override void ProcessRecord()
        {
            var db = new ClientConfigurationDbContext(this.ConnectionString, this.Schema);

            var entity = db.Clients.Single(c => c.ClientId == this.Client.ClientId);
            var currentId = entity.Id;
            var currentclient = entity.ToModel();

            var attachedEntry = db.Entry(entity);
            var newentity = this.Client.ToEntity();
            newentity.Id = currentId;
            attachedEntry.CurrentValues.SetValues(newentity);

            // Synchronize nav properties - FIXME ugly
            foreach (var uri in currentclient.RedirectUris.Union(this.Client.RedirectUris).Distinct())
            {
                if (currentclient.RedirectUris.Contains(uri) && !this.Client.RedirectUris.Contains(uri))
                {
                    var urientity = entity.RedirectUris.First(x => x.Uri == uri);
                    db.Entry(urientity).State = System.Data.Entity.EntityState.Deleted;
                    entity.RedirectUris.Remove(urientity);

                }
                else if (!currentclient.RedirectUris.Contains(uri) && this.Client.RedirectUris.Contains(uri))
                    entity.RedirectUris.Add(new ClientRedirectUri() { Uri = uri });
            }
            foreach (var uri in currentclient.PostLogoutRedirectUris.Union(this.Client.PostLogoutRedirectUris).Distinct())
            {
                if (currentclient.PostLogoutRedirectUris.Contains(uri) && !this.Client.PostLogoutRedirectUris.Contains(uri))
                {
                    var urientity = entity.PostLogoutRedirectUris.First(x => x.Uri == uri);
                    db.Entry(urientity).State = System.Data.Entity.EntityState.Deleted;
                    entity.PostLogoutRedirectUris.Remove(urientity);
                }
                else if (!currentclient.PostLogoutRedirectUris.Contains(uri) && this.Client.PostLogoutRedirectUris.Contains(uri))
                    entity.PostLogoutRedirectUris.Add(new ClientPostLogoutRedirectUri() { Uri = uri });
            }
            foreach (var gt in currentclient.CustomGrantTypeRestrictions.Union(this.Client.CustomGrantTypeRestrictions).Distinct())
            {
                if (currentclient.CustomGrantTypeRestrictions.Contains(gt) && !this.Client.CustomGrantTypeRestrictions.Contains(gt))
                {
                    var gtentity = entity.CustomGrantTypeRestrictions.First(x => x.GrantType == gt);
                    db.Entry(gtentity).State = System.Data.Entity.EntityState.Deleted;
                    entity.CustomGrantTypeRestrictions.Remove(gtentity);
                }
                else if (!currentclient.CustomGrantTypeRestrictions.Contains(gt) && this.Client.CustomGrantTypeRestrictions.Contains(gt))
                    entity.CustomGrantTypeRestrictions.Add(new ClientGrantTypeRestriction() { GrantType = gt });
            }
            foreach (var scope in currentclient.ScopeRestrictions.Union(this.Client.ScopeRestrictions).Distinct())
            {
                if (currentclient.ScopeRestrictions.Contains(scope) && !this.Client.ScopeRestrictions.Contains(scope))
                {
                    var scopenetity = entity.ScopeRestrictions.First(x => x.Scope == scope);
                    db.Entry(scopenetity).State = System.Data.Entity.EntityState.Deleted;
                    entity.ScopeRestrictions.Remove(scopenetity);
                }
                else if (!currentclient.ScopeRestrictions.Contains(scope) && this.Client.ScopeRestrictions.Contains(scope))
                    entity.ScopeRestrictions.Add(new ClientScopeRestriction() { Scope = scope });
            }
            foreach (var provider in currentclient.IdentityProviderRestrictions.Union(this.Client.IdentityProviderRestrictions).Distinct())
            {
                if (currentclient.IdentityProviderRestrictions.Contains(provider) && !this.Client.ScopeRestrictions.Contains(provider))
                {
                    var idpentity = entity.IdentityProviderRestrictions.First(x => x.Provider == provider);
                    db.Entry(idpentity).State = System.Data.Entity.EntityState.Deleted;
                    entity.IdentityProviderRestrictions.Remove(idpentity);
                }
                else if (!currentclient.IdentityProviderRestrictions.Contains(provider) && this.Client.IdentityProviderRestrictions.Contains(provider))
                    entity.IdentityProviderRestrictions.Add(new ClientIdPRestriction() { Provider = provider });
            }
            foreach (var secret in currentclient.ClientSecrets.Union(this.Client.ClientSecrets).Distinct())
            {
                if (currentclient.ClientSecrets.Any(x => x.Value == secret.Value) && !this.Client.ClientSecrets.Any(x => x.Value == secret.Value))
                    entity.ClientSecrets.Remove(entity.ClientSecrets.First(x => x.Value == secret.Value));
                else if (!currentclient.ClientSecrets.Any(x => x.Value == secret.Value) && this.Client.ClientSecrets.Any(x => x.Value == secret.Value))
                    entity.ClientSecrets.Add(new Thinktecture.IdentityServer.EntityFramework.Entities.ClientSecret
                    {
                        ClientSecretType = secret.ClientSecretType,
                        Description = secret.ClientSecretType,
                        Expiration = secret.Expiration,
                        Value = secret.Value
                    });
            }
            foreach (var claim in currentclient.Claims.Union(this.Client.Claims).Distinct())
            {
                if (currentclient.Claims.Any(x => x.Type == claim.Type) && !this.Client.Claims.Any(x => x.Type == claim.Type))
                    entity.Claims.Remove(entity.Claims.First(x => x.Type == claim.Type && x.Value == claim.Value));
                else if (!currentclient.Claims.Any(x => x.Type == claim.Type) && this.Client.Claims.Any(x => x.Type == claim.Type))
                    entity.Claims.Add(new Thinktecture.IdentityServer.EntityFramework.Entities.ClientClaim()
                    {
                        Type = claim.Type,
                        Value = claim.Value
                    });
            }

            try
            {
                db.SaveChanges();
            }
            catch (System.Data.Entity.Infrastructure.DbUpdateException dbue)
            {
                WriteError(new ErrorRecord(dbue.InnerException, "DbUpdateException", ErrorCategory.WriteError, entity));
                throw dbue;
            }
            WriteObject(entity.ToModel());
        }

    }

    [Cmdlet("Add", "IdSrvClient")]
    public class AddIdSrvClient : Cmdlet
    {
        [Parameter(Position = 0, Mandatory = true, HelpMessage = "Connection string to the database")]
        public string ConnectionString { get; set; }
        [Parameter(Position = 1, Mandatory = false, HelpMessage = "Schema of the database")]
        public string Schema { get; set; }

        [Parameter(Position = 2, Mandatory = true, HelpMessage = "The id of the client")]
        public string ClientId { get; set; }
        [Parameter(Position = 3, Mandatory = true, HelpMessage = "The name of the client")]
        public string ClientName { get; set; }
        [Parameter(Position = 4, Mandatory = false, HelpMessage = "A list of possible secrets for the client")]
        public string[] ClientSecrets { get; set; }
        [Parameter(Position = 5, Mandatory = true, HelpMessage = "The flow allowed for the client")]
        public Flows Flow { get; set; }
        [Parameter(Position = 6, Mandatory = false, HelpMessage = "Redirect Uris allowed after login")]
        public List<string> RedirectUris { get; set; }
        [Parameter(Position = 7, Mandatory = false, HelpMessage = "Redirect Uris allowed after logout")]
        public List<string> PostLogoutUris { get; set; }
        [Parameter(Position = 8, Mandatory = false, HelpMessage = "Scopes the client is allowed to request")]
        public List<string> ScopeRestrictions { get; set; }
        [Parameter(Position = 9, Mandatory = false, HelpMessage = "External identity providers allowed for this client")]
        public List<string> IdentityProviderRestrictions { get; set; }
        [Parameter(Position = 10, Mandatory = false, HelpMessage = "Type of access_token, Jwt or Reference")]
        public AccessTokenType AccessTokenType { get; set; }
        [Parameter(Position = 11, Mandatory = false, HelpMessage = "Tokens lifetime in seconds (for both identity and access)")]
        public int TokensLifetime { get; set; }
        [Parameter(Position = 12, Mandatory = false, HelpMessage = "Enable the new client right away")]
        public bool Enabled { get; set; }
        [Parameter(Position = 13, Mandatory = false, HelpMessage = "Always display the consent screen")]
        public bool RequireConsent { get; set; }
        [Parameter(Position = 14, Mandatory = false, HelpMessage = "Allow the user to remember the consent decision")]
        public bool AllowRememberConsent { get; set; }
        [Parameter(Position = 13, Mandatory = false, HelpMessage = "Home page of the client")]
        public string ClientUri { get; set; }
        [Parameter(Position = 14, Mandatory = false, HelpMessage = "Logo image to display for the client in consent screens")]
        public string LogoUri { get; set; }


        protected override void ProcessRecord()
        {
            var db = new ClientConfigurationDbContext(this.ConnectionString, this.Schema);

            var client = new Thinktecture.IdentityServer.Core.Models.Client
            {
                ClientId = this.ClientId,
                ClientName = this.ClientName,
                ClientSecrets = (from s in this.ClientSecrets
                                 select new Thinktecture.IdentityServer.Core.Models.ClientSecret(s)).ToList(),
                Flow = this.Flow,
                Enabled = this.Enabled,
                RequireConsent = this.RequireConsent,
                AllowRememberConsent = this.AllowRememberConsent,
                ClientUri = this.ClientUri,
                LogoUri = this.LogoUri,
                RedirectUris = this.RedirectUris,
                PostLogoutRedirectUris = this.PostLogoutUris,
                ScopeRestrictions = this.ScopeRestrictions,
                IdentityProviderRestrictions = this.IdentityProviderRestrictions,

                AccessTokenType = this.AccessTokenType,
                AccessTokenLifetime = this.TokensLifetime,
                IdentityTokenLifetime = this.TokensLifetime,
                AuthorizationCodeLifetime = this.TokensLifetime // bad

            };

            db.Clients.Add(client.ToEntity());
            db.SaveChanges();
            WriteObject(client);
        }
    }

    [Cmdlet("Remove", "IdSrvClient")]
    public class RemoveIdSrvClient : Cmdlet
    {
        [Parameter(Position = 0, Mandatory = true, HelpMessage = "Connection string to the database")]
        public string ConnectionString { get; set; }
        [Parameter(Position = 1, Mandatory = false, HelpMessage = "Schema of the database")]
        public string Schema { get; set; }

        [Parameter(Position = 2, Mandatory = true, HelpMessage = "ClientId of the client to remove")]
        public string ClientId { get; set; }

        protected override void ProcessRecord()
        {
            var db = new ClientConfigurationDbContext(this.ConnectionString, this.Schema);

            var entity = db.Clients.First(s => s.ClientId == this.ClientId);

            db.Clients.Remove(entity);
            db.SaveChanges();
        }
    }

}
