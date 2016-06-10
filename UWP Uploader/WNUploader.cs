///////////////////////////////////////////////////////////////////////
/////////////////////// DOWNLOAD FROM WIN NEVIS ///////////////////////
////////////////////// http://www.win-nevis.com ///////////////////////
//////////////////////////// Ramtin Jokar /////////////////////////////

//////////////////////////// WARNING!!!!!!!!!!!!!
// Make sure Internet (Client) and Internet (Client & Server) capabilities is checked in Package.appxmanifest
/////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using Windows.Storage;

namespace UWP_Uploader
{
    public class WNUploader
    {
        public event UploadGrabber OnUpload;

        private Upload r_upload = new Upload();
        public Upload Upload { get { return r_upload; } set { r_upload = value; } }
        public StorageFile FileToUpload { get; set; }
        private Stopwatch stopWatch = new Stopwatch();

        public WNUploader()
        {
            Upload = new Upload();
            stopWatch.Reset();
        }

        public WNUploader(Upload upload, StorageFile file)
        {
            Upload = upload;
            FileToUpload = file;
            stopWatch.Reset();
        }

        public void StartUpload()
        {
            Upload.State = UploaderState.None;
            Upload.ErrorMessage = "";
            Upload.UploadedUrl = "";
            Upload.ElapsedTime = TimeSpan.FromMilliseconds(0);
            Upload.BytesSent = 0;
            Upload.TotalBytes = 0;
            SendEvent(Upload);
            stopWatch.Reset();
            NameValueCollection nvc = new NameValueCollection();
            nvc.Add("id", "uploadfile");
            UploadFile("http://bhs11.uploadboy.com/cgi-bin/upload.cgi?upload_type=file",
                 FileToUpload, "file", nvc);
        }

        private async void UploadFile(string url, StorageFile file, string paramName, NameValueCollection nvc)
        {
            try
            {
                stopWatch.Start();

                Upload.State = UploaderState.PrepareUpload;
                SendEvent(Upload);

                string contentType = file.ContentType;
                Debug.WriteLine(string.Format("Uploading {0} to {1}", file, url));
                string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
                byte[] boundarybytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

                HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(url);
                wr.ContentType = "multipart/form-data; boundary=" + boundary;
                wr.Method = "POST";
                wr.Credentials = CredentialCache.DefaultCredentials;

                Stream rs = await wr.GetRequestStreamAsync();

                string formdataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";
                foreach (string key in nvc.Keys)
                {
                    await rs.WriteAsync(boundarybytes, 0, boundarybytes.Length);
                    string formitem = string.Format(formdataTemplate, key, nvc[key]);
                    byte[] formitembytes = Encoding.UTF8.GetBytes(formitem);
                    await rs.WriteAsync(formitembytes, 0, formitembytes.Length);
                }
                await rs.WriteAsync(boundarybytes, 0, boundarybytes.Length);

                string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";
                string header = string.Format(headerTemplate, paramName, file.Name, contentType);
                byte[] headerbytes = Encoding.UTF8.GetBytes(header);
                await rs.WriteAsync(headerbytes, 0, headerbytes.Length);


                using (Stream fileStream = (await file.OpenAsync(FileAccessMode.Read)).AsStream())
                {
                    byte[] buffer = new byte[4096];
                    int bytesRead = 0;
                    int readed = 0;

                    while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                    {
                        await rs.WriteAsync(buffer, 0, bytesRead);
                        readed += bytesRead;
                        Upload.State = UploaderState.Uploading;
                        Upload.ElapsedTime = stopWatch.Elapsed;
                        Upload.BytesSent = readed;
                        Upload.TotalBytes = fileStream.Length;
                        SendEvent(Upload);
                    }
                }
                Upload.State = UploaderState.GettingResponse;
                Upload.ElapsedTime = stopWatch.Elapsed;
                SendEvent(Upload);
                byte[] trailer = Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
                await rs.WriteAsync(trailer, 0, trailer.Length);
                rs.Dispose();
                Upload.ElapsedTime = stopWatch.Elapsed;
                SendEvent(Upload);
                WebResponse wresp = null;
                try
                {
                    wresp = await wr.GetResponseAsync();
                    using (Stream stream2 = wresp.GetResponseStream())
                    {
                        Upload.ElapsedTime = stopWatch.Elapsed;
                        SendEvent(Upload);
                        var jsonSerializer = new DataContractJsonSerializer(typeof(ResponseUPArray));

                        var responseJson = (ResponseUPArray)jsonSerializer.ReadObject(stream2);
                        //[{"file_code":"eamk4iwzov47","file_status":"OK"}]
                        if (responseJson[0].file_status == "OK")
                        {
                            Upload.State = UploaderState.Uploaded;
                            Upload.UploadedUrl = "http://uploadboy.com/" + responseJson[0].file_code;
                            SendEvent(Upload);
                        }
                        else
                        {
                            Upload.State = UploaderState.Error;
                            Upload.ErrorMessage = "file_status: " + responseJson[0].file_status;
                            SendEvent(Upload);
                        }
                        stopWatch.Stop();
                        Upload.ElapsedTime = stopWatch.Elapsed;
                        SendEvent(Upload);
                    }
                }
                catch (Exception ex)
                {
                    stopWatch.Stop();

                    Upload.State = UploaderState.Error;
                    Upload.ErrorMessage = "Error while gettingResponse.\r\rException: " + ex.Message;
                    SendEvent(Upload);
                    if (wresp != null)
                    {
                        wresp.Dispose();
                        wresp = null;
                    }
                }
                finally
                {
                    stopWatch.Stop();
                    wr = null;
                }
            }
            catch (WebException ex)
            {
                using (WebResponse response = ex.Response)
                {
                    HttpWebResponse httpResponse = (HttpWebResponse)response;
                    Upload.State = UploaderState.Error;
                    Upload.ErrorMessage = string.Format("Error code: {0}\tWebException: {1}", httpResponse.StatusCode, ex.Message);
                    SendEvent(Upload);
                }
            }
            catch (Exception ex)
            {
                Upload.State = UploaderState.Error;
                Upload.ErrorMessage = string.Format("Error while uploading\r\nException: {1}", ex.Message);
                SendEvent(Upload);
            }

        }

        private void SendEvent(Upload upload = null)
        {
            if (upload == null)
                OnUpload?.Invoke(this, Upload);
            else
                OnUpload?.Invoke(this, upload);
        }
    }

    public enum UploaderState
    {
        None = -1,
        PrepareUpload,
        Uploading,
        GettingResponse,
        Uploaded,
        Error
    }

    public class Upload
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string UploadedUrl { get; set; }
        public long TotalBytes { get; set; }
        public long BytesSent { get; set; }
        public UploaderState State { get; set; }
        public TimeSpan ElapsedTime { get; set; }
        public string ErrorMessage { get; set; }
        public override string ToString()
        {
            return Name.ToString();
        }
    }

    public delegate void UploadGrabber(object sender, Upload upload);

    public class ResponseUP
    {
        //[{"file_code":"eamk4iwzov47","file_status":"OK"}]
        public string file_code { get; set; }
        public string file_status { get; set; }
    }
    public class ResponseUPArray : List<ResponseUP> { }
}
