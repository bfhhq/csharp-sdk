using System;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Security.Cryptography;
using System.Reflection;
using System.Threading;

namespace Baofeng.Cloud {

    public class Upload2 : IDisposable {

        public class UploadProgressChangedEventArgs {
            public Int64 BytesSent;
            public Int64 TotalBytesToSend;
        }
        public delegate void UploadProgressChangedEventHandler(object sender, UploadProgressChangedEventArgs e);

        public class UploadCompletedEventArgs {
            public bool Cancelled;
            public Exception Error;
        }
        public delegate void UploadCompletedEventHandler(object sender, UploadCompletedEventArgs e);


         class Range {
            public Int64 pos;
            public Int64 end;

            public Range(Int64 pos, Int64 end) {
                this.pos = pos;
                this.end = end;
            }
            
            public void Inc(Int64 size) {
                this.pos += size;
            }

            public Int64 Len() {
                return end - pos;
            }
        }

        class UploadInfo{
            public WebClient wc;
            public Int64 uploadedBytes;
            public Int64 totalBytes;
        }

        private String localFilePath;
        private Int64 fileSize;
        private String uploadUrl;
        private List<Range> uploadRanges;
        private Dictionary<WebClient, UploadInfo> uploadInfos;
        private FileStream fs;
        private Int64 uploadedBytes;
        private bool cancelled;
        private Exception lastError;
        private Object infoLock = new object();

        public event UploadProgressChangedEventHandler UploadProgressChanged;
        public event UploadCompletedEventHandler UploadCompleted;
        private ManualResetEvent uploadCompletedEvent = new ManualResetEvent(true);

        public void UploadFileAsync(Profile profile, Baofeng.Cloud.ServiceType serviceType,
                    Baofeng.Cloud.FileType fileType, String localFilePath, String fileName, String fileKey, String callbackUrl) {

            Int64 fileSize = new System.IO.FileInfo(localFilePath).Length;

            String token = Token.CreateUploadToken(profile.accessKey,
                profile.secretKey,
                serviceType,
                fileType,
                UploadType.Partial,
                fileName,
                fileKey,
                fileSize,
                Const.TokenTimeoutSec,
                callbackUrl
                );

            UploadFileAsync(token, localFilePath, fileSize);
        }

        public void UploadFileAsync(String token, String localFilePath, Int64 fileSize) {

            this.localFilePath = localFilePath;
            this.fileSize = fileSize;

            fs = File.OpenRead(localFilePath);

            String uploadReqJson = "{\"token\":\"" + token + "\"}";
            this.uploadUrl = Upload.RequestUploadUrl(uploadReqJson);

            RequestUploadedRanges();

            StartUpload();
        }

        public void Cancel() {
            
            lock (infoLock) {
                if (uploadInfos != null) {
                    foreach (var kv in uploadInfos) {
                        kv.Key.CancelAsync();
                    }
                }
            }
        }

        public void Dispose() {

            Cancel();
            uploadCompletedEvent.WaitOne();

            if (fs != null) {
                fs.Close();
                fs = null;
            }
        }

        private void RequestUploadedRanges() {

            uploadRanges = new List<Range>();

            string rangeStr = "";

            try {
                var client = new System.Net.WebClient();
                client.Headers.Add("Content-Type", "application/json");
                client.Headers.Add("Content-Range", "bytes */" + fileSize.ToString());
                client.UploadData(uploadUrl, "POST", new byte[0]);
            } catch (System.Net.WebException ex) {
                var webResp = (System.Net.HttpWebResponse)ex.Response;

                if ((int)webResp.StatusCode != 308)
                    throw ex;

                rangeStr = webResp.GetResponseHeader("Range");
            }

            List<Range> havedRanges = new List<Range>();
        
            foreach (var line in rangeStr.Split(',')) {
                var r = line.Trim();

                string[] rr = r.Split('-');
                if(rr.Length < 2)
                    continue;

                Int64 pos = Int64.Parse(rr[0]);
                Int64 end = rr[1].Length > 0 ? Int64.Parse(rr[1]) + 1 : fileSize;

                havedRanges.Add(new Range(pos, end));
                uploadedBytes += end - pos;
            }

            for (int i = 0; i < havedRanges.Count - 1; i++) {
                uploadRanges.Add(new Range(havedRanges[i].end, havedRanges[i + 1].pos));
            }

            if (havedRanges.Count > 0 && havedRanges[havedRanges.Count - 1].end < fileSize)
                uploadRanges.Add(new Range(havedRanges[havedRanges.Count - 1].end, fileSize));

            if (havedRanges.Count == 0)
                uploadRanges.Add(new Range(0, fileSize));
        }

