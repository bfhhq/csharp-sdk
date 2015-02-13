using System;
using System.Net;
using System.Collections.Generic;
using System.Text;

namespace Baofeng.Cloud {
    
 public class Delete {

     public static void DeleteFile(Profile profile, Baofeng.Cloud.ServiceType serviceType,
             String fileName, String fileKey, String callbackUrl) {

         String token = Token.CreateDeleteToken(profile.accessKey,
             profile.secretKey,
             serviceType,
             fileName,
             fileKey,
             Const.TokenTimeoutSec,
             callbackUrl);

         DeleteFile(token);
     }

     public static void DeleteFile(String token) {

         try {

             String json = "{\"token\":\"" + token + "\"}";

             var client = new System.Net.WebClient();
             client.Headers.Add("Content-Type", "application/json");
             var resp = client.UploadData(Const.DeleteRequestUrl, "POST",
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
