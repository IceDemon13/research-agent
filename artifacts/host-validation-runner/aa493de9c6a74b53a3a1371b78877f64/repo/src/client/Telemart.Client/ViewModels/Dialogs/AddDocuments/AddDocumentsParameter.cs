using System.Collections.Generic;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.ViewModels.Dialogs.AddDocuments
{
    public class AddDocumentsParameter
    {
        public AddDocumentsParameter(int documentId, IReadOnlyCollection<string> files, string title = null, DocumentDto documentDto = null)
        {
            Title = title;
            DocumentId = documentId;
            Files = files;
            Document = documentDto;
        }

        public string Title { get; }

        public int DocumentId { get; }

        public DocumentDto Document { get; }

        public IReadOnlyCollection<string> Files { get; }
    }
}
