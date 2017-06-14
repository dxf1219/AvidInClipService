using System;
using System.Collections.Generic;
using System.Text;
using System.IO;


namespace xnewsInService
{
    static class CommonTools
    {

        public  static void Log(String logMessage, TextWriter w,string type)  //type: info error warn
        {

            string s = DateTime.Now.ToString("HH:mm:ss:fff") + "  " + type +" " +logMessage;
            w.WriteLine(s);
            if (logMessage == "软件关闭!")
            {

                w.WriteLine("                 ");

            }
            // Update the underlying file.
            w.Flush();
        }
        public static  void writeLog(string log,string path,string type)
        {
            string filename = DateTime.Now.ToString("yyyy-MM-dd") + ".txt";
            string s = path + "\\" + filename;
            try
            {
                StreamWriter w = File.AppendText(s);
                Log(log, w,type);
                w.Close();
            }
            catch (Exception ee)
            { 
               //
            }
        
        }
        /// <summary>
        /// 将Unix时间戳转换为DateTime类型时间
        /// </summary>
        /// <param name="d">double 型数字</param>
        /// <returns>DateTime</returns>
        public static System.DateTime ConvertIntDateTime(double d)
        {
            System.DateTime time = System.DateTime.MinValue;
            System.DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1));
            time = startTime.AddSeconds(d);
            return time;
        }
        /// <summary>
        /// 将c# DateTime时间格式转换为Unix时间戳格式
        /// </summary>
        /// <param name="time">时间</param>
        /// <returns>double</returns>
        public static double ConvertDateTimeInt(System.DateTime time)
        {
            double intResult = 0;
            System.DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1));
            intResult = (time - startTime).TotalSeconds;
            return intResult;
        }

        /// <summary>
        /// 去除尖括号标识符
        /// </summary>
        /// <param name="stingContent">替换内容</param>
        public static void filterBJ(ref string strContent)
        {
            int startIndexKH1 = strContent.IndexOf('<'); //找到右尖括号的位置
            if (startIndexKH1 != -1) //有尖括号存在
            {
                int endIndexKH2 = strContent.IndexOf('>');

                string ss = strContent.Substring(startIndexKH1, endIndexKH2 - startIndexKH1 + 1);
                if (ss == "</p>")
                {
                    strContent = strContent.Replace(ss, "\r\n");
                    //strContent = strContent.Replace(ss, "</br>");
                }
                else
                    strContent = strContent.Replace(ss, "");
                //if (ss != "</p>" && ss != "<p>")               
                //    strContent = strContent.Replace(ss, "");
                filterBJ(ref strContent);
            }
            else
            {
                return;
            }


        }

        /// <summary>
        /// 测试字符串是否只包含换行
        /// </summary>
        /// <param name="strContent">内容</param>
        public static void filterEnter(ref string strContent)
        {
            string tmp = strContent.Replace("\r\n", "");
            if (tmp.Length == 0)
            {
                strContent = "";
                return;
            }
            else
            {
                return;
            }
        }

        /// <summary>
        /// 去除SQL标签
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string replaceSpecialSQLSyntax(string str)
        {
            string sr = str;
            System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex("['\"“”/《》%]");
            System.Text.RegularExpressions.Match m = reg.Match(str);
            if (m.Success)
            {
                sr = reg.Replace(str, "");
            }
            return sr;
        }
    }
}
