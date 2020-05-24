using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
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

namespace nicojikkyo_download {
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window {
        String nicojshiftExePath = "";
        String wDirectoryPath = "";
        String chromeCookiePath = "";
        String jkCommentGetterRbPath = "";
        bool closeAfterSuccess = false;
        public MainWindow() {
            InitializeComponent();
        }

        private void Window_Initialized(object sender, EventArgs e) {
            String[] cmds = Environment.GetCommandLineArgs();
            foreach (var cmd in cmds) {
                if (String.IsNullOrEmpty(cmd)) {
                    continue;
                } else if (cmd.StartsWith("nicojShiftExe=")) {
                    this.nicojshiftExePath = cmd.Substring("nicojShiftExe=".Length);
                } else if (cmd.StartsWith("wDirectory=")) {
                    this.wDirectoryPath = cmd.Substring("wDirectory=".Length);
                } else if (cmd.StartsWith("chromeCookie=")) {
                    this.chromeCookiePath = cmd.Substring("chromeCookie=".Length);
                } else if (cmd.StartsWith("jkcommentgetterrb=")) {
                    this.jkCommentGetterRbPath = cmd.Substring("jkcommentgetterrb=".Length);
                } else if (System.IO.File.Exists(cmd) && cmd.ToLower().EndsWith(".ts")) {
                    this.TsFilePathTextBox.Text = cmd;
                    this.closeAfterSuccess = true;
                }
            }
            if (this.nicojshiftExePath == "") {
                this.nicojshiftExePath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "NicojShift.exe");
            }
            if (this.wDirectoryPath == "") {
                this.wDirectoryPath = System.IO.Directory.GetCurrentDirectory();
            }
            if (this.chromeCookiePath == "") {
                this.chromeCookiePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Google\Chrome\User Data\Default\Cookies";
            }
            if (this.jkCommentGetterRbPath == "") {
                this.jkCommentGetterRbPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "JKCommentGetter.rb");
            }

