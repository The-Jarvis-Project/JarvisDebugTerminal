using Newtonsoft.Json;
using System.Diagnostics;
using System.Text;

namespace JarvisDebugTerminal
{
    public partial class TerminalForm : Form
    {
        private static readonly HttpClient client = new HttpClient();
        private readonly List<(string, bool, long)> listContent = new List<(string, bool, long)>();
        private string currentContent;
        private bool canDownload, canSend;

        public TerminalForm()
        {
            InitializeComponent();
            textField.KeyDown += TextField_KeyPress;
            canDownload = true;
            canSend = true;
        }

        private async void TextField_KeyPress(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (canSend)
                {
                    canSend = false;
                    JarvisRequestDTO dto = new JarvisRequestDTO
                    {
                        Request = textField.Text,
                    };

                    string json = JsonConvert.SerializeObject(dto);
                    StringContent jsonContent = new StringContent(json, Encoding.UTF8, "application/json");
                    try
                    {
                        HttpResponseMessage response = await client.PostAsync(
                            "https://jarvislinker.azurewebsites.net/api/JarvisRequests", jsonContent);
                        if (response.IsSuccessStatusCode)
                            textField.Text = string.Empty;
                    }
                    catch (Exception x)
                    {
                        Debug.WriteLine(x.Message);
                    }
                    canSend = true;
                }
            }
        }

        private async void Timer_Tick(object sender, EventArgs e)
        {
            if (canDownload)
            {
                canDownload = false;
                try
                {
                    string requests = await client.GetStringAsync("https://jarvislinker.azurewebsites.net/api/JarvisRequests"),
                        responses = await client.GetStringAsync("https://jarvislinker.azurewebsites.net/api/JarvisResponses"),
                        receivedContent = requests + responses;
                    if (currentContent != receivedContent)
                        DecodeAndUpdateList(requests, responses);
                }
                catch (Exception x)
                {
                    Debug.WriteLine(x.Message);
                    if (currentContent != string.Empty)
                        DecodeAndUpdateList(string.Empty, string.Empty);
                }
                canDownload = true;
            }
        }

        private void DecodeAndUpdateList(string requestMsg, string responseMsg)
        {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            List<JarvisRequest> requests = JsonConvert.DeserializeObject<List<JarvisRequest>>(requestMsg);
            List<JarvisResponse> responses = JsonConvert.DeserializeObject<List<JarvisResponse>>(responseMsg);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

            listContent.Clear();
            if (requests != null)
            {
                for (int i = 0; i < requests.Count; i++)
                    listContent.Add((requests[i].Request, true, requests[i].Id));
            }
            if (responses != null)
            {
                for (int i = 0; i < listContent.Count; i++)
                {
                    for (int r = 0; r < responses.Count; r++)
                    {
                        if (responses[r].RequestId == listContent[i].Item3)
                        {
                            listContent.Insert(i + 1, (responses[r].Data, false, 1000000000));
                            i++;
                            break;
                        }
                    }
                }
            }

            listView.Items.Clear();
            for (int i = 0; i < listContent.Count; i++)
            {
                ListViewItem item;
                if (listContent[i].Item2)
                    item = new ListViewItem(new string[] { listContent[i].Item1 }, 0, Color.White, Color.SlateBlue, DefaultFont);
                else item = new ListViewItem(new string[] { listContent[i].Item1 }, 0, Color.Black, Color.LightGray, DefaultFont);
                listView.Items.Add(item);
            }
            currentContent = requestMsg + responseMsg;
        }

    }
}