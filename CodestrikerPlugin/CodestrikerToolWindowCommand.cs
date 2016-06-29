//------------------------------------------------------------------------------
// <copyright file="CodestrikerToolWindowCommand.cs" company="FH-Hagenberg">
//     Copyright (c) FH-Hagenberg.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace CodestrikerPlugin
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class CodestrikerToolWindowCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
         public static readonly Guid CommandSet = new Guid("721b2dca-7d23-4a0e-97c6-83d27bc104b1");
        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="CodestrikerToolWindowCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private CodestrikerToolWindowCommand(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException(nameof(package));
            }

            this.package = package;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandId = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(this.ShowToolWindow, menuCommandId);
                commandService.AddCommand(menuItem);
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static CodestrikerToolWindowCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get { return this.package; }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new CodestrikerToolWindowCommand(package);
        }

        /// <summary>
        /// Shows the tool window when the menu item is clicked.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void ShowToolWindow(object sender, EventArgs e)
        {
            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            ToolWindowPane window = this.package.FindToolWindow(typeof(CodestrikerToolWindow), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException("Cannot create tool window");
            }

            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
           
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }


        //properties for accessing the data in the optionspage
        public string OptionUsername
        {
            get
            {
                CodestrikerToolWindowPackage myToolsOptionsPackage = this.package as CodestrikerToolWindowPackage;
                if (myToolsOptionsPackage != null) return myToolsOptionsPackage.OptionUsername;
                return string.Empty;
            }
        }

        public string OptionPassword
        {
            get
            {
                CodestrikerToolWindowPackage myToolsOptionsPackage = this.package as CodestrikerToolWindowPackage;
                if (myToolsOptionsPackage != null) return myToolsOptionsPackage.OptionPassword;
                return string.Empty;
            }
        }

        public string OptionEmail
        {
            get
            {
                CodestrikerToolWindowPackage myToolsOptionsPackage = this.package as CodestrikerToolWindowPackage;
                if (myToolsOptionsPackage != null) return myToolsOptionsPackage.OptionEmail;
                return string.Empty;
            }
        }

        public string OptionCodestrikerURL
        {
            get
            {
                CodestrikerToolWindowPackage myToolsOptionsPackage = this.package as CodestrikerToolWindowPackage;
                if (myToolsOptionsPackage != null) return myToolsOptionsPackage.OptionCodestrikerUrl;
                return string.Empty;
            }
        }
       
    }
}
