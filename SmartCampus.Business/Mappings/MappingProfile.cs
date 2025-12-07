using AutoMapper;
using SmartCampus.Business.DTOs;
using SmartCampus.Entities;

namespace SmartCampus.Business.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<User, UserDto>()
                .ForMember(dest => dest.IsEmailVerified, opt => opt.MapFrom(src => src.EmailConfirmed))
                .ForMember(dest => dest.Role, opt => opt.Ignore()) // Role is set manually
                .ReverseMap()
                .ValidateMemberList(MemberList.Source)
                .ForSourceMember(src => src.Role, opt => opt.DoNotValidate())
                .ForSourceMember(src => src.IsEmailVerified, opt => opt.DoNotValidate());

            CreateMap<RegisterDto, User>(MemberList.Source)
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore()) 
                .ForSourceMember(src => src.Password, opt => opt.DoNotValidate())
                .ForSourceMember(src => src.ConfirmPassword, opt => opt.DoNotValidate())
                .ForSourceMember(src => src.Role, opt => opt.DoNotValidate())
                .ForSourceMember(src => src.StudentNumber, opt => opt.DoNotValidate())
                .ForSourceMember(src => src.EmployeeNumber, opt => opt.DoNotValidate())
                .ForSourceMember(src => src.DepartmentId, opt => opt.DoNotValidate());
            
            // Student/Faculty mapping placeholder
            // CreateMap<User, Student>()...
        }
    }
}
