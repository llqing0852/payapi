using Aop.Api;
using Aop.Api.Domain;
using Aop.Api.Request;
using Aop.Api.Response;
using Com.Alipay;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

public partial class wappay_wappay : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {

    }

    protected void BtnAlipay_Click(object sender, EventArgs e)
    {
        // 外部订单号，商户网站订单系统中唯一的订单号
        string out_trade_no = WIDout_trade_no.Text.Trim();

        // 订单名称
        string subject = WIDsubject.Text.Trim();

        // 付款金额
        string total_amout = WIDtotal_amount.Text.Trim();

        // 商品描述
        string body = WIDbody.Text.Trim();

        // 支付中途退出返回商户网站地址
        string quit_url = WIDquit_url.Text.Trim();

        try
        {
            var msg = AddNewOrder(out_trade_no, total_amout, subject, body, "950F76CC-B4E8-4733-A336-E3E2C6118579");

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

                Response.Write(response.Body);
            }
            else
            {
                Response.Write(msg);
            }
        }
        catch (Exception ex)
        {
            Logger.Log("Exception:" + ex.ToString());
        }
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
}