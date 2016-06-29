//------------------------------------------------------------------------------
// <copyright file="CodestrikerToolWindow.cs" company="FH-Hagenberg">
//     Copyright (c) FH-Hagenberg.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace CodestrikerPlugin
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.Shell;

    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid("65888a65-3de3-49f4-bcb4-dbbad6347213")]
    public class CodestrikerToolWindow : ToolWindowPane
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CodestrikerToolWindow"/> class.
        /// </summary>
        public CodestrikerToolWindow() : base(null)
        {
            this.Caption = Resources.CodestrikerIntegration;
            

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            this.Content = new CodestrikerToolWindowControl();
        }
    }
}
