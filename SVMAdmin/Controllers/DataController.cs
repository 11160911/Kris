﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using System.Data;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;


namespace SVMAdmin.Controllers
{
    [Route("api")]
    [ApiController]
    public class DataController : ControllerBase
    {
        private readonly IWebHostEnvironment _hostingEnvironment;

        public DataController(IWebHostEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
        }

        [Route("LoginSys")]
        public ActionResult LoginSys()
        {
            System.Data.DataSet ds = PubUtility.GetApiReturn(new string[] { "LoginSysOK", "" });

            DataTable dtMessage = ds.Tables["dtMessage"];
            try
            {

                IFormCollection rq = HttpContext.Request.Form;
                string USERID = rq["USERID"];
                string PASSWORD = rq["PASSWORD"];
                string CompanyID = rq["CompanyID"];
                UserInfo uu = new UserInfo();
                uu.UserID = "Login";
                uu.CompanyId = CompanyID;


                if (System.Environment.MachineName.ToUpper() == "ANDYNB4")
                {
                    USERID = "008";
                    PASSWORD = "008";
                }

                string sql = "select Man_ID,Man_Name,Password from EmployeeSV ";
                sql += " where CompanyCode='" + CompanyID.SqlQuote() + "'";
                sql += " and Man_ID='" + USERID.SqlQuote() + "'";
                sql += " and Password='" + PASSWORD.SqlQuote() + "'";
                sql += "  and Password<>''";
                DataTable dtTmp = PubUtility.SqlQry(sql, uu, "SYS");
                if (dtTmp.Rows.Count == 0)
                    throw new Exception("密碼錯誤");
                dtTmp.TableName = "dtEmployee";
                dtTmp.Columns.Add("token", typeof(string));
                uu.UserID = USERID;
                string token = PubUtility.GenerateJwtToken(uu);
                dtTmp.Rows[0]["token"] = token;

                ds.Tables.Add(dtTmp);
            }
            catch (Exception err)
            {
                dtMessage.Rows[0][0] = "Exception";
                dtMessage.Rows[0][1] = err.Message;
            }
            return PubUtility.DatasetXML(ds);
        }

        [Route("LoginSysByApi")]
        public ActionResult LoginSysByApi()
        {
            System.Data.DataSet ds = PubUtility.GetApiReturn(new string[] { "LoginSysOK", "" });

            DataTable dtMessage = ds.Tables["dtMessage"];
            try
            {

                IFormCollection rq = HttpContext.Request.Form;
                string USERID = rq["USERID"];
                string PASSWORD = rq["PASSWORD"];
                string CompanyID = rq["CompanyID"];
                UserInfo uu = new UserInfo();
                uu.UserID = "Login";
                uu.CompanyId = CompanyID;


                DataTable dt = new DataTable("Employee");
                dt.Columns.Add("Man_ID", typeof(string));
                dt.Columns.Add("Password", typeof(string));
                dt.Columns.Add("CompanyCode", typeof(string));

                if (System.Environment.MachineName.ToUpper() == "ANDYNB4")
                {
                    USERID = "008";
                    PASSWORD = "008";
                }

                dt.Rows.Add(new object[] { USERID, PASSWORD, CompanyID });
                DataSet dsP = new DataSet();
                dsP.Tables.Add(dt);

                iXmsApiParameter AP = new iXmsApiParameter();
                AP.user = uu;
                AP.Method = "LoginSys";
                AP.ObjName = "";
                AP.UnknowName = "";


                string strPara = PubUtility.GetSerString(AP, typeof(iXmsApiParameter));
                string url = ConstList.ThisSiteConfig.Companys
                    .Where<Config.Company>(C => C.CompanyID == uu.CompanyId).ToList()[0].iXmsApiUrl + "/SVMApi.aspx";


                Uri aUri = new Uri(url);
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(aUri);
                httpWebRequest.Headers.Add("ParaKey", strPara);
                httpWebRequest.ContentType = "text/xml";
                httpWebRequest.Accept = "text/xml";
                httpWebRequest.Method = "POST";

                dsP.WriteXml(httpWebRequest.GetRequestStream());
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                DataSet dsR = null;
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    dsR = new DataSet();
                    dsR.ReadXml(streamReader);
                    DataTable dtRS = dsR.Tables["dtProcessStatus"];
                    if (dtRS.Rows[0]["Error"].ToString() != "0")
                        throw new Exception(dtRS.Rows[0]["Msg_Code"].ToString());

                    DataTable dtE = dsR.Tables["dtEmployee"];
                    dtE.Columns.Add("token", typeof(string));
                    uu.UserID = USERID;
                    string token = PubUtility.GenerateJwtToken(uu);
                    dtE.Rows[0]["token"] = token;
                    ds.Tables.Add(dtE.Copy());
                }
            }
            catch (Exception err)
            {
                dtMessage.Rows[0][0] = "Exception";
                dtMessage.Rows[0][1] = err.Message;
            }
            return PubUtility.DatasetXML(ds);
        }

        [Route("GetMenuInit")]
        public ActionResult GetMenuInit()
        {
            UserInfo uu = PubUtility.GetCurrentUser(this);
            System.Data.DataSet ds = PubUtility.GetApiReturn(new string[] { "GetMenuInitOK", uu.UserID });
            DataTable dtMessage = ds.Tables["dtMessage"];
            try
            {
                DataTable dt = ConstList.AllFunction();
                if (ds.Tables["dtAllFunction"] == null)
                    if (dt.DataSet == null)
                        ds.Tables.Add(dt);
            }
            catch (Exception err)
            {
                dtMessage.Rows[0][0] = "Exception";
                dtMessage.Rows[0][1] = err.Message;
            }
            return PubUtility.DatasetXML(ds);
        }

