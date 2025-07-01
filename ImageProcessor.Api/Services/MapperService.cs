using AutoMapper;
using ImageProcessor.Api.Dtos;
using ImageProcessor.Api.Model;

namespace ImageProcessor.Api.Services
{
    public class MapperService : Profile
    {
        public MapperService()
        {
            CreateMap<SignUpDto, User>()
                .ForMember(d => d.PasswordHash, f => f.Ignore());

            CreateMap<User, UserDto>();

            CreateMap<User, SignInResponseDto>();
        }
    }
}
