#region Using directives
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Blazorise.Extensions;
using Blazorise.Localization;
using Blazorise.Modules;
using Blazorise.Utilities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
#endregion

namespace Blazorise
{
    /// <summary>
    /// Builds upon FileEdit component providing extra file uploading features.
    /// </summary>
    public partial class FilePicker : BaseComponent
    {
        #region Members

        private string ElementContainerId { get; set; }
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        protected override bool ShouldAutoGenerateId => true;

        /// <summary>
        /// Tracks the current file being uploaded.
        /// </summary>
        IFileEntry fileBeingUploaded;

        #endregion

        #region Methods

        /// <inheritdoc/>
        protected override void OnInitialized()
        {
            ElementContainerId = IdGenerator.Generate;

            base.OnInitialized();
        }

        /// <inheritdoc/>
        protected override async Task OnFirstAfterRenderAsync()
        {
            await JSFilePickerModule.Initialize( ElementRef, ElementContainerId );

            await base.OnFirstAfterRenderAsync();
        }

        /// <summary>
        /// Gets progress for percentage display.
        /// </summary>
        /// <returns></returns>
        protected int GetProgressPercentage()
            => (int)( FileEdit.GetCurrentProgress().Progress * 100d );

        /// <summary>
        /// Tracks whether the current file is being uploaded.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        protected bool IsFileBeingUploaded( IFileEntry file )
            => file.IsEqual( fileBeingUploaded );

        /// <summary>
        /// Tracks whether the FilePicker is busy and user interaction should be disabled.
        /// </summary>
        /// <returns></returns>
        protected bool IsBusy()
            => fileBeingUploaded is not null;

        /// <summary>
        /// Tracks whether the FilePicker has files ready to upload.
        /// </summary>
        /// <returns></returns>
        protected bool IsUploadReady()
            => FileEdit.Files?.Any( x => x.Status == FileEntryStatus.Ready ) ?? false;


        /// <summary>
        /// FilePicker's handling of the Started Event.
        /// </summary>
        /// <param name="fileStartedEventArgs"></param>
        /// <returns></returns>
        protected Task OnStarted( FileStartedEventArgs fileStartedEventArgs )
        {
            fileBeingUploaded = fileStartedEventArgs.File;
            return Started.InvokeAsync( fileStartedEventArgs );
        }

        /// <summary>
        /// FilePicker's handling of the Changed Event.
        /// </summary>
        /// <param name="fileChangedEventArgs"></param>
        /// <returns></returns>
        protected Task OnChanged( FileChangedEventArgs fileChangedEventArgs )
            => Changed.InvokeAsync( fileChangedEventArgs );

        /// <summary>
        /// FilePicker's handling of the Ended Event.
        /// </summary>
        /// <param name="fileEndedEventArgs"></param>
        /// <returns></returns>
        protected Task OnEnded( FileEndedEventArgs fileEndedEventArgs )
        {
            fileBeingUploaded = null;
            return Ended.InvokeAsync( fileEndedEventArgs );
        }

        /// <summary>
        /// FilePicker's handling of the Progressed Event.
        /// </summary>
        /// <param name="fileProgressedEventArgs"></param>
        /// <returns></returns>
        protected Task OnProgressed( FileProgressedEventArgs fileProgressedEventArgs )
        {
            return Progressed.InvokeAsync( fileProgressedEventArgs );
        }

        /// <summary>
        /// Converts the file size in bytes into a proper human readable format.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        protected string GetFileSizeReadable( IFileEntry file )
            => Formaters.GetBytesReadable( file.Size );


        /// <summary>
        /// Gets the File Status
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        protected string GetFileStatus( IFileEntry file )
        {
            //TODO: Localizer
            switch ( file.Status )
            {
                case FileEntryStatus.Ready:
                    return "Ready to upload";
                case FileEntryStatus.Uploaded:
                    return "Uploaded successfully";
                case FileEntryStatus.ExceedsMaximumSize:
                    return "File size is too large";
                case FileEntryStatus.Error:
                    return "Error uploading";
                default:
                    break;
            }
            return string.Empty;
        }

