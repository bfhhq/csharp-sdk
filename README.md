# Baofeng Cloud SDK for C# 

安装
``` bash
git clone https://github.com/baofengcloud/sharp-sdk

build
```
使用
``` cs
using Baofeng.Cloud;
```
配置AK/SK
``` cs
Profile profile = new Profile();
profile.accessKey = "";
profile.secretKey = "";
```
上传
``` cs
Upload.UploadFile(profile, ServiceType.Paas, FileType.Public, "C:\\test.mp4", "test.mp4", "", "")
```
查询
``` cs
Query.QueryFile(profile, ServiceType.Paas, "test.mp4", "")
```
删除
``` cs
Delete.DeleteFile(profile, ServiceType.Paas, "test.mp4", "", "")
```

# 关于

此 C# SDK 适用于 .NET 2.0 及以上版本，基于 [暴风云视频API](http://www.baofengcloud.com/apisdk/doc.html) 构建。
