using AngleSharp.Html.Parser;
using Newtonsoft.Json.Linq;
using Speech;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace QiitaTrendReader
{
    public partial class MainWindow : Window
    {
        private Queue<Item> Items = new Queue<Item>();
        private WebClient webClient = new WebClient();
        private ISpeechEngine engine;
        private const string Qiita = "https://qiita.com/";
        private string LibraryName = "";
        private bool IsPlaying = false;
        private bool IsCheckedNewArrivalOnly = false;
        private bool IsCheckedOrderByDate = false;

        public MainWindow()
        {
            InitializeComponent();
            webClient.Encoding = Encoding.UTF8;
            SetTTSEnginesToComboBox();
            buttonStartStop.Click += ButtonStartStop_Click;
            buttonSkip.Click += ButtonSkip_Click;
            comboBoxSelectTTS.SelectionChanged += ComboBoxSelectTTS_SelectionChanged;
            checkBoxNewArrivalOnly.Checked += (sender, e) => IsCheckedNewArrivalOnly = true;
            checkBoxNewArrivalOnly.Unchecked += (sender, e) => IsCheckedNewArrivalOnly = false;
            checkBoxOrderByDate.Checked += (sender, e) => IsCheckedOrderByDate = true;
            checkBoxOrderByDate.Unchecked += (sender, e) => IsCheckedOrderByDate = false;
        }

        private void ComboBoxSelectTTS_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LibraryName = comboBoxSelectTTS.SelectedItem.ToString();
        }

        private void ButtonSkip_Click(object sender, RoutedEventArgs e)
        {
            if (IsPlaying)
            {
                engine.Stop();
            }
        }

        private void ButtonStartStop_Click(object sender, RoutedEventArgs e)
        {
            if (IsPlaying)
            {
                Stop();
            }
            else
            {
                Start();
            }
        }

        private void Start()
        {
            var json = GetTrendJson();
            var items = JsonToItemList(json);
            if (IsCheckedNewArrivalOnly)
            {
                items = items.Where(item => item.IsNewArrival).ToList();
            }
            if (IsCheckedOrderByDate)
            {
                items = items.OrderByDescending(item => item.CreatedAt).ToList();
            }
            foreach (var item in items)
            {
                Items.Enqueue(item);
            }
            buttonStartStop.Content = "Stop";
            Play();
        }

        private string GetTrendJson()
        {
            var html = webClient.DownloadString(Qiita);
            var parser = new HtmlParser();
            var htmlDocument = parser.ParseDocument(html);
            var trend = htmlDocument.QuerySelector("div[data-hyperapp-app=\"Trend\"]");
            return trend.GetAttribute("data-hyperapp-props");
        }

        private List<Item> JsonToItemList(string json)
        {
            var jobject = JObject.Parse(json);
            var items = new List<Item>();
            foreach (var edge in jobject["trend"]["edges"])
            {
                var node = edge["node"];
                var title = node["title"].ToString();
                var author = node["author"]["urlName"].ToString();
                var uuid = node["uuid"].ToString();
                var createdAt = node["createdAt"].ToString();
                var isNewArrival = edge["isNewArrival"].ToObject<bool>();
                items.Add(new Item(title, author, uuid, createdAt, isNewArrival));
            }
            return items;
        }

        private void Play()
        {
            var item = Items.Dequeue();
            var text = GetArticle(item);
            engine = SpeechController.GetInstance(LibraryName);
            engine.Activate();
            engine.Finished += Engine_Finished;
            IsPlaying = true;
            engine.Play(text);
        }

        private void Engine_Finished(object sender, EventArgs e)
        {
            IsPlaying = false;
            if (Items.Count > 0)
            {
                engine.Dispose();
                Play();
            }
            else
            {
                Stop();
            }
        }

        private string GetArticle(Item item)
        {
            var url = Qiita + item.Author + "/items/" + item.Uuid;
            Dispatcher.BeginInvoke(new
                Action(() =>
                {
                    textBoxUrl.Text = url;
                    textBoxTitle.Text = item.Title;
                }));
            var html = webClient.DownloadString(url);
            var parser = new HtmlParser();
            var htmlDocument = parser.ParseDocument(html);
            var elements = htmlDocument.GetElementById("item-" + item.Uuid);
            var article = elements.TextContent;
            string patternStr = @"<.*?>";
            return Regex.Replace(article, patternStr, string.Empty, RegexOptions.Singleline);
        }

        private void Stop()
        {
            Items = new Queue<Item>();
            Dispatcher.BeginInvoke(new
                Action(() =>
                {
                    buttonStartStop.Content = "Start";
                }));
            IsPlaying = false;
            engine.Stop();
            engine.Dispose();
        }

        private void SetTTSEnginesToComboBox()
        {
            var engines = SpeechController.GetAllSpeechEngine().Select(x => $"{ x.LibraryName}").ToList();
            if (engines.Count() == 0)
            {
                MessageBox.Show("使用可能な音声合成ソフトウェアがインストールされていません");
                Environment.Exit(1);
                return;
            }
            comboBoxSelectTTS.ItemsSource = engines;
            comboBoxSelectTTS.Text = engines[0];
            LibraryName = engines[0];
        }
    }
}