        [Route("FileUpload")]
        public ActionResult FileUpload()
        {
            System.Data.DataSet ds = PubUtility.GetApiReturn(new string[] { "FileUploadOK", "" });
            UserInfo uu = PubUtility.GetCurrentUser(this);
            string picSGID = "";
            try
            {
                IFormFileCollection files = HttpContext.Request.Form.Files;
                string UploadFileType = HttpContext.Request.Form["UploadFileType"];
                if (UploadFileType == "PLU+Pic1" | UploadFileType == "PLU+Pic2")
                {
                    picSGID = ImportPLUPic(files, UploadFileType);
                    DataTable dtMessage = ds.Tables["dtMessage"];
                    dtMessage.Rows[0][1] = picSGID;
                }
            }
            catch (Exception err)
            {
                DataTable dtMessage = ds.Tables["dtMessage"];
                dtMessage.Rows[0][0] = err.Message;
            }
            return PubUtility.DatasetXML(ds);
        }

        [Route("GetImage")]
        public ActionResult GetImage()
        {
            try
            {
                IQueryCollection rq = HttpContext.Request.Query;
                string SGID = PubUtility.DecodeSGID(rq["SGID"]);
                string UU = rq["UU"];
                UserInfo uu = PubUtility.GetCurrentUser("Bearer " + UU);
                DataTable dt = PubUtility.SqlQry("select * from ImageTable where SGID='" + SGID + "'", uu, "SYS");
                DataRow dr = dt.Rows[0];
                string ContentType = dr["DocType"].ToString();
                HttpContext.Response.Headers.Add("content-disposition", "attachment; filename=" + System.Web.HttpUtility.UrlEncode(dr["FileName"].ToString()));
                return File(dr["DocImage"] as byte[], ContentType);
            }
            catch (Exception err)
            {
                string ContentType = "image/jpeg";
                HttpContext.Response.Headers.Add("content-disposition", "attachment; filename=No_Pic.jpg");
                string fn = ConstList.HostEnvironment.WebRootPath + @"\images\No_Pic.jpg";
                return File(System.IO.File.ReadAllBytes(fn), ContentType);
            }
        }

        [Route("SystemSetup/GetInitGMMacPLUSet")]
        public ActionResult SystemSetup_GetInitGMMacPLUSet()
        {
            UserInfo uu = PubUtility.GetCurrentUser(this);
            System.Data.DataSet ds = PubUtility.GetApiReturn(new string[] { "GetInitGMMacPLUSetOK", "" });
            DataTable dtMessage = ds.Tables["dtMessage"];
            try
            {
                string sql = "select Type_ID,Type_Name";
                sql += " from TypeData ";
                sql += " where CompanyCode='" + uu.CompanyId + "' And Type_Code='G' Order By Type_ID";
                DataTable dtDept = PubUtility.SqlQry(sql, uu, "SYS");
                dtDept.TableName = "dtDept";
                ds.Tables.Add(dtDept);

                sql = "select Type_ID,Type_Name";
                sql += " from TypeData ";
                sql += " where CompanyCode='" + uu.CompanyId + "' And Type_Code='L' Order By Type_ID";
                DataTable dtBGNo = PubUtility.SqlQry(sql, uu, "SYS");
                dtBGNo.TableName = "dtBGNo";
                ds.Tables.Add(dtBGNo);
                ds.Tables.Add(dtBGNo);

                ds.Tables.Add(dtBGNo);

            }
            catch (Exception err)
            {
                dtMessage.Rows[0][0] = "Exception";
                dtMessage.Rows[0][1] = err.Message;
            }
            return PubUtility.DatasetXML(ds);
        }

        //2021-04-27
        [Route("SystemSetup/GetInittest")]
        public ActionResult SystemSetup_GetInittest()
        {
            UserInfo uu = PubUtility.GetCurrentUser(this);
            System.Data.DataSet ds = PubUtility.GetApiReturn(new string[] { "GetInitGMMacPLUSetOK", "" });
            DataTable dtMessage = ds.Tables["dtMessage"];
            try
            {

            }
            catch (Exception err)
            {
                dtMessage.Rows[0][0] = "Exception";
                dtMessage.Rows[0][1] = err.Message;
            }
            return PubUtility.DatasetXML(ds);
        }

        //2021-04-28
        [Route("SystemSetup/GetInitInv")]
        public ActionResult SystemSetup_GetInitInv()
        {
            UserInfo uu = PubUtility.GetCurrentUser(this);
            System.Data.DataSet ds = PubUtility.GetApiReturn(new string[] { "GetInitInvOK", "" });
            DataTable dtMessage = ds.Tables["dtMessage"];
            try
            {
                string sql = "select * from Layer order by Type_ID";
                //DataTable dtPLU = PubUtility.SqlQry(sql, uu, "SYS");
                //dtPLU.TableName = "dtLayer";
                //ds.Tables.Add(dtPLU);

                sql = "select Distinct WhNo ,WhNo+ST_SName WhName ";
                sql += " from InventorySV a (NoLock) Left Join WarehouseSV b (NoLock) ";
                sql += " On a.WhNo=b.ST_ID And a.CompanyCode=b.CompanyCode ";
                sql += " Where a.CompanyCode='" + uu.CompanyId + "'";
                sql += " Order By WhNo ";

                DataTable dtWh = PubUtility.SqlQry(sql, uu, "SYS");
                dtWh.TableName = "dtInvWh";
                ds.Tables.Add(dtWh);

            }
            catch (Exception err)
            {
                dtMessage.Rows[0][0] = "Exception";
                dtMessage.Rows[0][1] = err.Message;
            }
            return PubUtility.DatasetXML(ds);
        }



        [Route("SystemSetup/SuspendPLU")]
        public ActionResult SystemSetup_SuspendPLU()
        {
            UserInfo uu = PubUtility.GetCurrentUser(this);
            System.Data.DataSet ds = PubUtility.GetApiReturn(new string[] { "SuspendPLUOK", "" });
            DataTable dtMessage = ds.Tables["dtMessage"];
            try
            {
                IFormCollection rq = HttpContext.Request.Form;
                string GD_NO = rq["GD_NO"];
                string SetSuspend = rq["SetSuspend"];
                string sql = "update PLUSV set GD_Flag1='"+ SetSuspend+"'";
                sql += " where CompanyCode='" + uu.CompanyId + "' And GD_NO='" + GD_NO.SqlQuote() + "'";
                PubUtility.ExecuteSql(sql, uu, "SYS");
                sql = "select a.*";
                sql += " ,Case When GD_Flag1='0' Then '未設定' When GD_Flag1='1' Then '啟用' When GD_Flag1='2' Then '停用' End GDStatus ";
                sql += " from PLUSV a";
                sql += " where CompanyCode='" + uu.CompanyId + "' And a.GD_NO='" + GD_NO.SqlQuote() + "'";
                //sql = "select a.*,b.GD_PRICES,b.GD_NAME";
                //sql += " from PLUSVM a";
                //sql += " inner join PLUSV b on a.GD_NO=b.GD_NO";
                //sql += " where b.GD_NO='" + GD_NO.SqlQuote() + "'";
                DataTable dtPLU = PubUtility.SqlQry(sql, uu, "SYS");
                dtPLU.TableName = "dtPLU";
                ds.Tables.Add(dtPLU);

            }
            catch (Exception err)
            {
                dtMessage.Rows[0][0] = "Exception";
                dtMessage.Rows[0][1] = err.Message;
            }
            return PubUtility.DatasetXML(ds);
        }

