using Jules.Util.Security.Contracts;
using Microsoft.Extensions.Logging;

namespace Jules.Util.Shared
{
    public class ServiceBase<T> where T : ServiceBase<T>
    {
        private readonly ILogger<T> logger;

        public ServiceBase(ILogger<T> logger)
        {
            this.logger = logger;
        }

        protected ILogger<T> Logger => logger;
    }
}