        /// <summary>
        /// Removes the file from FileEdit.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        protected ValueTask RemoveFile( IFileEntry file )
            => FileEdit.RemoveFile( file.Id );

        /// <summary>
        /// Clears the FileEdit by resetting the state.
        /// </summary>
        /// <returns></returns>
        protected Task Clear()
            => FileEdit.Reset().AsTask();

        /// <summary>
        /// Uploads the current files.
        /// </summary>
        /// <returns></returns>
        protected async Task UploadAll()
        {
            if ( Upload.HasDelegate && !FileEdit.Files.IsNullOrEmpty() )
                foreach ( var file in FileEdit.Files )
                {
                    if ( file.Status == FileEntryStatus.Ready )
                        await Upload.InvokeAsync( new( file ) );
                }
        }


        #endregion

        #region Properties

        /// <summary>
        /// Accesses the FileEdit
        /// </summary>
        public FileEdit FileEdit;

        /// <summary>
        /// Gets or sets the <see cref="IJSFilePickerModule"/> instance.
        /// </summary>
        [Inject] public IJSFilePickerModule JSFilePickerModule { get; set; }

        /// <summary>
        /// Input content.
        /// </summary>
        [Parameter] public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Placeholder for validation messages.
        /// </summary>
        [Parameter] public RenderFragment Feedback { get; set; }

        /// <summary>
        /// Enables the multiple file selection.
        /// </summary>
        [Parameter]
        public bool Multiple { get; set; }

        /// <summary>
        /// Sets the placeholder for the empty file input.
        /// </summary>
        [Parameter] public string Placeholder { get; set; }

        /// <summary>
        /// Specifies the types of files that the input accepts. https://www.w3schools.com/tags/att_input_accept.asp"
        /// </summary>
        [Parameter] public string Filter { get; set; }

        /// <summary>
        /// Gets or sets the max chunk size when uploading the file.
        /// </summary>
        [Parameter] public int MaxChunkSize { get; set; } = 20 * 1024;

        /// <summary>
        /// Maximum file size in bytes, checked before starting upload (note: never trust client, always check file
        /// size at server-side). Defaults to <see cref="long.MaxValue"/>.
        /// </summary>
        [Parameter] public long MaxFileSize { get; set; } = long.MaxValue;

        /// <summary>
        /// Gets or sets the Segment Fetch Timeout when uploading the file.
        /// </summary>
        [Parameter] public TimeSpan SegmentFetchTimeout { get; set; } = TimeSpan.FromMinutes( 1 );

        /// <summary>
        /// Occurs every time the selected file has changed, including when the reset operation is executed.
        /// </summary>
        [Parameter] public EventCallback<FileChangedEventArgs> Changed { get; set; }

        /// <summary>
        /// Occurs when an individual file upload has started.
        /// </summary>
        [Parameter] public EventCallback<FileStartedEventArgs> Started { get; set; }

        /// <summary>
        /// Occurs when an individual file upload has ended.
        /// </summary>
        [Parameter] public EventCallback<FileEndedEventArgs> Ended { get; set; }

        /// <summary>
        /// Occurs every time the part of file has being written to the destination stream.
        /// </summary>
        [Parameter] public EventCallback<FileWrittenEventArgs> Written { get; set; }

        /// <summary>
        /// Notifies the progress of file being written to the destination stream.
        /// </summary>
        [Parameter] public EventCallback<FileProgressedEventArgs> Progressed { get; set; }

        /// <summary>
        /// Occurs once the FilePicker's Upload is triggered for every file.
        /// </summary>
        [Parameter] public EventCallback<FileUploadEventArgs> Upload { get; set; }

        /// <summary>
        /// If true file input will be automatically reset after it has being uploaded.
        /// </summary>
        [Parameter] public bool AutoReset { get; set; } = false;

        /// <summary>
        /// Function used to handle custom localization that will override a default <see cref="ITextLocalizer"/>.
        /// </summary>
        [Parameter] public TextLocalizerHandler BrowseButtonLocalizer { get; set; }

        #endregion
    }
}