        private void StartUpload() {

            uploadInfos = new Dictionary<WebClient, UploadInfo>();
            uploadCompletedEvent.Reset();

            while (UploadNextRange()) { };
        }

        private bool UploadNextRange() {

            lock (infoLock) {

                if (this.uploadInfos.Count >= Const.UploadConnectionNum)
                    return false;

                Range r = GetNextUploadRange();
                if (r.Len() == 0)
                    return false;

                return UploadRange(r);

            }
        }

        private Range GetNextUploadRange() {

            lock (infoLock) {

                var iter = uploadRanges.GetEnumerator();
                while (iter.MoveNext()) {
                    Range r = iter.Current;
                    if (r.Len() == 0)
                        continue;

                    Int64 len = Const.UploadBlockSize;
                    if (len > r.Len())
                        len = r.Len();

                    var result = new Range(r.pos, r.pos + len);

                    r.Inc(len);

                    return result;
                }

            }

            return new Range(0, 0);

        }

        private bool UploadRange(Range r) {

            System.Net.WebClient client = new System.Net.WebClient();
            client.Headers.Add("Content-Range", 
                    "bytes " + r.pos.ToString() + "-" + (r.end - 1).ToString() + "/" + fileSize.ToString());

            byte[] data = new byte[r.Len()];
            fs.Seek(r.pos, SeekOrigin.Begin);
            int len = fs.Read(data, 0, (int)r.Len());
            if (len != r.Len())
                throw new IOException("unable read expected size.");
            
            client.Headers.Add("Content-MD5", CalcMD5Hex(data));
            

            client.UploadProgressChanged += new System.Net.UploadProgressChangedEventHandler(OnRangeUploadProgressChanged);
            client.UploadDataCompleted += new System.Net.UploadDataCompletedEventHandler(OnRangeUploadDataCompleted);

            UploadInfo info = new UploadInfo();
            info.wc = client;
            info.totalBytes = len;
            info.uploadedBytes = 0;
            uploadInfos[client] = info;

            client.UploadDataAsync(new Uri(uploadUrl), "POST", data, client);

            return true;
        }

        private void OnRangeUploadProgressChanged(Object sender, System.Net.UploadProgressChangedEventArgs e) {

            lock (infoLock) {

                if (!uploadInfos.ContainsKey((WebClient)sender))
                    return;

                uploadInfos[(WebClient)sender].uploadedBytes = e.BytesSent;

                Int64 bytesSent = this.uploadedBytes;
                foreach (var kv in uploadInfos) {
                    bytesSent += kv.Value.uploadedBytes;
                }

                UploadProgressChangedEventArgs ee = new UploadProgressChangedEventArgs();
                ee.BytesSent = bytesSent;
                ee.TotalBytesToSend = fileSize;

                UploadProgressChanged(this, ee);
            }

        }

        private void OnRangeUploadDataCompleted(Object sender, System.Net.UploadDataCompletedEventArgs e) {

            lock (infoLock) {

                WebClient client = (WebClient)sender;

                this.uploadedBytes += uploadInfos[client].totalBytes;

                uploadInfos.Remove(client);

                if (e.Cancelled || e.Error != null) {

                    if (e.Cancelled)
                        cancelled = true;

                    if (e.Error != null)
                        lastError = e.Error;

                } else {
                    int statusCode = GetStatusCode(client);

                    if (statusCode == 206) {
                        UploadNextRange();
                    } else if (statusCode == 200) {
                    }
                }

                if (uploadInfos.Count == 0) {
                    UploadCompletedEventArgs ee = new UploadCompletedEventArgs();
                    ee.Cancelled = cancelled;
                    ee.Error = lastError;

                    uploadCompletedEvent.Set();

                    UploadCompleted(this, ee);
                }

                client.Dispose();
            }
        }

        private string CalcMD5Hex(byte[] data) {
            using (var md5 = MD5.Create()) {
                var result = md5.ComputeHash(data);
                    return BitConverter.ToString(result).Replace("-", string.Empty);
            }
        }

        private static int GetStatusCode(WebClient client) {
            FieldInfo responseField = client.GetType().GetField("m_WebResponse", BindingFlags.Instance | BindingFlags.NonPublic);

            if (responseField != null) {
                HttpWebResponse response = responseField.GetValue(client) as HttpWebResponse;

                if (response != null) {
                    return (int)response.StatusCode;
                }
            }

            return 0;
        }

}
}