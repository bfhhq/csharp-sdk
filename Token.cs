using System.Collections.Generic;
using System;
using System.Security.Cryptography;

namespace Baofeng.Cloud {

    public class Token {

        public static String CreateUploadToken(String accessKey, String secretKey, ServiceType serviceType, FileType fileType, UploadType uploadType, string fileName, string fileKey, Int64 fileSize, Int64 timeoutSec, string callbackUrl) {
            Dictionary<string, object> jsonObj = new Dictionary<string, object>();

            jsonObj.Add("uptype", (int)uploadType);
            jsonObj.Add("servicetype", (int)serviceType);
            jsonObj.Add("filekey", fileKey);
            jsonObj.Add("filename", fileName);
            jsonObj.Add("filesize", fileSize);
            jsonObj.Add("filetype", (int)fileType);
            jsonObj.Add("callbackurl", callbackUrl);

            return CreateToken(jsonObj, secretKey, accessKey, timeoutSec);
        }

        public static String CreateDeleteToken(String accessKey, String secretKey, ServiceType serviceType, string fileName, string fileKey, Int64 timeoutSec, string callbackUrl) {
            Dictionary<string, object> jsonObj = new Dictionary<string, object>();

            jsonObj.Add("servicetype", (int)serviceType);
            jsonObj.Add("filekey", fileKey);
            jsonObj.Add("filename", fileName);
            jsonObj.Add("callbackurl", callbackUrl);

            return CreateToken(jsonObj, secretKey, accessKey, timeoutSec);
        }

        public static String CreateQueryToken(String accessKey, String secretKey, ServiceType serviceType, string fileName, string fileKey) {
            Dictionary<string, object> jsonObj = new Dictionary<string, object>();

            jsonObj.Add("servicetype", (int)serviceType);
            jsonObj.Add("filekey", fileKey);
            jsonObj.Add("filename", fileName);

            return CreateToken(jsonObj, secretKey, accessKey, /*timeoutSec*/0);
        }

        public static String CreateUpdateToken(String accessKey, String secretKey, ServiceType serviceType, FileType fileType, string fileName, string fileKey) {
            Dictionary<string, object> jsonObj = new Dictionary<string, object>();

            jsonObj.Add("servicetype", (int)serviceType);
            jsonObj.Add("filekey", fileKey);
            jsonObj.Add("filename", fileName);
            jsonObj.Add("filetype", (int)fileType);

            return CreateToken(jsonObj, secretKey, accessKey, /*timeoutSec*/0);
        }

        public static String CreatePlayTokenByUrl(String accessKey, String secretKey, String playUrl, Int64 timeoutSec)
        {
            String fid = "";
            foreach (var line in playUrl.Split('&'))
            {
                var kv = line.Split('=');
                if (kv[0] == "fid")
                    fid = kv[1];
            }

            if (fid.Length == 0)
                throw new CloudException(99, "fid not found in url!");

            return CreatePlayTokenByFid(accessKey, secretKey, fid, timeoutSec);
        }

        public static String CreatePlayTokenByFid(String accessKey, String secretKey, string fid, Int64 timeoutSec) {

            Int64 unixTime = (Int64)((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds);

            string url = "id=" + fid + "&deadline=" + (unixTime + timeoutSec);
            String encodedUrl = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(url));
            byte[] sign = Sign(secretKey, encodedUrl);
            String encodedSign = Convert.ToBase64String(sign);

            String token = accessKey + ":" + encodedSign + ":" + encodedUrl;

            return token;
        }

        public static String CreatePlayToken(String accessKey, String secretKey, string fid, Int64 timeoutSec)
        {
            return CreatePlayTokenByFid(accessKey, secretKey, fid, timeoutSec);
        }

        private static String CreateToken(Dictionary<string, object> jsonObj, String secretKey, String accessKey, Int64 timeoutSec) {

            if (timeoutSec > 0) {
                Int64 unixTime = (Int64)((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds);
                jsonObj.Add("deadline", unixTime + timeoutSec);
            }

            String json = Dict2Json(jsonObj);
            String encodedJson = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));
            byte[] sign = Sign(secretKey, encodedJson);
            String encodedSign = Convert.ToBase64String(sign);

            String token = accessKey + ":" + encodedSign + ":" + encodedJson;

            return token;
        }

        static string Dict2Json(Dictionary<string, object> dict) {

            String json = "{";

            foreach (KeyValuePair<string, object> pair in dict) {
                if (json.Length > 1)
                    json += ",";

                if (pair.Value.GetType() == typeof(string))
                    json += "\"" + pair.Key + "\":\"" + pair.Value + "\"";
                else if (pair.Value.GetType() == typeof(int) || pair.Value.GetType() == typeof(Int64))
                    json += "\"" + pair.Key + "\":" + pair.Value.ToString();
                else {
                    throw new Exception("unsupported type.");
                }

            }

            json += "}";

            return json;
        }

        static Byte[] Sign(String secretKey, String text) {
            HMACSHA1 hashFunc = new HMACSHA1(System.Text.Encoding.ASCII.GetBytes(secretKey));
            Byte[] hashResult = hashFunc.ComputeHash(System.Text.Encoding.ASCII.GetBytes(text));

            return hashResult;
        }

    }//class Token
}