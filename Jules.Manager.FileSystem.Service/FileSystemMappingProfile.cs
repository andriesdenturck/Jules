using AutoMapper;
using Jules.Access.Blob.Contracts.Models;
using Jules.Access.Archive.Contracts.Models;
using Jules.Engine.Parsing.Contracts.Models;
using Jules.Manager.FileSystem.Contracts.Models;

namespace Jules.Manager.FileSystem.Service;

public class FileSystemMappingProfile : Profile
{
    public FileSystemMappingProfile()
    {
        this.CreateMap<ItemInfo, Item>();
        this.CreateMap<ItemInfo, Item>();
        this.CreateMap<ItemInfo, FileItem>();
        this.CreateMap<FileContent, ItemInfo>()
            .ForMember(dest => dest.Size, opt => opt.MapFrom(src => src.Data.Length));
        this.CreateMap<ItemInfo, FileContent>();
        this.CreateMap<FileItem, FileContent>();
        this.CreateMap<FileContent, Blob>().ReverseMap();
    }
}