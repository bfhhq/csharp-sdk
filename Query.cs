using System;
using System.Net;
using System.Collections.Generic;
using System.Text;
using System.Web;

namespace Baofeng.Cloud {

    public class FileInfo {
        public int status;
        public string fileName;
        public string fileKey;
        public Int64 fileSize;
        public Int64 duration;
        public ServiceType serviceType;
        public FileType fileType;
        public string url;
    }

public    class Query {
        public static FileInfo QueryFile(Profile profile, Baofeng.Cloud.ServiceType serviceType,
                String fileName, String fileKey) {

            String token = Token.CreateQueryToken(profile.accessKey,
                profile.secretKey,
                serviceType,
                fileName,
                fileKey
                );

            return QueryFile(token);
        }

        public static FileInfo QueryFile(String token) {

            try {

                String json = "{\"token\":\"" + token + "\"}";

                var client = new System.Net.WebClient();
                client.Headers.Add("Content-Type", "application/json");
                var resp = client.UploadData(Const.QueryRequestUrl, "POST",
                            System.Text.Encoding.UTF8.GetBytes(json));

                var respStr = System.Text.Encoding.UTF8.GetString(resp);

                var result = SimpleJsonParser.Parse(respStr); ;
                                
                int status = 99;
                if (result.ContainsKey("status")) {
                    status = int.Parse(result["status"]);                    
                }

                //    {”status”:0,”filename”:”abc.mp4”,”filekey”:”tom”, ”filesize”:1324, ”duration”:100,”servicetype”:1,”ifpublic”:0, ”url”:”servicetype=1&uid=1&fid=adsfasdfasfasfd”}

                FileInfo info = new FileInfo();
                info.status = status;
                info.fileName = result.ContainsKey("filename") ? result["filename"] : "";
                info.fileKey = result.ContainsKey("filekey") ? result["filekey"] : "";
                info.fileSize = result.ContainsKey("filesize") ? Int64.Parse(result["filesize"])  : 0;
                info.duration = result.ContainsKey("duration") ? Int64.Parse(result["duration"]) : 0;
                info.serviceType = result.ContainsKey("servicetype") ? (ServiceType)int.Parse(result["servicetype"]) : ServiceType.Paas;
                info.fileType = result.ContainsKey("ifpublic") ? (FileType)int.Parse(result["ifpublic"]) : FileType.Private;
                info.url = result.ContainsKey("url") ? result["url"] : "";

                return info;

            } catch (WebException e) {
                throw new CloudException(99, e.ToString());
            }
        }

        public static String GetSwfPlayUrl(Profile profile, String fileName, String fileKey, Int64 timeoutSec, String url, FileType fileType, String swfUrl)
        {

            String fid = "";
            foreach (var line in url.Split('&'))
            {
                var kv = line.Split('=');
                if (kv[0] == "fid")
                    fid = kv[1];
            }

            if (fid.Length == 0)
                throw new CloudException(99, "fid not found in url!");

            string playToken = Token.CreatePlayTokenByFid(profile.accessKey, profile.secretKey, fid, timeoutSec);

            string encodeUrl = HttpUtility.UrlEncode(url);

            String playUrl = swfUrl.Length > 0 ? swfUrl : Const.SwfUrl + "?vk=" + encodeUrl;
            
            if(fileType == FileType.Private)
                playUrl += "&tk=" + HttpUtility.UrlEncode(playToken);

            return playUrl;
        }

    }
}
