# Azure AD Integration Guide for FullStackHero .NET Starter Kit

## Overview

This guide provides detailed instructions for replacing the current JWT-based authentication system with **Azure Active Directory (Azure AD / Microsoft Entra ID)** authentication. This integration will enable enterprise SSO, centralized identity management, and seamless integration with Microsoft 365 services.

---

## Table of Contents

1. [Current Authentication Architecture](#current-authentication-architecture)
2. [Azure AD Authentication Options](#azure-ad-authentication-options)
3. [Recommended Approach](#recommended-approach)
4. [Implementation Plan](#implementation-plan)
5. [Azure AD Configuration](#azure-ad-configuration)
6. [Code Changes Required](#code-changes-required)
7. [Claims Mapping Strategy](#claims-mapping-strategy)
8. [Multi-Tenancy Considerations](#multi-tenancy-considerations)
9. [Migration Strategy](#migration-strategy)
10. [Security Considerations](#security-considerations)
11. [Testing Strategy](#testing-strategy)

---

## Current Authentication Architecture

### How It Works Now

The starter kit uses a **custom JWT-based authentication** system:

#### Backend (API)
- **Location:** `Modules/Identity/Modules.Identity/`
- **Components:**
  - `TokenService` - Issues JWT access tokens and refresh tokens
  - `JwtOptions` - Configuration for JWT signing, issuer, audience
  - `ConfigureJwtBearerOptions` - Configures JWT validation
  - `CurrentUserService` - Resolves current user from ClaimsPrincipal
  - ASP.NET Core Identity for user/password management

#### Token Flow
1. User submits credentials to `/api/v1/identity/token/issue`
2. `GenerateTokenCommandHandler` validates credentials against ASP.NET Identity
3. `TokenService.IssueAsync()` creates JWT with custom claims
4. Returns access token (30 min) and refresh token (7 days)
5. Blazor client stores tokens in-memory and attaches via `BffAuthDelegatingHandler`

#### Claims Structure
```csharp
// Custom claims in JWT
- ClaimTypes.NameIdentifier (User ID)
- ClaimTypes.Email
- ClaimTypes.Name (First Name)
- ClaimTypes.Surname
- CustomClaims.Tenant (tenant identifier)
- CustomClaims.Fullname
- CustomClaims.Permission (multiple, one per permission)
- CustomClaims.ImageUrl
```

#### Key Files
- `Modules.Identity/Authorization/Jwt/Extensions.cs` - JWT authentication setup
- `Modules.Identity/Services/TokenService.cs` - Token generation
- `BuildingBlocks/Web/Auth/CurrentUserMiddleware.cs` - User context initialization
- `Playground.Blazor/Services/Api/BffAuthDelegatingHandler.cs` - Token attachment

---

## Azure AD Authentication Options

### Option 1: Azure AD with Backend Token Issuance (Hybrid)

**Description:** Use Azure AD to authenticate users, but continue issuing your own JWT tokens with custom claims.

**Flow:**
```
User ? Azure AD ? Authorization Code ? API validates with Azure AD ? 
API issues custom JWT with permissions ? API endpoints validate custom JWT
```

**Pros:**
- Retains control over token format and claims
- Existing permission system works unchanged
- Can include custom business logic in token issuance
- Easier migration path

**Cons:**
- More complex - two token systems
- Requires Azure AD token exchange
- Additional latency for token validation

**Best For:**
- Existing apps with complex permission systems
- Need custom claims not in Azure AD
- Gradual migration approach

---

### Option 2: Azure AD Native Integration (Recommended)

**Description:** Fully replace JWT system with Azure AD tokens validated directly by the API.

**Flow:**
```
User ? Azure AD ? Access Token ? API validates token with Azure AD ? 
Extract roles/permissions from token ? Authorize endpoints
```

**Pros:**
- Simpler architecture - single source of truth
- Leverages Azure AD's security features
- Built-in token refresh and validation
- Industry standard approach
- Better for enterprise SSO

**Cons:**
- Need to migrate permissions to Azure AD App Roles
- Less flexibility for custom claims
- Requires Azure AD Premium for advanced features

**Best For:**
- New applications
- Enterprise environments with Azure AD
- Standard RBAC scenarios

---

### Option 3: Azure AD B2C for Multi-Tenant SaaS

**Description:** Use Azure AD B2C for customer-facing multi-tenant SaaS applications.

**Flow:**
```
User ? Azure AD B2C (with branding) ? Access Token ? 
API validates with B2C ? Custom policies for registration/login
```

**Pros:**
- Purpose-built for customer identity
- Custom branding per tenant
- Social identity providers
- Self-service password reset
- Scales to millions of users

**Cons:**
- More expensive than Azure AD
- Complex policy configuration
- Separate service from Azure AD

**Best For:**
- Customer-facing SaaS applications
- Need social login (Google, Facebook, etc.)
- White-label solutions

---

## Recommended Approach

### **Option 2: Azure AD Native Integration**

**Rationale:**
- Cleanest architecture for enterprise applications
- The starter kit already uses permission-based authorization that maps well to Azure AD App Roles
- Multi-tenancy can be handled via Azure AD tenant ID
- Simplifies maintenance by removing custom token logic

---

## Implementation Plan

### Phase 1: Azure AD Setup
1. Register application in Azure AD
2. Configure redirect URIs
3. Define App Roles for permissions
4. Set up API permissions
5. Create service principal

### Phase 2: Backend Changes
1. Replace JWT configuration with Azure AD authentication
2. Update `IdentityModule` to use Azure AD tokens
3. Map Azure AD claims to `ICurrentUser`
4. Migrate permission checks to use Azure AD roles
5. Update middleware pipeline
6. Remove custom `TokenService`

### Phase 3: Blazor Client Changes
1. Add Microsoft.Identity.Web.UI package
2. Replace custom auth with MSAL.js or server-side flow
3. Update `BffAuthDelegatingHandler` to use Azure AD tokens
4. Implement token acquisition and refresh
5. Update login/logout flows

### Phase 4: Multi-Tenancy Integration
1. Map Azure AD tenant to application tenant
2. Update tenant resolution middleware
3. Handle cross-tenant scenarios
4. Implement tenant-specific App Roles

### Phase 5: Testing & Migration
1. Set up test Azure AD environment
2. Create test users and roles
3. Parallel run with existing auth (feature flag)
4. Migrate existing users
5. Decommission old auth system

---

## Azure AD Configuration

### Step 1: Register Application in Azure Portal

```
Navigate to: Azure Portal ? Azure Active Directory ? App registrations ? New registration

Name: FSH Playground API
Supported account types: 
  - Single tenant (for internal apps)
  - Multi-tenant (for SaaS apps)
Redirect URI: https://localhost:7030/signin-oidc (Blazor)
```

### Step 2: Configure API Permissions

```
API Permissions ? Add a permission ? Microsoft Graph

Delegated permissions:
- User.Read (basic profile)
- email
- openid
- profile

Application permissions (if using service-to-service):
- User.Read.All
```

### Step 3: Define App Roles (Permissions)

```json
// Navigate to: App roles ? Create app role
{
  "allowedMemberTypes": ["User"],
  "description": "Can view all users",
  "displayName": "Users.View",
  "id": "unique-guid-here",
  "isEnabled": true,
  "value": "Users.View"
}
```

**Map existing permissions to App Roles:**

Current Permission System (from `IdentityPermissionConstants`):
```
Identity.Users.View
Identity.Users.Create
Identity.Users.Update
Identity.Users.Delete
Identity.Roles.View
Identity.Roles.Create
Identity.Roles.Update
Identity.Roles.Delete
```

Create corresponding App Roles in Azure AD with the same naming convention.

### Step 4: Expose API Scopes

```
Expose an API ? Add a scope

Scope name: api.access
Admin consent display name: Access FSH API
Admin consent description: Allows the app to access FSH API on behalf of the signed-in user
State: Enabled
```

### Step 5: Configure Authentication

```
Authentication ? Platform configurations ? Add a platform ? Web

Redirect URIs:
- https://localhost:7030/signin-oidc (Blazor dev)
- https://yourdomain.com/signin-oidc (production)

Logout URL: https://localhost:7030/signout-callback-oidc

Token configuration:
? Access tokens (used for implicit flows)
? ID tokens (used for implicit and hybrid flows)
```

---

## Code Changes Required

### 1. API - Update Authentication Configuration

**File:** `Modules/Identity/Modules.Identity/Authorization/AzureAd/Extensions.cs` (NEW)

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;

namespace FSH.Modules.Identity.Authorization.AzureAd;

internal static class Extensions
{
    internal static IServiceCollection ConfigureAzureAdAuth(this IServiceCollection services, IConfiguration configuration)
    {
        // Use Microsoft.Identity.Web for Azure AD authentication
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApi(configuration.GetSection("AzureAd"))
            .EnableTokenAcquisitionToCallDownstreamApi()
            .AddInMemoryTokenCaches();

        services.AddAuthorizationBuilder()
            .AddRequiredPermissionPolicy(); // Keep existing permission system

        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = options.GetPolicy(RequiredPermissionDefaults.PolicyName);
        });

        return services;
    }
}
```

**Configuration:** `appsettings.json`

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "yourdomain.onmicrosoft.com",
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc",
    "Audience": "api://your-api-client-id"
  }
}
```

### 2. Update IdentityModule

**File:** `Modules/Identity/Modules.Identity/IdentityModule.cs`

```csharp
public void ConfigureServices(IHostApplicationBuilder builder)
{
    // REMOVE: services.ConfigureJwtAuth();
    // ADD: services.ConfigureAzureAdAuth(builder.Configuration);

    // Keep existing services
    services.AddScoped<ICurrentUser, CurrentUserService>();
    services.AddHeroDbContext<IdentityDbContext>();
    
    // REMOVE: TokenService (no longer needed)
    // services.AddScoped<ITokenService, TokenService>();
    
    // Keep user/role services for profile management
    services.AddTransient<IUserService, UserService>();
    services.AddTransient<IRoleService, RoleService>();
}
```

### 3. Update CurrentUserService to Map Azure AD Claims

**File:** `Modules/Identity/Modules.Identity/Services/CurrentUserService.cs`

```csharp
public class CurrentUserService : ICurrentUser, ICurrentUserInitializer
{
    private ClaimsPrincipal? _user;

    public Guid GetUserId()
    {
        // Azure AD uses 'oid' (object identifier) claim
        var oid = _user?.FindFirstValue("oid") 
                  ?? _user?.FindFirstValue(ClaimTypes.NameIdentifier);
        
        return IsAuthenticated() && Guid.TryParse(oid, out var userId)
            ? userId
            : Guid.Empty;
    }

    public string? GetUserEmail() =>
        _user?.FindFirstValue("preferred_username") // Azure AD claim
        ?? _user?.FindFirstValue(ClaimTypes.Email);

    public string? GetTenant() =>
        _user?.FindFirstValue("tid") // Azure AD tenant ID
        ?? _user?.FindFirstValue(CustomClaims.Tenant);

    public IEnumerable<string> GetRoles() =>
        _user?.FindAll("roles").Select(c => c.Value) ?? Enumerable.Empty<string>();
    
    // ... rest of implementation
}
```

### 4. Update Permission Authorization Handler

**File:** `Modules/Identity/Modules.Identity/Authorization/Permissions/PermissionAuthorizationHandler.cs`

```csharp
protected override async Task HandleRequirementAsync(
    AuthorizationHandlerContext context,
    PermissionRequirement requirement)
{
    // Check Azure AD App Roles (comes as 'roles' claim)
    var roles = context.User.FindAll("roles").Select(c => c.Value);
    
    if (roles.Contains(requirement.Permission))
    {
        context.Succeed(requirement);
        return;
    }

    // Fallback: Check database permissions (for migration period)
    var userId = context.User.GetUserId();
    var permissions = await _userService.GetPermissionsAsync(userId);
    
    if (permissions.Contains(requirement.Permission))
    {
        context.Succeed(requirement);
    }
}
```

### 5. Blazor Client - Update Authentication

**File:** `Playground/Playground.Blazor/Program.cs`

```csharp
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

var builder = WebApplication.CreateBuilder(args);

// Replace custom token handling with Microsoft.Identity.Web
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;
});

// REMOVE: Custom token services
// builder.Services.AddSingleton<ITokenStore, InMemoryTokenStore>();
// builder.Services.AddScoped<ITokenAccessor, TokenAccessor>();

builder.Services.AddHttpClient("FSH.API", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
})
.AddMicrosoftIdentityUserAuthenticationHandler("AzureAd"); // Automatic token acquisition

// ... rest of configuration
```

**Configuration:** `Playground.Blazor/appsettings.json`

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "yourdomain.onmicrosoft.com",
    "TenantId": "your-tenant-id",
    "ClientId": "your-blazor-client-id",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc"
  },
  "Api": {
    "BaseUrl": "https://localhost:5285",
    "Scopes": ["api://your-api-client-id/api.access"]
  }
}
```

### 6. Remove Token Generation Endpoints

**Files to Remove/Modify:**
- `Modules/Identity/Features/v1/Tokens/TokenGeneration/` - No longer needed
- `Modules/Identity/Features/v1/Tokens/RefreshToken/` - Azure AD handles refresh
- `Modules/Identity/Services/TokenService.cs` - Delete
- `Modules/Identity/Authorization/Jwt/JwtOptions.cs` - Delete

**Endpoints to Keep (modify for profile management only):**
- User management endpoints (still needed for admin operations)
- Role management endpoints (map to Azure AD App Role assignments)
- Profile endpoints (update user info in ASP.NET Identity)

---

## Claims Mapping Strategy

### Azure AD Claims ? Application Claims

| Azure AD Claim | Application Claim | Purpose |
|----------------|-------------------|---------|
| `oid` | `ClaimTypes.NameIdentifier` | User ID |
| `preferred_username` | `ClaimTypes.Email` | Email |
| `name` | `CustomClaims.Fullname` | Full name |
| `given_name` | `ClaimTypes.Name` | First name |
| `family_name` | `ClaimTypes.Surname` | Last name |
| `tid` | `CustomClaims.Tenant` | Tenant ID |
| `roles` | Permission checks | App Roles (permissions) |

### Claims Transformation (if needed)

**File:** `Modules/Identity/Authorization/AzureAd/ClaimsTransformation.cs` (NEW)

```csharp
public class AzureAdClaimsTransformation : IClaimsTransformation
{
    private readonly IUserService _userService;

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var identity = principal.Identity as ClaimsIdentity;
        if (identity == null) return principal;

        // Map Azure AD claims to custom claims
        var oid = principal.FindFirstValue("oid");
        if (!string.IsNullOrEmpty(oid))
        {
            // Add custom claims from database if needed
            var userProfile = await _userService.GetProfileAsync(oid);
            if (userProfile?.ImageUrl != null)
            {
                identity.AddClaim(new Claim(CustomClaims.ImageUrl, userProfile.ImageUrl));
            }

            // Add tenant context if not present
            if (!identity.HasClaim(c => c.Type == CustomClaims.Tenant))
            {
                var tenant = userProfile?.TenantId ?? "root";
                identity.AddClaim(new Claim(CustomClaims.Tenant, tenant));
            }
        }

        return principal;
    }
}
```

Register in `IdentityModule.ConfigureServices`:
```csharp
services.AddScoped<IClaimsTransformation, AzureAdClaimsTransformation>();
```

---

## Multi-Tenancy Considerations

### Approach 1: Azure AD Tenant = Application Tenant

**Best For:** B2B scenarios where each customer has their own Azure AD

**Implementation:**
- Each customer organization has their own Azure AD tenant
- Your app registered in each tenant (multi-tenant Azure AD app)
- Azure AD `tid` (tenant ID) maps directly to application tenant
- Use tenant ID from token to set current tenant context

**Configuration:**
```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "common", // Accept any Azure AD tenant
    "ClientId": "your-client-id",
    "ValidIssuers": [
      "https://login.microsoftonline.com/{tenant-id-1}/v2.0",
      "https://login.microsoftonline.com/{tenant-id-2}/v2.0"
    ]
  }
}
```

### Approach 2: Single Azure AD with Tenant Claim

**Best For:** SaaS scenarios where you control all users

**Implementation:**
- Single Azure AD tenant for all users
- Add custom "tenant" claim via extension attributes or groups
- Map Azure AD groups to application tenants
- Use claims transformation to inject tenant context

**Example:**
```csharp
// In ClaimsTransformation
var groups = principal.FindAll("groups").Select(c => c.Value);
var tenant = MapGroupToTenant(groups); // Custom logic
identity.AddClaim(new Claim(CustomClaims.Tenant, tenant));
```

### Approach 3: Azure AD B2C with Custom Attributes

**Best For:** Customer-facing multi-tenant SaaS

**Implementation:**
- Azure AD B2C with custom user attributes
- Store `tenantId` as user attribute in B2C
- Return as custom claim in ID token
- B2C custom policies for tenant selection

---

## Migration Strategy

### Phase 1: Parallel Authentication (Recommended)

Support both authentication systems during migration:

```csharp
services.AddAuthentication()
    .AddJwtBearer("LocalJWT", options => { /* existing config */ })
    .AddMicrosoftIdentityWebApi(options => { /* Azure AD config */ }, "AzureAd");

services.AddAuthorization(options =>
{
    // Accept either auth scheme
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .AddAuthenticationSchemes("LocalJWT", JwtBearerDefaults.AuthenticationScheme)
        .Build();
});
```

**Feature Flag:**
```json
{
  "FeatureFlags": {
    "UseAzureAd": true,
    "AllowLegacyJwt": true
  }
}
```

### Phase 2: User Migration

**Option A: Gradual Migration**
1. Keep ASP.NET Identity database
2. On first Azure AD login, link Azure AD `oid` to existing user
3. Store mapping: `AzureAdObjectId ? ApplicationUserId`
4. Gradually migrate users as they log in

**Option B: Bulk Migration**
1. Use Azure AD Graph API or Microsoft Graph
2. Create Azure AD users from existing users
3. Send password reset emails
4. Disable old authentication after cutover date

### Phase 3: Decommission Legacy Auth

1. Monitor Azure AD login adoption
2. Set cutoff date (e.g., 90 days)
3. Disable legacy JWT endpoints
4. Remove JWT authentication code
5. Remove `TokenService` and related code

---

## Security Considerations

### Token Validation

**Azure AD validates:**
- Signature (using Microsoft's public keys)
- Issuer (Azure AD tenant)
- Audience (your application's Client ID)
- Expiration (automatically)
- Not-before time

**Additional validation:**
```csharp
services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.FromMinutes(5) // Tolerance for time differences
    };
});
```

### App Role Assignment

Require admin consent for App Role assignments:

```csharp
// In Azure AD App Registration ? API permissions
// Add Application permissions (not Delegated)
// Require admin consent before users can be assigned roles
```

### Conditional Access

Leverage Azure AD Conditional Access policies:
- Require MFA for admin roles
- Block legacy authentication
- Require compliant devices
- Geo-fencing

### Token Caching

**For API (confidential client):**
- Use distributed cache (Redis) for tokens
- Microsoft.Identity.Web handles caching automatically

**For Blazor (public client):**
- MSAL.js handles in-browser token caching
- Refresh tokens stored securely in session

### Secrets Management

**Development:**
```bash
dotnet user-secrets set "AzureAd:ClientSecret" "your-secret"
```

**Production:**
- Use Azure Key Vault
- Managed Identity for accessing Key Vault
- Never commit secrets to source control

---

## Testing Strategy

### Unit Tests

**Test Azure AD token validation:**
```csharp
[Fact]
public async Task Should_Authenticate_Valid_AzureAd_Token()
{
    // Arrange
    var token = GenerateMockAzureAdToken();
    var client = _factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", token);

    // Act
    var response = await client.GetAsync("/api/v1/identity/users");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

### Integration Tests

**Use Microsoft Identity Web Test Utilities:**
```csharp
services.AddAuthentication()
    .AddMicrosoftIdentityWebApi(options =>
    {
        options.Instance = "https://login.microsoftonline.com/";
        options.TenantId = "test-tenant-id";
        options.ClientId = "test-client-id";
    });

// Mock token validation for testing
services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = false,
        ValidateIssuerSigningKey = false,
        SignatureValidator = (token, parameters) => new JwtSecurityToken(token)
    };
});
```

### Manual Testing

**Test Environment Setup:**
1. Create Azure AD test tenant
2. Create test users with different roles
3. Assign App Roles to users
4. Test authentication flows:
   - Login
   - Logout
   - Token refresh
   - Permission checks
   - Multi-tenant scenarios

**Test Scenarios:**
- ? User with Admin role can access admin endpoints
- ? User without role cannot access protected endpoints
- ? Token expiration triggers re-authentication
- ? Tenant context is correctly set
- ? Claims are mapped correctly
- ? Conditional Access policies are enforced

---

## Required NuGet Packages

### API Project

```xml
<PackageReference Include="Microsoft.Identity.Web" Version="2.15.0" />
<PackageReference Include="Microsoft.Identity.Web.MicrosoftGraph" Version="2.15.0" />
<PackageReference Include="Microsoft.Identity.Web.UI" Version="2.15.0" />
```

### Blazor Project

```xml
<PackageReference Include="Microsoft.Identity.Web" Version="2.15.0" />
<PackageReference Include="Microsoft.Identity.Web.UI" Version="2.15.0" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.OpenIdConnect" Version="8.0.0" />
```

---

## Monitoring and Diagnostics

### Azure AD Sign-in Logs

Monitor in Azure Portal:
- `Azure Active Directory ? Sign-in logs`
- Filter by application
- Review failed sign-ins
- Analyze conditional access results

### Application Insights

```csharp
builder.Services.AddApplicationInsightsTelemetry();

// Custom telemetry for authentication events
services.AddSingleton<ITelemetryInitializer, AuthTelemetryInitializer>();
```

### Logging

```csharp
builder.Logging.AddFilter("Microsoft.Identity.Web", LogLevel.Debug); // For development
```

---

## Cost Considerations

### Azure AD Pricing

| Feature | Free | Premium P1 | Premium P2 |
|---------|------|------------|------------|
| Basic authentication | ? | ? | ? |
| App Roles | ? | ? | ? |
| SSO to SaaS apps | 10 apps | Unlimited | Unlimited |
| Conditional Access | ? | ? | ? |
| Identity Protection | ? | ? | ? |
| Privileged Identity Management | ? | ? | ? |

**Cost:** ~$6/user/month (P1), ~$9/user/month (P2)

### Azure AD B2C Pricing

- First 50,000 authentications/month: Free
- Additional authentications: $0.00325 per authentication
- Premium features: Additional cost

---

## References

### Official Documentation

- [Microsoft Identity Platform](https://docs.microsoft.com/en-us/azure/active-directory/develop/)
- [Microsoft.Identity.Web Documentation](https://github.com/AzureAD/microsoft-identity-web)
- [Azure AD App Roles](https://docs.microsoft.com/en-us/azure/active-directory/develop/howto-add-app-roles-in-azure-ad-apps)
- [Multi-tenant applications](https://docs.microsoft.com/en-us/azure/active-directory/develop/howto-convert-app-to-be-multi-tenant)

### Sample Projects

- [ASP.NET Core Web API with Azure AD](https://github.com/Azure-Samples/active-directory-aspnetcore-webapp-openidconnect-v2)
- [Blazor Server with Azure AD](https://github.com/Azure-Samples/active-directory-aspnetcore-blazor-server)
- [Multi-tenant SaaS sample](https://github.com/Azure-Samples/active-directory-dotnet-webapp-multitenant-openidconnect)

---

## Decision Matrix

| Consideration | Keep JWT | Azure AD Native | Azure AD Hybrid |
|--------------|----------|-----------------|-----------------|
| Simplicity | ???? | ????? | ?? |
| Enterprise SSO | ? | ? | ? |
| Custom permissions | ? | ??? | ? |
| Maintenance | ?? | ????? | ??? |
| Cost | Free | $-$$ | $-$$ |
| Security | ??? | ????? | ???? |
| Multi-tenancy | Custom | Azure tenant or custom | Custom |
| Migration effort | N/A | ??? | ???? |

**Legend:** ? = Poor, ????? = Excellent

---

## Next Steps

### Immediate Actions

1. **Decision:** Choose authentication approach (Recommended: Azure AD Native)
2. **Azure Setup:** Create Azure AD tenant (if needed) and register application
3. **Proof of Concept:** Implement Azure AD auth in a branch
4. **Define App Roles:** Map existing permissions to Azure AD App Roles
5. **Test:** Validate with test users and roles

### Timeline Estimate

- **Phase 1 (Azure AD Setup):** 1-2 days
- **Phase 2 (Backend Implementation):** 3-5 days
- **Phase 3 (Blazor Client Implementation):** 2-3 days
- **Phase 4 (Multi-tenancy Integration):** 2-4 days
- **Phase 5 (Testing & Migration):** 1-2 weeks
- **Total:** 3-4 weeks for full implementation and testing

---

## Conclusion

Migrating to Azure AD authentication provides significant benefits for enterprise applications:
- **Enhanced Security:** Industry-standard OAuth 2.0/OpenID Connect
- **Simplified Maintenance:** Delegate authentication to Microsoft
- **Better UX:** Single Sign-On across Microsoft services
- **Compliance:** Meets enterprise security requirements

The recommended approach is **Azure AD Native Integration** with a phased migration strategy to minimize risk and disruption.

---

**Last Updated:** 2025
**Author:** GitHub Copilot
**Framework Version:** .NET 10
