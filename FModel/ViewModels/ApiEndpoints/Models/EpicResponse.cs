using System;
using System.Diagnostics;
using J = Newtonsoft.Json.JsonPropertyAttribute;

namespace FModel.ViewModels.ApiEndpoints.Models;

[DebuggerDisplay("{" + nameof(AccessToken) + "}")]
public class AuthResponse
{
    [J("access_token")] public string AccessToken { get; set; }
    [J("expires_at")] public DateTime ExpiresAt { get; set; }
}