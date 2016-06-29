using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using EnvDTE80;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Common;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TeamFoundation;
using Microsoft.TeamFoundation;

namespace CodestrikerPlugin.Util
{
    public delegate void ProjectContextChanged(ProjectData project);
    public delegate void CurrentUsernameChanged(string username);

    public enum FetchState
    {
        None,
        UserNotFound,
        UserNotLoggedIn,
        Successful,
        NoProject
    }

    public class TfsHelper
    {
        #region constants
        #endregion

        #region private members

        private VersionControlServer m_VersionControl;
        private readonly TeamFoundationServerExt m_FoundationServerExt;
        private TfsConfigurationServer m_TfsConfigurationServer;
        private static TfsHelper s_Instance;
        private static readonly string s_LinkCompletionVersioncontrol = "/_versionControl/shelveset?ss="; //necessary for linkcompletion
        private static readonly string s_PathParameter = "&path="; //parameter for filepath 
        private static readonly string s_ActionParameter = "&_a=compare"; //compare action
        private static readonly string s_RegexPattern = @"(?<prefix>[+]{3}[^:]*): (?<filename>(.*);)"; //regex for replacement of filename with the URL
        private static readonly string s_TeamFoundationServerExtString = "Microsoft.VisualStudio.TeamFoundation.TeamFoundationServerExt"; //TFS Server class
        private static readonly string s_TeamConfigServerIdProperty = "InstanceID";
        #endregion

        #region properties

        public ProjectData Project { get; set; }

        #endregion

        #region event declaration

        public event ProjectContextChanged ContextChanged;
        public event CurrentUsernameChanged UsernameChanged;

        #endregion

        #region Constructor

