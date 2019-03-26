using RestSharp;
using System;
using FModel.Parser;
using System.Windows.Forms;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace FModel
{
    public partial class ShopConfig : Form
    {
        private static string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments).ToString() + "\\FModel";

        public ShopConfig()
        {
            InitializeComponent();

            textBox1.Text = Properties.Settings.Default.email;
            textBox2.Text = Properties.Settings.Default.password;
            label3.Visible = false;
            label4.Visible = false;
        }

        private static string repo;
        public static string getAccessToken(string username, string password)
        {
            try
            {
                var getAccessToken = new RestClient("https://account-public-service-prod03.ol.epicgames.com/account/api/oauth/token");
                var gat = new RestRequest(Method.POST);

                gat.AddParameter("grant_type", "password");
                gat.AddParameter("username", username);
                gat.AddParameter("password", password);
                gat.AddParameter("includePerms", "true");

                gat.AddHeader("Authorization", "basic MzQ0NmNkNzI2OTRjNGE0NDg1ZDgxYjc3YWRiYjIxNDE6OTIwOWQ0YTVlMjVhNDU3ZmI5YjA3NDg5ZDMxM2I0MWE");
                gat.AddHeader("Content-Type", "application/x-www-form-urlencoded");

                repo = getAccessToken.Execute(gat).Content;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("[ERROR] ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(ex.Message);
            }
            return FModel.Parser.Access.AccessToken.FromJson(repo).AccessTokenAccessToken;
        }

        private static string accessCode;
        public static string getAccessCode(string accessToken)
        {
            try
            {
                var client = new RestClient("https://account-public-service-prod03.ol.epicgames.com/account/api/oauth/exchange");
                var req = new RestRequest(Method.GET);

                req.AddHeader("Authorization", "bearer " + accessToken);

                accessCode = client.Execute(req).Content;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("[ERROR] ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(ex.Message);
            }
            return FModel.Parser.Access.Code.AccessCode.FromJson(accessCode).Code;
        }

        private static string ExchangeToken;
        public string getExchangeToken(string accessCode)
        {
            try
            {
                var client = new RestClient("https://account-public-service-prod03.ol.epicgames.com/account/api/oauth/token");
                var req = new RestRequest(Method.POST);

                req.AddHeader("Authorization", "basic ZWM2ODRiOGM2ODdmNDc5ZmFkZWEzY2IyYWQ4M2Y1YzY6ZTFmMzFjMjExZjI4NDEzMTg2MjYyZDM3YTEzZmM4NGQ");
                req.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                req.AddParameter("grant_type", "exchange_code");
                req.AddParameter("exchange_code", accessCode);
                req.AddParameter("includePerms", true);
                req.AddParameter("token_type", "eg1");

                ExchangeToken = Parser.Exchange.ExchangeToken.FromJson(client.Execute(req).Content).AccessToken;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("[ERROR] ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(ex.Message);
            }
            return ExchangeToken;
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.email = textBox1.Text;
            Properties.Settings.Default.password = textBox2.Text;
            Properties.Settings.Default.Save();

            string accessToken = null;
            string accessCode = null;
            string exchangeToken = null;
            await Task.Run(() =>
            {
                accessToken = getAccessToken(Properties.Settings.Default.email, Properties.Settings.Default.password);
                accessCode = getAccessCode(accessToken);
                exchangeToken = getExchangeToken(accessCode);
            });
            if (!string.IsNullOrEmpty(accessToken) && !string.IsNullOrEmpty(accessCode) && !string.IsNullOrEmpty(exchangeToken))
            {
                this.Close();
                var shopForm = new ShopWindow(exchangeToken);
                shopForm.Show();
            }
            else
            {
                label3.Visible = true;
                label4.Visible = true;
            }
        }
    }
}
