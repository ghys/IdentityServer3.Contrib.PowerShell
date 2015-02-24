# IdentityServer3.Contrib.PowerShell
PowerShell cmdlets for administering Thinktecture IdentityServer3's Clients &amp; Scopes persisted with Entity Framework.

## Motive

IdentityServer3 is great, but the lack of administrative tools especially for devops and production teams who are not willing to hack into an unknown database may hamper adoption. This is an attempt to fix that, until there's something official which can perform this task. You can include this PowerShell module in deployment/install packages, use in scripts or give it to the devops so they can perform occasional maintenance of clients and scopes.

## Getting started

- You need a recent version of PowerShell, only 4.0 is supported for now. This is the one which comes with Windows Server 2008 R2 and Windows 8.1.

- Before building make sure the Thinktecture.IdentityServer3.EntityFramework NuGet package you're going to restore is the same version as the one used by your server. You might have to deal with schema changes exceptions otherwise.

- Launch a PowerShell console (or PowerShell ISE) and import the module :
```powershell
PS > Import-Module IdentityServer3.Contrib.PowerShell.dll
```
Verify that the module is imported and commands are available with:
```powershell
PS > Get-Module
ModuleType Version    Name                                ExportedCommands                                                                       
---------- -------    ----                                ----------------                                                                       
Binary     1.0.0.0    IdentityServer3.Contrib.PowerShell  {Get-IdSrvClient, Set-IdSrvClient, Add-IdSrvClient, Remove-IdSrvClient...}             
Manifest   3.1.0.0    Microsoft.PowerShell.Management     {Add-Computer, Add-Content, Checkpoint-Computer, Clear-Content...}                     
Manifest   3.1.0.0    Microsoft.PowerShell.Utility        {Add-Member, Add-Type, Clear-Variable, Compare-Object...}   
```
You can also put your connection string in a variable since you're going to use it all the time.
```powershell
PS > $connectionString = "server=(LocalDb)\ProjectsV12;database=IdSvr3Config;trusted_connection=yes;"
```

## Currently supported commands

The ```Get-*``` cmdlets return an instance of the appropriate ```Thinktecture.IdentityServer3.Core.Models``` class, and the ```Set-*``` cmdlets accept it as a parameter. The ```Add-*``` cmdlets create a client or scope with the most common property as parameters. Help might get added along the way, but for now they are quite self-explanatory:

### Client management
```powershell
Get-IdSrvClient [-ConnectionString] <string> [[-Schema] <string>] [[-ClientId] <string>] [<CommonParameters>]

Set-IdSrvClient [-ConnectionString] <string> [[-Schema] <string>] [[-Client] <Client>] [<CommonParameters>]

Add-IdSrvClient [-ConnectionString] <string> [[-Schema] <string>] [-ClientId] <string> [-ClientName] <string> [[-ClientSecrets] <string[]>] [-Flow] <Flows> [[-RedirectUris] <List[string]>] [[-PostLogoutUris] <List[string]>] [[-ScopeRestrictions] <List[string]>] [[-IdentityProviderRestrictions] <List[string]>] [[-AccessTokenType] <AccessTokenType>] [[-TokensLifetime] <int>] [[-Enabled] <bool>] [[-ClientUri] <string>] [[-LogoUri] <string>] [<CommonParameters>]

Remove-IdSrvClient [-ConnectionString] <string> [[-Schema] <string>] [-ClientId] <string> [<CommonParameters>]
```

### Scope management
```powershell
Get-IdSrvScope [-ConnectionString] <string> [[-Schema] <string>] [[-ScopeName] <string>] [<CommonParameters>]

Set-IdSrvScope [-ConnectionString] <string> [[-Schema] <string>] [[-Scope] <Scope>] [<CommonParameters>]

Add-IdSrvScope [-ConnectionString] <string> [[-Schema] <string>] [-Name] <string> [[-DisplayName] <string>] [[-Description] <string>] [[-ScopeType] <ScopeType>] [[-Emphasize] <bool>] [[-Discoverable] <bool>] [[-Required] <bool>] [<CommonParameters>]

Remove-IdSrvScope [-ConnectionString] <string> [[-Schema] <string>] [-ScopeName] <string> [<CommonParameters>]
```

