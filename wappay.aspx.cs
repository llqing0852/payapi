using Aop.Api;
using Aop.Api.Domain;
using Aop.Api.Request;
using Aop.Api.Response;
using Com.Alipay;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;

public partial class wappay : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        var type = Request.RequestType.ToUpper();
        var result = new AliPayResult()
        {
            Status = "FAILED",
            Msg = "",
            Body = ""
        };
        if (type.Equals("POST"))
        {
            try
            {
                var requestStr = ReadStream(Request.InputStream);
                var logMsg = bool.Parse(ConfigurationManager.AppSettings["LogMsg"].ToString());

                if (logMsg)
                    Logger.Log(requestStr);

                var turnOn = bool.Parse(ConfigurationManager.AppSettings["AllowPay"].ToString());
                if (turnOn)
                {
                    if (Config.WapAccounts.Count > 0)
                    {
                        // 外部订单号，商户网站订单系统中唯一的订单号
                        string out_trade_no = Request.Form["out_trade_no"];

                        // 订单名称
                        string subject = Request.Form["subject"];

                        // 付款金额
                        string total_amout = Request.Form["total_amout"];

                        // 商品描述
                        string body = Request.Form["body"];

                        // 支付中途退出返回商户网站地址
                        string quit_url = Request.Form["quit_url"];

                        // 支付成功回调商户地址
                        string notify_url = Request.Form["notify_url"];

                        string key = Request.Form["key"];

                        var msg = AddNewOrder(out_trade_no, total_amout, subject, body, key);

                        if (string.IsNullOrEmpty(msg))
                        {
                            var aliAccount = Config.WapAccounts[0];
                            var alipayPublicKey = string.Format(Config.alipay_public_key, aliAccount.AppName);
                            var alipayPrivateKey = string.Format(Config.merchant_private_key, aliAccount.AppName);
                            DefaultAopClient client = new DefaultAopClient(Config.gatewayUrl, aliAccount.AppId, alipayPrivateKey, "json", "1.0", Config.sign_type, alipayPublicKey, Config.charset, true);

                            // 组装业务参数model
                            AlipayTradeWapPayModel model = new AlipayTradeWapPayModel();
                            model.Body = body;
                            model.Subject = subject;
                            model.TotalAmount = total_amout;
                            model.OutTradeNo = out_trade_no;
                            model.ProductCode = "QUICK_WAP_WAY";
                            model.QuitUrl = quit_url;

                            AlipayTradeWapPayRequest request = new AlipayTradeWapPayRequest();
                            // 设置支付完成同步回调地址
                            // request.SetReturnUrl("");
                            // 设置支付完成异步通知接收地址
                            request.SetNotifyUrl("http://139.196.211.10/payapi/Notify_url.aspx");
                            // 将业务model载入到request
                            request.SetBizModel(model);

                            AlipayTradeWapPayResponse response = client.pageExecute(request, null, "post");

                            result.Body = response.Body;
                            result.Status = "SUCCESS";
                        }
                        else
                        {
                            result.Msg = msg;
                        }
                    }
                    else
                    {
                        result.Msg = "无可用通道...";
                    }
                }
                else
                {
                    result.Msg = "通道维护中...";
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Exception:" + ex.ToString());
                result.Msg = "请求支付宝服务遇到异常，请联系客服！";
            }
        }
        else
        {
            result.Msg = "请发送POST请求";
        }

        Response.ContentType = "application/json; charset=utf-8";
        Response.Write(JsonConvert.SerializeObject(result));
    }

    private string AddNewOrder(string orderNo, string amount, string subject, string body, string key)
    {
        var msg = "";
        try
        {
            var sql = string.Format("select count(*) from Merchant where MerchantKey='{0}'", key);

            if (DataAccess.ExecuteScalar<int>(sql) == 0)
            {
                msg = "无效的商户秘钥";

                return msg;
            }

            sql = string.Format("select count(*) from Merchant where MerchantKey='{0}' and ChannelStatus='开通'", key);

            if (DataAccess.ExecuteScalar<int>(sql) == 0)
            {
                msg = "商户支付通道关闭";

                return msg;
            }

            sql = string.Format("select count(*) from Orders where CaseNumber='{0}'", orderNo);

            if (DataAccess.ExecuteScalar<int>(sql) > 0)
            {
                msg = "订单号重复";

                return msg;
            }

            var sqlParms = new List<SqlParameter>();
            sqlParms.Add(new SqlParameter("@CaseNumber", SqlDbType.NVarChar, 50) { Direction = ParameterDirection.Input, Value = orderNo });
            sqlParms.Add(new SqlParameter("@Amount", SqlDbType.Float) { Direction = ParameterDirection.Input, Value = amount });
            sqlParms.Add(new SqlParameter("@Subject", SqlDbType.NVarChar, 50) { Direction = ParameterDirection.Input, Value = subject });
            sqlParms.Add(new SqlParameter("@Body", SqlDbType.NVarChar, 50) { Direction = ParameterDirection.Input, Value = body });
            sqlParms.Add(new SqlParameter("@Key", SqlDbType.NVarChar, 50) { Direction = ParameterDirection.Input, Value = key });

            DataAccess.ExecuteStoredProcedureNonQuery("AddNewOrder", sqlParms);
        }
        catch (Exception e)
        {
            Logger.Log("AddNewOrder Exception:" + e.ToString());

            msg = "新建支付订单异常";
        }

        return msg;
    }

    private static string ReadStream(Stream stream)
    {
        using (var reader = new StreamReader(stream, Encoding.UTF8))
        {
            return reader.ReadToEnd();
        }
    }

    public class AliPayResult
    {
        public string Status { get; set; }
        public string Body { get; set; }
        public string Msg { get; set; }
    }
}