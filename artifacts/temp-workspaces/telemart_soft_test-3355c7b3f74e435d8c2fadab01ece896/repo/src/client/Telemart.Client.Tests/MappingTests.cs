using AutoMapper;
using Newtonsoft.Json.Linq;
using Telemart.Client.Tests.Core;
using Xunit;

namespace Telemart.Client.Tests
{
    public class MappingTests
    {
        [Fact]
        public void MapperConfigurationTest()
        {
           MapperFactory.Mapper.ConfigurationProvider.AssertConfigurationIsValid();
        }

        [Fact]
        public void MapperTest()
        {
            MapperConfiguration configuration = new MapperConfiguration(x => x.AddProfile<MappingProfile>());

            IMapper mapper = new Mapper(configuration);
        }
    }

    public class Source
    {
        public JObject JObject { get; set; }
    }

    public class Dest
    {
        public JObject JObject { get; set; }
    }

    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Source, Dest>();
        }
    }
}