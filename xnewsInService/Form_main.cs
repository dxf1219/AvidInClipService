using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using Oracle.DataAccess.Client;
using System.Collections;
using System.Web;
using StackExchange.Redis;

namespace xnewsInService
{
    public partial class Form_main : Form
    {
        public Form_main()
        {
            InitializeComponent();

            logpath = Application.StartupPath + "\\log";
            if (!System.IO.Directory.Exists(logpath))
            {
                Directory.CreateDirectory(logpath);
            }
            string dbdir = Application.StartupPath + "\\DBTemp\\";
            if (!System.IO.Directory.Exists(dbdir))
            {
                Directory.CreateDirectory(dbdir);
            }

            try
            {
                redis = ConnectionMultiplexer.Connect(Properties.Settings.Default.redisconstring);
                CommonTools.writeLog("redis连接成功!" + Properties.Settings.Default.redisconstring, logpath, "info");
            }
            catch (Exception ee)
            {
                CommonTools.writeLog("redis连接失败!" + Properties.Settings.Default.redisconstring + ee.ToString(), logpath, "error");
                MessageBox.Show("redis连接失败!" + Properties.Settings.Default.redisconstring + ee.ToString());
                //return;
            }


            timer1.Enabled = false;
            timer1.Interval = 3000;
            toolStripStatusLabel3.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            CommonTools.writeLog("软件启动!",logpath,"info");
            this.Text = Properties.Settings.Default.AppTitle;

            SetText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + "软件启动!\n");
            readPathInfo();
            timer1.Enabled = true;
            scanxmlThread = new Thread(new ThreadStart(scanXMLFilesThread));
            scanxmlThread.IsBackground = true;
            scanxmlThread.Start();

        }
        //消息框代理
        private delegate void SetTextCallback(string text);
        private delegate void SetSelectCallback(object Msge);

