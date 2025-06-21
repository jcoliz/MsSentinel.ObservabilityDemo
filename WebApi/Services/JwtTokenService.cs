// Copyright (C) 2024 James Coliz, Jr. <jcoliz@outlook.com> All rights reserved
// Use of this source code is governed by the MIT license (see LICENSE file)
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Common.Identity.Services;

/// <summary>
/// Provides access tokens in the form of JWTs
/// </summary>
/// <remarks>
/// Note that these are relatively shortly-lived bearer tokens.
/// Not expected to be verified with the database.
/// </remarks>
public static class JwtTokenService
{
    /// <summary>
    /// Create an access token for this user
    /// </summary>
    /// <param name="userid">User who is being granted access</param>
    /// <returns>Generated token</returns>
    public static string CreateAccessTokenAsync(string userName)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, GuidFromName(userName).ToString()),
            new(JwtRegisteredClaimNames.Name, userName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),ClaimValueTypes.Integer32),
            new(ClaimTypes.Role, "User"),
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes("This is a secret key for signing JWTs, it must be at least 16 characters long.")),
            SecurityAlgorithms.HmacSha256
        );
        var issued_time = DateTime.UtcNow;
        var expiration = issued_time + TimeSpan.FromMinutes(30); // Set token expiration to 30 minutes

        var token = new JwtSecurityToken(
            "MsSentinel.ObservabilityDemo",
            "MsSentinel.ObservabilityDemo",
            claims,
            expires: expiration,
            signingCredentials: credentials
        );

        var tokenHandler = new JwtSecurityTokenHandler();

        var token_str = tokenHandler.WriteToken(token);

        return token_str;
    }

    private static Guid GuidFromName(string name)
    {
        // Use SHA1 to hash the string, then create a Guid from the first 16 bytes
        using (var sha1 = System.Security.Cryptography.SHA1.Create())
        {
            var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(name));
            byte[] guidBytes = new byte[16];
            Array.Copy(hash, guidBytes, 16);
            // Set the version to 5 (name-based, SHA-1)
            guidBytes[6] = (byte)((guidBytes[6] & 0x0F) | 0x50);
            // Set the variant to RFC 4122
            guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80);
            return new Guid(guidBytes);
        }
    }
}
