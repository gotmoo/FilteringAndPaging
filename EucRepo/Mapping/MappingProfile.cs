using AutoMapper;
using EucRepo.Helpers;
using EucRepo.Models;
using EucRepo.ModelsExport;
using EucRepo.ModelsView;

namespace EucRepo.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {

        CreateMap<DaasEntitlement, DaasEntitlementExportModel>()
            .ForMember(a => a.LastSeen, o => o.MapFrom(a => a.LastSeen.ToNullableShortDate()))
            .ForMember(a => a.Provisioned, o => o.MapFrom(a => a.Provisioned.ToShortDateString()));
        
    }
}