        private void SetText(string tt)
        {
            string text = tt;
            try
            {
                if (this.richTextBox1.InvokeRequired)
                {
                    SetTextCallback d = new SetTextCallback(SetText);
                    this.Invoke(d, new object[] { text });
                }
                else
                {
                    if (this.richTextBox1.Lines.Length < 10000 )
                    {
                        this.richTextBox1.AppendText(text);
                        of_SetRichCursor(richTextBox1);
                    }
                    else
                    {
                        this.richTextBox1.Clear();
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private void of_SetRichCursor(object msge)
        {
            try
            {
                RichTextBox richbox = (RichTextBox)msge;
                //设置光标的位置到文本尾
                if (richbox.InvokeRequired)
                {
                    SetSelectCallback d = new SetSelectCallback(of_SetRichCursor);
                    this.Invoke(d, new object[] { msge });
                }
                else
                {
                    richbox.Select(richbox.TextLength, 0);
                    //滚动到控件光标处
                    richbox.ScrollToCaret();
                }
            }
            catch (Exception)
            {
            }
        }

        //变量定义
        private string logpath = "";

        private Thread scanxmlThread=null;

        private Hashtable htpaths = null;

        private string pathid="ddtest001default";

        private ConnectionMultiplexer redis;

        public bool IsFileInUse(string fileName)
        {
            bool inUse = true;
            FileStream fs = null;
            try
            {
                fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.None);
                inUse = false;
            }
            catch
            {
            }
            finally
            {
                if (fs != null)

                    fs.Close();
            }
            return inUse;//true表示正在使用,false没有使用  
        }

        private void scanXMLFilesThread()
        {
            while (true)
            {
                try
                {
                    IDatabase db = redis.GetDatabase();
                    string key = this.Text + "live";
                    string value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    db.StringSet(key, value);
                }
                catch (Exception ee)
                {
                    CommonTools.writeLog("redis 写入key value 异常!" + ee.ToString(), logpath, "error");
                }

                string[] mediaMd5files = Directory.GetFiles(Properties.Settings.Default.scanstpFilePath, "*.md5sum",SearchOption.TopDirectoryOnly);  //扫描arcstp打包完成后的素材目录
                //判断文件素材是否已经做过
                foreach(string mediaMd5file in mediaMd5files)
                {
                    string mediafile = Properties.Settings.Default.scanstpFilePath + "\\" + Path.GetFileNameWithoutExtension(mediaMd5file);
                    if (!File.Exists(mediafile))
                    {
                        mediafile = mediafile + ".mxf";
                    }
                    try
                    {
                        FileInfo fimedia = new FileInfo(mediafile);
                        string extens = fimedia.Extension;
                        if (!extens.ToLower().Equals(Properties.Settings.Default.fileExtension))  //非mxf文件
                        {
                            CommonTools.writeLog("非avid mxf文件 不处理!" + mediafile, logpath, "info");
                            SetText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + "非avid mxf文件 不处理:" + Path.GetFileName(mediafile) + "\n");
                            continue;
                        }
                        if (IsFileInUse(mediafile))
                        {
                            continue;
                            //该文件正在被使用
                        }
                        else //不再被使用
                        {
                            CommonTools.writeLog("开始MediaManager--avidin流程:" + mediafile, logpath, "info");
                            SetText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + "开始开始MediaManager---avidin流程:" + Path.GetFileName(mediafile) + "\n");

                            //生成count文件
                            if (!Directory.Exists(Application.StartupPath + "\\counts"))
                            {
                                Directory.CreateDirectory(Application.StartupPath + "\\counts");
                            }
                            string newcountfile = Application.StartupPath + "\\counts\\" + Path.GetFileNameWithoutExtension(mediafile) + "count.xml";
                            try
                            {
                                if (!File.Exists(newcountfile))
                                {
                                    File.Copy(Application.StartupPath + "\\counts.xml", newcountfile, true);
                                }

                                XmlDocument doc = new XmlDocument();
                                doc.Load(newcountfile);
                                System.Xml.XmlElement root = doc.DocumentElement;

                                XmlNode countnode = root.SelectSingleNode("/root/counts");
                                string nowcount = countnode.InnerText;
                                int newcount = Convert.ToInt32(nowcount) + 1;
                                countnode.InnerText = newcount.ToString();
                                doc.Save(newcountfile);
                                if (newcount > Properties.Settings.Default.errorRetryCounts)
                                {
                                    //对文件进行重命名
                                    string newerrorfile = Properties.Settings.Default.scanstpFilePath + "\\" + Path.GetFileName(mediafile) + ".error";
                                    if (!File.Exists(newerrorfile))
                                    {
                                        try
                                        {
                                            File.Move(mediafile, newerrorfile);
                                            CommonTools.writeLog("对素材进行出错处理:" + newerrorfile, logpath, "info");
                                            SetText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + "对素材进行出错处理:" + Path.GetFileName(mediafile) + "\n");
                                        }
                                        catch (Exception ee)
                                        {
                                            CommonTools.writeLog("对素材进行出错处理失败:" + newerrorfile + ee.ToString(), logpath, "error");
                                        }
                                    }
                                    continue;
                                } //超过重试次数
                            }
                            catch (Exception ee)
                            {
                                CommonTools.writeLog("生成文件统计失败!" + ee.ToString(), logpath, "error");
                                SetText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + "生成文件统计失败!" + "\n");
                                continue;
                            }

                            string mxffilename = replaceSpecialSQLSyntax(mediafile);
                            mxffilename = mxffilename.Replace(" ", "");
                            if (mediafile.Equals(mxffilename))
                            {
                                //
                            }
                            else  //mpg文件 需要改名
                            {
                                try
                                {
                                    File.Move(mediafile, mxffilename);
                                    CommonTools.writeLog("素材修改名称成功!" + mxffilename, logpath, "info");
                                    SetText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + "素材修改名称成功:" + Path.GetFileName(mxffilename) + "\n");
                                }
                                catch (Exception ee)
                                {
                                    CommonTools.writeLog("素材修改名称失败!" + Path.GetFileName(mxffilename) + ee.ToString(), logpath, "info");
                                    continue;
                                }
                            }
                          
                            //从节目名称中获取节目ID 从oracle中获取节目ID 
                            string hrfile01 = Path.GetFileNameWithoutExtension(mediafile);

                            string getprogramid = "";
                            int findhr = hrfile01.IndexOf("_HR_HD");
                            try
                            {
                                if (findhr > 20)  //往前数20个字段获取节目ID
                                {
                                    getprogramid = hrfile01.Substring(findhr - 20, 20);
                                }
                            }
                            catch (Exception ee)
                            {
                                CommonTools.writeLog("获取节目ID异常：" + hrfile01 + " " + ee.ToString(), logpath, "error");
                                SetText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + "获取节目ID异常：" + hrfile01 + "\n");
                            }

                            string programID = getprogramid;
                            CommonTools.writeLog("获取节目ID：" + programID, logpath, "info");

                            XnewsInfo xinfo = new XnewsInfo();
                            xinfo.ChannelPath = Properties.Settings.Default.site;
                            xinfo.ProgramID = programID;
                            xinfo.MediafilePath = mxffilename;
                            xinfo.Mediafilename = Path.GetFileName(mxffilename);
                            xinfo.Creator = "mmadmin";
                           
                            if (programID.Length == 0)
                            {
                                CommonTools.writeLog("获取节目ID为空，为非MediaMangerSTP生成的文件!" + hrfile01, logpath, "error");
                                xinfo.ProgramID = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                                xinfo.Title = Path.GetFileNameWithoutExtension(mediafile); 
                                //直接复制mxf到内网 并生成videoxml
                                //复制视频文件
                                string destvideo = Properties.Settings.Default.destVideoPath + "\\" + Path.GetFileName(xinfo.MediafilePath);
                                try
                                {
                                    CommonTools.writeLog("开始复制视频文件:" + destvideo, logpath, "info");
                                    SetText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + "开始复制视频文件:" + destvideo + "\n");
                                    File.Copy(xinfo.MediafilePath, destvideo, true);
                                    CommonTools.writeLog("复制视频文件成功:" + destvideo, logpath, "info");
                                    SetText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + "复制视频文件成功:" + destvideo + "\n");
                                    //复制视频文件xml 
                                    //生成导入的xml文件
                                    createVideoInfo(xinfo);
                          
                                }
                                catch (Exception ee)
                                {
                                    CommonTools.writeLog("处理非MediaMangerSTP生成的文件流程异常!" + destvideo + ee.ToString(), logpath, "error");
                                }
                                continue;
                            }