            this.rTextBox.Document.Blocks.Clear();
            try {
                this.ChromeSessionTextBox.Text = this.getSessionFromChromeCookie();
            } catch (Exception err) {
                this.addLog($"chromeからセッションの読み込みに失敗\n{err.ToString()}");
            }
            if (this.ChromeSessionTextBox.Text == "") {
                this.addLog("chromeからセッションの読み込みに失敗");
            } else {
                this.addLog($"chromeからセッションの読み込み:{this.ChromeSessionTextBox.Text}");
            }
            try {
                this.rubySessionTextBox.Text = this.getSessionFromJKCommentGetterRb();
            } catch (Exception er) {
                this.addLog($"JKCommentGetter.rbからセッションの読み込みに失敗\n{er.ToString()}");
            }
            if (this.rubySessionTextBox.Text == "") {
                this.addLog("JKCommentGetter.rbからセッションの読み込みに失敗");
            } else {
                this.addLog($"JKCommentGetter.rbからセッションの読み込み:{this.ChromeSessionTextBox.Text}");
            }
            this.execNicoJShift();
        }
        /// <summary>
        /// chromeのクッキーファイルからセッション情報を取得する。ヒットしなかったら空文字を返す。
        /// 取得したセッション文字列が有効かどうかは判定しない
        /// </summary>
        /// <returns></returns>
        String getSessionFromChromeCookie() {
            foreach (var i in this.ReadCookies()) {
                if (i.Item1 == "user_session") {
                    this.addLog($@"chromeのセッション情報 createion:{i.Item3.ToLongDateString()}{i.Item3.ToLongTimeString()} expires:{i.Item4.ToLongDateString()}{i.Item4.ToLongTimeString()} lastAccess:{i.Item5.ToLongDateString()}{i.Item5.ToLongTimeString()} ", color: Colors.Gray);
                    return i.Item2;
                }
            }
            return "";
        }
        /// <summary>
        /// JKCommentGetter.rbからセッション情報を読み込んで返す
        /// </summary>
        /// <returns></returns>
        String getSessionFromJKCommentGetterRb() {
            this.addLog($@"JKCommentGetter.rbからセッション読み込み ""{this.jkCommentGetterRbPath}""", color: Colors.Gray);
            Tuple<String[], int> data = getSessionDataFromJkCommentGetterRb();
            if (data.Item2 == -1) {
                return "";
            } else {
                String hitLine = data.Item1[data.Item2];
                return hitLine.Replace("\t", "").Replace("'", "").Replace("user_session=", "").Replace(";", "");
            }
        }
        /// <summary>
        /// JKCommentGetter.rbを読み込んで、全部の行の配列とセッション情報が書かれている行の番号を返す
        /// </summary>
        /// <returns></returns>
        Tuple<String[], int> getSessionDataFromJkCommentGetterRb() {
            String[] lines;
            lines = System.IO.File.ReadAllLines(this.jkCommentGetterRbPath);
            Boolean isDef = false;
            for (int i = 0; i < lines.Length; i++) {
                String line = lines[i];
                if (line == null) {
                    continue;
                }
                if (line == "def getCookie") {
                    isDef = true;
                } else if (isDef == true && line.Trim().StartsWith("'")) {
                    return new Tuple<String[], int>(lines, i);
                }
            }
            return new Tuple<string[], int>(lines, -1);
        }
        /// <summary>
        /// chromeのクッキーファイルを読み込む
        /// </summary>
        /// <returns></returns>
        IEnumerable<Tuple<string, string, DateTime, DateTime, DateTime>> ReadCookies() {
            if (!System.IO.File.Exists(this.chromeCookiePath)) throw new System.IO.FileNotFoundException("Cant find cookie store", this.chromeCookiePath); // race condition, but i'll risk it
            String copyToPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), System.IO.Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetExecutingAssembly().Location) + "_cookie");
            System.IO.File.Copy(this.chromeCookiePath, copyToPath, true);
            this.addLog($@"chromeからクッキー読み込み ""{this.chromeCookiePath}"" を ""{copyToPath}"" にコピー", color: Colors.Gray);
            var connectionString = "Data Source=\"" + this.chromeCookiePath + "\";pooling=false;";
            using (var conn = new System.Data.SQLite.SQLiteConnection(connectionString))
            using (var cmd = conn.CreateCommand()) {
                cmd.CommandText = "SELECT name,encrypted_value,value,creation_utc,expires_utc,last_access_utc FROM cookies WHERE host_key = '.nicovideo.jp' and name like 'user_session%'";
                conn.Open();
                using (SQLiteDataReader reader = cmd.ExecuteReader()) {
                    while (reader.Read()) {
                        var encryptedData = (Byte[])reader.GetValue(1);
                        var rawData = (String)reader.GetValue(2);
                        // 1601/01/01スタート
                        DateTime createionUtc = new DateTimeOffset(1601, 1, 1, 0, 0, 0, 0, TimeSpan.FromMinutes(0)).AddMilliseconds(reader.GetInt64(3) / 1000).DateTime;
                        DateTime expiresUtc = new DateTimeOffset(1601, 1, 1, 0, 0, 0, 0, TimeSpan.FromMinutes(0)).AddMilliseconds(reader.GetInt64(4) / 1000).DateTime;
                        DateTime lastAccessUtc = new DateTimeOffset(1601, 1, 1, 0, 0, 0, 0, TimeSpan.FromMinutes(0)).AddMilliseconds(reader.GetInt64(5) / 1000).DateTime;

                        if (rawData != "") {
                            yield return Tuple.Create(reader.GetString(0), rawData, createionUtc, expiresUtc, lastAccessUtc);
                            continue;
                        }
                        byte[] decodedData;
                        try {
                            // 先頭n byteをカット
                            //int trimByte = 3;
                            //byte[] trimedEncryptData = new byte[encryptedData.Length - trimByte];
                            //Array.Copy(encryptedData, trimByte, trimedEncryptData, 0, trimedEncryptData.Length);
                            //this.addLog($@"------", color: Colors.Gray);
                            //this.addLog($@"{BitConverter.ToString(encryptedData)}", color: Colors.Gray);
                            //this.addLog($@"{BitConverter.ToString(trimedEncryptData)}", color: Colors.Gray);
                            //this.addLog($@"------", color: Colors.Gray);
                            decodedData = System.Security.Cryptography.ProtectedData.Unprotect(encryptedData, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
                        }catch(System.Security.Cryptography.CryptographicException e) {
                            //  {BitConverter.ToString(encryptedData)}
                            this.addLog($@"encryptDataを複合する事に失敗するデータがありました key={reader.GetString(0)} {e.Message.Trim()}", color: Colors.Gray);
                            continue;

                        }
                        var plainText = Encoding.ASCII.GetString(decodedData); // Looks like ASCII
                        yield return Tuple.Create(reader.GetString(0), plainText, createionUtc, expiresUtc, lastAccessUtc);
                    }
                }
                conn.Close();
            }
        }
        /// <summary>
        /// 非同期。NicojShiftからコメントを取得する
        /// </summary>
        async void execNicoJShift() {
            if (System.IO.File.Exists(this.nicojshiftExePath) == false) {
                this.addLog($@"NicojShiftのexeが見つかりませんでした ""{this.nicojshiftExePath}""", color: Colors.Red);
                return;
            }
            if (this.TsFilePathTextBox.Text == "") {
                this.addLog($@"対象の動画ファイルが見つかりませんでした ""{this.TsFilePathTextBox.Text}""", color: Colors.Red);
                return;
            }

            this.CommentGetButton.IsEnabled = false;
            Process process = new Process();
            process.StartInfo.StandardOutputEncoding = System.Text.Encoding.GetEncoding("sjis");
            process.StartInfo.StandardErrorEncoding = System.Text.Encoding.GetEncoding("utf-8");
            process.StartInfo.FileName = this.nicojshiftExePath;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(this.nicojshiftExePath);
            process.StartInfo.Arguments = $@"/T1 ,,,, /T2 ,,,, /NJC /F ""{this.TsFilePathTextBox.Text}"" /W {this.wDirectoryPath} /FTSP 0";
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();
            process.ErrorDataReceived += (a, b) => {
                if (b.Data == null) { return; }
                this.addLog(b.Data.Trim(), color: Colors.Red);
            };
            process.OutputDataReceived += (a, b) => {
                if (b.Data == null) { return; }
                this.addLog(b.Data.Trim());
            };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            while (true) {
                await Task.Delay(100);
                if (process.HasExited) {
                    break;
                }
            }
            if (this.closeAfterSuccess) {
                this.addLog($@"自動的にプロセスを終了します", color: Colors.Red);
                await Task.Delay(1000);
                this.Close();
                return;
                
            }
            this.CommentGetButton.IsEnabled = true;
        }
        /// <summary>
        /// ログに追記する。文字色の変更も可能。別スレッドok
        /// </summary>
        /// <param name="text"></param>
        /// <param name="color"></param>
        void addLog(String text, Color? color = null) {
            this.Dispatcher.BeginInvoke((Action)(() => {
                Inline inline = new Run(text);
                if (color.HasValue) {
                    inline.Foreground = new SolidColorBrush(color.Value);
                }
                Paragraph paragraph = new Paragraph();
                paragraph.Inlines.Add(inline);
                paragraph.Margin = new Thickness(0, 0, 0, 0);
                this.rTextBox.Document.Blocks.Add(paragraph);
                this.rTextBox.ScrollToEnd();
            }));
        }

        /// <summary>
        /// コメント取得ボタンを押した時
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void CommentGetButton_Click(object sender, RoutedEventArgs e) {
            this.execNicoJShift();
        }

        /// <summary>
        /// ChromeSessionTextBoxをrubyファイルに上書きする
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void SessionOverwriteButton_Click(object sender, RoutedEventArgs e) {
            // user_session_123456_0123456789abcedf
            System.Text.RegularExpressions.Regex sessionRegex = new System.Text.RegularExpressions.Regex(@"^user_session_\d+_[0-9a-f]+");
            if (sessionRegex.IsMatch(this.ChromeSessionTextBox.Text) == false) {
                this.addLog($@"chromeのクッキーの形式がおかしいです", color: Colors.Gray);
                return;
            }
            var rbData = this.getSessionDataFromJkCommentGetterRb();
            if (rbData.Item2 == -1) {
                this.addLog($@"JKCommentGetter.rbの取得に失敗しました", color: Colors.Gray);
                return;
            }
            rbData.Item1[rbData.Item2] = $"\t'user_session={this.ChromeSessionTextBox.Text}'";
            System.IO.File.WriteAllLines(this.jkCommentGetterRbPath, rbData.Item1);
            this.addLog($@"JKCommentGetter.rbのセッション情報を書き換えました", color: Colors.Gray);
            this.rubySessionTextBox.Text = this.getSessionFromJKCommentGetterRb();
        }
        /// <summary>
        /// ファイルがドロップされた時。最初のファイルのみを値に入れる
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Window_Drop(object sender, DragEventArgs e) {
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files != null) {
                this.addLog($@"ファイルがドロップされた ""{files[0]}""");
                this.TsFilePathTextBox.Text = files[0];
                this.execNicoJShift();
            }
        }
        /// <summary>
        /// ファイルがドラッグされた時のイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Window_PreviewDragOver(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, true)) {
                e.Effects = DragDropEffects.Copy;
            } else {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }
    }
}
