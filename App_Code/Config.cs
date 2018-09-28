using System;
using System.Collections.Generic;
using System.Web;

namespace Com.Alipay
{
    /// <summary>
    /// config 的摘要说明
    /// </summary>
    public class Config
    {
        private static string _appDomainPath;
        private static List<AliAccount> _appAccounts;
        private static List<AliAccount> _payAccounts;
        private static List<AliAccount> _f2fAccounts;
        private static List<AliAccount> _wapAccounts;

        public Config()
        {
            //
            // TODO: 在此处添加构造函数逻辑
            //
        }

        public static string app_id = "2018082161153022";

        // 支付宝网关
        public static string gatewayUrl = "https://openapi.alipay.com/gateway.do";

        // 商户私钥，您的原始格式RSA私钥
        public static string merchant_private_key = AppDomainPath + "keys\\rsa_private_key_{0}.pem";

        // 支付宝公钥,查看地址：https://openhome.alipay.com/platform/keyManage.htm 对应APPID下的支付宝公钥。
        public static string alipay_public_key = AppDomainPath + "keys\\alipay_rsa_public_key_{0}.pem";


        // 签名方式
        public static string sign_type = "RSA2";

        // 编码格式
        public static string charset = "UTF-8";

        public static List<AliAccount> AppAccounts
        {
            get
            {
                if (_appAccounts == null)
                {
                    _appAccounts = new List<AliAccount>();

                    var sql = "select AppId,PID,SellerId,AppName,SignType from Accounts where Status=N'正常' and Type='App'";

                    DataAccess.ExecuteReader(sql, reader =>
                    {
                        while (reader.Read())
                        {
                            _appAccounts.Add(new AliAccount()
                            {
                                AppId = reader.GetString(0),
                                PID = reader.GetString(1),
                                SellerId = reader.GetString(2),
                                AppName = reader.GetString(3),
                                SignType = reader.GetString(4)
                            });
                        }
                    });
                }

                return _appAccounts;
            }
        }

        public static List<AliAccount> F2FAccounts
        {
            get
            {
                if (_f2fAccounts == null)
                {
                    _f2fAccounts = new List<AliAccount>();

                    var sql = "select AppId,PID,SellerId,AppName,SignType from Accounts where Status=N'正常' and Type='F2F'";

                    DataAccess.ExecuteReader(sql, reader =>
                    {
                        while (reader.Read())
                        {
                            _f2fAccounts.Add(new AliAccount()
                            {
                                AppId = reader.GetString(0),
                                PID = reader.GetString(1),
                                SellerId = reader.GetString(2),
                                AppName = reader.GetString(3),
                                SignType = reader.GetString(4)
                            });
                        }
                    });
                }

                return _f2fAccounts;
            }
        }

        public static List<AliAccount> WapAccounts
        {
            get
            {
                if (_wapAccounts == null)
                {
                    _wapAccounts = new List<AliAccount>();

                    var sql = "select AppId,PID,SellerId,AppName,SignType from Accounts where Status=N'正常' and Type='Wap'";

                    DataAccess.ExecuteReader(sql, reader =>
                    {
                        while (reader.Read())
                        {
                            _wapAccounts.Add(new AliAccount()
                            {
                                AppId = reader.GetString(0),
                                PID = reader.GetString(1),
                                SellerId = reader.GetString(2),
                                AppName = reader.GetString(3),
                                SignType = reader.GetString(4)
                            });
                        }
                    });
                }

                return _wapAccounts;
            }
        }

        public static List<AliAccount> PayAccounts
        {
            get
            {
                if (_payAccounts == null)
                {
                    _payAccounts = new List<AliAccount>();

                    var sql = "select AppId,PID,SellerId,AppName,SignType from Accounts where Status=N'正常' and Type='Pay'";

                    DataAccess.ExecuteReader(sql, reader =>
                    {
                        while (reader.Read())
                        {
                            _payAccounts.Add(new AliAccount()
                            {
                                AppId = reader.GetString(0),
                                PID = reader.GetString(1),
                                SellerId = reader.GetString(2),
                                AppName = reader.GetString(3),
                                SignType = reader.GetString(4)
                            });
                        }
                    });
                }

                return _payAccounts;
            }
        }

        public static AliAccount getAliAccount(string sellerEmail)
        {
            var account = new AliAccount();
            var sql = string.Format("select AppId,PID,SellerId,AppName,SignType from Accounts where SellerId='{0}'", sellerEmail);

            DataAccess.ExecuteReader(sql, reader =>
            {
                if (reader.Read())
                {
                    account.AppId = reader.GetString(0);
                    account.PID = reader.GetString(1);
                    account.SellerId = reader.GetString(2);
                    account.AppName = reader.GetString(3);
                    account.SignType = reader.GetString(4);
                }
            });

            return account;
        }

        public static string AppDomainPath
        {
            get
            {
                if (_appDomainPath == null)
                {
                    string appDomainAppPath = HttpRuntime.AppDomainAppPath;
                    if (appDomainAppPath == null)
                    {
                        throw new ApplicationException("AppDomainAppPath is null");
                    }
                    _appDomainPath = appDomainAppPath.ToString();
                }
                return _appDomainPath;
            }
        }

    }

    public class AliAccount
    {
        public string AppId { get; set; }
        public string PID { get; set; }
        public string SellerId { get; set; }
        public string AppName { get; set; }
        public string SignType { get; set; }
    }
}