                            string scripPath = Properties.Settings.Default.scanScripPath + "\\" + programID;

                            //查询oracle数据库 根据节目ID查询目录
                            #region oracle 数据库查询
                            OracleConnection conn = new OracleConnection(Properties.Settings.Default.oracleConn);
                            try
                            {
                                conn.Open();
                                string sql = "SELECT MEDIADIRID ,MEDIACREATER  FROM TPROGRAMMEDIA WHERE (MEDIAID ='" + programID + "')";
                                OracleCommand cmd = new OracleCommand(sql, conn);
                                OracleDataReader dr = cmd.ExecuteReader();

                                if (dr.HasRows)
                                {
                                    dr.Read();
                                    pathid = dr[0].ToString();
                                    xinfo.Creator = dr[1].ToString();
                                }
                                dr.Close();
                            }
                            catch (Exception ee)
                            {
                                CommonTools.writeLog("数据库异常:" + ee.ToString(), logpath, "error");
                            }
                            #endregion

                            if (!Directory.Exists(scripPath))
                            {
                                CommonTools.writeLog("获取文稿信息目录不存在!" + scripPath, logpath, "error");
                            }
                            else
                            {
                                string localdescpath = Application.StartupPath + "\\DBTemp\\" + programID + "_DescData.xml";
                                File.Copy(scripPath + "\\DescData.xml", localdescpath, true);
                                CommonTools.writeLog("复制描述信息xml到本地:" + localdescpath, logpath, "info");
                                SetText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + "复制描述信息xml到本地:" + Path.GetFileName(localdescpath) + "\n");
                                XmlDocument xDoc = new XmlDocument();
                                xDoc.Load(localdescpath);

