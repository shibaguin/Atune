using System.Linq;
using AutoMapper;
using Atune.Models;
using Atune.Models.Dtos;

namespace Atune.Extensions
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Album, TopAlbumDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
                .ForMember(dest => dest.CoverArtPath, opt => opt.MapFrom(src => src.CoverArtPath))
                .ForMember(dest => dest.TrackCount, opt => opt.MapFrom(src => src.TrackCount))
                .ForMember(dest => dest.ArtistName, opt => opt.MapFrom(src => src.Tracks.SelectMany(mi => mi.TrackArtists)
                    .Select(ta => ta.Artist.Name).FirstOrDefault() ?? string.Empty))
                .ForMember(dest => dest.Year, opt => opt.MapFrom(src => src.Year != 0
                    ? (uint)src.Year
                    : (uint)src.Tracks.Select(mi => mi.Year).FirstOrDefault()))
                .ForMember(dest => dest.PlayCount, opt => opt.Ignore());
        }
    }
}