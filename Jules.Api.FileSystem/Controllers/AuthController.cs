using Jules.Api.FileSystem.Models;
using Jules.Util.Security.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jules.Api.FileSystem.Controllers
{
    /// <summary>
    /// Controller responsible for user authentication, registration, role management, and deletion.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ISecurity _securityService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthController"/> class.
        /// </summary>
        /// <param name="securityService">Security service for authentication and authorization operations.</param>
        public AuthController(ISecurity securityService)
        {
            _securityService = securityService ?? throw new ArgumentNullException(nameof(securityService), "Security service cannot be null.");
        }

        /// <summary>
        /// Registers a new user. Password should be securely hashed by the security service.
        /// </summary>
        /// <param name="userLogin">The user's login credentials (username and password).</param>
        /// <returns>
        /// 201 Created if registration is successful;  
        /// 400 BadRequest if model is invalid;  
        /// 409 Conflict if user already exists;  
        /// 500 InternalServerError for unexpected errors.
        /// </returns>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserLogin userLogin)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var success = await _securityService.Register(userLogin.Username, userLogin.Password);

                if (!success)
                {
                    return Conflict("User already exists or registration failed.");
                }

                return CreatedAtAction(nameof(Register), new { username = userLogin.Username }, "User registered successfully.");
            }
            catch (ArgumentException ex)
            {
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// Authenticates a user and returns a JWT token on success.
        /// </summary>
        /// <param name="userLogin">The user's login credentials (username and password).</param>
        /// <returns>
        /// 200 OK with JWT token;  
        /// 400 BadRequest if model is invalid;  
        /// 401 Unauthorized if credentials are incorrect or user lacks permissions;  
        /// 500 InternalServerError for unexpected errors.
        /// </returns>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLogin userLogin)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var token = await _securityService.Login(userLogin.Username, userLogin.Password, userLogin.Role);

                if (string.IsNullOrWhiteSpace(token))
                {
                    return Unauthorized("Invalid username or password.");
                }

                return Ok(new AuthResponse { Token = token });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// Assigns a role to a user. Requires authorization.
        /// </summary>
        /// <param name="username">The username to assign the role to.</param>
        /// <param name="role">The role to assign (e.g., "admin", "user").</param>
        /// <returns>
        /// 200 OK if role assignment is successful;  
        /// 400 BadRequest if input is invalid;  
        /// 401 Unauthorized if caller lacks permission;  
        /// 500 InternalServerError for unexpected errors.
        /// </returns>
        [Authorize]
        [HttpPost("assignrole")]
        public async Task<IActionResult> AssignRole([FromQuery] string username, [FromQuery] string role)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(role))
            {
                return BadRequest("Username and role must be provided.");
            }

            try
            {
                var success = await _securityService.AssignRole(username, role);

                if (success)
                {
                    return Ok($"Role '{role}' assigned to user '{username}' successfully.");
                }

                return StatusCode(500, "Failed to assign role.");
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes an existing user. Requires authorization.
        /// </summary>
        /// <param name="username">The username of the user to delete.</param>
        /// <returns>
        /// 200 OK if deletion is successful;  
        /// 400 BadRequest if input is invalid;  
        /// 401 Unauthorized if caller lacks permission;  
        /// 404 NotFound if user does not exist;  
        /// 500 InternalServerError for unexpected errors.
        /// </returns>
        [Authorize]
        [HttpDelete("deleteuser")]
        public async Task<IActionResult> DeleteUser([FromQuery] string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return BadRequest("Username cannot be empty.");
            }

            try
            {
                var success = await _securityService.DeleteUser(username);

                if (success)
                {
                    return Ok($"User '{username}' deleted successfully.");
                }

                return StatusCode(500, "Failed to delete user.");
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }
    }
}
