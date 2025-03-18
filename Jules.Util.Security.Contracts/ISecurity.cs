namespace Jules.Util.Security.Models
{
    /// <summary>
    /// Defines methods for managing user security, including registration, login, role assignments, and user deletion.
    /// </summary>
    public interface ISecurity
    {
        /// <summary>
        /// Assigns a role to an existing user.
        /// </summary>
        /// <param name="userName">The username of the user.</param>
        /// <param name="userRole">The role to assign to the user.</param>
        /// <returns>A task that represents the asynchronous operation, with a boolean result indicating success.</returns>
        Task<bool> AssignRole(string userName, string userRole);

        /// <summary>
        /// Deletes an existing user.
        /// </summary>
        /// <param name="userName">The username of the user to delete.</param>
        /// <returns>A task that represents the asynchronous operation, with a boolean result indicating success.</returns>
        Task<bool> DeleteUser(string userName);

        /// <summary>
        /// Authenticates a user and returns a JWT token if credentials are valid.
        /// </summary>
        /// <param name="userName">The username of the user.</param>
        /// <param name="userPassword">The password of the user.</param>
        /// <param name="userRole">The role of the user (used for role-based claims).</param>
        /// <returns>A task that represents the asynchronous operation, with a string result containing the JWT token.</returns>
        Task<string> Login(string userName, string userPassword, string userRole);

        /// <summary>
        /// Registers a new user with a username and password.
        /// </summary>
        /// <param name="userName">The username for the new user.</param>
        /// <param name="userPassword">The password for the new user.</param>
        /// <returns>A task that represents the asynchronous operation, with a boolean result indicating success.</returns>
        Task<bool> Register(string userName, string userPassword);
    }
}