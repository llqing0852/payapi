using Aop.Api;
using Aop.Api.Request;
using Aop.Api.Response;
using Com.Alipay;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;

public partial class payto : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        var type = Request.RequestType.ToUpper();
        var result = new ResponseResult()
        {
            Status = "FAILED",
            Msg = "",
            Amount = 0,
            OrderNo = ""
        };

        if (type.Equals("POST"))
        {
            var logMsg = bool.Parse(ConfigurationManager.AppSettings["LogMsg"].ToString());
            var startTime = int.Parse(ConfigurationManager.AppSettings["StartTime"].ToString());
            var endTime = int.Parse(ConfigurationManager.AppSettings["EndTime"].ToString());
            var ip = Request.ServerVariables.Get("Remote_Addr").ToString();
            if (logMsg) Logger.Log(ip);

            var turnOn = bool.Parse(ConfigurationManager.AppSettings["AllowPayTo"].ToString());

            if (turnOn)
            {
                var sql = string.Format("select COUNT(*) from Merchant where IPAddress=N'{0}'", ip);

                if (DataAccess.ExecuteScalar<int>(sql) == 1)
                {
                    if (DateTime.Now.Hour < startTime || DateTime.Now.Hour >= endTime)
                    {
                        Logger.Log("Channel turn on from " + startTime + ":00 to " + endTime + ":00");
                        result.Msg = "通道开启时间为 " + startTime + ":00 到 " + endTime + ":00";
                    }
                    else
                    {
                        try
                        {
                            var requestStr = ReadStream(Request.InputStream);

                            if (logMsg) Logger.Log(requestStr);

                            var orderNo = string.Empty;
                            var account = string.Empty;
                            var amount = string.Empty;
                            var showName = string.Empty;
                            var realName = string.Empty;
                            var remark = string.Empty;
                            var key = string.Empty;

                            try
                            {
                                JObject obj = (JObject)JsonConvert.DeserializeObject(requestStr);
                                orderNo = obj["orderNo"].ToString();
                                account = obj["account"].ToString();
                                amount = obj["amount"].ToString();
                                showName = obj["showName"].ToString();
                                realName = obj["realName"].ToString();
                                remark = obj["remark"].ToString();
                                key = obj["key"].ToString();
                            }
                            catch (JsonReaderException)
                            {
                                orderNo = Request.Form["orderNo"];
                                account = Request.Form["account"];
                                amount = Request.Form["amount"];
                                showName = Request.Form["showName"];
                                realName = Request.Form["realName"];
                                remark = Request.Form["remark"];
                                key = Request.Form["key"];
                            }

                            if (DataAccess.ExecuteScalar<int>(string.Format("select count(*) from Merchant where MerchantKey='{0}'", key)) == 0)
                            {
                                result.Msg = "无效的商户秘钥";
                            }
                            else if (DataAccess.ExecuteScalar<int>(string.Format("select count(*) from Merchant where MerchantKey='{0}' and ChannelStatus='开通'", key)) == 0)
                            {
                                result.Msg = "商户通道关闭";
                            }
                            else if (DataAccess.ExecuteScalar<int>(string.Format("select count(*) from Merchant where MerchantKey='{0}' and IPAddress='{1}'", key, ip)) == 0)
                            {
                                result.Msg = string.Format("商户秘钥和IP地址{0}不匹配", ip);
                            }
                            else
                            {
                                var totalInSum = double.Parse(DataAccess.ExecuteScalar<decimal>(string.Format("SELECT SUM(amount) FROM Orders where CaseStatus=N'付款成功' and MerchantId in(select Id from Merchant where MerchantKey='{0}')", key)).ToString());
                                var totalOutSum = double.Parse(DataAccess.ExecuteScalar<decimal>(string.Format("select SUM(amount) from PayOrders where CaseStatus=N'付款成功' and MerchantId in(select Id from Merchant where MerchantKey='{0}')", key)).ToString());
                                var totalOutCount = double.Parse(DataAccess.ExecuteScalar<int>(string.Format("select count(*) from PayOrders where CaseStatus=N'付款成功' and MerchantId in(select Id from Merchant where MerchantKey='{0}')", key)).ToString());
                                var actualBalance = totalInSum * 0.97 - totalOutSum - totalOutCount * 3;
                                int balance = 0;

                                var balanceConfig = ConfigurationManager.AppSettings["Balance"];

                                if (!string.IsNullOrEmpty(balanceConfig))
                                {
                                    balance = int.Parse(balanceConfig);
                                }
                                //totalOut = totalOut + double.Parse(amount);

                                if (actualBalance - double.Parse(amount) > balance)
                                {
                                    var aliAccount = Config.PayAccounts[new Random().Next() % Config.PayAccounts.Count];
                                    var alipayPublicKey = string.Format(Config.alipay_public_key, aliAccount.AppName);

                                    float a = 0;
                                    if (float.TryParse(amount, out a))
                                    {
                                        IAopClient client = new DefaultAopClient("https://openapi.alipay.com/gateway.do", aliAccount.AppId, string.Format(Config.merchant_private_key, aliAccount.AppName), "json", "1.0", aliAccount.SignType, alipayPublicKey, Config.charset, true);
                                        AlipayFundTransToaccountTransferRequest request = new AlipayFundTransToaccountTransferRequest();
                                        request.BizContent = "{" +
                                        "\"out_biz_no\":\"" + orderNo + "\"," +
                                        "\"payee_type\":\"ALIPAY_LOGONID\"," +
                                        "\"payee_account\":\"" + account + "\"," +
                                        "\"amount\":\"" + amount + "\"," +
                                        "\"payer_show_name\":\"" + showName + "\"," +
                                        "\"payee_real_name\":\"" + realName + "\"," +
                                        "\"remark\":\"" + remark + "\"" +
                                        "  }";

                                        AlipayFundTransToaccountTransferResponse response = client.Execute(request);

                                        try
                                        {
                                            JObject retObj = (JObject)JsonConvert.DeserializeObject(response.Body);
                                            var code = retObj["alipay_fund_trans_toaccount_transfer_response"]["code"].ToString();

                                            AddNewOrder(new PayOrder()
                                            {
                                                OrderNo = orderNo,
                                                Account = account,
                                                RealName = realName,
                                                Amount = amount,
                                                ShowName = showName,
                                                Remark = remark,
                                                CaseStatus = code == "10000" ? "付款成功" : "付款失败",
                                                Response = response.Body,
                                                Key = key
                                            });

                                            if (code == "10000")
                                            {
                                                result.Status = "SUCCESS";
                                                result.Msg = "支付成功";
                                            }
                                            else
                                            {
                                                var msg = retObj["alipay_fund_trans_toaccount_transfer_response"]["sub_msg"];

                                                if (msg != null)
                                                    result.Msg = msg.ToString();
                                                else
                                                    result.Msg = "支付失败";

                                                result.Status = "FAILED";
                                            }

                                            result.Amount = float.Parse(amount);
                                            result.OrderNo = orderNo;

                                        }
                                        catch (Exception ex)
                                        {
                                            result.Msg = "服务器异常";
                                            Logger.Log("Response Body:" + response.Body);
                                            Logger.Log("Exception:" + ex.ToString());
                                        }

                                    }
                                    else
                                    {
                                        result.Msg = "Amount参数格式错误";
                                    }
                                }
                                else
                                {
                                    Logger.Log("余额沉淀'" + actualBalance + "'不足以支付'" + amount + "'");
                                    Logger.Log("totalInSum:" + totalInSum);
                                    Logger.Log("totalOutSum:" + totalOutSum);
                                    Logger.Log("totalOutCount:" + totalOutCount);
                                    Logger.Log("actualBalance:" + actualBalance);
                                    Logger.Log("key:" + key);

                                    result.Msg = "总收入扣除手续费小于总支出";
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Log("Exception:" + ex.ToString());
                            result.Msg = "服务器异常";
                        }
                    }
                }
                else
                {
                    result.Msg = "未授权的IP地址";
                }
            }
            else
            {
                result.Msg = "支付通道维护中...";
            }
        }
        else
        {
            result.Msg = "请发POST请求";
        }

        Response.ContentType = "application/json; charset=utf-8";
        Response.Write(JsonConvert.SerializeObject(result));
    }

    private void AddNewOrder(PayOrder order)
    {
        try
        {
            var sqlParms = new List<SqlParameter>();
            sqlParms.Add(new SqlParameter("@OrderNo", SqlDbType.NVarChar, 50) { Direction = ParameterDirection.Input, Value = order.OrderNo });
            sqlParms.Add(new SqlParameter("@Account", SqlDbType.NVarChar, 50) { Direction = ParameterDirection.Input, Value = order.Account });
            sqlParms.Add(new SqlParameter("@RealName", SqlDbType.NVarChar, 50) { Direction = ParameterDirection.Input, Value = order.RealName });
            sqlParms.Add(new SqlParameter("@Amount", SqlDbType.Float) { Direction = ParameterDirection.Input, Value = order.Amount });
            sqlParms.Add(new SqlParameter("@ShowName", SqlDbType.NVarChar, 50) { Direction = ParameterDirection.Input, Value = order.ShowName });
            sqlParms.Add(new SqlParameter("@Remark", SqlDbType.NVarChar, 50) { Direction = ParameterDirection.Input, Value = order.Remark });
            sqlParms.Add(new SqlParameter("@CaseStatus", SqlDbType.NVarChar, 50) { Direction = ParameterDirection.Input, Value = order.CaseStatus });
            sqlParms.Add(new SqlParameter("@Response", SqlDbType.NVarChar, 500) { Direction = ParameterDirection.Input, Value = order.Response });
            sqlParms.Add(new SqlParameter("@Key", SqlDbType.NVarChar, 50) { Direction = ParameterDirection.Input, Value = order.Key });

            DataAccess.ExecuteStoredProcedureNonQuery("AddPayOrder", sqlParms);
        }
        catch (Exception e)
        {
            Logger.Log("AddPayOrder Exception:" + e.ToString());
            throw e;
        }
    }

    private static string ReadStream(Stream stream)
    {
        using (var reader = new StreamReader(stream, Encoding.UTF8))
        {
            return reader.ReadToEnd();
        }
    }

    public class ResponseResult
    {
        public string Status { get; set; }
        public string Msg { get; set; }
        public string OrderNo { get; set; }
        public float Amount { get; set; }
    }

    public class PayOrder
    {
        public string OrderNo { get; set; }
        public string Account { get; set; }
        public string RealName { get; set; }
        public string Amount { get; set; }
        public string ShowName { get; set; }
        public string Remark { get; set; }
        public string CaseStatus { get; set; }
        public string Response { get; set; }
        public string Key { get; set; }
    }
}