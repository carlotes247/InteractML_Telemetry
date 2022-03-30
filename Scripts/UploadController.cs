using System;
using System.Collections.Generic;
using Dasync.Collections;
using System.IO;
using System.Threading.Tasks;
using System.Net;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Net.Http;
using System.Threading;

namespace InteractML.Telemetry
{
    /// <summary>
    /// Handles read/write logic to external server
    /// </summary>
    public class UploadController : MonoBehaviour
    {
        #region Variables

        /// <summary>
        /// Has the file been sent? Used to allow sending files only once
        /// </summary>
        bool fileSent = false;

        /// <summary>
        /// Specifies the implementation to use when doing a REST upload
        /// </summary>
        public enum UploadRESTOptions { DotNetWebRequest, UnityWebRequest, WWW }

        /// <summary>
        /// The high-level server url including protocol (i.e. HTTPS), domain (i.e. blog.contoso.com), and any other relevant directory info (i.e. /data/b)
        /// </summary>
        private string m_ServerURL = "https://firebasestorage.googleapis.com/v0/b/";
        /// <summary>
        /// Globabl subdirectoy where all POST querys will go into (i.e. /blog , firebase-project-id.appspot.com, etc.)
        /// </summary>
        private string m_ServerGlobalSubDirectory = "iml-features-study.appspot.com";
        /// <summary>
        /// Specifying HTTP header content type to be interpreted by server (i.e. application/json, application/force-fownload, text/html)
        /// more info: www.iana.org/assignments/media-types/media-types.xhtml 
        /// </summary>
        private string m_ContentType = "application/force-download";

        #endregion

        #region Public Methods

