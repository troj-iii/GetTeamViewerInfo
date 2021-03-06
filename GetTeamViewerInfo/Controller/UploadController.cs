﻿using GetTeamViewerInfo.Commands;
using GetTeamViewerInfo.Model;
using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace GetTeamViewerInfo.Controller
{
    public class UploadController
    {
        private static TeamViewerInfo tvi;
        private CookieContainer cookies = new CookieContainer();
        private static DateTime dt = DateTime.Now;

        public UploadController()
        {
            //_autoUploadTimer = new System.Threading.Timer(UploadTimer, null, 1000, 10000);
            ThreadPool.QueueUserWorkItem(UploadTimer, "one");
            TeamViewerInfo.IdChange += IdOrPwdChange;
            TeamViewerInfo.PwdChange += IdOrPwdChange;
        }

        private static void UploadTimer(object o)
        {
            try
            {
                while (true)
                {
                    if (MainConfig.Config == null)
                        continue;
                    if (MainConfig.Config.UploadEnable)
                    {
                        IntPtr mainHandler = FindWindow(null, "TeamViewer");
                        if (mainHandler == IntPtr.Zero)
                            continue;
                        IntPtr xHandler = FindWindowEx(mainHandler, IntPtr.Zero, "#32770", null);
                        IntPtr preHandler = FindWindowEx(xHandler, IntPtr.Zero, null, "伙伴ID");
                        if (preHandler == IntPtr.Zero)
                        {
                            preHandler = FindWindowEx(xHandler, IntPtr.Zero, null, "Partner ID");// Support English Version.
                        }
                        IntPtr idHandler = FindWindowEx(xHandler, preHandler, null, null);
                        IntPtr pwdHandler = FindWindowEx(xHandler, idHandler, null, null);
                        StringBuilder id = new StringBuilder();
                        StringBuilder pwd = new StringBuilder();
                        SendMessage(idHandler, 0x000D, 20, id);
                        SendMessage(pwdHandler, 0x000D, 20, pwd);
                        if (tvi == null)
                        {
                            tvi = new TeamViewerInfo() {};
                            tvi.id = id.ToString();
                            tvi.pwd = pwd.ToString();
                            LogController.Info(string.Format("New TeamViewerInfo.id={0}&pwd={1}", id, pwd));
                            continue;
                        }
                        TimeSpan ts = DateTime.Now - dt;
                        if (ts.Hours > 1)
                        {
                            tvi.pwd = tvi.pwd;
                            dt = DateTime.Now;
                        }
                        if (tvi.id != id.ToString() || tvi.pwd != pwd.ToString())
                        {
                            tvi.id = id.ToString();
                            tvi.pwd = pwd.ToString();
                            LogController.Info(string.Format("New TeamViewerInfo.id={0}&pwd={1}", id, pwd));
                        }
                    }
                }
            }catch(Exception e)
            {
                LogController.Error(e);
                UploadTimer(o);
            }
        }

        private void IdOrPwdChange(object sender,EventArgs e)
        {
            if (tvi == null || tvi.id == null || tvi.pwd == null)
                return;
            string tmp = tvi.id.Replace(" ", "");
            int x = 0;
            if (!int.TryParse(tmp, out x))
                return;
            using (CookieWebClient _webClient = new CookieWebClient(cookies))
            {
                string _getPostString = string.Format("tvid={0}&tvpwd={1}&addr={2}", tvi.id, tvi.pwd, MainConfig.Config.Addr);
                byte[] requestData = _webClient.UploadData(MainConfig.Config.WebApiUpUri, "POST", Encoding.UTF8.GetBytes(_getPostString));
                var responseText = Encoding.UTF8.GetString(requestData);
                if (responseText.IndexOf("success") >= 0)
                    LogController.Info("Upload OK...");
            }
        }

        [DllImport("user32.dll", EntryPoint = "SendMessage")]
        public static extern bool SendMessage(IntPtr hWnd, uint Msg, int wParam, StringBuilder lParam);

        [DllImport("user32.dll", EntryPoint = "FindWindow")]
        private extern static IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", EntryPoint = "FindWindowEx")]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
    }
}
