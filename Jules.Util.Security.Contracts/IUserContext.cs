namespace Jules.Util.Security.Contracts
{
    /// <summary>
    /// Defines methods for accessing information about the current user in the context of the application.
    /// </summary>
    public interface IUserContext
    {
        /// <summary>
        /// Gets the unique Name of the current user.
        /// This is typically extracted from the user's ClaimsPrincipal
        /// </summary>
        string UserName { get; }

        /// <summary>
        /// Gets the unique identifier (GUID) of the current user.
        /// This is typically extracted from the user's claims, such as the "userId" claim.
        /// </summary>
        Guid UserId { get; }

        /// <summary>
        /// Checks whether the current user is assigned to a specific role.
        /// </summary>
        /// <param name="role">The role to check.</param>
        /// <returns>True if the user is in the specified role, otherwise false.</returns>
        Task<bool> IsInRole(string role);
    }
}