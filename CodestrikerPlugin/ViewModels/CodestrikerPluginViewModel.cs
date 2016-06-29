using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CodestrikerPlugin.Annotations;
using CodestrikerPlugin.Util;
using Microsoft.TeamFoundation.MVVM;
using UploadManager.Util;
using UploadManager;
using System.Net;
using System.Windows;
using Microsoft.TeamFoundation.Common;

namespace CodestrikerPlugin.ViewModels
{
    public delegate void ProjectsUpdated();

    public class CodestrikerPluginViewModel : INotifyPropertyChanged
    {
        #region NotifyPropertyChanged  

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(
            [CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
        #region events

        public event PropertyChangedEventHandler PropertyChanged;
        public event ProjectsUpdated ProjectsUpdated;

        #endregion

        #region private members

        private static CodestrikerPluginViewModel s_Instance;
        private readonly TfsHelper m_TfsHelper;
        private ProjectData m_Project;
        private string m_TfsUsername;
        private bool m_IsCreateTopicPending;
        private Visibility m_ErrorBoxVisibility;
        private const string CodestrikerUrlPrefix = "http://";
        private const string CodestrikerUrlAppendix = "/codestriker/codestriker.pl";

        #endregion

        #region properties

        public DiffTransportObject TransportObject { get; set; }

        public string TfsUsername
        {
            get { return m_TfsUsername; }
            set
            {
                m_TfsUsername = value;
                OnPropertyChanged();
            }
        }

        public ProjectData Project
        {
            get { return m_Project; }
            set
            {
                m_Project = value;
                ErrorText = string.Empty;
                OnPropertyChanged();
            }
        }

        public bool IsCreateTopicPending
        {
            get { return m_IsCreateTopicPending; }
            set
            {
                m_IsCreateTopicPending = value;
                OnPropertyChanged();
            }
        }

        public Visibility ErrorBoxVisibility
        {
            get { return m_ErrorBoxVisibility; }
            set
            {
                m_ErrorBoxVisibility = value;
                OnPropertyChanged();
            }
        }




        //Codestriker info
        public string CsUsername { get; set; }
        public string CsPassword { get; set; }
        public string CsUri { get; set; }

        public RelayCommand ReviewCommand { get; set; }
        public RelayCommand RefreshCommand { get; set; }

        #endregion

        #region constructor
        public static CodestrikerPluginViewModel Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new CodestrikerPluginViewModel();
                }
                return s_Instance;
            }
        }

        private string m_ErrorText;

        public string ErrorText
        {
            get { return m_ErrorText; }
            set
            {
                m_ErrorText = value;
                ErrorBoxVisibility = !string.IsNullOrEmpty(m_ErrorText) ? Visibility.Visible : Visibility.Collapsed;
                OnPropertyChanged();
            }
        }

        //Constructor
        public CodestrikerPluginViewModel()
        {
            TransportObject = new DiffTransportObject();

            m_TfsHelper = TfsHelper.Instance;
            m_TfsHelper.UsernameChanged += M_TfsHelper_UsernameChanged;
            m_TfsHelper.ContextChanged += ProjectsChanged;
            m_IsCreateTopicPending = false;
            m_ErrorBoxVisibility = Visibility.Collapsed;

            CreateCommands();
        }

        private void M_TfsHelper_UsernameChanged(string username)
        {
            TfsUsername = username;
        }

        #endregion

        #region methods

        private void CreateCommands()
        {
            ReviewCommand = new RelayCommand(ReviewExecute, CanExecute);
            RefreshCommand = new RelayCommand(RefreshExecute);
        }

