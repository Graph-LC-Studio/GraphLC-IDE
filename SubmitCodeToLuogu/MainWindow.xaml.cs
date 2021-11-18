using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using MahApps.Metro.Controls.Dialogs;
using ControlzEx.Theming;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Net.Http;
using Path = System.IO.Path;
using System.Threading;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace SubmitCodeToLuogu
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MahApps.Metro.Controls.MetroWindow
    {
        private JObject config = null;
        private string[] args = null;
        private string DefaultTitle = "提交代码至 Luogu";

        private void ChangeTitle(string title) => this.Title = string.Format("{0} ({1})", DefaultTitle, title);

        public MainWindow()
        {
            InitializeComponent();
            ThemeManager.Current.ChangeTheme(this, "Dark.Blue");
            this.TextBoxProblem.Focus();
            this.Topmost = true;

            args = Environment.GetCommandLineArgs();
            Init();
        }

        private void MetroWindow_Closing(object sender, EventArgs e)
        {
            try
            {
                File.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, args[1]));
            }
            catch { }

            try
            {
                config["enableO2"] = CheckBoxO2.IsChecked.GetValueOrDefault();
                config["lang"] = ComboBoxLanguage.SelectedIndex;

                using (StreamWriter r = new StreamWriter(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.txt")))
                    r.WriteLine(config.ToString());
            }
            catch { }
        }

        private void Init()
        {
            try
            {
                using (StreamReader r = new StreamReader(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.txt")))
                    config = (JObject)JsonConvert.DeserializeObject(r.ReadToEnd());

                CheckBoxO2.IsChecked = (bool)(config["enableO2"] ?? false);
                ComboBoxLanguage.SelectedIndex = (int)(config["lang"] ?? 0);
            }
            catch
            {
                ChangeTitle("配置未能加载");
            }
        }

        public string GetSingleTagValueByAttr(string inputstring, string tagName, string attrname, string key)
        {
            string regStr = $"(?<={tagName} {attrname}=\"{key}\" content=\").*?(?=\")";

            Regex reg = new Regex(regStr, RegexOptions.IgnoreCase);
            MatchCollection matchs = reg.Matches(inputstring);
            string result = string.Empty;
            foreach (Match match in matchs)
            {
                string matchValue = match.Value;
                if (!string.IsNullOrEmpty(matchValue))
                    return matchValue;
            }
            return "";
        }


        private async Task<string> GetCsrfToken()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("cookie", string.Format("__client_id={0}; _uid={1}", config["__client_id"].ToString(), config["_uid"].ToString()));
            return GetSingleTagValueByAttr(await client.GetStringAsync("https://www.luogu.com.cn/"), "meta", "name", "csrf-token");
        }
        
        private void Submit(object sender, RoutedEventArgs e)
        {
            async Task<string> GetRid(string problem, string code, int lang, bool enableO2)
            {
                var client = new HttpClient(new HttpClientHandler() { AllowAutoRedirect = true, UseCookies = true });
                client.DefaultRequestHeaders.Add("cookie", string.Format("__client_id={0}; _uid={1}", config["__client_id"].ToString(), config["_uid"].ToString()));
                client.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/95.0.4638.69 Safari/537.36 Edg/95.0.1020.44");
                client.DefaultRequestHeaders.Add("x-csrf-token", await GetCsrfToken());
                client.DefaultRequestHeaders.Add("referer", "https://www.luogu.com.cn/");

                var response = await client.PostAsync(
                    "https://www.luogu.com.cn/fe/api/problem/submit/" + problem,
                    new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(new { enableO2 = enableO2 ? 1 : 0, lang, code }), Encoding.UTF8, "application/json")
                );

                var json = (JObject)JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());
                return (json["rid"] ?? json).ToString();
            }

                var problem = TextBoxProblem.Text;
                var enalbeO2 = CheckBoxO2.IsChecked.GetValueOrDefault();
                var lang = 3;
                if (ComboBoxLanguage.SelectedItem is ComboBoxItem item) lang = int.Parse(item.Tag.ToString());
                var code = "";

                new Thread(() =>
                {
                        using (StreamReader r = new StreamReader(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, args[1])))
                            code = r.ReadToEnd();
                        var rid = GetRid(problem, code, lang, enalbeO2);
                        rid.Wait();

                        Process.Start(new ProcessStartInfo()
                        {
                            FileName = "https://www.luogu.com.cn/record/" + rid.Result,
                            UseShellExecute = true
                        });

                    this.Dispatcher.Invoke(() => { Application.Current.Shutdown(); });

                }).Start();
        }
    }
}
