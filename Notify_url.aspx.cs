using Aop.Api.Util;
using Com.Alipay;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;

public partial class Notify_url : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        /* 实际验证过程建议商户添加以下校验。
        1、商户需要验证该通知数据中的out_trade_no是否为商户系统中创建的订单号，
        2、判断total_amount是否确实为该订单的实际金额（即商户订单创建时的金额），
        3、校验通知中的seller_id（或者seller_email) 是否为out_trade_no这笔单据的对应的操作方（有的时候，一个商户可能有多个seller_id/seller_email）
        4、验证app_id是否为该商户本身。
        */
        Logger.Log("Notify_url::Call back from alipay...");
        Dictionary<string, string> sArray = GetRequestPost();
        if (sArray.Count != 0)
        {
            var seller_email = Request.Form["seller_email"];
            var aliAccount = Config.getAliAccount(seller_email);
            var alipayPublicKey = string.Format(Config.alipay_public_key, aliAccount.AppName);

            bool flag = AlipaySignature.RSACheckV1(sArray, alipayPublicKey, Config.charset, Config.sign_type, true);
            if (flag)
            {
                //交易状态
                //判断该笔订单是否在商户网站中已经做过处理
                //如果没有做过处理，根据订单号（out_trade_no）在商户网站的订单系统中查到该笔订单的详细，并执行商户的业务程序
                //请务必判断请求时的total_amount与通知时获取的total_fee为一致的
                //如果有做过处理，不执行商户的业务程序

                //注意：
                //退款日期超过可退款期限后（如三个月可退款），支付宝系统发送该交易状态通知

                string trade_status = Request.Form["trade_status"];
                string trade_no = Request.Form["trade_no"];
                Logger.Log("Callback for '" + trade_no + "' with status " + trade_status);

                if (trade_status == "TRADE_SUCCESS" || trade_status == "TRADE_FINISHED")
                {
                    if (SucceedAnOrder())
                    {
                        Response.Write("success");//Don't delete this
                    }
                    else
                    {
                        Response.Write("fail");
                    }
                }
                else
                {
                    if (FailedAnOrder())
                    {
                        Response.Write("success");//Don't delete this
                    }
                    else
                    {
                        Response.Write("fail");
                    }
                }
            }
            else
            {
                Logger.Log("Notify_url::Verify Sign Fail");
                Response.Write("fail");
            }
        }
    }

    public Dictionary<string, string> GetRequestPost()
    {
        int i = 0;
        Dictionary<string, string> sArray = new Dictionary<string, string>();
        NameValueCollection coll;
        coll = Request.Form;
        string[] requestItem = coll.AllKeys;
        for (i = 0; i < requestItem.Length; i++)
        {
            sArray.Add(requestItem[i], Request.Form[requestItem[i]]);
            //Logger.Log(requestItem[i] + "=" + Request.Form[requestItem[i]]);
        }
        return sArray;

    }

    #region PassAnOrder

    private bool SucceedAnOrder()
    {
        var succeed = false;
        var trade_no = Request.Form["trade_no"];
        var app_id = Request.Form["app_id"];
        var seller_email = Request.Form["seller_email"];
        var buyer_logon_id = Request.Form["buyer_logon_id"];
        var buyer_pay_amount = Request.Form["buyer_pay_amount"];
        var trade_status = Request.Form["trade_status"];
        var amount = buyer_pay_amount;
        var channel = "alipay_wap";
        var order_no = Request.Form["out_trade_no"];

        if (trade_status == "TRADE_SUCCESS")
        {
            var sql = string.Format("select COUNT(*) from Orders where CaseNumber='{0}' and CallBackStatus='SUCCESS'", order_no);

            if (DataAccess.ExecuteScalar<int>(sql) == 0)
            {
                sql = string.Format("update Orders set CaseStatus=N'付款成功', trade_no='{0}',app_id='{1}',PayTime=GetDate(),Channel='{2}',seller_email='{3}',buyer_logon_id='{4}',buyer_pay_amount='{5}',trade_status='{6}' where CaseNumber='{7}'"
                                             , trade_no, app_id, channel, seller_email, buyer_logon_id, buyer_pay_amount, trade_status, order_no);

                succeed = DataAccess.ExecuteNonQuery(sql) == 1;

                if (!succeed)
                {
                    Logger.Log("回调处理失败:" + order_no);
                    Logger.Log(sql);
                }
            }

            sql = string.Format("select COUNT(*) from Orders where CaseNumber='{0}'", order_no);

            if (DataAccess.ExecuteScalar<int>(sql) == 0)
                succeed = true;
        }

        return succeed;
    }

    private bool FailedAnOrder()
    {
        var succeed = false;
        var trade_no = Request.Form["trade_no"];
        var app_id = Request.Form["app_id"];
        var seller_email = Request.Form["seller_email"];
        var buyer_logon_id = Request.Form["buyer_logon_id"];
        var buyer_pay_amount = Request.Form["buyer_pay_amount"];
        var trade_status = Request.Form["trade_status"];

        var amount = buyer_pay_amount;
        var channel = "alipay";
        var order_no = Request.Form["out_trade_no"];
        var sql = string.Format("select COUNT(*) from Orders where CaseNumber='{0}'", order_no);

        if (DataAccess.ExecuteScalar<int>(sql) == 0)
            succeed = true;
        else if (trade_status != "TRADE_SUCCESS")
        {
            sql = string.Format("update Orders set CaseStatus=N'付款失败', trade_no='{0}',app_id='{1}',PayTime=GetDate(),Channel='{2}',seller_email='{3}',buyer_logon_id='{4}',buyer_pay_amount='{5}',trade_status='{6}' where CaseNumber='{7}'"
                                         , trade_no, app_id, channel, seller_email, buyer_logon_id, buyer_pay_amount, trade_status, order_no);
            succeed = DataAccess.ExecuteNonQuery(sql) == 1;
            if (!succeed)
            {
                Logger.Log("AliPay回调处理失败:" + order_no);
                Logger.Log(sql);
            }
        }

        return succeed;
    }

    #endregion
}