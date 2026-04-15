using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Postgres.Identity
{
    public class ApplicationToken : IdentityUserToken<Guid>
    {
        public DateTimeOffset? RefreshTokenExpiryTime { get; set; }

    }
}
