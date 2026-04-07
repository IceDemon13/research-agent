using System;
using System.Collections.ObjectModel;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Windows;
using System.Xml;
using System.Xml.Serialization;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Data.Options;
using Telemart.Client.ViewModels;

namespace Telemart.Client.SingleInstance
{
    public class SingleInstanceAppProcessor : IDisposable
    {
        private const string ChannelNamePrefix = "SingeInstanceNamedPipe";

        private readonly ILogger<SingleInstanceAppProcessor> _logger;
        private readonly object _namedPiperServerThreadLock = new object();
        private readonly string _chanelName;
        private readonly Guid _instanceGuid;

        private ISingleInstanceApp _app;

        private NamedPipeServerStream _namedPipeServerStream;

        private string _refreshTokenForNewClient;
        private ReadOnlyCollection<Cookie> _cookies;
        private bool _disposedValue = false; // To detect redundant calls

        public SingleInstanceAppProcessor(AppOptions options, ILogger<SingleInstanceAppProcessor> logger)
        {
            _logger = logger;

            AppUniqueName = options.UniqueName ?? "Telemart.Client";

            string applicationIdentifier = AppUniqueName + Environment.UserName;

            _chanelName = string.Concat(ChannelNamePrefix, "_", applicationIdentifier);

            _instanceGuid = Guid.NewGuid();
        }

        public string AppUniqueName { get; }

        public void Init(ISingleInstanceApp app)
        {
            _app = app;

            NamedPipeServerCreateServer();
            NamedPipeClientSendOptions(null, _cookies);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        public void WaitForNewClientSendTokenAndShutdown(string refreshToken, ReadOnlyCollection<Cookie> cookies)
        {
            _refreshTokenForNewClient = refreshToken;
            _cookies = cookies;
        }

        private void NamedPipeClientSendOptions(string token, ReadOnlyCollection<Cookie> cookies, bool kill = false)
        {
            try
            {
                using (NamedPipeClientStream namedPipeClientStream = new NamedPipeClientStream(".", _chanelName, PipeDirection.Out))
                {
                    namedPipeClientStream.Connect(TimeSpan.FromSeconds(3).Milliseconds);

                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(NamedPipeXmlPayload));

                    NamedPipeXmlPayload namedPipeXmlPayload = new NamedPipeXmlPayload()
                    {
                        Token = token,
                        Kill = kill,
                        InstanceGuid = _instanceGuid,
                        SaveCookies = cookies?.Select(x => new SaveCookie { Name = x.Name, Value = x.Value, Path = x.Path, Domain = x.Domain }).ToArray()
                    };

                    xmlSerializer.Serialize(namedPipeClientStream, namedPipeXmlPayload);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed connect to single instance pipe");
            }
        }

        private void NamedPipeServerCreateServer()
        {
            // Create pipe and start the async connection wait
            _namedPipeServerStream = new NamedPipeServerStream(
                _chanelName,
                PipeDirection.In,
                4,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly,
                0,
                0);

            // Begin async wait for connections
            _namedPipeServerStream.BeginWaitForConnection(NamedPipeServerConnectionCallback, _namedPipeServerStream);
        }

        private void NamedPipeServerConnectionCallback(IAsyncResult result)
        {
            try
            {
                _namedPipeServerStream.EndWaitForConnection(result);

                // Read data and prevent access to _namedPipeXmlPayload during threaded operations
                lock (_namedPiperServerThreadLock)
                {
                    using XmlReader xmlReader = XmlReader.Create(_namedPipeServerStream);

                    XmlSerializer serializer = new XmlSerializer(typeof(NamedPipeXmlPayload));
                    NamedPipeXmlPayload namedPipeXmlPayload = (NamedPipeXmlPayload)serializer.Deserialize(xmlReader);

                    if (namedPipeXmlPayload?.InstanceGuid != _instanceGuid)
                    {
                        if (namedPipeXmlPayload?.Kill == true)
                        {
                            Application.Current.Dispatcher.Invoke(Application.Current.Shutdown);
                            return;
                        }

                        if (_refreshTokenForNewClient is null)
                        {
                            _app.SignalExternalCommandLineArgs();

                            if (string.IsNullOrWhiteSpace(namedPipeXmlPayload?.Token))
                            {
                                // if there no token from another client we kill another client
                                NamedPipeClientSendOptions(null, namedPipeXmlPayload?.SaveCookies?.Select(x => new Cookie(x.Name, x.Value,  x.Path, x.Domain)).ToReadOnlyCollection(), true);
                            }
                            else
                            {
                                // if we get token from another client we init current client with this token
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    if (Application.Current.MainWindow.DataContext is MainWindowViewModel mainWindowViewModel)
                                    {
                                        mainWindowViewModel.InitWithRefreshTokenAsync(
                                            namedPipeXmlPayload.Token,
                                            namedPipeXmlPayload.SaveCookies?.Select(x => new Cookie(x.Name, x.Value,  x.Path, x.Domain)).ToReadOnlyCollection());
                                    }
                                });
                            }
                        }
                        else
                        {
                            NamedPipeClientSendOptions(_refreshTokenForNewClient, _cookies);

                            Application.Current.Dispatcher.Invoke(Application.Current.Shutdown);
                        }
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to receive message from pipe client");
            }
            finally
            {
                _namedPipeServerStream.Dispose();
            }

            NamedPipeServerCreateServer();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _namedPipeServerStream?.Dispose();
                }

                _disposedValue = true;
            }
        }

        public class NamedPipeXmlPayload
        {
            [XmlAttribute("token")]
            public string Token { get; set; }

            [XmlAttribute("kill")]
            public bool Kill { get; set; }

            [XmlAttribute("instance_guid")]
            public Guid InstanceGuid { get; set; }

            [XmlArray("coockies")]
            [XmlArrayItem("coockie")]
            public SaveCookie[] SaveCookies { get; set; }
        }

        public class SaveCookie
        {
            [XmlAttribute("name")]
            public string Name { get; set; }

            [XmlAttribute("value")]
            public string Value { get; set; }

            [XmlAttribute("wiki_domain")]
            public string Domain { get; set; }

            [XmlAttribute("path")]
            public string Path { get; set; }
        }
    }
}