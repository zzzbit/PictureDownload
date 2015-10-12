using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;           //URLEncode
using System.Diagnostics;   //统计线程数
using System.Collections;   //List

namespace 凡客图片信息下载
{
    public partial class Form1 : Form
    {

        Encoding myEncoding = Encoding.GetEncoding("UTF-8");
        int total = 0;
        string path = @"C:\Test\vancl\";

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < 50; i++)
            {
                Thread thread = new Thread(spiderGoogle);
                thread.Start(i*10);
            }
        }

        /*
        *  通过Url下载源码
        *  @param [in] url 网址
        *  @return 网址源码
        */
        private string getContentByUrl(string url, Encoding urlEncoding)
        {
            string res = string.Empty;
            try
            {
                //发送请求
                HttpWebRequest myReq = (HttpWebRequest)WebRequest.Create(url);
                myReq.UserAgent = "Mozilla/5.0 (Windows NT 6.2; WOW64; Trident/7.0; rv:11.0) like Gecko";

                //获取响应流，得到源码
                HttpWebResponse response = (HttpWebResponse)myReq.GetResponse();
                StreamReader reader = new StreamReader(response.GetResponseStream(), urlEncoding);
                res = reader.ReadToEnd();
                reader.Close();
                response.Close();

                //写入文件
                //FileStream fs = new FileStream("Code.html", FileMode.Create);
                //StreamWriter sw = new StreamWriter(fs, myEncoding);
                //sw.Write(res);
                //sw.Close();
                //fs.Close();
            }
            catch (Exception e1)
            {
                Console.WriteLine(e1.Message);
            }
            return res;
        }


        /*
        *  通过Url下载文件
        *  @param [in] url 网址
        *  @param [in] picPath 保存图片路径
        *  @return 空
        */
        private void downloadPicByUrl(object argc)
        {
            //处理传入参数
            string arg = (string)argc;
            int position = arg.IndexOf('|');
            string url = arg.Substring(0, position);
            string picPath = arg.Substring(position + 1);

            try
            {
                //发送请求
                HttpWebRequest myReq = (HttpWebRequest)WebRequest.Create(url);
                myReq.UserAgent = "Mozilla/5.0 (Windows NT 6.2; WOW64; Trident/7.0; rv:11.0) like Gecko";

                //获取响应流
                HttpWebResponse response = (HttpWebResponse)myReq.GetResponse();
                Stream stream = response.GetResponseStream();

                //写入文件
                Stream fs = new FileStream(picPath, FileMode.Create);
                byte[] bytes = new byte[1024];
                int osize = stream.Read(bytes, 0, (int)bytes.Length);
                while (osize > 0)
                {
                    fs.Write(bytes, 0, osize);
                    osize = stream.Read(bytes, 0, (int)bytes.Length);
                }
                stream.Close();
                response.Close();
                fs.Close();
            }
            catch (Exception e1)
            {
                Console.WriteLine(e1.Message);
            }
        }

        /*
       *  Google图库爬虫
       *  @param [in] categoryName 类别名
       *  @return 空
       */
        public void spiderGoogle(object categoryName)
        {
            try
            {
                //各种变量的初始化，以及字符串的分割
                int start = (int)categoryName;

                //遍历不同的类别
                for (int k = start; k < start + 10; k++)
                {
                    //获取图库源码
                    string res = getContentByUrl("http://m.vancl.com/Product/ProductList?pageIndex=" + k + "&keyWord=" + HttpUtility.UrlEncode("衣服"), myEncoding);
                    //分析源码，得到链接
                    string pattern = @"href=""/style/index/(?<Url>.+?)"""; //正则表达式
                    MatchCollection matchs = Regex.Matches(res, pattern);
                    res = null;
                    foreach (Match m in matchs)
                    {
                        //唯一ID
                        int ID;
                        lock (this)
                        {
                            total++;
                            ID = total;
                        }
                        //写入文件
                        FileStream fs = new FileStream(path + ID + ".txt", FileMode.Create);
                        StreamWriter sw = new StreamWriter(fs, myEncoding);
                        Regex regex;

                        string Url = m.Groups["Url"].Value;
                        sw.WriteLine("http://m.vancl.com/style/index/" + Url);
                        //获取物品源码
                        string res1 = getContentByUrl("http://m.vancl.com/style/index/" + Url, myEncoding);
                        string pattern1 = @"class=""lazy""   src=""(?<picUrl>.+?)"""; //正则表达式
                        regex = new Regex(pattern1);
                        if (regex.IsMatch(res1))
                        {
                            downloadPicByUrl(regex.Match(res1).Groups["picUrl"].Value + "|" + path + ID + ".jpg");
                        }
                        pattern1 = @"售价:<span class=""red"">￥(?<content>.+?)</span>"; //正则表达式
                        regex = new Regex(pattern1);
                        if (regex.IsMatch(res1))
                        {
                            sw.WriteLine(regex.Match(res1).Groups["content"].Value);
                        }
                        else
                        {
                            sw.WriteLine();
                        }
                        pattern1 = @"(?<content>用户评论.+?)<"; //正则表达式
                        regex = new Regex(pattern1);
                        if (regex.IsMatch(res1))
                        {
                            sw.WriteLine(regex.Match(res1).Groups["content"].Value);
                        }
                        else
                        {
                            sw.WriteLine();
                        }
                        pattern1 = @"(?<content>购买咨询.+?)<"; //正则表达式
                        regex = new Regex(pattern1);
                        if (regex.IsMatch(res1))
                        {
                            sw.WriteLine(regex.Match(res1).Groups["content"].Value);
                        }
                        else
                        {
                            sw.WriteLine();
                        }
                        //获取物品详细信息源码
                        res1 = getContentByUrl("http://m.vancl.com/style/details/" + Url, myEncoding);
                        pattern1 = @"<b>产品属性</b>(?<content>.+?)</div>";
                        regex = new Regex(pattern1, RegexOptions.Singleline);
                        if (regex.IsMatch(res1)){
                            sw.WriteLine(regex.Match(res1).Groups["content"].Value.Replace(" ", "").Replace("<br/>", ""));
                        }
                        else
                        {
                            sw.WriteLine();
                        }
                        sw.Flush();
                        sw.Close();
                        fs.Close();
                    }//foreach
                }//for

            }
            catch (Exception e1)
            {
                Console.WriteLine(e1.Message);
            }
        }
    }
}
