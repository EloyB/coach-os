using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoachOS.Infrastructure.Identity;

public sealed class AccountEmailTokenOptions : DataProtectionTokenProviderOptions
{
    public AccountEmailTokenOptions()
    {
        Name = "CoachOS.AccountEmailChange";
        TokenLifespan = TimeSpan.FromMinutes(30);
    }
}

public sealed class AccountEmailTokenProvider(
    IDataProtectionProvider protection, IOptions<AccountEmailTokenOptions> options,
    ILogger<DataProtectorTokenProvider<ApplicationUser>> logger)
    : DataProtectorTokenProvider<ApplicationUser>(protection, options, logger);