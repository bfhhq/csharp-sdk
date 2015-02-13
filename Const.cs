using System;
using System.Collections.Generic;
using System.Text;

namespace Baofeng.Cloud {
    public enum ServiceType {
        Paas = 0,
        Saas = 1
    }

    public enum FileType {
        Private = 0,
        Public = 1
    }

    public enum UploadType {
        Full = 0,
        Partial = 1
    }

    class Const {
        public static int TokenTimeoutSec = 3600;
        public static int UploadBlockSize = 4 * 1024 * 1024;
        public static int UploadConnectionNum = 3;
        public static string UploadRequestUrl = @"http://access.baofengcloud.com/upload";
        public static string DeleteRequestUrl = @"http://access.baofengcloud.com/delete";
        public static string QueryRequestUrl = @"http://access.baofengcloud.com/query";
        public static string UpdateRequestUrl = @"http://access.baofengcloud.com/change";
        public static string SwfUrl = @"http://www.baofengcloud.com/html/swf/player/cloud.swf";
    }
}
