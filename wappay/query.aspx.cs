using Aop.Api;
using Aop.Api.Domain;
using Aop.Api.Request;
using Aop.Api.Response;
using Com.Alipay;
using System;

public partial class wappay_query : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {

    }

    protected void BtnAlipay_Click(object sender, EventArgs e)
    {
        DefaultAopClient client = new DefaultAopClient(Config.gatewayUrl, Config.app_id, Config.merchant_private_key, "json", "1.0", Config.sign_type, Config.alipay_public_key, Config.charset, true);

        // 商户订单号，和交易号不能同时为空
        string out_trade_no = WIDout_trade_no.Text.Trim();

        // 支付宝交易号，和商户订单号不能同时为空
        string trade_no = WIDtrade_no.Text.Trim();

        AlipayTradeQueryModel model = new AlipayTradeQueryModel();
        model.OutTradeNo = out_trade_no;
        model.TradeNo = trade_no;

        AlipayTradeQueryRequest request = new AlipayTradeQueryRequest();
        request.SetBizModel(model);

        AlipayTradeQueryResponse response = null;
        try
        {
            response = client.Execute(request);
            WIDresule.Text = response.Body;

        }
        catch (Exception exp)
        {
            throw exp;
        }
    }
}