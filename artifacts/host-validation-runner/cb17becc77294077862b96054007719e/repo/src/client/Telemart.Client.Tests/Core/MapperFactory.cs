using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Telemart.Client.ViewModels.Store.Order;

namespace Telemart.Client.Tests.Core
{
    public static class MapperFactory
    {
        private static readonly Lazy<IMapper> Factory = new(() =>
        {
            return new MapperConfiguration(Configure).CreateMapper();

            void Configure(IMapperConfigurationExpression cfg)
            {
                IEnumerable<Type> types = typeof(OrderViewModel).Assembly
                    .GetTypes()
                    .Where(t => t.IsClass && t.BaseType != null && t.BaseType == typeof(Profile));

                foreach (Type type in types)
                {
                    cfg.AddProfile(type);
                }
            }
        });

        public static IMapper Mapper => Factory.Value;
    }
}