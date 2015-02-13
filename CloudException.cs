using System;
using System.Collections.Generic;
using System.Text;

namespace Baofeng.Cloud {

    public class CloudException : Exception {

        int status;
        String errmsg;

        public CloudException(int status, string errmsg) : base(errmsg){
            this.status = status;
            this.errmsg = errmsg;
        }

        public int getStatus() {
            return status;
        }

        override public String ToString() {
            return "status:" + this.status.ToString() + " errmsg: " + errmsg;
        }
    }

}