        /// <summary>
        /// Uploads a file or directory to the firebase server
        /// </summary>
        /// <param name="localFilePath">path in disk</param>
        /// <param name="serverFilePath">path we want to upload to in the server</param>
        public void UploadAsync(string localFilePath, string serverFilePath, bool useTasks = true)
        {
            if (fileSent)
            {
                Debug.LogError("Already uploaded to server, it won't upload again with the same instance (to avoid uploading twice when not needed)");
                //return;
            }

            // Get timestamp
            //string date = DateTime.Today.ToString("f");
            string date = DateTime.Now.ToLocalTime().ToString("s") + "/"; // prepared for path
                                                                          //string date = "";

            // Add timestampt to serverfilepath
            serverFilePath = Path.Combine(serverFilePath, date);

            var uploadCoroutine = UploadREST(localFilePath, serverFilePath, UploadRESTOptions.DotNetWebRequest, useTasks);
            StartCoroutine(uploadCoroutine);
            fileSent = true;

        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Upload files using REST API (all platforms, slower)
        /// </summary>
        /// <param name="localFilePath"></param>
        /// <param name="serverFilePath"></param>
        /// <returns></returns>
        private IEnumerator UploadREST(string localFilePath, string serverFilePath, UploadRESTOptions options = UploadRESTOptions.DotNetWebRequest, bool useTasks = true)
        {
            if (string.IsNullOrEmpty(localFilePath))
            {
                Debug.LogError("No file specified but uploadFile called!");
                yield break;
            }

            // Wait for a few miliseconds, to allow the training examples to be fully written in disk
            yield return new WaitForSeconds(0.2f);

            if (useTasks)
            {
                Task.Run(async () => await UploadRESTAsync(localFilePath, serverFilePath, isDirectory: true));

                //Task.Factory.StartNew(() => UploadRESTAsync(localFilePath, serverFilePath, isDirectory: true), TaskCreationOptions.LongRunning);

                //Thread t = new Thread(async () => 
                //{
                //    Thread.CurrentThread.IsBackground = true;
                //    await UploadRESTAsync(localFilePath, serverFilePath, isDirectory: true);
                //});
                //t.Start();
                //Debug.Log("Thread started!");


                // wait a frame
                yield return null;
            }
            else
            {
                // Is it a file or a folder?
                bool fileDetected = false;
                bool directoryDetected = false;
                FileAttributes attr = File.GetAttributes(localFilePath);
                //detect whether its a directory or file
                if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                    directoryDetected = true;
                else
                    fileDetected = true;

                // Upload a file on start
                // Locate file
                if (fileDetected && File.Exists(localFilePath))
                {
                    //Debug.Log("Attempting file upload...");
                    StartCoroutine(UploadFileREST(localFilePath, serverFilePath, options));

                }
                else if (directoryDetected && Directory.Exists(localFilePath))
                {
                    //Debug.Log("Attempting directory upload...");

                    // First, find all the folders
                    // Iterate to upload all files in folder, including subdirectories
                    string[] files = Directory.GetFiles(localFilePath, "*", SearchOption.AllDirectories);

                    foreach (string file in files)
                    {
                        if (Path.GetExtension(file) == ".meta")
                        {
                            // Skip meta files
                            continue;
                        }
                        StartCoroutine(UploadFileREST(file, serverFilePath, options));

                    }
                }
            }

        }

        /// <summary>
        /// Upload files using REST API (all platforms, slower)
        /// </summary>
        /// <param name="localFilePath"></param>
        /// <param name="serverFilePath"></param>
        /// <returns></returns>
        private async Task UploadRESTAsync(string localFilePath, string serverFilePath, bool isDirectory = false)
        {
            await Task.Run(async () =>
            {
                if (isDirectory && Directory.Exists(localFilePath))
                {
                    //Debug.Log("Attempting directory upload...");
                    await UploadFolderRESTAsync(localFilePath, serverFilePath);
                }
                else
                {
                    // Is it a file or a folder?
                    bool fileDetected = false;
                    bool directoryDetected = false;
                    FileAttributes attr = File.GetAttributes(localFilePath);
                    //detect whether its a directory or file
                    if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                        directoryDetected = true;
                    else
                        fileDetected = true;

                    // Upload a file on start
                    // Locate file
                    if (fileDetected && File.Exists(localFilePath))
                    {
                        //Debug.Log("Attempting file upload...");
                        await UploadFileRESTAsync(localFilePath, serverFilePath);
                    }
                    else if (directoryDetected && Directory.Exists(localFilePath))
                    {
                        //Debug.Log("Attempting directory upload...");

                        await UploadFolderRESTAsync(localFilePath, serverFilePath);
                    }


                }
            });

        }

        /// <summary>
        /// Uploads a folder using REST API
        /// </summary>
        /// <param name="localFilePath"></param>
        /// <param name="serverFilePath"></param>
        /// <returns></returns>
        private async Task UploadFolderRESTAsync(string localFilePath, string serverFilePath)
        {
            await Task.Run(async () =>
            {
                // First, find all the folders
                // Iterate to upload all files in folder, including subdirectories
                string[] files = Directory.GetFiles(localFilePath, "*", SearchOption.AllDirectories);
                await files.ParallelForEachAsync(async file =>
                {
                    if (Path.GetExtension(file) != ".meta")
                    {
                        string folderName = new DirectoryInfo(@Path.GetDirectoryName(file)).Name + "/";
                        string auxServerFilePath = Path.Combine(serverFilePath, folderName);
                        await UploadFileRESTAsync(file, auxServerFilePath);
                    }
                },
                maxDegreeOfParallelism: 5);

            });

        }

        /// <summary>
        /// Uploads a single file using REST API (all platforms, slower)
        /// </summary>
        /// <param name="fileToUpload"></param>
        /// <param name="serverFilePath"></param>
        /// <returns></returns>
        private IEnumerator UploadFileREST(string fileToUpload, string serverFilePath, UploadRESTOptions options = UploadRESTOptions.DotNetWebRequest)
        {

            string fileName = Path.GetFileName(fileToUpload);

            // Encoding urls (it seems that unitywebrequest doesn't work)
            //string fileNameEscaped = System.Web.HttpUtility.UrlEncode(fileName);
            //string serverFilePathEscaped = System.Web.HttpUtility.UrlEncode(serverFilePath);
            string fileNameEscaped = UnityWebRequest.EscapeURL(fileName);
            string serverFilePathEscaped = UnityWebRequest.EscapeURL(serverFilePath);

            byte[] fileBinary = File.ReadAllBytes(fileToUpload);

            // HTTP
            string firebaseProjectID = m_ServerGlobalSubDirectory;
            string urlFirebase = m_ServerURL +
                firebaseProjectID + "/o/" + serverFilePathEscaped + fileNameEscaped;
            string contentType = m_ContentType;

            // wait a frame
            yield return null;

            // choose implementation
            switch (options)
            {
                case UploadRESTOptions.DotNetWebRequest:
                    // C# WEBREQUEST
                    WebRequest request = WebRequest.Create(urlFirebase);
                    request.Method = "POST";
                    request.ContentLength = fileBinary.Length;
                    request.ContentType = contentType;
                    request.Proxy = null; // this is known to speed up requests
                    Stream dataStream = request.GetRequestStream();
                    dataStream.Write(fileBinary, 0, fileBinary.Length);
                    dataStream.Close();
                    // wait a frame
                    yield return null;
                    // Send request to server
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    // wait a frame
                    yield return null;
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        Debug.LogError($"Upload failed! {response.StatusDescription}");
                    }
                    else
                        Debug.Log($"Upload succeeded! {response.StatusDescription}");
                    // releases resources of response
                    response.Close();


                    break;


                case UploadRESTOptions.UnityWebRequest:
                    // UNITYWEBREQUEST 
                    // Post file to server
                    // Define a filled in upload handler
                    UploadHandlerRaw uploadHandler = new UploadHandlerRaw(fileBinary);
                    uploadHandler.contentType = contentType;
                    // Empty downloadHandler since we are only posting
                    DownloadHandlerBuffer downloadHandlerBuffer = new DownloadHandlerBuffer();
                    // Prepare post request with data
                    UnityWebRequest webRequest = new UnityWebRequest(urlFirebase, "POST", downloadHandlerBuffer, uploadHandler);
                    //UnityWebRequest webRequest = UnityWebRequest.Post(urlFirebase, fileText);
                    //webRequest.SetRequestHeader("Content-type", "text/plain");
                    // Send
                    yield return webRequest.SendWebRequest();
                    // errors?
                    if (webRequest.result != UnityWebRequest.Result.Success)
                        Debug.LogError(webRequest.error);
                    break;

                case UploadRESTOptions.WWW:
                    // WWW
                    Dictionary<string, string> wwwHeaders = new Dictionary<string, string>();
                    wwwHeaders.Add("Content-type", contentType);
                    WWW www = new WWW(urlFirebase, fileBinary, wwwHeaders);
                    yield return www;
                    // Any errors?
                    if (www.error != null)
                        Debug.LogError(www.error);
                    // wait a frame
                    yield return null;
                    break;
                default:
                    break;
            }

            // wait a frame
            yield return null;

        }