        [Route("SystemSetup/UpdatePLU")]
        public ActionResult SystemSetup_UpdatePLU()
        {
            UserInfo uu = PubUtility.GetCurrentUser(this);
            System.Data.DataSet ds = PubUtility.GetApiReturn(new string[] { "UpdatePLUOK", "" });
            DataTable dtMessage = ds.Tables["dtMessage"];
            try
            {
                DataTable dtRec = new DataTable("PLUSV");
                PubUtility.AddStringColumns(dtRec, "CompanyCode,GD_NO,GD_Sname,Photo1");
                DataSet dsRQ = new DataSet();
                dsRQ.Tables.Add(dtRec);
                PubUtility.FillDataFromRequest(dsRQ, HttpContext.Request.Form);
                DataRow dr = dtRec.Rows[0];
                dr["CompanyCode"] = uu.CompanyId;
                string sql = "";
                using (DBOperator dbop = new DBOperator())
                {
                    using (System.Transactions.TransactionScope ts = new System.Transactions.TransactionScope(System.Transactions.TransactionScopeOption.Required))
                    {
                        try
                        {
                            sql = "select * from PLUSV where CompanyCode='" + uu.CompanyId + "' And GD_NO='" + dr["GD_NO"].ToString().SqlQuote() + "'";
                            DataTable dtOld = dbop.Query(sql, uu, "SYS");
                            //sql = "update PLUSVM set ";
                            //sql += " GD_Sname='" + dr["GD_Sname"].ToString().SqlQuote() + "'";
                            //sql += ",Photo1='" + dr["Photo1"].ToString().SqlQuote() + "'";
                            //sql += ",Photo2='" + dr["Photo2"].ToString().SqlQuote() + "'";
                            //sql += " where GD_NO='" + dr["GD_NO"].ToString().SqlQuote() + "'";
                            //dbop.ExecuteSql(sql, uu, "SYS");
                            dbop.Update("PLUSV", dtRec, new string[] { "CompanyCode" , "GD_NO" }, uu, "SYS");

                            string OldPhoto1 = dtOld.Rows[0]["Photo1"].ToString();
                            if (OldPhoto1 != "" & OldPhoto1 != dr["Photo1"].ToString())
                            {
                                sql = "delete from ImageTable where SGID='" + OldPhoto1 + "'";
                                dbop.ExecuteSql(sql, uu, "SYS");
                            }
                            //string OldPhoto2 = dtOld.Rows[0]["Photo2"].ToString();
                            //if (OldPhoto2 != "" & OldPhoto2 != dr["Photo2"].ToString())
                            //{
                            //    sql = "delete from ImageTable where SGID='" + OldPhoto2 + "'";
                            //    dbop.ExecuteSql(sql, uu, "SYS");
                            //}

                            sql = "Update PLUSV Set GD_Flag1='1' ";
                            sql += "Where CompanyCode='" + uu.CompanyId + "' And GD_No='" + dr["GD_NO"].ToString().SqlQuote() + "' ";
                            sql += " And GD_Flag1='0' ";
                            dbop.ExecuteSql(sql, uu, "SYS");

                            ts.Complete();
                        }
                        catch (Exception err)
                        {
                            ts.Dispose();
                            dbop.Dispose();
                            throw new Exception(err.Message);
                        }
                        dbop.Dispose();
                    }
                }
                sql = "select a.*";
                sql += " from PLUSV a";
                //sql += " inner join PLU b on a.GD_NO=b.GD_NO";
                sql += " where a.GD_NO='" + dr["GD_NO"].ToString().SqlQuote() + "'";
                DataTable dtPLU = PubUtility.SqlQry(sql, uu, "SYS");
                dtPLU.TableName = "dtPLU";
                ds.Tables.Add(dtPLU);
            }
            catch (Exception err)
            {
                dtMessage.Rows[0][0] = err.Message;
                dtMessage.Rows[0][1] = err.Message;
            }
            return PubUtility.DatasetXML(ds);
        }

        [Route("SystemSetup/SearchPLU")]
        public ActionResult SystemSetup_SearchPLU()
        {
            UserInfo uu = PubUtility.GetCurrentUser(this);
            System.Data.DataSet ds = PubUtility.GetApiReturn(new string[] { "SearchPLUOK", "" });
            DataTable dtMessage = ds.Tables["dtMessage"];
            try
            {
                IFormCollection rq = HttpContext.Request.Form;
                string KeyWord = rq["KeyWord"];
                string GDDept = rq["GDDept"];
                string GDBGNo = rq["GDBGNo"];
                string GDStatus = rq["GDStatus"];
                string sql = "select a.*";
                sql += " ,Case When GD_Flag1='0' Then '未設定' When GD_Flag1='1' Then '啟用' When GD_Flag1='2' Then '停用' End GDStatus ";
                sql += " from PLUSV a";
                //sql += " inner join PLUSV b on a.GD_NO=b.GD_NO";
                sql += " where CompanyCode='" + uu.CompanyId + "' ";
                if (KeyWord != "")
                {
                    sql += " and (";
                    sql += " a.GD_NAME like '%" + KeyWord + "%'";
                    sql += " or a.GD_NO Like '%" + KeyWord + "%'";
                    sql += " or a.GD_Sname Like '%" + KeyWord + "%'";
                    sql += ")";
                }
                if (GDDept != "" & GDDept != null)
                {
                    sql += " and a.GD_Dept='" + GDDept + "'";
                }
                if (GDBGNo != "" & GDBGNo != null)
                {
                    sql += " and a.GD_BGNo='" + GDBGNo + "'";
                }
                if (GDStatus != "" & GDStatus != null)
                {
                    sql += " and a.GD_Flag1='" + GDStatus + "'";
                }
                sql += " Order By GD_No ";

                DataTable dtPLU = PubUtility.SqlQry(sql, uu, "SYS");
                dtPLU.TableName = "dtPLU";
                ds.Tables.Add(dtPLU);
            }
            catch (Exception err)
            {
                dtMessage.Rows[0][0] = "Exception";
                dtMessage.Rows[0][1] = err.Message;
            }
            return PubUtility.DatasetXML(ds);
        }

