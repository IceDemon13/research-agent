using System;
using System.Linq.Expressions;
using AutoMapper;

namespace Telemart.Client.Extensions
{
    public static class MapperExtensions
    {
        public static IMappingExpression<TSource, TDestination> Ignore<TSource, TDestination, TMember>(
            this IMappingExpression<TSource, TDestination> expression,
            Expression<Func<TDestination, TMember>> destinationMember)
        {
            return expression.ForMember(destinationMember, y => y.Ignore());
        }
    }
}
