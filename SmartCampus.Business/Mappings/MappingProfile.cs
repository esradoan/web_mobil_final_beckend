using AutoMapper;
using SmartCampus.Business.DTOs;
using SmartCampus.Entities;

namespace SmartCampus.Business.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<User, UserDto>().ReverseMap();
            CreateMap<RegisterDto, User>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore()); // Password will be hashed manually
            
            // Student/Faculty mapping placeholder
            // CreateMap<User, Student>()...
        }
    }
}
