using System;
using System.Collections.Generic;
using System.Text;

namespace Baofeng.Cloud {
class SimpleJsonParser {
       public static Dictionary<string, string> Parse(String json) {

           Dictionary<string, string> result = new Dictionary<string, string>();

           json = json.Replace("{", "");
           json = json.Replace("}", "");

           foreach( var pair in json.Split(',')){

               string[] kv = pair.Split(new char[]{':'}, 2);
               if (kv.Length != 2)
                   continue;

               var k = kv[0].Replace("\"", "");
               var v = kv[1].Replace("\"", "");
               v = v.Replace(@"\/", @"/");

               result.Add(k, v);
           }
           
           return result;
        }
    }
}
