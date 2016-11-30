using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevLib.ExtensionMethods;
using DevLib.ModernUI.Forms;
using RestSharp;

namespace ssmh
{
    public partial class Form1 : ModernForm
    {
        public Form1()
        {
            InitializeComponent();

        }

        private void modernButton2_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd=new FolderBrowserDialog();
            fbd.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            fbd.ShowDialog();
            modernTextBox2.Text = fbd.SelectedPath;
        }

        private async void modernButton1_Click(object sender, EventArgs e)
        {
            items.Clear();
            if (!modernTextBox1.Text.StartsWith("http://www.wnacg.com/photos"))
            {
                MessageBox.Show("uri error");
                return;
            }
            modernButton3.Enabled = false;
            title = string.Empty;
            await geteachpage(modernTextBox1.Text);
            modernButton3.Enabled = true;
        }
        BindingList<Item> items=new BindingList<Item>();
        private string title = "";

        private void getregex(string text)
        {
            var matches = Regex.Matches(text,
                "<div class=\"pic_box\"><a href=\"(?<grp0>[^\"]+)\">");
            HashSet<string> its = new HashSet<string>();
            foreach (Match match in matches)
            {
                its.Add(match.Groups["grp0"].Value);
            }
            foreach (string url in its)
            {
                Item i = geteachimg(url);
                this.Invoke(new Action((() =>
                {

                    items.Add(i);


                })));


            }

        }

        private Item geteachimg(string text)
        {
            RestClient rc = new RestClient(new Uri("http://www.wnacg.com" + text));
            RestRequest req = new RestRequest();
            req.Method = Method.GET;
            IRestResponse res = rc.Execute(req);
            var m =
                Regex.Match(res.Content,
                    "<img id=\"picarea\" class=\"photo\" alt=\"(?<grp0>[^\\D]+)\" src=\"(?<grp1>[^\"]+)\" />");
            Item i=new Item();
            i.Title = m.Groups["grp0"].Value;
            i.Url = "http://www.wnacg.com" + m.Groups["grp1"].Value;
            return i;

        }
        private async Task<bool> geteachpage(string url)
        {
            return await Task.Run((async () =>
            {
                

                RestClient rc = new RestClient(new Uri(url));
                RestRequest req = new RestRequest();
                req.Method = Method.GET;
                IRestResponse res = rc.Execute(req);
                getregex(res.Content);
                this.Invoke(new Action((() =>
                {
                    if (string.IsNullOrEmpty(title))
                    {
                        title = Regex.Match(res.Content, "<h2>(?<grp0>.+?)</h2>").Groups["grp0"].Value;
                    }

                })));
                var match = Regex.Match(res.Content, "<a href=\"(?<grp0>[^\"]+)\">後頁");
                if (match.Success)
                {
                    await geteachpage("http://www.wnacg.com" + match.Groups["grp0"].Value);
                }
                return true;
            }));
            
        }

        private async void modernButton3_Click(object sender, EventArgs e)
        {
            modernButton1.Enabled = false;
            modernButton2.Enabled = false;
            modernButton3.Enabled = false;
           await Task.Run((() =>
           {
               string t=null;
                this.Invoke(new Action((() =>
                {
                    t = title;
                })));
             
               StringBuilder rBuilder = new StringBuilder(t);
               foreach (char rInvalidChar in Path.GetInvalidFileNameChars())
                   rBuilder.Replace(rInvalidChar.ToString(), string.Empty);

               string outpath = modernTextBox2.Text+"\\" + rBuilder + "\\";
               Directory.CreateDirectory(outpath);
                foreach (Item item in items)
                {
                   this.Invoke(new Action((() =>
                   {
                       var currentrow = modernDataGridView1.Rows[items.IndexOf(item)];
                       modernDataGridView1.ClearSelection();
                       currentrow.Selected = true;
                       modernDataGridView1.CurrentCell = currentrow.Cells[0];
                       item.状态 = "下载中";
                   })));
                   RestClient rc = new RestClient(new Uri(item.Url));
                    RestRequest req = new RestRequest();
                    req.Method = Method.GET;
                    IRestResponse res = rc.Execute(req);
                    File.WriteAllBytes(outpath + item.Title.Replace(" ", "") + ".jpg", res.RawBytes);
                    this.Invoke(new Action((() =>
                    {
                        item.状态 = "下载完成";
                    })));
                    
                }
            }));
            modernDataGridView1.Refresh();
            ModernMessageBox.Show("OK", "Message");
            modernButton1.Enabled = true;
            modernButton2.Enabled = true;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            modernDataGridView1.DataSource = items;
            modernTextBox2.Text= Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            modernDataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            modernDataGridView1.Columns[1].AutoSizeMode=DataGridViewAutoSizeColumnMode.Fill;
        }

        private void modernTextBox1_Click(object sender, EventArgs e)
        {
            modernTextBox1.Text = "";
        }
    }

    public class Item
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public string 状态 { get; set; }
    }
}
