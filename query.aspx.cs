using Aop.Api;
using Aop.Api.Domain;
using Aop.Api.Request;
using Aop.Api.Response;
using Com.Alipay;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

public partial class query : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        // 商户订单号，和交易号不能同时为空
        string out_trade_no = Request.Form["out_trade_no"];
        // 支付宝交易号，和商户订单号不能同时为空
        string trade_no = Request.Form["trade_no"];
        string key = Request.Form["key"];
        object result = null;

        try
        {
            if (DataAccess.ExecuteScalar<int>(string.Format("select count(*) from Merchant where MerchantKey='{0}'", key)) == 0)
            {
                result = new ResponseResult()
                {
                    code = "40001",
                    msg = "Invalid merchant key",
                    sub_code = "ACQ.INVALID_KEY",
                    sub_msg = "无效的商户秘钥",
                    buyer_pay_amount = "0.00",
                    invoice_amount = "0.00",
                    out_trade_no = out_trade_no,
                    point_amount = "0.00",
                    receipt_amount = "0.00"
                };
            }
            else
            {

                var sql = string.Format("select count(*) from Orders where CaseNumber='{0}'", out_trade_no);

                if (DataAccess.ExecuteScalar<int>(sql) == 1)
                {
                    sql = string.Format("select seller_email from Orders where CaseNumber='{0}'", out_trade_no);
                    var seller_email = DataAccess.ExecuteScalar<string>(sql);
                    var aliAccount = Config.getAliAccount(seller_email);
                    var alipayPublicKey = string.Format(Config.alipay_public_key, aliAccount.AppName);
                    var alipayPrivateKey = string.Format(Config.merchant_private_key, aliAccount.AppName);

                    DefaultAopClient client = new DefaultAopClient(Config.gatewayUrl, aliAccount.AppId, alipayPrivateKey, "json", "1.0", Config.sign_type, alipayPublicKey, Config.charset, true);

                    AlipayTradeQueryModel model = new AlipayTradeQueryModel();
                    model.OutTradeNo = out_trade_no;
                    model.TradeNo = trade_no;

                    AlipayTradeQueryRequest request = new AlipayTradeQueryRequest();
                    request.SetBizModel(model);

                    AlipayTradeQueryResponse response = client.Execute(request);

                    JObject jo = (JObject)JsonConvert.DeserializeObject(response.Body);
                    result = jo["alipay_trade_query_response"];
                }
                else
                {
                    result = new ResponseResult()
                    {
                        code = "40004",
                        msg = "Business Failed",
                        sub_code = "ACQ.TRADE_NOT_EXIST",
                        sub_msg = "交易不存在",
                        buyer_pay_amount = "0.00",
                        invoice_amount = "0.00",
                        out_trade_no = out_trade_no,
                        point_amount = "0.00",
                        receipt_amount = "0.00"
                    };
                }
            }
        }
        catch (Exception exp)
        {
            Logger.Log("query::" + exp.ToString());
            result = new ResponseResult()
            {
                code = "50000",
                msg = "Internal server error",
                sub_code = "ACQ.INTERNAL_SERVER_ERROR",
                sub_msg = "服务器异常",
                buyer_pay_amount = "0.00",
                invoice_amount = "0.00",
                out_trade_no = out_trade_no,
                point_amount = "0.00",
                receipt_amount = "0.00"
            };
        }

        Response.ContentType = "application/json; charset=utf-8";
        Response.Write(JsonConvert.SerializeObject(result));
    }

    public class ResponseResult
    {
        public string code { get; set; }
        public string msg { get; set; }
        public string sub_code { get; set; }
        public string sub_msg { get; set; }
        public string buyer_pay_amount { get; set; }
        public string invoice_amount { get; set; }
        public string out_trade_no { get; set; }
        public string point_amount { get; set; }
        public string receipt_amount { get; set; }
    }
}