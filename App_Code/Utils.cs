using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Aop.Api;
using Aop.Api.Request;
using Aop.Api.Response;

namespace Com.Alipay
{
    public class Utils
    {
        public static bool SettlePayment(string caseNumber)
        {
            bool succeed = false;
            try
            {
                var sellerId = DataAccess.ExecuteScalar<string>(string.Format("select seller_email from Orders where CaseNumber='{0}'", caseNumber));
                var tradeNo = DataAccess.ExecuteScalar<string>(string.Format("select trade_no from Orders where CaseNumber='{0}'", caseNumber));
                var f2fAccount = Config.AppAccounts[0];
                var payAccount = Config.PayAccounts[0];
                var outRequestNo = string.Format("JZ{0}{1}", DateTime.Now.ToString("yyyyMMddHHmmssfff"), Utils.GetUniqueKey());

                var alipayPublicKey = string.Format(Config.alipay_public_key, f2fAccount.AppName);

                IAopClient client = new DefaultAopClient("https://openapi.alipay.com/gateway.do", f2fAccount.AppId, string.Format(Config.merchant_private_key, f2fAccount.AppName), "json", "1.0", f2fAccount.SignType, alipayPublicKey, Config.charset, true);
                AlipayTradeOrderSettleRequest request = new AlipayTradeOrderSettleRequest();
                request.BizContent = "{" +
                string.Format("\"out_request_no\":\"{0}\",", outRequestNo) +
                string.Format("\"trade_no\":\"{0}\",", tradeNo) +
                "      \"royalty_parameters\":[{" +
                string.Format("        \"trans_out\":\"{0}\",", f2fAccount.PID) +
                string.Format("\"trans_in\":\"{0}\",", payAccount.PID) +
                //"\"amount\":100," +
                "\"amount_percentage\":100," +
                string.Format("\"desc\":\"从{0}分账到{1}\"", f2fAccount.SellerId, payAccount.SellerId) +
                "        }]," +
                "\"operator_id\":\"A0001\"" +
                "  }";
                AlipayTradeOrderSettleResponse response = client.Execute(request);

                if (response.Code == "10000")
                    DataAccess.ExecuteNonQuery(string.Format("update Orders set SettleResponse='{0}',trade_status='分账成功' where CaseNumber='{1}'", response.Body, caseNumber));
                else
                    DataAccess.ExecuteNonQuery(string.Format("update Orders set SettleResponse='{0}',trade_status='分账失败' where CaseNumber='{1}'", response.Body, caseNumber));

                succeed = response.Code == "10000";
            }
            catch (Exception e)
            {
                Logger.Log(string.Format("SettlePayment: {0}", e.ToString()));
            }

            return succeed;
        }

        internal static string GetUniqueKey()
        {
            int maxSize = 8;
            char[] chars = new char[62];
            string a;
            //a = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
            a = "1234567890";
            chars = a.ToCharArray();
            int size = maxSize;
            byte[] data = new byte[1];
            var crypto = new RNGCryptoServiceProvider();
            crypto.GetNonZeroBytes(data);
            size = maxSize;
            data = new byte[size];
            crypto.GetNonZeroBytes(data);
            var result = new StringBuilder(size);
            foreach (byte b in data)
            {
                result.Append(chars[b % (chars.Length - 1)]);
            }
            return result.ToString();
        }
    }
}