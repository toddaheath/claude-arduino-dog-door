using AutoMapper;
using DogDoor.Api.Models;

namespace DogDoor.Api.DTOs;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Animal, AnimalDto>()
            .ForCtorParam("PhotoCount", opt => opt.MapFrom(src => src.Photos.Count));

        CreateMap<CreateAnimalDto, Animal>();

        CreateMap<AnimalPhoto, PhotoDto>();

        CreateMap<DoorEvent, DoorEventDto>()
            .ForCtorParam("AnimalName", opt => opt.MapFrom(src => src.Animal != null ? src.Animal.Name : null))
            .ForCtorParam("Side", opt => opt.MapFrom(src => src.Side != null ? src.Side.ToString() : null))
            .ForCtorParam("Direction", opt => opt.MapFrom(src => src.Direction != null ? src.Direction.ToString() : null))
            .ForCtorParam("ImageUrl", opt => opt.MapFrom(src => src.ImagePath != null ? $"/api/v1/accesslogs/{src.Id}/image" : null));

        CreateMap<DoorConfiguration, DoorConfigurationDto>();

        CreateMap<User, UserProfileDto>();
        CreateMap<User, UserSummaryDto>();

        CreateMap<NotificationPreferences, NotificationPreferencesDto>();
    }
}
