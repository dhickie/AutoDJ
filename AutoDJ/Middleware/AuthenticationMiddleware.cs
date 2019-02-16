using AutoDJ.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Threading.Tasks;

namespace AutoDJ.Middleware
{
    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _authenticationKey;

        private const string AUTH_HEADER = "Auth-Key";

        public AuthenticationMiddleware(RequestDelegate next, IOptions<AppOptions> options)
        {
            _next = next;
            _authenticationKey = options.Value.AuthenticationKey;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Headers.ContainsKey(AUTH_HEADER))
            {
                var authHeader = context.Request.Headers[AUTH_HEADER];
                if (authHeader.FirstOrDefault() == _authenticationKey)
                {
                    await _next(context);
                    return;
                }
            }

            context.Response.StatusCode = 403;
        }
    }
}
