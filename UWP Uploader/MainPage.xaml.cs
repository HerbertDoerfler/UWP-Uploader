///////////////////////////////////////////////////////////////////////
/////////////////////// DOWNLOAD FROM WIN NEVIS ///////////////////////
////////////////////// http://www.win-nevis.com ///////////////////////
//////////////////////////// Ramtin Jokar /////////////////////////////

//////////////////////////// WARNING!!!!!!!!!!!!!
// Make sure Internet (Client) and Internet (Client & Server) capabilities is checked in Package.appxmanifest
/////////////////////////////////////////////////

using System;
using System.Text;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace UWP_Uploader
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }
        StorageFile fileToUpload = null;
        private async void btnOpenFile_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var filePicker = new FileOpenPicker();
            filePicker.ViewMode = PickerViewMode.Thumbnail;
            filePicker.CommitButtonText = "Add";
            filePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.VideosLibrary;
            filePicker.FileTypeFilter.Add("*");
            fileToUpload = await filePicker.PickSingleFileAsync();

            if (fileToUpload == null)
                return;
            string text = "FileName: " + fileToUpload.Name + Environment.NewLine;
            text += "FileType: " + fileToUpload.FileType;
            txtFile.Text = text;
        }

        private void btnUploadFile_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (fileToUpload == null)
                return;
            Upload upload = new Upload();
            upload.Name = fileToUpload.Name;
            upload.Path = fileToUpload.Path;
            WNUploader uploader = new WNUploader(upload, fileToUpload);
            uploader.OnUpload += new UploadGrabber(OnUpload);
            uploader.StartUpload();
        }
        private async void OnUpload(object sender, Upload upload)
        {
            if (sender != null && upload != null)
            {
                var dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;
                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("FileName: " + upload.Name);
                    sb.AppendLine(string.Format("TotalBytes: {0}kb", upload.TotalBytes / 1024));
                    sb.AppendLine(string.Format("BytesSent: {0}kb", upload.BytesSent / 1024));
                    sb.AppendLine(string.Format("ElapsedTime: {0}.{1} sec", upload.ElapsedTime.Seconds.ToString("00"),
                        upload.ElapsedTime.Milliseconds.ToString("00")));

                    sb.AppendLine("State: " + upload.State);
                    if (upload.State == UploaderState.Error)
                        sb.AppendLine("ErrorMessage: " + upload.ErrorMessage);
                    else if (upload.State == UploaderState.Uploaded)
                        sb.AppendLine("UploadedUrl: " + upload.UploadedUrl);

                    switch (upload.State)
                    {
                        case UploaderState.Error:
                            txtUpload.Foreground = new SolidColorBrush(Colors.Red);
                            break;
                        case UploaderState.Uploaded:
                            txtUpload.Foreground = new SolidColorBrush(Colors.Green);
                            break;
                        default:
                            txtUpload.Foreground = new SolidColorBrush(Colors.Cyan);
                            break;
                    }
                


                    txtUpload.Text = sb.ToString();
                });
            }
        }
    }
}
