using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows;
using ZXing;

namespace BH3PCLauncher4BiliVer
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public class UserInfo
        {
            public string Name
            {
                get;
                set;
            }

            public string Password
            {
                get;
                set;
            }
        }
        public MainWindow()
        {
            InitializeComponent();
            List<UserInfo> userInfo = new List<UserInfo>();
            try
			{
                JObject users = JObject.Parse(System.IO.File.ReadAllText("users.json"));
                foreach (var user in users)
                {
                    userInfo.Add(new UserInfo { Name = user.Key.ToString(), Password = user.Value.ToString() });
                }
                c_userSelect.ItemsSource = userInfo;
                c_userSelect.DisplayMemberPath = "Name";
                c_userSelect.SelectedValuePath = "Password";
                c_userSelect.SelectedIndex = 0;
            }
            catch(Exception ex)
			{
                MessageBox.Show(ex.Message);
			}
		}
        public static Bitmap GetScreenSnapshot()
        {
            Rectangle rc = System.Windows.Forms.SystemInformation.VirtualScreen;
            Bitmap bitmap = new Bitmap(rc.Width, rc.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(rc.X, rc.Y, 0, 0, rc.Size, CopyPixelOperation.SourceCopy);
            }
            return bitmap;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var scr = GetScreenSnapshot();
            BarcodeReader reader = new BarcodeReader();
            reader.Options.CharacterSet = "UTF-8";
            Result result = reader.Decode(scr);
            if (result != null)
            {
                string res = result.ToString();
                c_result.Text = res;
                BiliLogin.UserLoginAsync(
                    ((UserInfo)c_userSelect.SelectedItem).Name,
                    ((UserInfo)c_userSelect.SelectedItem).Password,
                    res.Substring(res.Length - 24, 24));
            }
            else
            {
                c_result.Text = "没有识别结果";
            }
        }
    }
}