        private bool CanExecute(object o)
        {
            var shelveset = o as PendingSetWrapper;
            if (shelveset == null)
                return false;

            return !string.IsNullOrEmpty(TfsUsername);
        }


      
        /// <summary>
        /// Loads the shelvesets of the user which name is currently in the Codestriker Integration Window
        /// </summary>
        private async void RefreshExecute()
        {
            ErrorText = string.Empty;

            IsCreateTopicPending = true;
            FetchState state = await m_TfsHelper.FetchProjectsAsync(TfsUsername);
            IsCreateTopicPending = false;

            switch (state)
            {
                case FetchState.UserNotFound:
                    Project = new ProjectData();
                    ErrorText = Resources.UserNotFound;
                    break;
                case FetchState.UserNotLoggedIn:
                    ErrorText = Resources.UserNotLoggedIn;
                    break;
                case FetchState.NoProject:
                    ErrorText = Resources.NoProjectsFound;
                    break;
                case FetchState.Successful: // nothing to do here
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

       
        /// <summary>
        /// handle the input from the user, if it is not a complete url
        /// </summary>
        /// <param name="url">url from the options page</param>
        /// <returns>a complete url to the codestriker server</returns>
        private string CheckCodestrikerUrl(string url)
        {

            if (!url.StartsWith(CodestrikerUrlPrefix)) // http://
            {
                url = CodestrikerUrlPrefix + url;
            }
            if (!url.EndsWith(CodestrikerUrlAppendix)) // /codestriker/codestriker.pl
            {
                url = url + CodestrikerUrlAppendix;
            }

            return url;
        }

        /// <summary>
        /// Set the information in the TransportObject and send Shelveset to Codestriker.
        /// Also handles the errormessages and on successful response opens the browser with a topic url
        /// </summary>
        /// <param name="shelveset">The object is the selected PendingSet from the Codestriker Integration View</param>
        private async void ReviewExecute(
            object shelveset)
        {
            ErrorText = string.Empty;

            if (shelveset != null)
            {
                IsCreateTopicPending = true;
                CsUsername = OptionPageProxy.OptionUsername;
                CsPassword = OptionPageProxy.OptionPassword;
                if (OptionPageProxy.OptionCodestrikerUrl.IsNullOrEmpty())
                {
                    ErrorText = Resources.MissingConfigData;
                    IsCreateTopicPending = false;
                    return;
                }

                CsUri = CheckCodestrikerUrl(OptionPageProxy.OptionCodestrikerUrl);
                string email = OptionPageProxy.OptionEmail;
                if (!CsUsername.IsNullOrEmpty() && !CsPassword.IsNullOrEmpty() && !CsUri.IsNullOrEmpty() && !email.IsNullOrEmpty())
                {
                    PendingSetWrapper pendingSet = shelveset as PendingSetWrapper;
                    if (pendingSet != null)
                    {
                        TransportObject.TopicTitle = pendingSet.Set.Name;
                        TransportObject.Filename = pendingSet.Set.Name;
                        TransportObject.DiffString = m_TfsHelper.ReviewShelveset(pendingSet.Set);
                        //standart text in the topic properties page
                        TransportObject.TopicDescription = $"\n\n\nShelveset: \"{pendingSet.Set.Name};{pendingSet.Set.OwnerName}\"\nBranch:";
                        TransportObject.Email = OptionPageProxy.OptionEmail;
                        TransportObject.Reviewers = OptionPageProxy.OptionEmail;
                        TransportObject.TopicState = Resources.TopicState;
                    }


                    try
                    {
                        CodestrikerUploadManager uploadManager =
                            new CodestrikerUploadManager(new Uri(CsUri), CsUsername, CsPassword);
                        HttpStatusCode statusCode = await uploadManager.SendRequest(TransportObject);

                        if (statusCode == HttpStatusCode.OK)
                        {
                            try
                            {
                                // open browser and navigate to codestriker to the created topic
                                uploadManager.NavigateToTopic();
                            }
                            catch (Win32Exception)
                            {
                                ErrorText = Resources.NoDefaultBrowser;
                            }
                        }
                    }
                    catch (ArgumentException)
                    {
                        ErrorText = Resources.TopicLinkNotFound;
                    }
                    catch (UriFormatException)
                    {
                        ErrorText = Resources.InvalidUrlFormat;
                    }
                    catch (WebException wex)
                    {
                        switch (wex.Status)
                        {
                            case WebExceptionStatus.Success:
                                ErrorText = wex.Message;
                                break;
                            case WebExceptionStatus.NameResolutionFailure:
                                ErrorText = wex.Message;
                                break;
                            case WebExceptionStatus.ConnectFailure:
                                ErrorText = wex.Message;
                                break;
                            case WebExceptionStatus.ReceiveFailure:
                                ErrorText = wex.Message;
                                break;
                            case WebExceptionStatus.SendFailure:
                                ErrorText = wex.Message;
                                break;
                            case WebExceptionStatus.PipelineFailure:
                                ErrorText = wex.Message;
                                break;
                            case WebExceptionStatus.RequestCanceled:
                                ErrorText = wex.Message;
                                break;
                            case WebExceptionStatus.ProtocolError:
                                ErrorText = wex.Message;
                                break;
                            case WebExceptionStatus.ConnectionClosed:
                                ErrorText = wex.Message;
                                break;
                            case WebExceptionStatus.TrustFailure:
                                ErrorText = wex.Message;
                                break;
                            case WebExceptionStatus.SecureChannelFailure:
                                ErrorText = wex.Message;
                                break;
                            case WebExceptionStatus.ServerProtocolViolation:
                                ErrorText = wex.Message;
                                break;
                            case WebExceptionStatus.KeepAliveFailure:
                                ErrorText = wex.Message;
                                break;
                            case WebExceptionStatus.Pending:
                                ErrorText = wex.Message;
                                break;
                            case WebExceptionStatus.Timeout:
                                ErrorText = wex.Message;
                                break;
                            case WebExceptionStatus.ProxyNameResolutionFailure:
                                ErrorText = wex.Message;
                                break;
                            case WebExceptionStatus.UnknownError:
                                ErrorText = wex.Message;
                                break;
                            case WebExceptionStatus.MessageLengthLimitExceeded:
                                ErrorText = wex.Message;
                                break;
                            case WebExceptionStatus.CacheEntryNotFound:
                                ErrorText = wex.Message;
                                break;
                            case WebExceptionStatus.RequestProhibitedByCachePolicy:
                                ErrorText = wex.Message;
                                break;
                            case WebExceptionStatus.RequestProhibitedByProxy:
                                ErrorText = wex.Message;
                                break;
                        }
                    }
                }
                else
                {
                    ErrorText = Resources.MissingConfigData;
                }
                IsCreateTopicPending = false;
            }
            else
            {
                ErrorText = Resources.NoShelvesetSelected;
            }
        }

        private void ProjectsChanged(
            ProjectData project)
        {
            Project = project;
            OnProjectsUpdated();
        }

        protected void OnProjectsUpdated()
        {
            ProjectsUpdated?.Invoke();
        }

        #endregion
    }
}
