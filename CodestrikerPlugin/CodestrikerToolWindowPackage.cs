//------------------------------------------------------------------------------
// <copyright file="CodestrikerToolWindowPackage.cs" company="FH-Hagenberg">
//     Copyright (c) FH-Hagenberg.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using CodestrikerPlugin.OptionPage;
using CodestrikerPlugin.Util;
using CodestrikerPlugin.ViewModels;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;

namespace CodestrikerPlugin
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    /// 
    [ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionHasMultipleProjects_string)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionHasSingleProject_string)]
    
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [ProvideMenuResource("Menus.ctmenu", 3)]
    [ProvideToolWindow(typeof(CodestrikerToolWindow))]
    [ProvideOptionPage(typeof(CodestrikerOptionPageGrid), "Codestriker Integration", "User Data", 0, 0, true)]
    [Guid(PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class CodestrikerToolWindowPackage : Package
    {
        /// <summary>
        /// CodestrikerToolWindowPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "61f2557f-5540-4066-b903-4aef72d1c99a";
        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            CodestrikerToolWindowCommand.Initialize(this);
            base.Initialize();
            TfsHelper tfsHelper = TfsHelper.Instance;
            tfsHelper.ContextChanged += TfsHelper_ContextChanged;
            tfsHelper.UsernameChanged += TfsHelper_UsernameChanged;
            tfsHelper.ConnectToFirstServerInList();
        }


        private void TfsHelper_UsernameChanged(
            string username)
        {
            CodestrikerPluginViewModel viewModelContext = CodestrikerPluginViewModel.Instance;
            viewModelContext.TfsUsername = username;
        }

        private void TfsHelper_ContextChanged(ProjectData project)
        {
            CodestrikerPluginViewModel viewModelContext = CodestrikerPluginViewModel.Instance;
            viewModelContext.Project = project;
        }

        #endregion

        public string OptionUsername
        {
            get
            {
                CodestrikerOptionPageGrid page = (CodestrikerOptionPageGrid)GetDialogPage(typeof(CodestrikerOptionPageGrid));
                return page.OptionUsername;
            }
        }

        public string OptionPassword
        {
            get
            {
                CodestrikerOptionPageGrid page = (CodestrikerOptionPageGrid)GetDialogPage(typeof(CodestrikerOptionPageGrid));
                return page.OptionPassword;
            }
        }

        public string OptionEmail
        {
            get
            {
                CodestrikerOptionPageGrid page = (CodestrikerOptionPageGrid)GetDialogPage(typeof(CodestrikerOptionPageGrid));
                return page.OptionEmail;
            }
        }

        public string OptionCodestrikerUrl
        {
            get
            {
                CodestrikerOptionPageGrid page = (CodestrikerOptionPageGrid)GetDialogPage(typeof(CodestrikerOptionPageGrid));
                return page.OptionCodestrikerUrl;
            }
        }
    }
}
