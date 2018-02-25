using Microsoft.Bot.Connector.DirectLine;
using Microsoft.CognitiveServices.SpeechRecognition;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BotClient
{
    public partial class Client : Form
    {
        private static string directLineSecret = ConfigurationManager.AppSettings["DirectLineSecret"];
        private static string botId = ConfigurationManager.AppSettings["BotId"];
        private static string speechKey = ConfigurationManager.AppSettings["SpeechKey"];
        private static string authenticationUri = ConfigurationManager.AppSettings["AuthenticationUri"];
        private static string fromUser = "DirectLineSampleClientUser";

        private DirectLineClient client;
        private Microsoft.Bot.Connector.DirectLine.Conversation conversation;
        private SpeechSynthesizer voice;
        private MicrophoneRecognitionClient micClient;
        private SpeechRecognitionMode Mode = SpeechRecognitionMode.ShortPhrase;


        public Client()
        {
            InitializeComponent();
            SetupVoice();
            SetupMicrophone();
            StartBotConversation();
        }
        #region Speech Client

        private void SetupMicrophone()
        {
            try
            {
                CreateMicrophoneRecoClient();
                this.micClient.StartMicAndRecognition();
                //MessageBox.Show("Hi");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void CreateMicrophoneRecoClient()
        {
            this.micClient = SpeechRecognitionServiceFactory.CreateMicrophoneClient(
                this.Mode,
                "en-US",
                speechKey);
            this.micClient.AuthenticationUri = authenticationUri;

            // Event handlers for speech recognition results
            this.micClient.OnMicrophoneStatus += this.OnMicrophoneStatus;
            this.micClient.OnPartialResponseReceived += this.OnPartialResponseReceivedHandler;
            if (this.Mode == SpeechRecognitionMode.ShortPhrase)
            {
                this.micClient.OnResponseReceived += this.OnMicShortPhraseResponseReceivedHandler;
            }
            else if (this.Mode == SpeechRecognitionMode.LongDictation)
            {
                this.micClient.OnResponseReceived += this.OnMicDictationResponseReceivedHandler;
            }

            this.micClient.OnConversationError += this.OnConversationErrorHandler;
        }

        private void OnConversationErrorHandler(object sender, SpeechErrorEventArgs e)
        {
            this.SetText(metaText, "--- OnConversationErrorHandler ---");
        }

        private void OnMicDictationResponseReceivedHandler(object sender, SpeechResponseEventArgs e)
        {
            this.SetText(metaText, "--- OnMicDictationResponseReceivedHandler ---");
        }

        private void OnMicShortPhraseResponseReceivedHandler(object sender, SpeechResponseEventArgs e)
        {
            Invoke((Action)(() =>
            {
                this.SetText(metaText, "--- OnMicShortPhraseResponseReceivedHandler ---");

                // we got the final result, so it we can end the mic reco.  No need to do this
                // for dataReco, since we already called endAudio() on it as soon as we were done
                // sending all the data.
                this.micClient.EndMicAndRecognition();

                this.WriteResponseResult(e);
            }));
        }

        private void OnMicrophoneStatus(object sender, MicrophoneEventArgs e)
        {
            this.SetText(metaText, "--- OnMicrophoneStatus ---");
        }

        private void OnPartialResponseReceivedHandler(object sender, PartialSpeechResponseEventArgs e)
        {
            this.SetText(metaText, "--- OnMicrophoneStatus ---");
        }

        private void WriteResponseResult(SpeechResponseEventArgs e)
        {
            if (e.PhraseResponse.Results.Length == 0)
            {
                this.WriteLine("No phrase response is available.");
            }
            else
            {
                this.WriteLine("********* Final n-BEST Results *********");
                for (int i = 0; i < e.PhraseResponse.Results.Length; i++)
                {
                    this.WriteLine(
                        "[{0}] Confidence={1}, Text=\"{2}\"",
                        i,
                        e.PhraseResponse.Results[i].Confidence,
                        e.PhraseResponse.Results[i].DisplayText);


                    if (e.PhraseResponse.Results[i].Confidence == Confidence.High)
                    {
                        sendText.Text = e.PhraseResponse.Results[i].DisplayText.Replace(".", ""); ;
                        sendButton_Click(this, new EventArgs());
                    }
                }

                this.WriteLine("\n");
            }
        }

        private void WriteLine(string format, params object[] args)
        {
            var formattedStr = string.Format(format, args);
            Trace.WriteLine(formattedStr);
            SetText(metaText, formattedStr);
        }

        #endregion

        #region Bot

        private async Task StartBotConversation()
        {
            client = new DirectLineClient(directLineSecret);

            conversation = await client.Conversations.StartConversationAsync();

            new System.Threading.Thread(async () => await ReadBotMessagesAsync(client, conversation.ConversationId)).Start();

            SetText(sendText, "Hi");

        }

        private void SetupVoice()
        {
            voice = new SpeechSynthesizer();
            voice.SelectVoiceByHints(VoiceGender.Female);
            voice.Volume = 100;
            voice.Rate = 0;
            voice.SpeakCompleted += Voice_SpeakCompleted;
            voice.SpeakStarted += Voice_SpeakStarted;
        }

        private void Voice_SpeakStarted(object sender, SpeakStartedEventArgs e)
        {
            this.micClient.EndMicAndRecognition();
        }

        private void Voice_SpeakCompleted(object sender, SpeakCompletedEventArgs e)
        {
            this.micClient.StartMicAndRecognition();
        }

        private async void Send(string input)
        {
            if (!string.IsNullOrEmpty(input))
            {
                Activity userMessage = new Activity
                {
                    From = new ChannelAccount(fromUser),
                    Text = input,
                    Type = ActivityTypes.Message
                };

                await client.Conversations.PostActivityAsync(conversation.ConversationId, userMessage);
            }
        }

        private async Task ReadBotMessagesAsync(DirectLineClient client, string conversationId)
        {
            string watermark = null;

            while (true)
            {
                var activitySet = await client.Conversations.GetActivitiesAsync(conversationId, watermark);
                watermark = activitySet?.Watermark;

                var activities = from x in activitySet.Activities
                                 where x.From.Id == botId
                                 select x;

                foreach (Activity activity in activities)
                {
                    SetText(chatHistoryText, activity.Text);

                    if (activity.Attachments != null)
                    {
                        foreach (Attachment attachment in activity.Attachments)
                        {
                            switch (attachment.ContentType)
                            {
                                case "application/vnd.microsoft.card.hero":
                                    RenderHeroCard(attachment);
                                    break;

                                case "image/png":
                                    SetText(metaText, $"Opening the requested image '{attachment.ContentUrl}'");

                                    Process.Start(attachment.ContentUrl);
                                    break;
                            }
                        }
                    }

                    SetText(metaText, JsonConvert.SerializeObject(activity));
                }

                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            }
        }

        private void SetText(TextBox txt, string text)
        {
            try
            {
                if (txt.InvokeRequired)
                {
                    Invoke((MethodInvoker)(() => txt.AppendText(text + "\n")));
                }
                else
                {
                    txt.AppendText(text);
                }
            }
            catch (Exception)
            {
                //sink this
            }


            // Read Text
            if (txt.Name == chatHistoryText.Name)
            {
                voice.SpeakAsync(text);
            }
        }

        private void RenderHeroCard(Attachment attachment)
        {
            const int Width = 70;
            Func<string, string> contentLine = (content) => string.Format($"{{0, -{Width}}}", string.Format("{0," + ((Width + content.Length) / 2).ToString() + "}", content));

            var heroCard = JsonConvert.DeserializeObject<HeroCard>(attachment.Content.ToString());

            if (heroCard != null)
            {
                SetText(chatHistoryText, string.Format("/{0}", new string('*', Width + 1)));
                SetText(chatHistoryText, string.Format("*{0}*", contentLine(heroCard.Title)));
                SetText(chatHistoryText, string.Format("*{0}*", new string(' ', Width)));
                SetText(chatHistoryText, string.Format("*{0}*", contentLine(heroCard.Text)));
                SetText(chatHistoryText, string.Format("{0}/", new string('*', Width + 1)));
            }
        }
        #endregion

        #region Event Handlers
        private void sendButton_Click(object sender, EventArgs e)
        {
            Send(sendText.Text);
            sendText.Clear();
        }

        private void sendText_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                Send(sendText.Text);
                sendText.Clear();
            }
        }


        #endregion

        private void Client_FormClosing(object sender, FormClosingEventArgs e)
        {
            Application.Exit();
        }

        private void Client_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (System.Windows.Forms.Application.MessageLoop)
            {
                this.micClient.EndMicAndRecognition();
                this.micClient = null;
                this.voice = null;
                // Use this since we are a WinForms app
                System.Windows.Forms.Application.Exit();
                System.Environment.Exit(1);
            }
            else
            {
                // Use this since we are a console app
                System.Environment.Exit(1);
            }
        }
    }
}
