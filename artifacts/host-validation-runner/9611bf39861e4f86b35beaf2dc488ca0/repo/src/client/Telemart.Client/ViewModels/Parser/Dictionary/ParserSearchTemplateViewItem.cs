using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects.ParserSearchTemplate;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Parser.Dictionary
{
    public class ParserSearchTemplateViewItem : TelemartCloneableViewItemBase
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public int CategoryId
        {
            get { return GetProperty(() => CategoryId); }
            set { SetProperty(() => CategoryId, value); }
        }

        public ObservableCollection<ParserSearchTemplateFeatureViewItem> Features
        {
            get { return GetProperty(() => Features); }
            set { SetProperty(() => Features, value); }
        }
    }
}