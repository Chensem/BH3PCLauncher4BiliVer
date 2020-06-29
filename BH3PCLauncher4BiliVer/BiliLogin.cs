using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json.Linq;

namespace BH3PCLauncher4BiliVer
{
    class LoginUtils
    {
        /*
         * bflag 13位时间戳
         */
        public static string GetUnix(bool bflag = false)
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            string ret = string.Empty;
            if (bflag)
                ret = Convert.ToInt64(ts.TotalSeconds).ToString();
            else
                ret = Convert.ToInt64(ts.TotalMilliseconds).ToString();
            return ret;
        }
        public class miHoYo
        {
            public static async Task<JObject> PostRequestAsync(string url, string data)
            {
                WebClient thisClient = new WebClient
                {
                    Headers = new WebHeaderCollection
                    {
                        "User-Agent:okhttp/3.10.0",
                        "Content-Type:application/json; charset=utf-8",
                        "Accept-Encoding:gzip"
                    }
                };

                try
                {
                    string res = await thisClient.UploadStringTaskAsync(url, data);
                    JObject tmp = JObject.Parse(res);
                    return tmp;
                }
                catch (WebException ex)
                {
                    using (HttpWebResponse hr = (HttpWebResponse)ex.Response)
                    {
                        int statusCode = (int)hr.StatusCode;
                        StringBuilder sb = new StringBuilder();
                        StreamReader sr = new StreamReader(hr.GetResponseStream(), Encoding.UTF8);
                        sb.Append(sr.ReadToEnd());
                        JObject tmp = new JObject(JObject.Parse(sb.ToString()));
                        throw new Exception(tmp["errors"]["system"]["message"].ToString());
                    }
                }
            }
            public static string PhttpReq(string url, string postDataStr, bool isLogin = false)
            {
                HttpWebRequest hRequest = (HttpWebRequest)HttpWebRequest.Create(url);
                if (isLogin)
                {
                    hRequest.Headers = new WebHeaderCollection
                    {
                        { "x-rpc-channel_id", "14" },
                        { "x-rpc-channel_version", "2.7.0" },
                        { "x-rpc-client_type", "2" },
                        { "x-rpc-device_id", "aabbccddeeff2333aabbccddeeff2333" },
                        { "x-rpc-device_model", "Mi+6" },
                        { "x-rpc-device_name", "hello" },
                        { "x-rpc-language", "zh-cn" },
                        { "x-rpc-sys_version", "9" }
                    };
                }
                hRequest.UserAgent = "okhttp/3.10.0";
                hRequest.Headers.Add("Accept-Encoding", "gzip");
                hRequest.KeepAlive = true;
                hRequest.Method = "POST";
                hRequest.ContentType = "application/json; charset=utf-8";

                byte[] lbPostBuffer = Encoding.UTF8.GetBytes(postDataStr);
                hRequest.ContentLength = lbPostBuffer.Length;
                hRequest.Timeout = 10 * 1000;
                hRequest.AutomaticDecompression = DecompressionMethods.GZip;
                hRequest.GetRequestStream().Write(lbPostBuffer, 0, lbPostBuffer.Length);

                HttpWebResponse response = (HttpWebResponse)hRequest.GetResponse();
                Stream myResponseStream = response.GetResponseStream();
                StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.UTF8);
                string retString = myStreamReader.ReadToEnd();
                myStreamReader.Close();
                myResponseStream.Close();
                return retString;
            }
            public static string GetSign(string value)
            {
                byte[] key = Encoding.UTF8.GetBytes("0ebc517adb1b62c6b408df153331f9aa");
                byte[] encryptText = Encoding.UTF8.GetBytes(value);
                string hash = null;
                using (HMACSHA256 hmac = new HMACSHA256(key))
                {
                    byte[] res = hmac.ComputeHash(encryptText);
                    if (res != null)
                    {
                        for (int i = 0; i < res.Length; i++)
                        {
                            hash += res[i].ToString("X2");
                        }
                    }
                }
                return hash.ToLower();
            }
        }
        public class Bili
        {
            public static async Task<JObject> PostRequestAsync(string url, string data)
            {
                WebClient thisClient = new WebClient
                {
                    Headers = new WebHeaderCollection
                    {
                        "User-Agent:Mozilla/5.0 BSGameSDK",
                        "Content-Type:application/x-www-form-urlencoded"
                    }
                };
                data += "&sign=" + GetSign(data);
                try
                {
                    string res = await thisClient.UploadStringTaskAsync(url, data);
                    JObject tmp = JObject.Parse(res);
                    return tmp;
                }
                catch (WebException ex)
                {
                    using (HttpWebResponse hr = (HttpWebResponse)ex.Response)
                    {
                        int statusCode = (int)hr.StatusCode;
                        StringBuilder sb = new StringBuilder();
                        StreamReader sr = new StreamReader(hr.GetResponseStream(), Encoding.UTF8);
                        sb.Append(sr.ReadToEnd());
                        JObject tmp = new JObject(JObject.Parse(sb.ToString()));
                        throw new Exception(tmp["errors"]["system"]["message"].ToString());
                    }
                }
            }
            private static string GetMd5Hash(string input)
            {
                StringBuilder sBuilder = new StringBuilder();
                using (MD5 md5Hash = MD5.Create())
                {
                    byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
                    for (int i = 0; i < data.Length; i++)
                    {
                        sBuilder.Append(data[i].ToString("x2"));
                    }
                }
                return sBuilder.ToString();
            }
            public static string GetSign(string data)
            {
                var queryString = HttpUtility.ParseQueryString(data);
                var orderedKeys = queryString.Cast<string>().Where(k => k != null).OrderBy(k => k);
                data = string.Join("&", orderedKeys.Select(k => string.Format("{0}={1}", k, queryString[k])));
                data = data.Replace('+', '`');
                NameValueCollection qscoll = HttpUtility.ParseQueryString(data, Encoding.UTF8);
                StringBuilder sb = new StringBuilder("");
                foreach (string s in qscoll.AllKeys)
                {
                    sb.Append(qscoll[s]);
                }
                string sig = sb.ToString() + BiliUrl.Client_SignKey;
                sig = sig.Replace('`', '+');
                using (MD5 md5Hash = MD5.Create())
                {
                    sig = GetMd5Hash(sig);
                }
                return sig;
            }
            public static string GetPwd(string publicKey, string pwd)
            {
                publicKey = publicKey.Replace("-----BEGIN PUBLIC KEY-----", "");
                publicKey = publicKey.Replace("-----END PUBLIC KEY-----", "");
                publicKey = publicKey.Replace("\n", "");
                byte[] bytes = Encoding.UTF8.GetBytes(pwd);
                return RSAPcks12Helper.EncryptWithPublicKey(publicKey, bytes);
            }
        }
    }
    public static class BiliUrl
    {
        public static string LoginHost = "https://line1-sdk-center-login-sh.biligame.net";
        public static string Client_GetRsa = LoginHost + "/api/client/rsa";
        public static string Client_Login = LoginHost + "/api/client/login";
        public static string Client_UserInfo = LoginHost + "/api/client/user.info";
        public static string Client_SignKey = "dbf8f1b4496f430b8a3c0f436a35b931";
    }
    public static class miHoYoUrl
    {
        public static string SDKHost = "https://api-sdk.mihoyo.com";
        public static string Combo_Login = SDKHost + "/combo/granter/login/login";
        public static string Combo_GetProtocol = SDKHost + "/combo/granter/api/getProtocol?major=0&language=zh-cn&app_id=1&minimum=10";
        public static string Combo_QRScan = SDKHost + "/combo/panda/qrcode/scan";
        public static string Combo_Confirm = SDKHost + "/combo/panda/qrcode/confirm";
    }
    class BiliLogin
    {
        private static string access_key = null;
        private static string uid = null;
        private static string ReqLongParam() => "cur_buvid=NONE&client_timestamp="
                + LoginUtils.GetUnix(true) +
                "&sdk_type=1&isRoot=0&merchant_id=590&dp=2560*1440&mac=AA%3ABB%3ACC%3ADD%3AEE%3AFF&uid="
                + uid +
                "&support_abis=arm64-v8a%2Carmeabi-v7a%2Carmeabi&platform_type=3&old_buvid=NONE&operators=5&model=Mi+6&udid=bm9uZQ==&net=4&app_id=180&brand=Android&game_id=180&timestamp="
                + LoginUtils.GetUnix(true) +
                "&ver=3.8.0&c=1&version_code=280&server_id=378&version=1&domain_switch_count=0&pf_ver=9&access_key="
                + access_key +
                "domain=line1-sdk-center-login-sh.biligame.net&original_domain=&imei=123456789012345&sdk_log_type=1&sdk_ver=2.7.0&android_id=aabbccddeeff2333&channel_id=1";
        public static async void UserLoginAsync(string username, string password, string ticket)
        {
            try
            {
                //client/rsa
                string postData = ReqLongParam();
                JObject tmp = await LoginUtils.Bili.PostRequestAsync(BiliUrl.Client_GetRsa, postData);
                string publicKey = tmp["rsa_key"].ToString();
                string hash = tmp["hash"].ToString();
                string pwd = Uri.EscapeDataString(LoginUtils.Bili.GetPwd(publicKey, hash + password));

                //client/login
                postData = ReqLongParam() + "&pwd=" + pwd + "&user_id=" + username;
                tmp = await LoginUtils.Bili.PostRequestAsync(
                        BiliUrl.Client_Login,
                        postData);
                if(tmp["code"].ToObject<int>() != 0)
				{
                    throw new Exception("登陆错误：" + tmp["message"].ToString());
				}
                access_key = tmp["access_key"].ToString();
                uid = tmp["uid"].ToString();

                //client/user.info
                postData = ReqLongParam();
                await LoginUtils.Bili.PostRequestAsync(
                        BiliUrl.Client_UserInfo,
                        postData);

                //combo/granter/login
                string sign = LoginUtils.miHoYo.GetSign("app_id=1&channel_id=14&data={\"uid\":" + uid + ",\"access_key\":\"" + access_key + "\"}&device=aabbccddeeff2333aabbccddeeff2333");
                postData = "{\"data\":\"{\\\"uid\\\":" + uid + ",\\\"access_key\\\":\\\"" + access_key + "\\\"}\",\"sign\":\"" + sign + "\",\"app_id\":1,\"channel_id\":14,\"device\":\"aabbccddeeff2333aabbccddeeff2333\"}";
                tmp = JObject.Parse(
                    LoginUtils.miHoYo.PhttpReq(
                        miHoYoUrl.Combo_Login,
                        postData,
                        true));

                if (tmp["retcode"].ToObject<int>() != 0)
                {
                    throw new Exception("login error");
                }
                string combo_token = tmp["data"]["combo_token"].ToString();
                string combo_id = tmp["data"]["combo_id"].ToString();

                //combo/panda/qrcode/scan
                string ts = LoginUtils.GetUnix(true);
                sign = LoginUtils.miHoYo.GetSign("app_id=1&device=aabbccddeeff2333aabbccddeeff2333&ticket=" + ticket + "&ts=" + ts);
                postData = "{\"sign\":\"" + sign + "\",\"ticket\":\"" + ticket + "\",\"app_id\":1,\"device\":\"aabbccddeeff2333aabbccddeeff2333\",\"ts\":" + ts + "}";
                tmp = JObject.Parse(
                    LoginUtils.miHoYo.PhttpReq(
                        miHoYoUrl.Combo_QRScan,
                        postData));

                if (tmp["message"].ToString() != "OK")
                {
                    throw new Exception("qrscan error");
                }

                //combo/panda/qrcode/confirm
                ts = LoginUtils.GetUnix(true);
                sign = LoginUtils.miHoYo.GetSign("app_id=1&device=aabbccddeeff2333aabbccddeeff2333&payload.ext={\"data\":{\"accountType\":2,\"accountID\":\"" + uid + "\",\"accountToken\":\"" + combo_token + "\",\"dispatch\":{\"account_url\":\"https://gameapi.account.mihoyo.com\", \"account_url_backup\":\"http://webapi.account.mihoyo.com\", \"asset_boundle_url\":\"https://bundle.bh3.com/asset_bundle/bb01/1.0\", \"ex_resource_url\":\"bundle.bh3.com/tmp/Original\", \"ext\":{\"data_use_asset_boundle\":\"1\", \"disable_msad\":\"1\", \"res_use_asset_boundle\":\"1\"}, \"gameserver\":{\"ip\":\"106.14.219.183\", \"port\":\"15100\"}, \"gateway\":{\"ip\":\"106.14.219.183\", \"port\":\"15100\"}, \"oaserver_url\":\"http://139.196.248.220:1080\", \"region_name\":\"bb01\", \"retcode\":\"0\", \"server_ext\":{\"cdkey_url\":\"https://api-takumi.mihoyo.com/common/\", \"is_official\":\"1\"}}}}&payload.proto=Combo&payload.raw={\"device_id\":\"aabbccddeeff2333aabbccddeeff2333\",\"open_id\":\"" + uid + "\",\"heartbeat\":false,\"asterisk_name\":\"" + username.Substring(0, 6) + "...\",\"app_id\":1,\"channel_id\":14,\"combo_id\":" + combo_id + ",\"combo_token\":\"" + combo_token + "\"}&ticket=" + ticket + "&ts=" + ts);

                postData = "{\"sign\":\"" + sign + "\",\"ticket\":\"" + ticket + "\",\"app_id\":1,\"device\":\"aabbccddeeff2333aabbccddeeff2333\",\"payload\":{\"ext\":\"{\\\"data\\\":{\\\"accountType\\\":2,\\\"accountID\\\":\\\"" + uid + "\\\",\\\"accountToken\\\":\\\"" + combo_token + "\\\",\\\"dispatch\\\":{\\\"account_url\\\":\\\"https://gameapi.account.mihoyo.com\\\", \\\"account_url_backup\\\":\\\"http://webapi.account.mihoyo.com\\\", \\\"asset_boundle_url\\\":\\\"https://bundle.bh3.com/asset_bundle/bb01/1.0\\\", \\\"ex_resource_url\\\":\\\"bundle.bh3.com/tmp/Original\\\", \\\"ext\\\":{\\\"data_use_asset_boundle\\\":\\\"1\\\", \\\"disable_msad\\\":\\\"1\\\", \\\"res_use_asset_boundle\\\":\\\"1\\\"}, \\\"gameserver\\\":{\\\"ip\\\":\\\"106.14.219.183\\\", \\\"port\\\":\\\"15100\\\"}, \\\"gateway\\\":{\\\"ip\\\":\\\"106.14.219.183\\\", \\\"port\\\":\\\"15100\\\"}, \\\"oaserver_url\\\":\\\"http://139.196.248.220:1080\\\", \\\"region_name\\\":\\\"bb01\\\", \\\"retcode\\\":\\\"0\\\", \\\"server_ext\\\":{\\\"cdkey_url\\\":\\\"https://api-takumi.mihoyo.com/common/\\\", \\\"is_official\\\":\\\"1\\\"}}}}\",\"raw\":\"{\\\"device_id\\\":\\\"aabbccddeeff2333aabbccddeeff2333\\\",\\\"open_id\\\":\\\"" + uid + "\\\",\\\"heartbeat\\\":false,\\\"asterisk_name\\\":\\\"" + username.Substring(0, 6) + "...\\\",\\\"app_id\\\":1,\\\"channel_id\\\":14,\\\"combo_id\\\":" + combo_id + ",\\\"combo_token\\\":\\\"" + combo_token + "\\\"}\",\"proto\":\"Combo\"},\"ts\":" + ts + "}";

                tmp = JObject.Parse(
                    LoginUtils.miHoYo.PhttpReq(
                        miHoYoUrl.Combo_Confirm,
                        postData));

                if (tmp["message"].ToString() != "OK")
                {
                    throw new Exception("qrscan confirm error.\r\n" + tmp["message"].ToString());
                }
            }
            catch (Exception ex)
            {
				System.Windows.MessageBox.Show(ex.Message+"\r\n"+ex.StackTrace);
            }
        }
    }
}