Note that ISE 3.0+ offers a nice GUI, how cool is that e.g. when adding a new client:
![ISE](http://i.imgur.com/jkwjtys.png)

## TODO

- Don't look too much at the code, it needs some serious refactoring ;)
- Maybe add some helper cmdlet for quickly adding e.g. a new redirect URI to a client
- Management of operational data? (tokens, consents, auth. codes...)


## Examples

### List scopes in a table
```powershell
PS > Get-IdSrvScope -ConnectionString $connectionString | ft Name, DisplayName, Type, Claims -AutoSize

Name           DisplayName          Type Claims                                         
----           -----------          ---- ------                                         
openid                          Identity {sub}                                          
profile                         Identity {name, family_name, given_name, middle_name...}
email                           Identity {email, email_verified}                        
offline_access                  Resource {}                                             
read           Read data        Resource {}                                             
write          Write data       Resource {}                                             
test           Manage test data Resource {}                                             
```

### Get clients with some properties in a list

```powershell
PS > Get-IdSrvClient -ConnectionString $connectionString | fl ClientName, Flow, RedirectUris, ScopeRestrictions


ClientName        : Code Flow Clients
Flow              : AuthorizationCode
RedirectUris      : {https://localhost:44320/oidccallback, https://localhost:44312/callback}
ScopeRestrictions : {openid, profile, email, offline_access...}

ClientName        : Implicit Clients
Flow              : Implicit
RedirectUris      : {oob://localhost/wpfclient, http://localhost:21575/index.html, 
                    http://localhost:11716/account/signInCallback, http://localhost:2671/}
ScopeRestrictions : {openid, profile, email, read...}

ClientName        : Resource Owner Flow Client
Flow              : ResourceOwner
RedirectUris      : {}
ScopeRestrictions : {offline_access, read, write}

ClientName        : My Client
Flow              : AuthorizationCode
RedirectUris      : {https://localhost:44301/callback, https://example.org/callback}
ScopeRestrictions : {}
```

### Add a client

```powershell
PS > Add-IdSrvClient -ClientId myclient -ClientName "My Client" -ConnectionString $connectionString -Flow Implicit -AccessTokenType Jwt -AllowRememberConsent $True -ClientSecrets secret1,secret2 -ClientUri http://example.org -Enabled $True -RedirectUris https://localhost:44301/callback,https://example.org/callback -ScopeRestrictions read,write -TokensLifetime 3600
```

### Get a client, change a few things and save it back

```powershell
PS > Get-IdSrvClient -ConnectionString $connectionString -ClientId myclient

PS > $client.ClientSecrets

Description                           Value                                 Expiration                            ClientSecretType                    
-----------                           -----                                 ----------                            ----------------                    
                                      secret1                                                                                                         
                                      secret2                                                                                                         


PS > $secret = $client.ClientSecrets[0]

PS > $newsecret = new-object -TypeName $secret.GetType()

PS > $newsecret.Value = "secret3"

PS > $client.ClientSecrets.Add($newsecret)

PS > $client.RedirectUris.Remove("https://developers.google.com/oauthplayground")

PS > $client.IdentityTokenLifetime = 14400

PS > $client.AccessTokenLifetime = 14400

PS > Set-IdSrvClient -ConnectionString $connectionString -Client $client


Enabled                      : True
ClientId                     : myclient
ClientSecrets                : {Thinktecture.IdentityServer.Core.Models.ClientSecret, Thinktecture.IdentityServer.Core.Models.ClientSecret, 
                               Thinktecture.IdentityServer.Core.Models.ClientSecret}
ClientName                   : My Client
ClientUri                    : 
LogoUri                      : 
RequireConsent               : False
AllowRememberConsent         : False
Flow                         : AuthorizationCode
RedirectUris                 : {https://localhost:44301/callback, https://example.org/callback}
PostLogoutRedirectUris       : {}
ScopeRestrictions            : {}
IdentityTokenLifetime        : 14400
AccessTokenLifetime          : 14400
AuthorizationCodeLifetime    : 3600
AbsoluteRefreshTokenLifetime : 2592000
SlidingRefreshTokenLifetime  : 1296000
RefreshTokenUsage            : OneTimeOnly
RefreshTokenExpiration       : Absolute
AccessTokenType              : Jwt
EnableLocalLogin             : True
IdentityProviderRestrictions : {}
IncludeJwtId                 : False
Claims                       : {}
AlwaysSendClientClaims       : False
PrefixClientClaims           : True
CustomGrantTypeRestrictions  : {}
```