        /// <summary>
        /// Uploads a single file using REST API (all platforms, slower)
        /// </summary>
        /// <param name="fileToUpload"></param>
        /// <param name="serverFilePath"></param>
        /// <returns></returns>
        private IEnumerator UploadFileREST(byte[] fileToUpload, string fileName, string serverFilePath)
        {

            //string fileName = "testfile.json";
            //string fileToUpload = Path.Combine(IMLDataSerialization.GetAssetsPath(), fileName);
            //string fileText = File.ReadAllText(fileToUpload);

            string fileNameEscaped = Uri.EscapeUriString(fileName);
            string serverFilePathEscaped = Uri.EscapeUriString(serverFilePath);
            byte[] fileBinary = fileToUpload;

            // HTTP
            string firebaseProjectID = m_ServerGlobalSubDirectory;
            string urlFirebase = m_ServerURL +
                firebaseProjectID + "/o/" + serverFilePathEscaped + fileNameEscaped;

            // UNITYWEBREQUEST 
            // Post file to server
            // Define a filled in upload handler
            UploadHandlerRaw uploadHandler = new UploadHandlerRaw(fileBinary);
            uploadHandler.contentType = m_ContentType;
            // Empty downloadHandler since we are only posting
            DownloadHandlerBuffer downloadHandlerBuffer = new DownloadHandlerBuffer();
            // Prepare post request with data
            UnityWebRequest webRequest = new UnityWebRequest(urlFirebase, "POST", downloadHandlerBuffer, uploadHandler);
            //UnityWebRequest webRequest = UnityWebRequest.Post(urlFirebase, fileText);
            //webRequest.SetRequestHeader("Content-type", "text/plain");
            // Send
            yield return webRequest.SendWebRequest();

            //// WWW
            //Dictionary<string, string> wwwHeaders = new Dictionary<string, string>();
            //wwwHeaders.Add("Content-type", "application/force-download");
            //WWW www = new WWW(urlFirebase, fileBinary, wwwHeaders);
            //yield return www;

            //// wait a frame
            //yield return null;

            // UNITYWEBREQUEST
            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(webRequest.error);
            }

            //// WWW
            //if (www.error == null)
            //{
            //    Debug.Log("Upload of test file complete!");

            //}
            //else
            //{
            //    Debug.LogError(www.error);
            //}