        public static TfsHelper Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new TfsHelper();
                }
                return s_Instance;
            }
        }

        private TfsHelper()
        {
            Project = new ProjectData();
            var dte2 = (DTE2)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SDTE));
            m_FoundationServerExt =
                dte2.GetObject(s_TeamFoundationServerExtString) as
                    TeamFoundationServerExt;

            //register on the ProjectContextChanged event
            if (m_FoundationServerExt != null) { 
                m_FoundationServerExt.ProjectContextChanged +=
                    FoundationServerExt_ProjectContextChanged;
            }
        }


       
        /// <summary>
        ///if no connection is estblished, connect to the first registered server  
        /// </summary>
        public void ConnectToFirstServerInList()
        {
            RegisteredProjectCollection[] tfsList = RegisteredTfsConnections.GetProjectCollections();
            if (tfsList.Length > 0)
            {
                var tfsUri = tfsList[0].Uri;
                ConnectToTfsServer(tfsUri);
            }
        }

        #endregion

        #region methods

        // ProjectList Changed
        protected void OnChanged(
            ProjectData project)
        {
            ContextChanged?.Invoke(project);
        }

        protected void OnSetUsername(
           string username)
        {
            UsernameChanged?.Invoke(username);
        }

         
        /// <summary>
        /// connect to a TFS Server and load the shelvesets after the ProjectContext has changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void FoundationServerExt_ProjectContextChanged(
            object sender,
            EventArgs e)
        {
            ProjectContextExt context = m_FoundationServerExt.ActiveProjectContext;
           
            if (!string.IsNullOrEmpty(context.DomainUri))
            {
                Uri tfsUri = new Uri(context.DomainUri);
                ConnectToTfsServer(tfsUri);
            }
            
        }

        
        /// <summary>
        /// connect to a TFS Server.
        /// </summary>
        /// <param name="tfsUri">Possible URI of the TFS Server</param>
        /// <returns>The correct Tfs Uri</returns>
        public Uri ConnectToTfsServer(Uri tfsUri)
        {           
            bool tfsIsConnected = false;

            //The URI we get from FoundationServerExt_ProjectContextChanged does not allow us to connect to the server
            //Trim the URI from back to front till connection can be established
            while (!tfsIsConnected && tfsUri.AbsolutePath != tfsUri.Authority)
            {
                m_TfsConfigurationServer =
                    TfsConfigurationServerFactory.GetConfigurationServer(tfsUri);
                tfsUri =
                    new Uri(
                        tfsUri.OriginalString.Substring(0, tfsUri.OriginalString.LastIndexOf('/')));

                // workaround: if  tfsConfigurationServer.EnsureAuthenticated() fails it doesn't set tfsIsConnected to true and continues to trim uri
                try
                {
                    m_TfsConfigurationServer.EnsureAuthenticated();
                    tfsIsConnected = true;
                }
                catch (TeamFoundationServerException)
                {
                    tfsIsConnected = false;
                }
            }

            if (tfsIsConnected)
            {
                FetchProjects();
            }

            return tfsUri;
        }


       
        /// <summary>
        /// Load projects from certain user
        /// </summary>
        /// <param name="username">The name of the user which Shelvesets should be fetched</param>
        /// <returns>State if fetching Projects was succesfull</returns>
        public FetchState FetchProjects(string username = "")
        {
            if (m_TfsConfigurationServer == null)
            {
                return FetchState.UserNotLoggedIn;
            }
            var configurationServerNode = m_TfsConfigurationServer.CatalogNode;
            if(configurationServerNode == null)
            {
                return FetchState.NoProject;
            }
            IReadOnlyCollection<CatalogNode> tfsTeamProjectNodes = configurationServerNode.QueryChildren(
                new Guid[] { CatalogResourceTypes.ProjectCollection },
                false,
                CatalogQueryOptions.None);

           
            foreach (CatalogNode projectNode in tfsTeamProjectNodes)
            {
                var tfsTeamProjectCollection = m_TfsConfigurationServer.GetTeamProjectCollection(
                    new Guid(projectNode.Resource.Properties[s_TeamConfigServerIdProperty]));
                m_VersionControl = tfsTeamProjectCollection.GetService<VersionControlServer>();
               PendingSet[] allSets;
               
                // at start up or context change use authenticated user and on search the given username
                if (string.IsNullOrEmpty(username))
                {
                    TeamFoundationIdentity identity;
                    
                    m_TfsConfigurationServer.GetAuthenticatedIdentity(out identity);
                    allSets = m_VersionControl.QueryShelvedChanges(
                        null,
                        identity.DisplayName);
                    username = identity.DisplayName;
                }
                else
                {
                    try
                    {
                        allSets = m_VersionControl.QueryShelvedChanges(
                                               null,
                                               username);
                        
                    }
                    catch (Microsoft.TeamFoundation.VersionControl.Client.IdentityNotFoundException)
                    {
                        return FetchState.UserNotFound;
                        //nothing to do here
                        //m_VersionControl.QueryShelvedChanges throws an exception if the given username is not valid
                    }
                }

                IList<PendingSetWrapper> wrapper = new List<PendingSetWrapper>();
                
                //get Shelveset from PendingSet.
                //need the CreatedDate from the Shelveset to order the PndingSets 
                foreach (PendingSet set in allSets)
                {
                    Shelveset[] shelvesets = m_VersionControl.QueryShelvesets(set.Name, username);
                    wrapper.Add(new PendingSetWrapper(set, shelvesets[0].CreationDate));
                }
                var orderedWrapper = wrapper.OrderByDescending(d => d.CreationDate.Date);

                Project = new ProjectData(projectNode.Resource.DisplayName, orderedWrapper.ToArray());               
            }

            OnSetUsername(username);
            OnChanged(Project);

            return FetchState.Successful;
        }


        /// <summary>
        /// Fetch the projects asynchronously
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public Task<FetchState> FetchProjectsAsync(string username = "")
        {
            return Task.Factory.StartNew(() => FetchProjects(username));
        }

        /// <summary>
        /// Create a difference string from a shelveset
        /// Also sets the URl to the file on the TFS Server
        /// </summary>
        /// <param name="set">The selected PendingSet</param>
        /// <returns>the diff String which is sent to Codestriker</returns>
        public string ReviewShelveset(
            PendingSet set)
        {
            if (set == null)
                throw new ArgumentNullException(nameof(set));
            
            
            MemoryStream stream = new MemoryStream();

            StreamWriter writer = new StreamWriter(stream);
            {
                DiffOptions options = new DiffOptions
                {
                    Flags = DiffOptionFlags.EnablePreambleHandling,
                    OutputType = DiffOutputType.Unified,
                    TargetEncoding = Encoding.UTF8,
                    SourceEncoding = Encoding.UTF8,
                    Recursive = true,
                    StreamWriter = writer
                };

                StringBuilder sb = new StringBuilder();
                int streampos = 0;
                foreach (var pendingchange in set.PendingChanges)
                {
                    if (pendingchange.ItemType != ItemType.Folder)
                    {
                        
                        string fileUrl = string.Format(pendingchange.VersionControlServer.TeamProjectCollection.Uri.AbsoluteUri);
                        fileUrl += s_LinkCompletionVersioncontrol;
                        fileUrl +=
                            $"{HttpUtility.UrlEncode(pendingchange.PendingSetName + ";" + pendingchange.PendingSetOwner)}{s_PathParameter}";
                        fileUrl += HttpUtility.UrlEncode(pendingchange.LocalOrServerItem);
                        //the semicolons after s_ActionParameter and pendingchange.FileName are important for the right parsing on codestriker
                        fileUrl += $"{s_ActionParameter}&#path={HttpUtility.UrlEncode(pendingchange.LocalOrServerItem)}{s_ActionParameter};{pendingchange.LocalOrServerItem};";
                        
                        var diffChange = new DiffItemShelvedChange(set.Name, pendingchange);
                        var diffVersion = Difference.CreateTargetDiffItem(m_VersionControl, pendingchange, null);

                        Difference.DiffFiles(
                            m_VersionControl,
                            diffVersion,
                            diffChange,
                            options,
                            string.Empty, 
                            true);

                        writer.Flush();
                        sb.Append(writer.GetHashCode()).Append(Environment.NewLine);
                        string pendingChangeString = Encoding.UTF8.GetString(stream.ToArray().Skip(streampos).ToArray());
              
                        Regex rgx = new Regex(s_RegexPattern);

                        //replace the filename with the tfs url path of the file
                        //necessary for the link between codestriker and TFS
                        pendingChangeString = rgx.Replace(
                            pendingChangeString,
                            m => m.Groups["prefix"].Value + ": " + fileUrl,
                            1);

                        sb.Append(pendingChangeString);
                        streampos = stream.ToArray().Length;
                        
                    }
                }
                
                return sb.ToString();
            }

            #endregion
        }
    }
}