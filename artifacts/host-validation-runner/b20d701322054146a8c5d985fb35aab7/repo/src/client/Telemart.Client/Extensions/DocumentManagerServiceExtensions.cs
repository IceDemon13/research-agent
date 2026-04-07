using System;
using System.Linq;
using DevExpress.Mvvm;
using Microsoft.Extensions.DependencyInjection;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.Extensions
{
    public static class DocumentManagerServiceExtensions
    {
        private const string ViewModelEndString = "Model";

        public static IServiceProvider ServiceProvider { get; set; }

        public static void ShowView(this IDocumentManagerService documentManagerService, string viewName, object viewModel, object parameter, ISupportServices parentViewModel)
        {
            IDocument document = documentManagerService.FindDocumentByIdOrCreate(
                viewName,
                service =>
                {
                    IDocument doc = service.CreateDocument(viewName, viewModel, parameter, parentViewModel);
                    doc.Id = viewName;
                    doc.DestroyOnClose = true;

                    return doc;
                });

            documentManagerService.ActiveDocument = document;
            document.Show();
        }

        public static void ShowEditorView<TParameter>(this IDocumentManagerService documentManagerService, Type viewModelType, TParameter id, object parameter, ISupportServices parentViewModel)
        {
            string viewName = GetViewName(viewModelType.Name);

            string documentKey = $"{viewModelType.Name}_{id}";

            IDocument document = documentManagerService.FindDocumentByIdOrCreate(
                documentKey,
                service =>
                {
                    IDocument doc = service.CreateDocument(viewName, parameter ?? id, parentViewModel);
                    doc.Id = documentKey;
                    doc.DestroyOnClose = true;

                    return doc;
                });

            documentManagerService.ActiveDocument = document;
            document.Show();
        }

        public static void ShowEditorView<TViewModel>(this IDocumentManagerService documentManagerService, int id, object parameter, ISupportServices parentViewModel)
            where TViewModel : ViewModelBase
        {
            documentManagerService.ShowEditorView<TViewModel, int>(id, parameter, parentViewModel);
        }

        public static T ShowView<T>(this IDocumentManagerService documentManagerService, string viewName, object parameter, ISupportServices parentViewModel)
            where T : ViewModelBase
        {
            IDocument document = documentManagerService.FindDocumentByIdOrCreate(
                viewName,
                service =>
                {
                    IDocument doc = service.CreateDocument(viewName, parameter, parentViewModel);
                    doc.Id = viewName;
                    doc.DestroyOnClose = true;

                    return doc;
                });

            document.Show();

            return (T)document.Content;
        }

        public static TViewModel ShowView<TViewModel>(this IDocumentManagerService documentManagerService, object parameter, ISupportServices parentViewModel)
            where TViewModel : ViewModelBase
        {
            ViewNameAttribute[] viewNameAttributes = typeof(TViewModel)
                .GetCustomAttributes(typeof(ViewNameAttribute), true)
                .Cast<ViewNameAttribute>()
                .ToArray();

            string viewName;

            if (viewNameAttributes.Length == 1)
            {
                viewName = viewNameAttributes[0].ViewName;

                object viewModel = ServiceProvider.GetService<TViewModel>();

                return ShowView(documentManagerService, viewName, (TViewModel)viewModel, parameter, parentViewModel);
            }

            viewName = GetViewName(typeof(TViewModel).Name);
            return ShowView<TViewModel>(documentManagerService, viewName, parameter, parentViewModel);
        }

        public static DialogResult<TResult> ShowView<TViewModel, TParameter, TResult>(this IDocumentManagerService documentManagerService, TParameter parameter, ISupportServices parentViewModel)
            where TViewModel : TelemartDialogViewModelBase<TParameter, TResult>
        {
            string viewName = GetViewName(typeof(TViewModel).Name);
            TelemartDialogViewModelBase<TParameter, TResult> viewModel = ShowView<TViewModel>(documentManagerService, viewName, parameter, parentViewModel);

            return viewModel.GetResult();
        }

        public static TViewModel ShowView<TViewModel>(this IDocumentManagerService documentManagerService, string viewName, TViewModel viewModel, object parameter = null, ISupportServices parentViewModel = null)
        {
            IDocument document = documentManagerService.FindDocumentByIdOrCreate(
                viewName,
                service =>
                {
                    IDocument doc = service.CreateDocument(viewName, viewModel, parameter, parentViewModel);
                    doc.Id = viewName;
                    doc.DestroyOnClose = true;
                    return doc;
                });

            document.Show();

            return (TViewModel)document.Content;
        }

        private static void ShowEditorView<TViewModel, TParameter>(this IDocumentManagerService documentManagerService, TParameter id, object parameter, ISupportServices parentViewModel)
            where TViewModel : ViewModelBase
        {
            Type viewModelType = typeof(TViewModel);

            ShowEditorView(documentManagerService, viewModelType, id, parameter, parentViewModel);
        }

        private static string GetViewName(string viewModelName)
        {
            if (viewModelName.EndsWith(ViewModelEndString))
            {
                return viewModelName.Substring(0, viewModelName.Length - ViewModelEndString.Length);
            }

            throw new ArgumentException(@"Wrong view model type name", viewModelName);
        }
    }
}