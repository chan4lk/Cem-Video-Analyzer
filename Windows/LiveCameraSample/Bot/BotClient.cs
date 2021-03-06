﻿using Microsoft.Bot.Connector.DirectLine;
using Microsoft.CognitiveServices.SpeechRecognition;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;

namespace LiveCameraSample.Bot
{
    public class BotClient
    {
        private static string directLineSecret = Properties.Settings.Default.DirectLineSecret;
        private static string speechKey = Properties.Settings.Default.SpeechKey;
        private static string authenticationUri = Properties.Settings.Default.AuthURL;
        private static string fromUser = "DirectLineSampleClientUser";
        private static string botId = "samolebotdialog";
        private DirectLineClient client;
        private Microsoft.Bot.Connector.DirectLine.Conversation conversation;
        private SpeechSynthesizer voice;
        private MicrophoneRecognitionClient micClient;
        private SpeechRecognitionMode Mode = SpeechRecognitionMode.ShortPhrase;
        private static string recognizeFailed = "I'm sorry, I cannot recognize you.";
        private static string lastQuestion = string.Empty;

        public delegate void OnResponseRecived(string message, MessageType type);
        public event OnResponseRecived OnResponse;

        public delegate void OnTextRecived(string input);
        public event OnTextRecived OnText;

        public delegate void OnVoiceReconized(string answer);
        public event OnVoiceReconized OnVoice;

        public delegate void OnInitialized();
        public event OnInitialized OnInit;

        public delegate void OnConversationEnded();
        public event OnConversationEnded OnEnd;

        public event EventHandler OnError;

        public bool UserRecognized { get; set; }
        private bool isStopped = false;

        public BotClient()
        {
            this.initialize();
        }

        private void initialize()
        {
            this.isStopped = false;
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
                //this.micClient.StartMicAndRecognition();
                //MessageBox.Show("Hi");
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, new UnhandledExceptionEventArgs(ex, false));
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
            this.SetText(MessageType.Metadata, "--- OnConversationErrorHandler ---");
        }

        private void OnMicDictationResponseReceivedHandler(object sender, SpeechResponseEventArgs e)
        {
            this.SetText(MessageType.Metadata, "--- OnMicDictationResponseReceivedHandler ---");
        }

        private void OnMicShortPhraseResponseReceivedHandler(object sender, SpeechResponseEventArgs e)
        {

            this.SetText(MessageType.Metadata, "--- OnMicShortPhraseResponseReceivedHandler ---");

            // we got the final result, so it we can end the mic reco.  No need to do this
            // for dataReco, since we already called endAudio() on it as soon as we were done
            // sending all the data.
            this.micClient.EndMicAndRecognition();

            this.WriteResponseResult(e);

        }

        private void OnMicrophoneStatus(object sender, MicrophoneEventArgs e)
        {
            this.SetText(MessageType.Metadata, "--- OnMicrophoneStatus ---");
        }

        private void OnPartialResponseReceivedHandler(object sender, PartialSpeechResponseEventArgs e)
        {
            this.SetText(MessageType.Metadata, "--- OnMicrophoneStatus ---");
        }
        private int missedResonseCount = 0;

        private void WriteResponseResult(SpeechResponseEventArgs e)
        {
            if (e.PhraseResponse.Results.Length == 0)
            {
                //this.WriteLine("Please anwser the question");
                //if (missedResonseCount > 1)
                //{
                //    this.voice.Speak("I'm sorry. I could not hear you properly.");
                //}                
                //missedResonseCount++;               
                Send("dummy");
                //this.micClient.StartMicAndRecognition();
            }
            else
            {
                missedResonseCount = 0;
                this.WriteLine("********* Final n-BEST Results *********");
                for (int i = 0; i < e.PhraseResponse.Results.Length; i++)
                {
                    this.WriteLine(
                        "[{0}] Confidence={1}, Text=\"{2}\"",
                        i,
                        e.PhraseResponse.Results[i].Confidence,
                        e.PhraseResponse.Results[i].DisplayText);

                }

                var firstGuess = e.PhraseResponse.Results.FirstOrDefault().DisplayText.Replace(".", "");

                Send(firstGuess);
                this.WriteLine("\n");
            }
        }

        private void WriteLine(string format, params object[] args)
        {
            var formattedStr = string.Format(format, args);
            Trace.WriteLine(formattedStr);
            SetText(MessageType.Metadata, formattedStr);
        }

        #endregion

        #region Bot

        private async Task StartBotConversation()
        {
            try
            {
                client = new DirectLineClient(directLineSecret);

                conversation = await client.Conversations.StartConversationAsync();

                new System.Threading.Thread(async () =>
                {
                    try
                    {
                        await ReadBotMessagesAsync(client, conversation.ConversationId);
                    }
                    catch (Exception)
                    {
                    }
                }).Start();

                OnInit?.Invoke();
            }
            catch (Exception)
            {
            }
        }

        private void SetupVoice()
        {
            voice = new SpeechSynthesizer();
            voice.SelectVoiceByHints(VoiceGender.Male);
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
            if (UserRecognized)
                this.micClient.StartMicAndRecognition();
        }

        public async void Send(string input)
        {
            //this.micClient.EndMicAndRecognition();
            if (!isStopped)
            {
                try
                {
                    OnResponse?.Invoke(input, MessageType.History);

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
                catch (Exception)
                {

                }
               
                
            }
        }

        private async void SendResetActivity()
        {
            try
            {
                Activity activity = new Activity
                {
                    Type = ActivityTypes.EndOfConversation
                };

                await client.Conversations.PostActivityAsync(conversation.ConversationId, activity);
            }
            catch (Exception)
            {

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
                    SetText(MessageType.History, activity.Text);

                    SetText(MessageType.Metadata, JsonConvert.SerializeObject(activity));

                    if (activity.Text.ToLower().Contains("please enjoy"))
                    {
                        
                        this.Stop();
                        
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            }
        }

        private void SetText(MessageType type, string text)
        {
            
            OnResponse?.Invoke(text, type);

            // Read Text
            if (type == MessageType.History)
            {
                if (text.ToLower().Contains("code is"))
                    voice.Speak("I'm sorry. I could not hear you properly.");
                else
                voice.SpeakAsync(text);
            }
        }

        public void Reset()
        {
            this.Stop();
            this.initialize();
        }

        internal void Stop()
        {
            this.isStopped = true;
            this.micClient.EndMicAndRecognition();

            SendResetActivity();

            UserRecognized = false;

            if (this.Mode == SpeechRecognitionMode.ShortPhrase)
            {
                this.micClient.OnResponseReceived -= this.OnMicShortPhraseResponseReceivedHandler;
            }
            else if (this.Mode == SpeechRecognitionMode.LongDictation)
            {
                this.micClient.OnResponseReceived -= this.OnMicDictationResponseReceivedHandler;
            }

            this.micClient.OnConversationError -= this.OnConversationErrorHandler;

            OnEnd?.Invoke();
        }

        #endregion


    }
}
