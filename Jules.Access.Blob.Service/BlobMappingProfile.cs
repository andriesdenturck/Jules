using AutoMapper;
using Jules.Access.Blob.Service.Models;

namespace Jules.Access.Blob.Service;

public class BlobMappingProfile : Profile
{
    public BlobMappingProfile()
    {
        this.CreateMap<BlobDb, Contracts.Models.Blob>().ReverseMap();
    }
}