                                XmlNodeList lxDescItemNodeList = xDoc.SelectNodes("//DescItem");

                                #region 读取文稿xml信息
                                foreach (XmlNode lxDescItemNode in lxDescItemNodeList)
                                {
                                    if (lxDescItemNode.Attributes["EName"].Value.Equals("title"))
                                    {
                                        xinfo.Title = lxDescItemNode.FirstChild.NextSibling.InnerText;
                                    }
                                    if (lxDescItemNode.Attributes["EName"].Value.Equals("writer"))
                                    {
                                        xinfo.Author = lxDescItemNode.FirstChild.NextSibling.InnerText;
                                    }
                                    if (lxDescItemNode.Attributes["EName"].Value.Equals("txt"))
                                    {
                                        xinfo.Texts = lxDescItemNode.FirstChild.NextSibling.InnerText;
                                    }
                                    if (lxDescItemNode.Attributes["EName"].Value.Equals("v_bumen"))
                                    {
                                        xinfo.Vbumen = lxDescItemNode.FirstChild.NextSibling.InnerText;
                                    }
                                    if (lxDescItemNode.Attributes["EName"].Value.Equals("channel"))
                                    {
                                        xinfo.ChannelPath = lxDescItemNode.FirstChild.NextSibling.InnerText;
                                    }
                                    if (lxDescItemNode.Attributes["EName"].Value.Equals("platform"))
                                    {
                                        xinfo.PlatForm = lxDescItemNode.FirstChild.NextSibling.InnerText;
                                    }
                                    if (lxDescItemNode.Attributes["EName"].Value.Equals("site"))
                                    {
                                        xinfo.Sites = lxDescItemNode.FirstChild.NextSibling.InnerText;
                                    }
                                }//foreach
                                #endregion

                            } //文稿目录存在 

                            int result = 0;
                            //生成导入内网的xml 
                            result = createAvidInfo(xinfo);
                            if (result == 0)
                            {
                                //移动完成目录
                                //更新数据库 移动到其它目录中去  AVID发布完成  20160725102749410810
                                string destpath = Properties.Settings.Default.destMediaManagerPathID;
                                string MEDIADIRIDNew = "";
                                try
                                {
                                    MEDIADIRIDNew = destpath;
                                }
                                catch (Exception ee)
                                {
                                    CommonTools.writeLog("获取发布完成目录异常!" + ee.ToString(), logpath, "error");
                                }

                                if (conn.State == ConnectionState.Closed)
                                {
                                    try
                                    {
                                        conn.Open();
                                        CommonTools.writeLog("数据库处于关闭状态!重新打开!", logpath, "info");
                                    }
                                    catch (Exception ee)
                                    {
                                        CommonTools.writeLog("数据库处于关闭状态!重新打开异常:" + ee.ToString(), logpath, "error");
                                    }
                                }
                                string updatesql = "";
                                if (!string.IsNullOrEmpty(MEDIADIRIDNew))
                                {
                                    updatesql = " update TPROGRAMMEDIA set  MEDIADIRID='" + MEDIADIRIDNew + "' where MEDIAID = '" + programID + "' ";
                                    OracleCommand cmdupdate = new OracleCommand(updatesql, conn);
                                    try
                                    {
                                        int resultupdate = cmdupdate.ExecuteNonQuery();
                                        if (resultupdate > 0)
                                        {
                                            CommonTools.writeLog("移动到发布目录成功!", logpath, "info");
                                        }
                                        else
                                        {
                                            string delsql = " delete TPROGRAMMEDIA  where MEDIAID = '" + programID + "' ";
                                            OracleCommand cmddel = new OracleCommand(delsql, conn);
                                            try
                                            {
                                                int resultdel = cmddel.ExecuteNonQuery();
                                                CommonTools.writeLog("删除原目录下的打包记录成功!" + resultdel.ToString(), logpath, "info");
                                            }
                                            catch (Exception ee)
                                            {
                                                CommonTools.writeLog("删除原目录下的打包记录异常:" + delsql + ee.ToString(), logpath, "error");
                                            }

                                        }  //else 更新失败
                                    }
                                    catch (Exception ee)
                                    {
                                        CommonTools.writeLog("update异常:" + updatesql + ee.ToString(), logpath, "error");
                                    }
                                }
                                else
                                {
                                    string delsql = " delete TPROGRAMMEDIA  where MEDIAID = '" + programID + "' ";
                                    OracleCommand cmddel = new OracleCommand(delsql, conn);
                                    try
                                    {
                                        int resultdel = cmddel.ExecuteNonQuery();
                                        CommonTools.writeLog("删除原目录下的打包记录成功!" + resultdel.ToString(), logpath, "info");
                                    }
                                    catch (Exception ee)
                                    {
                                        CommonTools.writeLog("删除原目录下的打包记录异常!" + delsql + ee.ToString(), logpath, "error");
                                    }
                                }
                                try
                                {
                                    conn.Close();
                                }
                                catch (Exception ee)
                                {
                                    CommonTools.writeLog("数据库关闭异常:" + ee.ToString(), logpath, "error");
                                }


                                File.Delete(mxffilename);
                                CommonTools.writeLog("删除文件:" + mxffilename, logpath, "info");

                                File.Delete(mediaMd5file);
                                CommonTools.writeLog("删除md5文件:" + mediaMd5file, logpath, "info");
                                SetText("\n");
                            }
                            Thread.Sleep(10);
                        } //else //不再被使用
                    } 
                    catch (Exception ee)
                    {
                        CommonTools.writeLog("redis 写入key value 异常!" + ee.ToString(), logpath, "error");
                    }


                }//foreach(string mediafile in mediafiles)
                System.Threading.Thread.Sleep(Properties.Settings.Default.ScanXmlInterval);
            }
        }

        private int createVideoInfo(XnewsInfo xinfo)
        {
            try
            {
                string xmlPath = Application.StartupPath + "\\videoinfo";
                if (!Directory.Exists(xmlPath))
                {
                    Directory.CreateDirectory(xmlPath);
                }
                string xmlfile = xmlPath + "\\" + xinfo.ProgramID + ".xml";
                //生成导入的xml文件
                XmlDocument xmlDoc = new XmlDocument();
                //创建Xml声明部分，即<?xml version="1.0" encoding="utf-8" ?>
                XmlDeclaration Declaration = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", null);

                //创建根节点
                XmlNode rootNode = xmlDoc.CreateElement("root");

                XmlNode filePathdNode = xmlDoc.CreateElement("filePath");
                filePathdNode.InnerText = Properties.Settings.Default.destVideoPath;
                rootNode.AppendChild(filePathdNode);

                XmlNode fileNameNode = xmlDoc.CreateElement("fileName");
                fileNameNode.InnerText = xinfo.Mediafilename;
                rootNode.AppendChild(fileNameNode);

                XmlNode newNameNode = xmlDoc.CreateElement("newName");
                newNameNode.InnerText = xinfo.Title;
                rootNode.AppendChild(newNameNode);

                XmlNode srcXmlPathNode = xmlDoc.CreateElement("srcXmlPath");
                srcXmlPathNode.InnerText = Properties.Settings.Default.destScriptPath;
                rootNode.AppendChild(srcXmlPathNode);

                XmlNode srcXmlNameNode = xmlDoc.CreateElement("srcXmlName");
                srcXmlNameNode.InnerText = "1111.xml";
                rootNode.AppendChild(srcXmlNameNode);

                //Supplier
                XmlNode SupplierNode = xmlDoc.CreateElement("Supplier");
                SupplierNode.InnerText = Properties.Settings.Default.site;
                rootNode.AppendChild(SupplierNode);

                //附加根节点
                xmlDoc.AppendChild(rootNode);
                xmlDoc.InsertBefore(Declaration, xmlDoc.DocumentElement);
                xmlDoc.Save(xmlfile);
                CommonTools.writeLog("生成video info xmlfile 文件:" + xmlfile, logpath, "info");
                SetText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + "生成video info xmlfile 文件:"  + Path.GetFileName(xmlfile) + "\n");
                string destvideoinfoxml = Properties.Settings.Default.destVideoPath + "\\"+Path.GetFileName(xmlfile); 
                File.Copy(xmlfile, destvideoinfoxml,true);
                CommonTools.writeLog("复制 video info xmlfile 文件成功:" + destvideoinfoxml, logpath, "info");
                SetText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + "复制 video info xmlfile 文件成功:" + destvideoinfoxml  + "\n");
                return 0;
            }
            catch (Exception ee)
            {
                CommonTools.writeLog("生成video info xml 异常:" + ee.ToString(), logpath, "error");
                return -1;
            }
        }
        private int createAvidInfo( XnewsInfo xinfo)
        {
            //保存Xml文档
            string xmlPath = Application.StartupPath + "\\script";
            if (!Directory.Exists(xmlPath))
            {
                Directory.CreateDirectory(xmlPath);
            }
            string xmlfile = xmlPath + "\\" + xinfo.ProgramID + ".xml";
            try
            {
                //生成导入的xml文件
                XmlDocument xmlDoc = new XmlDocument();
                //创建Xml声明部分，即<?xml version="1.0" encoding="utf-8" ?>
                XmlDeclaration Declaration = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", null);

                //创建根节点
                XmlNode rootNode = xmlDoc.CreateElement("root");

                XmlNode sourceSystemNode = xmlDoc.CreateElement("sourceSystem");
                sourceSystemNode.InnerText = "MediaManager";
                rootNode.AppendChild(sourceSystemNode);

                XmlNode XInterplayPathNode = xmlDoc.CreateElement("XInterplayPath");

                string xinterplayPath = pathid + ";"+Properties.Settings.Default.destMediaManagerPathID ;  //打包的目录 和打包完成的目录

                XInterplayPathNode.InnerText = xinterplayPath;

                rootNode.AppendChild(XInterplayPathNode);

                XmlNode XInterplayFileNameNode = xmlDoc.CreateElement("XInterplayFileName");
                string titletemp = replaceSpecialSQLSyntax(xinfo.Title);
                titletemp = titletemp.Replace(" ","");

                XInterplayFileNameNode.InnerText = titletemp;
                xinfo.Title = titletemp;
            
                rootNode.AppendChild(XInterplayFileNameNode);

                XmlNode uniqueIdNode = xmlDoc.CreateElement("uniqueId");
                uniqueIdNode.InnerText = xinfo.ProgramID;
                rootNode.AppendChild(XInterplayFileNameNode);

                XmlNode userIdNode = xmlDoc.CreateElement("userId");
                userIdNode.InnerText = xinfo.Creator;
                rootNode.AppendChild(userIdNode);

                XmlNode userNameNode = xmlDoc.CreateElement("userName");
                userNameNode.InnerText = xinfo.Author;
                rootNode.AppendChild(userNameNode);

                //加入script 节点
                XmlNode scriptNode = xmlDoc.CreateElement("script");
                rootNode.AppendChild(scriptNode);
                #region 生成文稿节点
           
                    XmlNode textNode = xmlDoc.CreateElement("text");
                    scriptNode.AppendChild(textNode);

                    XmlNode videoidNode = xmlDoc.CreateElement("video-id");
                    textNode.AppendChild(videoidNode);

                    XmlNode presenterNode = xmlDoc.CreateElement("presenter");
                    textNode.AppendChild(presenterNode);

                    XmlNode daoyuNode = xmlDoc.CreateElement("daoyu");
                    textNode.AppendChild(daoyuNode);

                    XmlNode bianhouNode = xmlDoc.CreateElement("bianhou");
                    textNode.AppendChild(bianhouNode);

                    XmlNode jiweiNode = xmlDoc.CreateElement("jiwei");
                    textNode.AppendChild(jiweiNode);

                    XmlNode titleNode = xmlDoc.CreateElement("title");
                    titleNode.InnerText = xinfo.Title;
                    textNode.AppendChild(titleNode);

                    XmlNode vdaoyuztcNode = xmlDoc.CreateElement("v-daoyuztc");
                    textNode.AppendChild(vdaoyuztcNode);

                    XmlNode tihuazimuNode = xmlDoc.CreateElement("tihuazimu");
                    textNode.AppendChild(tihuazimuNode);

                    XmlNode airtypeNode = xmlDoc.CreateElement("airtype");
                    airtypeNode.InnerText = "图像";
                    textNode.AppendChild(airtypeNode);

                    XmlNode secondNode = xmlDoc.CreateElement("second");
                    textNode.AppendChild(secondNode);

                    XmlNode audiotimeNode = xmlDoc.CreateElement("audio-time");
                    textNode.AppendChild(audiotimeNode);

                    XmlNode runstimeNode = xmlDoc.CreateElement("runs-time");
                    textNode.AppendChild(runstimeNode);

                    XmlNode totaltimeNode = xmlDoc.CreateElement("total-time");
                    textNode.AppendChild(totaltimeNode);

                    XmlNode jingbianNode = xmlDoc.CreateElement("jingbian");
                    textNode.AppendChild(jingbianNode);

                    XmlNode peiyinNode = xmlDoc.CreateElement("peiyin");
                    textNode.AppendChild(peiyinNode);

                    XmlNode writerNode = xmlDoc.CreateElement("writer");
                    writerNode.InnerText = xinfo.Author;
                    textNode.AppendChild(writerNode);

                    XmlNode cameramanNode = xmlDoc.CreateElement("cameraman");
                    textNode.AppendChild(cameramanNode);

                    XmlNode zhizuotishiNode = xmlDoc.CreateElement("zhizuotishi");
                    textNode.AppendChild(zhizuotishiNode);

                    XmlNode platformNode = xmlDoc.CreateElement("platform");
                    platformNode.InnerText = xinfo.PlatForm;
                    textNode.AppendChild(platformNode);

                    XmlNode siteNode = xmlDoc.CreateElement("site");
                    siteNode.InnerText = xinfo.Sites;
                    textNode.AppendChild(siteNode);

                    XmlNode airdateNode = xmlDoc.CreateElement("air-date");
                    textNode.AppendChild(airdateNode);

                    XmlNode vbumendNode = xmlDoc.CreateElement("v-bumen");
                    vbumendNode.InnerText = xinfo.Vbumen;
                    textNode.AppendChild(vbumendNode);

                    XmlNode endorsebyNode = xmlDoc.CreateElement("endorse-by");
                    textNode.AppendChild(endorsebyNode);

                    XmlNode createbyNode = xmlDoc.CreateElement("create-by");
                    createbyNode.InnerText = xinfo.Creator;
                    textNode.AppendChild(createbyNode);

                    XmlNode createdateNode = xmlDoc.CreateElement("create-date");
                    string nowtimestemp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    DateTime dtnows = Convert.ToDateTime(nowtimestemp);
                    createdateNode.InnerText = CommonTools.ConvertDateTimeInt(dtnows).ToString();
                    textNode.AppendChild(createdateNode);

                    XmlNode modifybyNode = xmlDoc.CreateElement("modify-by");
                    modifybyNode.InnerText = xinfo.Creator;
                    textNode.AppendChild(modifybyNode);

                    XmlNode modifydateNode = xmlDoc.CreateElement("modify-date");
                    modifydateNode.InnerText = CommonTools.ConvertDateTimeInt(dtnows).ToString();
                    textNode.AppendChild(modifydateNode);

                    XmlNode channelNode = xmlDoc.CreateElement("channel");
                    channelNode.InnerText = xinfo.ChannelPath;
                    textNode.AppendChild(channelNode);

                    XmlNode modifydevNode = xmlDoc.CreateElement("modify-dev");
                    textNode.AppendChild(modifydevNode);

                    XmlNode txtsNode = xmlDoc.CreateElement("txts");
                    txtsNode.InnerText = xinfo.Texts;
                    textNode.AppendChild(txtsNode);

           
                #endregion

                //附加根节点
                xmlDoc.AppendChild(rootNode);
                xmlDoc.InsertBefore(Declaration, xmlDoc.DocumentElement);
                xmlDoc.Save(xmlfile);
                CommonTools.writeLog("生成xmlfile 文件:" + xmlfile, logpath, "info");
                SetText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + "生成xmlfile 文件:" +Path.GetFileName(xmlfile) + "\n");

                string destscriptxml = Properties.Settings.Default.destScriptPath +"\\"+ xinfo.ProgramID + ".xml";
                File.Copy(xmlfile, destscriptxml,true);
                CommonTools.writeLog("复制文稿xml 文件成功:" + destscriptxml, logpath, "info");
                SetText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + "复制文稿xml 文件成功:"  + destscriptxml + "\n");
                
                //复制视频文件
                string destvideo = Properties.Settings.Default.destVideoPath + "\\" + Path.GetFileName(xinfo.MediafilePath);
                CommonTools.writeLog("开始复制视频文件:" + destvideo, logpath, "info");
                SetText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + "开始复制视频文件:" + destvideo+ "\n");
                File.Copy(xinfo.MediafilePath, destvideo,true);
                CommonTools.writeLog("复制视频文件成功:" + destvideo, logpath, "info");
                SetText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + "复制视频文件成功:" + destvideo + "\n");
                //复制视频文件xml 
                //生成导入的xml文件
                int resultd =  createVideoInfo(xinfo);
                if (resultd == 0)
                {
                    return 0;
                }
                else
                {
                    return -1;
                }
            }
            catch (Exception ee)
            {
                CommonTools.writeLog(" 异常:" + ee.ToString(), logpath, "error");
                return -12;
            }

        }

        private void readPathInfo()
        {
            XmlDocument docavidconfig = new XmlDocument();
            docavidconfig.Load(Application.StartupPath + "\\pathinfo.xml");
            System.Xml.XmlElement avidconfig = docavidconfig.DocumentElement;

            XmlNodeList relationlists = avidconfig.SelectNodes("//relation");
            htpaths = new Hashtable();  //路径对应关系
            foreach (XmlNode relationNode in relationlists)
            {
                string keyinpath = relationNode.FirstChild.InnerText;
                string valueinpath = relationNode.FirstChild.NextSibling.InnerText;
                htpaths.Add(keyinpath, valueinpath);
            }
        }

        private string replaceSpecialSQLSyntax(string str)
        {
            string sr = str;
            Regex reg = new Regex("['\"“”/《》%]");
            Match m = reg.Match(str);
            if (m.Success)
            {
                sr = reg.Replace(str, "");
            }
            return sr;
        }
 
        private void button1_Click(object sender, EventArgs e)
        {
            //ftpserverList();
            string filename = @"\\10.27.137.111\smg\test\DBoxRoot\arcstp\4609-CHINA-FOREIGN_EXCHANGE_RESERVEJUNE_20160721170119206272_HR_HD_Final_EBSINEWS5_20160721182501.mp4.mp4";
            string name = Path.GetFileNameWithoutExtension(filename);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            toolStripStatusLabel3.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }




    }
}
