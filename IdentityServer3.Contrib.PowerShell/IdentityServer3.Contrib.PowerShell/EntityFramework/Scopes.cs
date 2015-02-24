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
    [Cmdlet("Get", "IdSrvScope")]
    public class GetIdSrvScopes : Cmdlet
    {
        [Parameter(Position = 0, Mandatory = true, HelpMessage = "Connection string to the database")]
        public string ConnectionString { get; set; }
        [Parameter(Position = 1, Mandatory = false, HelpMessage = "Schema of the database")]
        public string Schema { get; set; }

        [Parameter(Position = 2, Mandatory = false, HelpMessage = "Scope name to retrieve")]
        public string ScopeName { get; set; }

        protected override void ProcessRecord()
        {
            //var db = new ClientConfigurationDbContext(this.ConnectionString, this.Schema);
            var db = new ScopeConfigurationDbContext(this.ConnectionString, this.Schema);

            if (!string.IsNullOrEmpty(ScopeName))
            {
                var scope = db.Scopes.FirstOrDefault(s => s.Name == ScopeName);
                WriteObject(scope.ToModel());
            }
            else
            {
                var dbscopes = db.Scopes.ToList();
                var scopes = from c in dbscopes select c.ToModel();
                WriteObject(scopes);
            }
        }

    }

    [Cmdlet("Add", "IdSrvScope")]
    public class AddIdSrvScope : Cmdlet
    {
        [Parameter(Position = 0, Mandatory = true, HelpMessage = "Connection string to the database")]
        public string ConnectionString { get; set; }
        [Parameter(Position = 1, Mandatory = false, HelpMessage = "Schema of the database")]
        public string Schema { get; set; }

        [Parameter(Position = 2, Mandatory = true, HelpMessage = "The name of the scope")]
        public string Name { get; set; }
        [Parameter(Position = 3, Mandatory = false, HelpMessage = "The display name of the scope (in consent screens etc.)")]
        public string DisplayName { get; set; }
        [Parameter(Position = 4, Mandatory = false, HelpMessage = "The description of the scope (in consent screens etc.)")]
        public string Description { get; set; }
        [Parameter(Position = 5, Mandatory = false, HelpMessage = "The type of the scope, Identity or Resource")]
        public ScopeType ScopeType { get; set; }
        [Parameter(Position = 6, Mandatory = false, HelpMessage = "Emphasize on consent screens")]
        public bool Emphasize { get; set; }
        [Parameter(Position = 7, Mandatory = false, HelpMessage = "Include in discovery document")]
        public bool Discoverable { get; set; }
        [Parameter(Position = 8, Mandatory = false, HelpMessage = "Scope must be consented to")]
        public bool Required { get; set; }

        protected override void ProcessRecord()
        {
            var db = new ScopeConfigurationDbContext(this.ConnectionString, this.Schema);

            var scope = new Thinktecture.IdentityServer.Core.Models.Scope
            {
                Name = this.Name,
                DisplayName = this.DisplayName,
                Description = this.Description,
                Type = this.ScopeType,
                ShowInDiscoveryDocument = this.Discoverable,
                Emphasize = this.Emphasize,
                Required = this.Required,
            };

            db.Scopes.Add(scope.ToEntity());
            db.SaveChanges();
            WriteObject(scope);
        }
    }

    [Cmdlet("Set", "IdSrvScope")]
    public class SetIdSrvScope : Cmdlet
    {
        [Parameter(Position = 0, Mandatory = true, HelpMessage = "Connection string to the database")]
        public string ConnectionString { get; set; }
        [Parameter(Position = 1, Mandatory = false, HelpMessage = "Schema of the database")]
        public string Schema { get; set; }

        [Parameter(Position = 2, Mandatory = false, HelpMessage = "Scope object to update")]
        public Thinktecture.IdentityServer.Core.Models.Scope Scope { get; set; }

        protected override void ProcessRecord()
        {
            var db = new ScopeConfigurationDbContext(this.ConnectionString, this.Schema);

            var entity = db.Scopes.Single(c => c.Name == this.Scope.Name);
            var currentId = entity.Id;
            var currentscope = entity.ToModel();

            var attachedEntry = db.Entry(entity);
            var newentity = this.Scope.ToEntity();
            newentity.Id = currentId;
            attachedEntry.CurrentValues.SetValues(newentity);

            // Synchronize scope claims nav property - FIXME ugly
            foreach (var scope in currentscope.Claims.Union(this.Scope.Claims).Distinct())
            {
                if (currentscope.Claims.Any(x => x.Name == scope.Name) && !this.Scope.Claims.Any(x => x.Name == scope.Name))
                    entity.ScopeClaims.Remove(entity.ScopeClaims.First(x => x.Name == scope.Name));
                else if (!currentscope.Claims.Any(x => x.Name == scope.Name) && this.Scope.Claims.Any(x => x.Name == scope.Name))
                    entity.ScopeClaims.Add(new Thinktecture.IdentityServer.EntityFramework.Entities.ScopeClaim()
                    {
                        Name = scope.Name,
                        Description = scope.Description,
                        AlwaysIncludeInIdToken = scope.AlwaysIncludeInIdToken
                    });
            }

            db.SaveChanges();
        }

    }

    [Cmdlet("Remove", "IdSrvScope")]
    public class RemoveIdSrvScope : Cmdlet
    {
        [Parameter(Position = 0, Mandatory = true, HelpMessage = "Connection string to the database")]
        public string ConnectionString { get; set; }
        [Parameter(Position = 1, Mandatory = false, HelpMessage = "Schema of the database")]
        public string Schema { get; set; }

        [Parameter(Position = 2, Mandatory = true, HelpMessage = "Name of the scope to remove")]
        public string ScopeName { get; set; }

        protected override void ProcessRecord()
        {
            var db = new ScopeConfigurationDbContext(this.ConnectionString, this.Schema);

            var entity = db.Scopes.First(s => s.Name == this.ScopeName);

            db.Scopes.Remove(entity);
            db.SaveChanges();
        }
    }

}
