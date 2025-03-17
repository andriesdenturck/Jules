using AutoMapper;
using Jules.Access.Archive.Service.Models;
using ItemInfo = Jules.Access.Archive.Contracts.Models.ItemInfo;

namespace Jules.Access.Archive.Service;

public class ArchiveMappingProfile : Profile
{
    public ArchiveMappingProfile()
    {
        this.CreateMap<FileInfoDb, ItemInfo>().ReverseMap();
        this.CreateMap<ArchiveItemDb, ItemInfo>()
            .ForMember(fi => fi.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(fi => fi.MimeType, opt => opt.MapFrom(src => src.FileInfo.MimeType))
            .ForMember(fi => fi.TokenId, opt => opt.MapFrom(src => src.FileInfo.TokenId))
            .ForMember(fi => fi.Size, opt => opt.MapFrom(src => src.FileInfo.Size));
    }
}