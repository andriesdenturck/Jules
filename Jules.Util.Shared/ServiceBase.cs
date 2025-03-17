using Jules.Util.Security.Contracts;
using Microsoft.Extensions.Logging;

namespace Jules.Util.Shared
{
    public class ServiceBase<T> where T : ServiceBase<T>
    {
        protected readonly IUserContext userContext;
        private readonly ILogger<T> logger;

        public ServiceBase(IUserContext userContext, ILogger<T> logger)
        {
            this.userContext = userContext;
            this.logger = logger;
        }

        protected ILogger<T> Logger => logger;
    }
}