            // wait a frame
            yield return null;

        }

        /// <summary>
        /// Runs the async task in a background thread pool (as per https://docs.microsoft.com/en-us/archive/msdn-magazine/2015/july/async-programming-brownfield-async-development)
        /// </summary>
        /// <param name="fileToUpload"></param>
        /// <param name="serverFilePath"></param>
        private async Task UploadFileRESTAsync(string fileToUpload, string serverFilePath)
        {
            // Runs the async task in a background thread pool (as per https://docs.microsoft.com/en-us/archive/msdn-magazine/2015/july/async-programming-brownfield-async-development)
            await UploadFileRESTDotNetWebRequestAsync(fileToUpload, serverFilePath);

        }

        /// <summary>
        /// Uploads a single file using REST API (all platforms, slower)
        /// </summary>
        /// <param name="fileToUpload"></param>
        /// <param name="serverFilePath"></param>
        /// <returns></returns>
        private async Task UploadFileRESTDotNetWebRequestAsync(string fileToUpload, string serverFilePath, bool debug = true)
        {
            await Task.Run(async () =>
            {
                //Debug.Log("Attempting file upload...");
                string fileName = Path.GetFileName(fileToUpload);
                //string fileNameEscaped = System.Web.HttpUtility.UrlEncode(fileName); // THIS CAUSES THE SUDDENT STOP OF THE TASK ON OCULUS QUEST!! NEVER USE!!
                //string serverFilePathEscaped = System.Web.HttpUtility.UrlEncode(serverFilePath);
                string fileNameEscaped = UnityWebRequest.EscapeURL(fileName);
                string serverFilePathEscaped = UnityWebRequest.EscapeURL(serverFilePath);
                //Debug.Log("Name and url escaped for server upload");
                //byte[] fileBinary = File.ReadAllBytes(fileToUpload);
                byte[] fileBinary;

                //Debug.Log("Reading file...");
                // Async binary read 
                using (FileStream SourceStream = File.Open(fileToUpload, FileMode.Open))
                {
                    fileBinary = new byte[SourceStream.Length];
                    await SourceStream.ReadAsync(fileBinary, 0, (int)SourceStream.Length);
                    SourceStream.Close();
                }
                //Debug.Log("File read from disk, ready to upload...");

                // HTTP
                string firebaseProjectID = m_ServerGlobalSubDirectory;
                string urlFirebase = m_ServerURL +
                    firebaseProjectID + "/o/" + serverFilePathEscaped + fileNameEscaped;
                string contentType = m_ContentType;

                // C# HTTPCLIENT
                //string jsonResponse;
                //using (var client = new HttpClient())
                //{
                //    HttpResponseMessage response = await client.PostAsync(urlFirebase, new ByteArrayContent(fileBinary));
                //    response.EnsureSuccessStatusCode();
                //    jsonResponse = await response.Content.ReadAsStringAsync(); // async sending as per https://stackoverflow.com/questions/65329127/unity-c-await-freeze-sync-process-which-should-be-executed-before-await-in-as
                //    Debug.Log(jsonResponse);
                //}

                // C# WEBREQUEST async ---> I never got it to work without freezing the main thread
                WebRequest request = WebRequest.CreateHttp(urlFirebase);
                request.Method = "POST";
                request.ContentLength = fileBinary.Length;
                request.ContentType = contentType;
                request.Proxy = null; // this is known to speed up requests
                Stream dataStream = await request.GetRequestStreamAsync();
                await dataStream.WriteAsync(fileBinary, 0, fileBinary.Length);
                dataStream.Close();
                //Debug.Log("Uploading...");
                // Send request to server
                // We need to use task factory since "getresponseasync" is actually synchronous (as per https://stackoverflow.com/questions/65329127/unity-c-await-freeze-sync-process-which-should-be-executed-before-await-in-as)
                await Task.Factory.FromAsync(request.BeginGetResponse,
                    request.EndGetResponse, null).ContinueWith(task =>
                    {
                        var response = (HttpWebResponse)task.Result;

                        if (debug)
                        {
                            if (response.StatusCode != HttpStatusCode.OK)
                                Debug.LogError($"Upload failed! {response.StatusDescription}");
                            else
                                Debug.Log($"Upload successful! {response.StatusDescription}");


                        }

                    // releases resources of response
                    response.Close();

                    });

            });

        }

        #endregion
    }
}
