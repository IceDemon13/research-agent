using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using AutoMapper;
using DevExpress.Mvvm.Native;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.AutoMappings.ValueResolvers
{
    public sealed class DictionaryItemsToObservableCollectionValueResolver<TDestMember> : IMemberValueResolver<object, object, List<int>, ObservableCollection<TDestMember>>
        where TDestMember : DictionaryItemBase
    {
        public DictionaryItemsToObservableCollectionValueResolver(IDictionaries dictionaries)
        {
            Dictionaries = dictionaries ?? throw new ArgumentNullException(nameof(dictionaries));
        }

        private IDictionaries Dictionaries { get; }

        public ObservableCollection<TDestMember> Resolve(
            object source,
            object destination,
            List<int> sourceMembers,
            ObservableCollection<TDestMember> destMember,
            ResolutionContext context)
        {
            sourceMembers ??= new List<int>();

            return Dictionaries.GetItems<TDestMember>().Where(x => sourceMembers.Contains(x.Id)).ToObservableCollection();
        }
    }
}