        [Route("SystemSetup/ImportPLU")]
        public ActionResult SystemSetup_ImportPLU()
        {

            UserInfo uu = PubUtility.GetCurrentUser(this);
            System.Data.DataSet ds = PubUtility.GetApiReturn(new string[] { "ImportPLUOK", "" });
            DataTable dtMessage = ds.Tables["dtMessage"];
            try
            {
                DataTable dtRec = new DataTable("PLUSVM");
                PubUtility.AddStringColumns(dtRec, "GD_NO,GD_Sname,Photo1,Photo2");
                DataSet dsRQ = new DataSet();
                dsRQ.Tables.Add(dtRec);
                PubUtility.FillDataFromRequest(dsRQ, HttpContext.Request.Form);
                DataRow dr = dtRec.Rows[0];
                
                string GD_NO = dtRec.Rows[0]["GD_NO"].ToString();
                string sql = "select * from PLU ";
                sql += " where GD_NO='" + GD_NO.SqlQuote() + "'";
                sql += " and CompanyCode='" + uu.CompanyId + "'";
                DataTable dtPLU = PubUtility.SqlQry(sql, uu, "SYS");
                if (dtPLU.Rows.Count > 0)
                    throw new Exception("這個貨號已經存在,不可匯入");

                DataSet dsP = new DataSet();
                dsP.Tables.Add(dtRec.Copy());
                iXmsApiParameter AP = new iXmsApiParameter();
                AP.user = uu;
                AP.Method = "SystemSetupImportPLU";
                AP.ObjName = "";
                AP.UnknowName = "";
                string strPara = PubUtility.GetSerString(AP, typeof(iXmsApiParameter));
                string url = ConstList.ThisSiteConfig.Companys
                    .Where<Config.Company>(C => C.CompanyID == uu.CompanyId).ToList()[0].iXmsApiUrl + "/SVMApi.aspx";

                Uri aUri = new Uri(url);
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(aUri);
                httpWebRequest.Headers.Add("ParaKey", strPara);
                httpWebRequest.ContentType = "text/xml";
                httpWebRequest.Accept = "text/xml";
                httpWebRequest.Method = "POST";

                dsP.WriteXml(httpWebRequest.GetRequestStream());
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                DataSet dsR = null;
                DataTable dtPLUi = null;
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    dsR = new DataSet();
                    dsR.ReadXml(streamReader);
                    DataTable dtRS = dsR.Tables["dtProcessStatus"];
                    if (dtRS.Rows[0]["Error"].ToString() != "0")
                        throw new Exception(dtRS.Rows[0]["Msg_Code"].ToString());
                    dtPLUi = dsR.Tables["dtPLU"];
                }
                if (dtPLUi.Rows.Count == 0)
                    throw new Exception("這個貨號不存在,不可匯入");

                using (DBOperator dbop = new DBOperator())
                {
                    using (System.Transactions.TransactionScope ts = new System.Transactions.TransactionScope(System.Transactions.TransactionScopeOption.Required))
                    {
                        try
                        {
                            for (int i= dtPLUi.Columns.Count-1; i >-1; i--)
                            {
                                string fname = dtPLUi.Columns[i].ColumnName;
                                if (!dtPLU.Columns.Contains(fname))
                                    dtPLUi.Columns.Remove(fname);
                            }
                            //dbop.BulkCopy("PLU", dtPLUi, uu, "SYS");
                            sql = PubUtility.ConvertInsertSql("PLU", dtPLUi.Rows[0], uu);
                            dbop.ExecuteSql(sql, uu, "SYS");

                            List<string> fds = "CompanyCode,CrtUser,CrtDate,CrtTime,ModUser,ModDate,ModTime".Split(new char[] { ',' }).ToList<string>();
                            foreach (string str in fds)
                                dtRec.Columns.Add(str, typeof(string));
                            sql = PubUtility.ConvertInsertSql("PLUSVM", dtRec.Rows[0], uu);
                            dbop.ExecuteSql(sql, uu, "SYS");
                            ts.Complete();
                        }
                        catch (Exception err)
                        {
                            ts.Dispose();
                            dbop.Dispose();
                            throw err;
                        }
                        dbop.Dispose();
                    }
                }
                sql = "select a.*,b.GD_PRICES,b.GD_NAME";
                sql += " from PLUSVM a";
                sql += " inner join PLU b on a.GD_NO=b.GD_NO";
                sql += " where b.GD_NO='" + dr["GD_NO"].ToString().SqlQuote() + "'";
                dtPLU = PubUtility.SqlQry(sql, uu, "SYS");
                dtPLU.TableName = "dtPLU";
                ds.Tables.Add(dtPLU);
            }
            catch (Exception err)
            {
                dtMessage.Rows[0][0] = "Exception";
                dtMessage.Rows[0][1] = err.Message;
            }
            return PubUtility.DatasetXML(ds);
        }

        [Route("SystemSetup/GetPLUFromIXms")]
        public ActionResult GetPLUFromIXms()
        {
            UserInfo uu = PubUtility.GetCurrentUser(this);
            string term = HttpContext.Request.Form["term"].ToString().SqlQuote();
            string sql = "";
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            if (term != "")
            {
                DataTable dtP = new DataTable("dtPLUTerm");
                dtP.Columns.Add("KeyWord", typeof(string));

                dtP.Rows.Add(new object[] { term });
                DataSet dsP = new DataSet();
                dsP.Tables.Add(dtP);

                iXmsApiParameter AP = new iXmsApiParameter();
                AP.user = uu;
                AP.Method = "GetPLUFromIXmsByKeyWord";
                AP.ObjName = "";
                AP.UnknowName = "";
                string strPara = PubUtility.GetSerString(AP, typeof(iXmsApiParameter));
                string url = ConstList.ThisSiteConfig.Companys
                    .Where<Config.Company>(C => C.CompanyID == uu.CompanyId).ToList()[0].iXmsApiUrl + "/SVMApi.aspx";

                Uri aUri = new Uri(url);
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(aUri);
                httpWebRequest.Headers.Add("ParaKey", strPara);
                httpWebRequest.ContentType = "text/xml";
                httpWebRequest.Accept = "text/xml";
                httpWebRequest.Method = "POST";

                dsP.WriteXml(httpWebRequest.GetRequestStream());
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                DataSet dsR = null;
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    dsR = new DataSet();
                    dsR.ReadXml(streamReader);
                    DataTable dtRS = dsR.Tables["dtProcessStatus"];
                    if (dtRS.Rows[0]["Error"].ToString() != "0")
                        throw new Exception(dtRS.Rows[0]["Msg_Code"].ToString());

                    DataTable dt = dsR.Tables["dtPLU"];
                    if (dt != null)
                    {
                        AutoCompleteDataArray ACDA = new AutoCompleteDataArray();
                        ACDA.list = new List<AutoCompleteData>();
                        foreach (DataRow dr in dt.Rows)
                        {
                            AutoCompleteData acd = new AutoCompleteData();
                            acd.label = dr["GD_NAME"].ToString();
                            acd.value = dr["GD_NO"].ToString();
                            //ACDA.List[dt.Rows.IndexOf(dr)] = acd;
                            ACDA.list.Add(acd);
                        }
                        System.Runtime.Serialization.Json.DataContractJsonSerializer js =
                                    new System.Runtime.Serialization.Json.DataContractJsonSerializer(ACDA.GetType());
                        js.WriteObject(ms, ACDA);
                    }
                }
            }
            string ss = System.Text.Encoding.UTF8.GetString(ms.ToArray());
            return Content(ss, "application/json");
        }


        [Route("SystemSetup/SearchInv")]
        public ActionResult SystemSetup_SearchInv()
        {
            UserInfo uu = PubUtility.GetCurrentUser(this);
            System.Data.DataSet ds = PubUtility.GetApiReturn(new string[] { "SearchInvOK", "" });
            DataTable dtMessage = ds.Tables["dtMessage"];
            try
            {
                IFormCollection rq = HttpContext.Request.Form;
                string KeyWord = rq["KeyWord"];
                string WhNo = rq["WhNo"];
                string CkNo = rq["CkNo"];
                string GDLayer = rq["GDLayer"];
                string sql = "select a.*,b.GD_SNAME,";
                sql += " cast(case when DisPlayNum>0 then Cast(Round(Cast(a.PtNum as numeric(5,1))/Cast(a.DisPlayNum as numeric(5,1))*100,0) As Int) Else 0 End As Varchar(10)) + '%' Share";
                sql += " from InventorySV a";
                sql += " inner join PLUSV b on a.PLU=b.GD_NO And a.CompanyCode=b.CompanyCode";
                sql += " where a.CompanyCode='" + uu.CompanyId + "'";
                if (KeyWord != "")
                {
                    sql += " and (";
                    sql += " b.GD_NAME like '" + KeyWord + "%'";
                    sql += " or b.GD_NO='" + KeyWord + "'";
                    sql += " or b.GD_Sname='" + KeyWord + "'";
                    sql += ")";
                }
                if (WhNo != "")
                {
                    sql += " and a.WhNo='" + WhNo + "'";
                }
                if (GDLayer != "")
                {
                    sql += " and a.Layer='" + GDLayer + "'";
                }
                sql += " Order By a.WhNo, a.CkNo, a.Layer ";
                DataTable dtInv = PubUtility.SqlQry(sql, uu, "SYS");
                dtInv.TableName = "dtInv";
                ds.Tables.Add(dtInv);
            }
            catch (Exception err)
            {
                dtMessage.Rows[0][0] = "Exception";
                dtMessage.Rows[0][1] = err.Message;
            }
            return PubUtility.DatasetXML(ds);
        }


        //2021-05-07
        [Route("SystemSetup/GetWhCkNo")]
        public ActionResult SystemSetup_GetWhCkNo()
        {
            UserInfo uu = PubUtility.GetCurrentUser(this);
            System.Data.DataSet ds = PubUtility.GetApiReturn(new string[] { "GetWhCkNoOK", "" });
            DataTable dtMessage = ds.Tables["dtMessage"];
            try
            {
                IFormCollection rq = HttpContext.Request.Form;
                string WhNo = rq["WhNo"];
                string sql = "select a.CkNo ";
                sql += " from WarehouseDSV a (NoLock) ";
                sql += " where CompanyCode='" + uu.CompanyId + "' and ST_ID='" + WhNo + "'";
                sql += " Order By CkNo ";
                DataTable dtCK = PubUtility.SqlQry(sql, uu, "SYS");
                dtCK.TableName = "dtCK";
                ds.Tables.Add(dtCK);
            }
            catch (Exception err)
            {
                dtMessage.Rows[0][0] = "Exception";
                dtMessage.Rows[0][1] = err.Message;
            }
            return PubUtility.DatasetXML(ds);
        }


        [Route("SystemSetup/GetLayer")]
        public ActionResult SystemSetup_GetLayer()
        {
            UserInfo uu = PubUtility.GetCurrentUser(this);
            System.Data.DataSet ds = PubUtility.GetApiReturn(new string[] { "GetLayerOK", "" });
            DataTable dtMessage = ds.Tables["dtMessage"];
            try
            {
                IFormCollection rq = HttpContext.Request.Form;
                string KeyWord = rq["KeyWord"];
                string sql = "select a.Type_ID,a.Type_Name ";
                sql += " from Layer a";
                sql += " where 1=1";
                //if (KeyWord != "")
                //{
                //sql += " and (";
                //sql += ")";
                //}
                sql += " Order By Type_ID ";
                DataTable dtInv = PubUtility.SqlQry(sql, uu, "SYS");
                dtInv.TableName = "dtLayer";
                ds.Tables.Add(dtInv);
            }
            catch (Exception err)
            {
                dtMessage.Rows[0][0] = "Exception";
                dtMessage.Rows[0][1] = err.Message;
            }
            return PubUtility.DatasetXML(ds);
        }


        private string ImportPLUPic(IFormFileCollection files, string ParaType)
        {
            string sgid = "";
            UserInfo uu = PubUtility.GetCurrentUser(this);
            foreach (IFormFile file in files)
            {
                string filename = file.FileName;
                if (filename.ToLower().IndexOf(".jpg") < 0 & filename.ToLower().IndexOf(".jpeg") < 0)
                {
                    throw new Exception("必須是.jpg檔案!");
                }
                System.IO.MemoryStream ms = new System.IO.MemoryStream();
                file.CopyTo(ms);
                DataTable dtF = new DataTable();
                dtF.Columns.Add("DataType", typeof(string));
                dtF.Columns.Add("FileName", typeof(string));
                dtF.Columns.Add("DocType", typeof(string));
                dtF.Columns.Add("DocImage", typeof(byte[]));

                DataRow drF = dtF.NewRow();
                drF["DataType"] = HttpContext.Request.Form["UploadFileType"];
                drF["FileName"] = file.FileName;
                drF["DocType"] = file.ContentType;
                drF["DocImage"] = ms.ToArray();
                dtF.Rows.Add(drF);
                sgid = PubUtility.AddTable("ImageTable", dtF, uu, "SYS");
            }
            return sgid;
        }


        //2021-05-07 Larry
        [Route("SystemSetup/GetInitVMN29")]
        public ActionResult SystemSetup_GetInitVMN29()
        {
            UserInfo uu = PubUtility.GetCurrentUser(this);
            System.Data.DataSet ds = PubUtility.GetApiReturn(new string[] { "GetInitVMN29OK", "" });
            DataTable dtMessage = ds.Tables["dtMessage"];
            try
            {

            }
            catch (Exception err)
            {
                dtMessage.Rows[0][0] = "Exception";
                dtMessage.Rows[0][1] = err.Message;
            }
            return PubUtility.DatasetXML(ds);
        }


        //2021-05-07 Larry
        [Route("SystemSetup/GetRack")]
        public ActionResult SystemSetup_GetRack()
        {
            UserInfo uu = PubUtility.GetCurrentUser(this);
            System.Data.DataSet ds = PubUtility.GetApiReturn(new string[] { "GetRackOK", "" });
            DataTable dtMessage = ds.Tables["dtMessage"];
            try
            {
                string sql = "select * from Rack Where CompanyCode='" + uu.CompanyId + "' order by Type_ID";

                DataTable dtRack = PubUtility.SqlQry(sql, uu, "SYS");
                dtRack.TableName = "dtRack";
                ds.Tables.Add(dtRack);

            }
            catch (Exception err)
            {
                dtMessage.Rows[0][0] = "Exception";
                dtMessage.Rows[0][1] = err.Message;
            }
            return PubUtility.DatasetXML(ds);
        }

        //2021-05-07 Larry
        [Route("SystemSetup/ChkRackUsed")]
        public ActionResult SystemSetup_ChkRackUsed()
        {
            UserInfo uu = PubUtility.GetCurrentUser(this);
            System.Data.DataSet ds = PubUtility.GetApiReturn(new string[] { "ChkRackUsedOK", "" });
            DataTable dtMessage = ds.Tables["dtMessage"];
            try
            {
                IFormCollection rq = HttpContext.Request.Form;
                string Type_ID = rq["Type_ID"];
                string sql = "select Distinct ChannelType from MachineListSpec Where CompanyCode='" + uu.CompanyId + "' And ChannelType='" + Type_ID + "'";

                DataTable dtRack = PubUtility.SqlQry(sql, uu, "SYS");
                dtRack.TableName = "dtRack";
                ds.Tables.Add(dtRack);

            }
            catch (Exception err)
            {
                dtMessage.Rows[0][0] = "Exception";
                dtMessage.Rows[0][1] = err.Message;
            }
            return PubUtility.DatasetXML(ds);
        }


        //2021-05-07 Larry
        [Route("SystemSetup/ChkRackExist")]
        public ActionResult SystemSetup_ChkRackExist()
        {
            UserInfo uu = PubUtility.GetCurrentUser(this);
            System.Data.DataSet ds = PubUtility.GetApiReturn(new string[] { "ChkRackUsedOK", "" });
            DataTable dtMessage = ds.Tables["dtMessage"];
            try
            {
                IFormCollection rq = HttpContext.Request.Form;
                string Type_ID = rq["Type_ID"];
                string sql = "select * from Rack Where CompanyCode='" + uu.CompanyId + "' And Type_ID='" + Type_ID + "'";

                DataTable dtRack = PubUtility.SqlQry(sql, uu, "SYS");
                dtRack.TableName = "dtRack";
                ds.Tables.Add(dtRack);

            }
            catch (Exception err)
            {
                dtMessage.Rows[0][0] = "Exception";
                dtMessage.Rows[0][1] = err.Message;
            }
            return PubUtility.DatasetXML(ds);
        }




        //2021-05-07 Larry
        [Route("SystemSetup/UpdateRack")]
        public ActionResult SystemSetup_UpdateRack()
        {
            UserInfo uu = PubUtility.GetCurrentUser(this);
            System.Data.DataSet ds = PubUtility.GetApiReturn(new string[] { "UpdateRackOK", "" });
            DataTable dtMessage = ds.Tables["dtMessage"];
            try
            {
                DataTable dtRec = new DataTable("Rack");
                PubUtility.AddStringColumns(dtRec, "OldType_ID,Type_ID,Type_Name,DisplayNum");
                DataSet dsRQ = new DataSet();
                dsRQ.Tables.Add(dtRec);
                PubUtility.FillDataFromRequest(dsRQ, HttpContext.Request.Form);
                DataRow dr = dtRec.Rows[0];

                string sql = "";
                using (DBOperator dbop = new DBOperator())
                {
                    using (System.Transactions.TransactionScope ts = new System.Transactions.TransactionScope(System.Transactions.TransactionScopeOption.Required))
                    {
                        try
                        {
                            if (dr["Type_ID"].ToString() == dr["OldType_ID"].ToString())
                            {
                                sql = "update Rack set ";
                                sql += " Type_Name='" + dr["Type_Name"].ToString().SqlQuote() + "'";
                                sql += ",DisplayNum=" + dr["DisplayNum"].ToString().SqlQuote() + "";
                                sql += ",ModDate=convert(char(10),getdate(),111)";
                                sql += ",ModTime=convert(char(12),getdate(),108)";
                                sql += ",ModUser='" + uu.UserID + "'";
                                sql += " where CompanyCode='" + uu.CompanyId + "' And Type_ID='" + dr["Type_ID"].ToString().SqlQuote() + "'";
                                dbop.ExecuteSql(sql, uu, "SYS");
                            }
                            else
                            {
                                sql = "update Rack set ";
                                sql += " Type_ID='" + dr["Type_ID"].ToString().SqlQuote() + "'";
                                sql += " ,Type_Name='" + dr["Type_Name"].ToString().SqlQuote() + "'";
                                sql += " ,DisplayNum=" + dr["DisplayNum"].ToString().SqlQuote() + "";
                                sql += " ,ModDate=convert(char(10),getdate(),111)";
                                sql += " ,ModTime=convert(char(12),getdate(),108)";
                                sql += " ,ModUser='" + uu.UserID + "'";
                                sql += " where CompanyCode='" + uu.CompanyId + "' And Type_ID='" + dr["OldType_ID"].ToString().SqlQuote() + "'";
                                dbop.ExecuteSql(sql, uu, "SYS");
                            }
                            //dbop.Update("Rack", dtRec, new string[] { "Type_ID" }, uu, "SYS");

                            ts.Complete();
                        }
                        catch (Exception err)
                        {
                            ts.Dispose();
                            dbop.Dispose();
                            throw new Exception(err.Message);
                        }
                        dbop.Dispose();
                    }
                }
                sql = "select a.*";
                sql += " from Rack a";
                sql += " where a.CompanyCode='" + uu.CompanyId + "' And a.Type_ID='" + dr["Type_ID"].ToString().SqlQuote() + "'";
                DataTable dtRack = PubUtility.SqlQry(sql, uu, "SYS");
                dtRack.TableName = "dtRack";
                ds.Tables.Add(dtRack);
            }
            catch (Exception err)
            {
                dtMessage.Rows[0][0] = "Exception";
                dtMessage.Rows[0][1] = err.Message;
            }
            return PubUtility.DatasetXML(ds);
        }


        //2021-05-07 Larry
        [Route("SystemSetup/AddRack")]
        public ActionResult SystemSetup_AddRack()
        {
            UserInfo uu = PubUtility.GetCurrentUser(this);
            System.Data.DataSet ds = PubUtility.GetApiReturn(new string[] { "AddRackOK", "" });
            DataTable dtMessage = ds.Tables["dtMessage"];
            try
            {
                DataTable dtRec = new DataTable("Rack");
                PubUtility.AddStringColumns(dtRec, "Type_ID,Type_Name,DisplayNum");
                DataSet dsRQ = new DataSet();
                dsRQ.Tables.Add(dtRec);
                PubUtility.FillDataFromRequest(dsRQ, HttpContext.Request.Form);
                DataRow dr = dtRec.Rows[0];

                string sql = "";
                using (DBOperator dbop = new DBOperator())
                {
                    using (System.Transactions.TransactionScope ts = new System.Transactions.TransactionScope(System.Transactions.TransactionScopeOption.Required))
                    {
                        try
                        {
                            //sql = "Insert Into Rack (CompanyCode, CrtUser, CrtDate, CrtTime ";
                            //sql += " ,ModUser, ModDate, ModTime";
                            //sql += ", Type_ID, Type_Name, DisplayNum) Values ";
                            //sql += " ('" + uu.CompanyId + "', '" + uu.UserID + "', convert(char(10),getdate(),111), convert(char(12),getdate(),108) ";
                            //sql += " ,'" + uu.UserID + "',convert(char(10),getdate(),111), convert(char(12),getdate(),108) ";
                            //sql += " ,'" + dr["Type_ID"].ToString().SqlQuote() + "','" + dr["Type_Name"].ToString().SqlQuote() + "'," + dr["DisplayNum"].ToString().SqlQuote() + ")";
                            //dbop.ExecuteSql(sql, uu, "SYS");
                            dbop.Add("Rack", dtRec, uu, "SYS");
                            ts.Complete();
                        }
                        catch (Exception err)
                        {
                            ts.Dispose();
                            dbop.Dispose();
                            throw new Exception(err.Message);
                        }
                        dbop.Dispose();
                    }
                }
                sql = "select a.*";
                sql += " from Rack a";
                sql += " where a.CompanyCode='" + uu.CompanyId + "' And Type_ID='" + dr["Type_ID"].ToString().SqlQuote() + "'";
                DataTable dtRack = PubUtility.SqlQry(sql, uu, "SYS");
                dtRack.TableName = "dtRack";
                ds.Tables.Add(dtRack);
            }
            catch (Exception err)
            {
                dtMessage.Rows[0][0] = "Exception";
                dtMessage.Rows[0][1] = err.Message;
            }
            return PubUtility.DatasetXML(ds);
        }


        //2021-05-07 Larry
        [Route("SystemSetup/DelRack")]
        public ActionResult SystemSetup_DelRack()
        {
            UserInfo uu = PubUtility.GetCurrentUser(this);
            System.Data.DataSet ds = PubUtility.GetApiReturn(new string[] { "DelRackOK", "" });
            DataTable dtMessage = ds.Tables["dtMessage"];
            try
            {
                DataTable dtRec = new DataTable("Rack");
                PubUtility.AddStringColumns(dtRec, "Type_ID");
                DataSet dsRQ = new DataSet();
                dsRQ.Tables.Add(dtRec);
                PubUtility.FillDataFromRequest(dsRQ, HttpContext.Request.Form);
                DataRow dr = dtRec.Rows[0];

                string sql = "";
                using (DBOperator dbop = new DBOperator())
                {
                    using (System.Transactions.TransactionScope ts = new System.Transactions.TransactionScope(System.Transactions.TransactionScopeOption.Required))
                    {
                        try
                        {
                            sql = "Delete From Rack ";
                            sql += " where CompanyCode='" + uu.CompanyId + "' And Type_ID='" + dr["Type_ID"].ToString().SqlQuote() + "'";
                            dbop.ExecuteSql(sql, uu, "SYS");

                            ts.Complete();
                        }
                        catch (Exception err)
                        {
                            ts.Dispose();
                            dbop.Dispose();
                            throw new Exception(err.Message);
                        }
                        dbop.Dispose();
                    }
                }
                sql = "select a.*";
                sql += " from Rack a";
                sql += " where a.CompanyCode='" + uu.CompanyId + "' ";
                DataTable dtRack = PubUtility.SqlQry(sql, uu, "SYS");
                dtRack.TableName = "dtRack";
                ds.Tables.Add(dtRack);
            }
            catch (Exception err)
            {
                dtMessage.Rows[0][0] = "Exception";
                dtMessage.Rows[0][1] = err.Message;
            }
            return PubUtility.DatasetXML(ds);
        }


        //2021-05-18 Larry
        [Route("SystemSetup/GetInitVXT03")]
        public ActionResult SystemSetup_GetInitVXT03()
        {
            UserInfo uu = PubUtility.GetCurrentUser(this);
            System.Data.DataSet ds = PubUtility.GetApiReturn(new string[] { "GetInitVXT03OK", "" });
            DataTable dtMessage = ds.Tables["dtMessage"];
            try
            {

                string sql = "select ST_ID ,ST_ID+ST_SName STName ";
                sql += " from WarehouseSV (NoLock) ";
                sql += " Where CompanyCode='" + uu.CompanyId + "' And ST_Type ='6'";
                sql += " Order By ST_ID ";

                DataTable dtWh = PubUtility.SqlQry(sql, uu, "SYS");
                dtWh.TableName = "dtWh";
                ds.Tables.Add(dtWh);


                sql = "select ST_ID ,ST_ID+ST_SName STName ";
                sql += " from WarehouseSV (NoLock) ";
                sql += " Where CompanyCode='" + uu.CompanyId + "' And ST_Type Not In ('2','3','6')";
                sql += " Order By ST_ID ";

                DataTable dtS = PubUtility.SqlQry(sql, uu, "SYS");
                dtS.TableName = "dtS";
                ds.Tables.Add(dtS);

            }
            catch (Exception err)
            {
                dtMessage.Rows[0][0] = "Exception";
                dtMessage.Rows[0][1] = err.Message;
            }
            return PubUtility.DatasetXML(ds);
        }


        //2021-05-18 Larry
        [Route("SystemSetup/SearchVXT03")]
        public ActionResult SystemSetup_SearchVXT03()
        {
            UserInfo uu = PubUtility.GetCurrentUser(this);
            System.Data.DataSet ds = PubUtility.GetApiReturn(new string[] { "SearchVXT03OK", "" });
            DataTable dtMessage = ds.Tables["dtMessage"];
            try
            {
                IFormCollection rq = HttpContext.Request.Form;

                string WhNo = rq["WhNo"];
                string CkNo = rq["CkNo"];
                string SWhNo = rq["SWhNo"];
                string VMStatus = rq["VMStatus"];
                string NetStatus = rq["NetStatus"];

                string sql = "select a.*,c.ST_SNAME+b.ckno+'機' VMName,b.ST_ID,b.ckno,d.ST_SName SWhName";
                sql += " from MachineList a";
                sql += " inner join WarehouseDSV b on a.SNNo=b.SNNo And a.CompanyCode=b.CompanyCode";
                sql += " inner join WarehouseSV c on b.ST_ID=c.ST_ID And b.CompanyCode=c.CompanyCode";
                sql += " inner join WarehouseSV d on b.WhNoIn=d.ST_ID And b.CompanyCode=d.CompanyCode";
                sql += " where a.CompanyCode='" + uu.CompanyId + "'";
                if (WhNo != "")
                {
                    sql += " and b.ST_ID='" + WhNo + "'";
                }
                if (CkNo != "")
                {
                    sql += " and b.CkNo='" + CkNo + "'";
                }
                if (SWhNo != "")
                {
                    sql += " and b.WhNoIn='" + SWhNo + "'";
                }
                if (VMStatus != "")
                {
                    sql += " and a.FlagUse='" + VMStatus + "'";
                }
                if (NetStatus != "")
                {
                    sql += " and a.FlagNet='" + NetStatus + "'";
                }
                sql += " Order By a.SNNo ";
                DataTable dtInv = PubUtility.SqlQry(sql, uu, "SYS");
                dtInv.TableName = "dtInv";
                ds.Tables.Add(dtInv);
            }
            catch (Exception err)
            {
                dtMessage.Rows[0][0] = "Exception";
                dtMessage.Rows[0][1] = err.Message;
            }
            return PubUtility.DatasetXML(ds);
        }


        //2021-05-18
        [Route("SystemSetup/GetWhDSVCkNo")]
        public ActionResult SystemSetup_GetWhDSVCkNo()
        {
            UserInfo uu = PubUtility.GetCurrentUser(this);
            System.Data.DataSet ds = PubUtility.GetApiReturn(new string[] { "GetWhDSVCkNoOK", "" });
            DataTable dtMessage = ds.Tables["dtMessage"];
            try
            {
                IFormCollection rq = HttpContext.Request.Form;
                string WhNo = rq["WhNo"];
                string sql = "select a.CkNo ";
                sql += " from WarehouseDSV a (NoLock) ";
                sql += " where CompanyCode='" + uu.CompanyId + "' and ST_ID='" + WhNo + "'";
                sql += " Order By CkNo ";
                DataTable dtCK = PubUtility.SqlQry(sql, uu, "SYS");
                dtCK.TableName = "dtCK";
                ds.Tables.Add(dtCK);
            }
            catch (Exception err)
            {
                dtMessage.Rows[0][0] = "Exception";
                dtMessage.Rows[0][1] = err.Message;
            }
            return PubUtility.DatasetXML(ds);
        }









    }
}
