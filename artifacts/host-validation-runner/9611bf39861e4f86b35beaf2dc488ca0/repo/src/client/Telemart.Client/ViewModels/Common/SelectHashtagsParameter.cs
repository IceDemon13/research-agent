using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Telemart.Client.ViewModels.Common
{
   public class SelectHashtagsParameter
    {
        public SelectHashtagsParameter(
            string title,
            bool minusHashtagsEnabled,
            bool plusHashtagsEnabled,
            bool minusHashtagsRequired,
            bool plusHashtagsRequired,
            Func<(List<int> plusHashTagIds, List<int> minusHashtagIds), Task<bool>> okCommand)
        {
            MinusHashtagsEnabled = minusHashtagsEnabled;
            PlusHashtagsEnabled = plusHashtagsEnabled;
            MinusHashtagsRequired = minusHashtagsRequired;
            PlusHashtagsRequired = plusHashtagsRequired;
            OkCommand = okCommand;
            Title = title;
        }

        public bool MinusHashtagsEnabled { get; }

        public bool PlusHashtagsEnabled { get; }

        public bool MinusHashtagsRequired { get; }

        public bool PlusHashtagsRequired { get; }

        public string Title { get; }

        public Func<(List<int> plusHashTagIds, List<int> minusHashtagIds), Task<bool>> OkCommand { get; }
    }
}
