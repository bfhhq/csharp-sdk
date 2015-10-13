using System;
using System.Net;
using System.Collections.Generic;
using System.Text;

namespace Baofeng.Cloud {

    public class LiveChannelInfo {
        public string channelID;
        public string gcid;
        public string url;
    }

    public class Live {
        public static LiveChannelInfo CreateChannel(Profile profile, FileType fileType, string channelName) {

            String token = Token.LiveCreateToken(profile.accessKey,
                profile.secretKey,
                fileType,
                channelName,
                Const.TokenTimeoutSec);

            return CreateChannel(token);
        }

        public static LiveChannelInfo CreateChannel(String token) {

            LiveChannelInfo info = new LiveChannelInfo();

            try {

                String json = "{\"token\":\"" + token + "\"}";

                var client = new System.Net.WebClient();
                client.Headers.Add("Content-Type", "application/json");
                var resp = client.UploadData(Const.LiveCreateUrl, "POST",
                            System.Text.Encoding.UTF8.GetBytes(json));

                var respStr = System.Text.Encoding.UTF8.GetString(resp);

                var result = SimpleJsonParser.Parse(respStr); ;

                int status = 99;

                if (result.ContainsKey("status")) {
                    status = int.Parse(result["status"]);
                    if (status == 0) {

                        info.channelID = result["channelid"];
                        info.gcid = result["gcid"];
                        info.url = result["url"];

                        return info;
                    }
                }

                throw new CloudException(status, "");

            } catch (WebException e) {
                throw new CloudException(99, e.ToString());
            }
        }
    
        public static void DeleteChannel(Profile profile, string channelID) {

            String token = Token.LiveDeleteToken(profile.accessKey,
                profile.secretKey,
                channelID,
                Const.TokenTimeoutSec);

            DeleteChannel(token);
        }

        public static void DeleteChannel(String token) {

        try {

                String json = "{\"token\":\"" + token + "\"}";

                var client = new System.Net.WebClient();
                client.Headers.Add("Content-Type", "application/json");
                var resp = client.UploadData(Const.LiveDeleteUrl, "POST",
                            System.Text.Encoding.UTF8.GetBytes(json));

                var respStr = System.Text.Encoding.UTF8.GetString(resp);

                var result = SimpleJsonParser.Parse(respStr); ;

                int status = 99;

                if (result.ContainsKey("status")) {
                    status = int.Parse(result["status"]);
                    if (status == 0) {
                        return;
                    }
                }

                throw new CloudException(status, "");

            } catch (WebException e) {
                throw new CloudException(99, e.ToString());
            }
        }
    

    }


}
