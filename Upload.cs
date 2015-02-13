using System;
using System.Net;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace Baofeng.Cloud {

public class AsyncUploadHandle {

    public event UploadProgressChangedEventHandler UploadProgressChanged;
    public event UploadFileCompletedEventHandler UploadFileCompleted;

    public AsyncUploadHandle() {}

    public AsyncUploadHandle(WebClient wc){
        this.wc = wc;

        wc.UploadProgressChanged +=  new UploadProgressChangedEventHandler(delegate(Object sender, UploadProgressChangedEventArgs e) {
                this.UploadProgressChanged(sender, e);
        });

        wc.UploadFileCompleted += new UploadFileCompletedEventHandler(delegate(Object sender, UploadFileCompletedEventArgs e) {
            this.UploadFileCompleted(sender, e);
        });
    }

    public void Cancel() {
        wc.CancelAsync();
    }

    private WebClient wc;
}


public  class Upload {

    public static AsyncUploadHandle UploadFileAsync(Profile profile, Baofeng.Cloud.ServiceType serviceType,
                Baofeng.Cloud.FileType fileType, String localFilePath, String fileName, String fileKey, String callbackUrl) {

        return UploadFileInternal(profile, serviceType, fileType, localFilePath, fileName, fileKey, callbackUrl, true);
    }

public static void UploadFile(Profile profile, Baofeng.Cloud.ServiceType serviceType,
            Baofeng.Cloud.FileType fileType, String localFilePath, String fileName, String fileKey, String callbackUrl) {
               UploadFileInternal(profile, serviceType, fileType, localFilePath, fileName, fileKey, callbackUrl, false);
}

protected static AsyncUploadHandle UploadFileInternal(Profile profile, Baofeng.Cloud.ServiceType serviceType,
            Baofeng.Cloud.FileType fileType, String localFilePath, String fileName, String fileKey, String callbackUrl, bool isAsync) {
            
            Int64 fileSize = new System.IO.FileInfo(localFilePath).Length;

            String token = Token.CreateUploadToken(profile.accessKey, 
                profile.secretKey, 
                serviceType, 
                fileType, 
                UploadType.Full,
                fileName,
                fileKey,
                fileSize,
                Const.TokenTimeoutSec,
                callbackUrl
                );

            if(isAsync){
                return UploadFileAsync(token, localFilePath);
            }
            else{
                UploadFile(token, localFilePath);
                return null;
            }
        }

public static AsyncUploadHandle UploadFileAsync(String token, String localFilePath) {
    return new AsyncUploadHandle(UploadFileInternal(token, localFilePath, true));
}

public static void UploadFile(String token, String localFilePath) {
    UploadFileInternal(token, localFilePath, false);
}

protected static WebClient UploadFileInternal(String token, String localFilePath, bool isAync) {

    try {

        String uploadReqJson = "{\"token\":\"" + token + "\"}";
        String uploadUrl = RequestUploadUrl(uploadReqJson);

        Int64 fileSize = new System.IO.FileInfo(localFilePath).Length;

        var client = new System.Net.WebClient();
        client.Headers.Add("Content-Range", "bytes 0-" + (fileSize - 1).ToString() + "/" + fileSize.ToString());
        //client.Headers.Add("Content-Type", "application/octet-stream");
        client.Headers.Add("Content-MD5", GetFileMD5Hex(localFilePath));

        if (isAync)
            client.UploadFileAsync(new Uri(uploadUrl), localFilePath);
        else
            client.UploadFile(new Uri(uploadUrl), localFilePath);

        return client;

    } catch (WebException e) {
        throw new CloudException(99, e.ToString());
    }
}

static String GetFileMD5Hex(String localFilePath) {
    using (var md5 = MD5.Create()) {
        using (var stream = File.OpenRead(localFilePath)) {
            var result = md5.ComputeHash(stream);
            return BitConverter.ToString(result).Replace("-", string.Empty);
        }
    }
}

public static String RequestUploadUrl(String json) {
    var client = new System.Net.WebClient();
    client.Headers.Add("Content-Type", "application/json");
    var resp = client.UploadData(Const.UploadRequestUrl, "POST",
                System.Text.Encoding.UTF8.GetBytes(json));

    var respStr = System.Text.Encoding.UTF8.GetString(resp);

    var result = SimpleJsonParser.Parse(respStr); ;

    int status = 99;
    String errMsg = "";

    if (result.ContainsKey("status")) {
        status = int.Parse(result["status"]);
        if (status == 0) {
            return result["url"];
        }
    }

    if (result.ContainsKey("errmsg")) {
        errMsg = result["errmsg"];
            }

            throw new CloudException(status, errMsg);
    }

    }
}
