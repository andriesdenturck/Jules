using AutoMapper;
using Jules.Access.Blob.Contracts;
using Jules.Access.Blob.Service.Models;
using Jules.Util.Security.Contracts;
using Jules.Util.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jules.Access.Blob.Service
{
    /// <summary>
    /// Implementation of <see cref="IBlobAccess"/> that manage blobs, including creating, reading, deleting, and bulk deleting blobs.
    /// </summary>
    public class BlobAccess : ServiceBase<BlobAccess>, IBlobAccess
    {
        private readonly BlobDbContext dbContext;
        private readonly IEncryptionService encryption;
        private readonly IMapper mapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlobAccess"/> class.
        /// </summary>
        /// <param name="dbContext">The database context for accessing blob data.</param>
        /// <param name="encryption">The encryption service used for token encryption and decryption.</param>
        /// <param name="userContext">The user context used for logging and tracking user information.</param>
        /// <param name="logger">The logger used to log any errors or information.</param>
        public BlobAccess(BlobDbContext dbContext,
            IEncryptionService encryption,
            IUserContext userContext,
            ILogger<BlobAccess> logger) : base(userContext, logger)
        {
            this.dbContext = dbContext;
            this.encryption = encryption;

            MapperConfiguration configBlob = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new BlobMappingProfile());
            });

            this.mapper = new Mapper(configBlob);
        }

        /// <inheritdoc />
        public async Task<Contracts.Models.Blob> ReadAsync(string token)
        {
            try
            {
                BlobDb fileDb = await GetBlobByToken(token);
                return Map(fileDb);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to read blob with token: {token}", token);
                throw; // or return null / custom error model
            }
        }

        /// <inheritdoc />
        public async Task<string> CreateAsync(Contracts.Models.Blob file)
        {
            try
            {
                var fileDb = ReverseMap(file);

                dbContext.Add(fileDb);
                await dbContext.SaveChangesAsync();

                return this.encryption.Encrypt(fileDb.Id.ToString());
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to create blob.");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<string> UpdateAsync(Contracts.Models.Blob file)
        {
            ArgumentNullException.ThrowIfNull(file);

            try
            {
                // Map the incoming blob to the database entity model
                var fileDb = ReverseMap(file);

                fileDb.Id = Guid.Parse(this.encryption.Decrypt(file.TokenId));

                // Check if the entity is being tracked by the DbContext and detach if needed
                var existingBlob = await dbContext.Blobs.FindAsync(fileDb.Id);
                if (existingBlob != null)
                {
                    dbContext.Entry(existingBlob).State = EntityState.Detached;  // Detach the existing entity
                }

                // Update the blob record in the database
                dbContext.Blobs.Update(fileDb);

                // Save the changes to the database
                await dbContext.SaveChangesAsync();

                // Return the encrypted ID of the updated blob as a token for future access
                return file.TokenId;
            }
            catch (Exception ex)
            {
                // Log the error if an exception occurs
                Logger.LogError(ex, "Failed to update blob.");

                // Rethrow the exception so that it can be handled higher up the call stack
                throw;
            }
        }

        /// <inheritdoc />
        public async Task DeleteAsync(string token)
        {
            try
            {
                BlobDb fileDb = await GetBlobByToken(token);
                dbContext.Blobs.Remove(fileDb);
                dbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to delete blob with token: {token}", token);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task BulkDeleteAsync(IEnumerable<string> tokens)
        {
            try
            {
                IEnumerable<BlobDb> blobDbs = await Task.WhenAll(tokens.Select(async t => await GetBlobByToken(t)));
                dbContext.Blobs.RemoveRange(blobDbs);
                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to bulk delete blobs.");
                throw;
            }
        }

        /// <inheritdoc />
        private async Task<BlobDb> GetBlobByToken(string token)
        {
            try
            {
                var decryptedToken = Guid.Parse(this.encryption.Decrypt(token));
                var fileDb = await dbContext.Blobs.SingleAsync(x => x.Id == decryptedToken);
                return fileDb;
            }
            catch (FormatException ex)
            {
                Logger.LogError(ex, "Token format is invalid: {token}", token);
                throw new ArgumentException("Invalid token format.", nameof(token), ex);
            }
            catch (InvalidOperationException ex)
            {
                Logger.LogError(ex, "No blob found for token: {token}", token);
                throw new KeyNotFoundException("Blob not found.", ex);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error retrieving blob for token: {token}", token);
                throw;
            }
        }

        private BlobDb ReverseMap(Contracts.Models.Blob blob)
        {
            try
            {
                return this.mapper.Map<BlobDb>(blob);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to map Blob to BlobDb.");
                throw;
            }
        }

        private Contracts.Models.Blob Map(BlobDb blobDb)
        {
            try
            {
                return this.mapper.Map<Contracts.Models.Blob>(blobDb);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to map BlobDb to Blob.");
                throw;
            }
        }
    }
}