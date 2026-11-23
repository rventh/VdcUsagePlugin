using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyPlugin.Service.Vspc;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Veeam.AC.Service.PluginManager;
using Veeam.AC.Service.Users;

namespace MyPlugin.Service.Security;

public sealed record AccessDecision(bool Allowed, string Code, string Message, string UserId);

/// <summary>
/// Requests reach the plugin through the VSPC UI proxy with an X-User-Uid header. Usage data covers every tenant,
/// so only Portal Administrators are allowed. The role is looked up from VSPC (cached briefly).
/// </summary>
public sealed class UserAccess
{
    private static readonly TimeSpan RoleCacheTime = TimeSpan.FromSeconds(60);

    private readonly VspcConnection _connection;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<UserAccess> _logger;
    private readonly ConcurrentDictionary<string, (string Role, DateTimeOffset At)> _roles = new();

    public UserAccess(VspcConnection connection, IWebHostEnvironment environment, ILogger<UserAccess> logger)
    {
        _connection = connection;
        _environment = environment;
        _logger = logger;
    }

    public async Task<AccessDecision> CheckAsync(HttpContext context, CancellationToken ct)
    {
        if (_environment.IsDevelopment())
        {
            return new AccessDecision(true, string.Empty, string.Empty, "dev");
        }

        var userId = context.Request.Headers["X-User-Uid"].ToString().Trim();
        if (string.IsNullOrEmpty(userId))
        {
            return new AccessDecision(false, "unauthenticated", "You are not signed in to VSPC.", string.Empty);
        }

        if (_roles.TryGetValue(userId, out var cached) && DateTimeOffset.UtcNow - cached.At < RoleCacheTime)
        {
            return Decide(cached.Role, userId);
        }

        try
        {
            var location = await _connection.DiscoverAsync(VspcServiceName.Users, ct);
            var client = new UserService.UserServiceClient(_connection.GetChannel(location));
            var request = new UserRequest { PluginId = _connection.PluginIdText };
            if (int.TryParse(userId, out var numericId))
            {
                request.Id = numericId;
            }
            else
            {
                request.InstanceUid = userId;
            }

            var reply = await client.getUserAsync(request, cancellationToken: ct);
            var role = reply.User?.Role.ToString() ?? string.Empty;
            _roles[userId] = (role, DateTimeOffset.UtcNow);
            return Decide(role, userId);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception error)
        {
            _logger.LogWarning(error, "Could not verify the role of the calling user.");
            return new AccessDecision(false, "forbidden", "Your VSPC role could not be verified.", userId);
        }
    }

    private static AccessDecision Decide(string role, string userId)
    {
        // proto enum UserRole.administrator = Portal Administrator
        return string.Equals(role, "Administrator", StringComparison.OrdinalIgnoreCase)
            ? new AccessDecision(true, string.Empty, string.Empty, userId)
            : new AccessDecision(false, "forbidden", "This page is available to Portal Administrators only.", userId);
    }
}
