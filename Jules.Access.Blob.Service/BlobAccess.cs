using AutoMapper;
using Jules.Access.Blob.Contracts;
using Jules.Access.Blob.Service.Models;
using Jules.Util.Security.Contracts;
using Jules.Util.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jules.Access.Blob.Service;

/// <summary>
/// Implementation of <see cref="IBlobAccess"/> that manages blobs, including creating, reading, updating, and deleting them.
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
    /// <param name="encryptionService">The encryption service used for token encryption and decryption.</param>
    /// <param name="logger">The logger used to log any errors or information.</param>
    public BlobAccess(BlobDbContext dbContext,
                      IEncryptionService encryptionService,
                      ILogger<BlobAccess> logger) : base(logger)
    {
        this.dbContext = dbContext;
        this.encryption = encryptionService;

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new BlobMappingProfile());
        });

        this.mapper = new Mapper(config);
    }

    /// <inheritdoc />
    public async Task<string> CreateAsync(Contracts.Models.Blob file)
    {
        try
        {
            var fileDb = ReverseMap(file);

            dbContext.Add(fileDb);
            await dbContext.SaveChangesAsync();

            return encryption.Encrypt(fileDb.Id.ToString());
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create blob.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<Contracts.Models.Blob> ReadAsync(string token)
    {
        try
        {
            var fileDb = await GetBlobByToken(token);
            return Map(fileDb);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to read blob with token: {token}", token);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<string> UpdateAsync(Contracts.Models.Blob file)
    {
        ArgumentNullException.ThrowIfNull(file);

        try
        {
            var existingBlob = await GetBlobByToken(file.TokenId);

            if (existingBlob != null)
            {
                dbContext.Entry(existingBlob).State = EntityState.Detached;
            }

            var fileDb = ReverseMap(file);

            fileDb.Id = existingBlob.Id;

            dbContext.Blobs.Update(fileDb);

            await dbContext.SaveChangesAsync();

            return file.TokenId;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update blob.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string token)
    {
        try
        {
            var fileDb = await GetBlobByToken(token);
            dbContext.Blobs.Remove(fileDb);
            await dbContext.SaveChangesAsync();
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
            var blobDbs = await Task.WhenAll(tokens.Select(GetBlobByToken));
            dbContext.Blobs.RemoveRange(blobDbs);
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to bulk delete blobs.");
            throw;
        }
    }

    private async Task<BlobDb> GetBlobByToken(string token)
    {
        try
        {
            var decryptedId = Guid.Parse(encryption.Decrypt(token));
            var fileDb = await dbContext.Blobs.SingleAsync(x => x.Id == decryptedId);
            return fileDb;
        }
        catch (FormatException ex)
        {
            Logger.LogError(ex, "Invalid token format: {token}", token);
            throw new ArgumentException("Invalid token format.", nameof(token), ex);
        }
        catch (InvalidOperationException ex)
        {
            Logger.LogError(ex, "No blob found for token: {token}", token);
            throw new KeyNotFoundException("Blob not found.", ex);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error retrieving blob: {token}", token);
            throw;
        }
    }

    private BlobDb ReverseMap(Contracts.Models.Blob blob)
    {
        try
        {
            return mapper.Map<BlobDb>(blob);
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
            return mapper.Map<Contracts.Models.Blob>(blobDb);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to map BlobDb to Blob.");
            throw;
        }
    }
}