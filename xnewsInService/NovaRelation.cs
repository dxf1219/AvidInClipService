using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace xnewsInService
{
    public class MediaXMLInfos
    {
        private string audioCount;

        public string AudioCount
        {
            get { return audioCount; }
            set { audioCount = value; }
        } 
        private string m_Format; //封装格式

        public string Format
        {
            get { return m_Format; }
            set { m_Format = value; }
        }

        private string m_Codec;

        public string Codec
        {
            get { return m_Codec; }
            set { m_Codec = value; }
        }
        private string m_FrameCount;

        public string FrameCount
        {
            get { return m_FrameCount; }
            set { m_FrameCount = value; }
        }
        private string m_FrameRate;

        public string FrameRate
        {
            get { return m_FrameRate; }
            set { m_FrameRate = value; }
        }
        private string m_Width;

        public string Width
        {
            get { return m_Width; }
            set { m_Width = value; }
        }
        private string m_Height;

        public string Height
        {
            get { return m_Height; }
            set { m_Height = value; }
        }
        private string m_BitRate;

        public string BitRate
        {
            get { return m_BitRate; }
            set { m_BitRate = value; }
        }

        
    }

    public enum FileOperations
    {
        copy, move, none
    }
    

    public class XnewsInfo
    {
        public string Creator { set; get; }
        public string ProgramID { set; get; }
        public string Sites { set; get; }
        public string PlatForm { set; get; }
        public string Vbumen { set; get; }
        private string xmlreptime;

        public string Xmlreptime
        {
            get { return xmlreptime; }
            set { xmlreptime = value; }
        }

        private string channelPath;

        public string ChannelPath
        {
            get { return channelPath; }
            set { channelPath = value; }
        }
        private string title;

        public string Title
        {
            get { return title; }
            set { title = value; }
        }
        private string texts;

        public string Texts
        {
            get { return texts; }
            set { texts = value; }
        }
        private string author;

        public string Author
        {
            get { return author; }
            set { author = value; }
        }
        private string keywords;

        public string Keywords
        {
            get { return keywords; }
            set { keywords = value; }
        }

        private string tags;

        public string Tags
        {
            get { return tags; }
            set { tags = value; }
        }

        private string mediafilePath;

        public string MediafilePath
        {
            get { return mediafilePath; }
            set { mediafilePath = value; }
        }

        private string mediafilePathinfos;  //  d:\123/0814  反斜杠路径

        public string MediafilePathinfos
        {
            get { return mediafilePathinfos; }
            set { mediafilePathinfos = value; }
        }

        private string mediafilename;

        public string Mediafilename
        {
            get { return mediafilename; }
            set { mediafilename = value; }
        }

        private string xmlpath;

        public string Xmlpath
        {
            get { return xmlpath; }
            set { xmlpath = value; }
        }

        //转码后的文件路径
        private string transcodefilepath;

        public string Transcodefilepath
        {
            get { return transcodefilepath; }
            set { transcodefilepath = value; }
        }
        //转码后的文件名称
        private string transcodefilename;

        public string Transcodefilename
        {
            get { return transcodefilename; }
            set { transcodefilename = value; }
        }

        private double transcodeprocess;

        public double Transcodeprocess
        {
            get { return transcodeprocess; }
            set { transcodeprocess = value; }
        }

        private string transcodepid;

        public string Transcodepid
        {
            get { return transcodepid; }
            set { transcodepid = value; }
        }

        private int register;  //-1 注册失败的  1 注册成功的

        public int Register
        {
            get { return register; }
            set { register = value; }
        }



    }

}
