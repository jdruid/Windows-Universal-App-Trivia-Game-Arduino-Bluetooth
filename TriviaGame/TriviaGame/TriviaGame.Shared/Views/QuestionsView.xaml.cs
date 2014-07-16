// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
// PARTICULAR PURPOSE. 
// 
// Copyright (c) Microsoft Corporation. All rights reserved 
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Windows.UI.Popups;

// Timer
using System.Threading;
using System.Threading.Tasks;

// Trvia Game
using TriviaGame;
using TriviaGame.DataModel;
using TriviaGame.Common;
using TriviaGame.Common.Arduino;
using TriviaGame.Controls;

// Speech
using Windows.Media.SpeechSynthesis;
using Windows.Media;

using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace TriviaGame.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class QuestionsView : Page
    {
        #region Variables
        //Game Settings
        public const int GAME_TIME = 120;
        public const int SKIP_TIME = 8;
        public const int SCORE_INCREMENT = 100;
        public const int SCORE_DECREMENT = 50;
        public const int SKIP_DECREMENT = 2;
        public const int SCORE_TIME_INCREMENT = 1;
        //Game Variables
        private int score;
        private bool flashRed;
        private int timeLeft, skipTimeLeft;
        private Timer timer;
        private int currentQuestionCount;
        private int questionCount;
        private int categoryIndex;
        //Questions
        List<TriviaQuestionItem> questionItems;
        List<TriviaQuestionGroup> triviaGroups;
        TriviaQuestionItem questionItem;
        //Speech
        private SpeechSynthesizer synthesizer;
        private bool disposed = false;      
        #endregion

        public QuestionsView()
        {
            this.InitializeComponent();

            App.BluetoothManager.MessageReceived += BluetoothManager_MessageReceived;
            App.BluetoothManager.ExceptionOccured += BluetoothManager_ExceptionOccured;
                      
            this.synthesizer = new SpeechSynthesizer();

            TrueButton.IsEnabled = false;
            FalseButton.IsEnabled = false;

            ResetDuinoLED();
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            TextBlockQuestion.Width = GameGrid.RenderSize.Width;
        }

        #region Bluetooth Lifecycle
        protected override void OnNavigatedFrom(Windows.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            App.BluetoothManager.Disconnect(); // clean up the mess
        }

        private async void BluetoothManager_ExceptionOccured(object sender, Exception ex)
        {
            var md = new MessageDialog(ex.Message, "We've got a problem with bluetooth...");
            md.Commands.Add(new UICommand("Doh!"));
            md.DefaultCommandIndex = 0;
            var result = await md.ShowAsync();
        }

        private void BluetoothManager_MessageReceived(object sender, string message)
        {
            switch (message)
            {
                case "RED_PRESSED":
                    ValidateAnswer("false");
                    break;
                case "GREEN_PRESSED":
                    ValidateAnswer("true");
                    break;
            }

            System.Diagnostics.Debug.WriteLine(message);
        }
        #endregion
       
        #region Event Handlers - buttons
        private async void Connect_Click(object sender, RoutedEventArgs e)
        {
            //ask the user to connect
            await App.BluetoothManager.EnumerateDevicesAsync(GetElementRect((FrameworkElement)sender));
        }
        
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            //cancel current connection attempts
            App.BluetoothManager.AbortConnection();
        }
        
        private void Disconnect_Click(object sender, RoutedEventArgs e)
        {
            //disconnect the connection
            App.BluetoothManager.Disconnect();
        }
       
        /// <summary>
        /// Kick the application off
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartGame_Click(object sender, RoutedEventArgs e)
        {
            TrueButton.IsEnabled = true;
            FalseButton.IsEnabled = true;

            InitGame();
            ConfigureCategory();
            ConfigureQuestion();
        }

        /// <summary>
        /// Handle the button touch events
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AnswerButton_Click(object sender, RoutedEventArgs e)
        {
            Button _button = (Button)sender;
            string value = _button.CommandParameter.ToString();

            ValidateAnswer(value);
        }
        
        #endregion

        #region GUI Dropdown
        public static Rect GetElementRect(FrameworkElement element)
        {
            GeneralTransform buttonTransform = element.TransformToVisual(null);
            Point point = buttonTransform.TransformPoint(new Point(0, 0));
            return new Rect(point, new Size(element.ActualWidth, element.ActualHeight));
        }
        #endregion

        #region Game Events
        private void InitGame()
        {
            currentQuestionCount = 0;
            questionCount = 0;
            score = 0;
            timeLeft = GAME_TIME;
            TextBlockScore.Text = "0";

            UpdateTimer();

            timer = new Timer(TimerTick, null, 1000, 1000);
            flashRed = false;
        }

        // If there were multiple categories, sample data only has 1 category
        private void ConfigureCategory()
        {
            triviaGroups = TriviaQuestionsDataSource.GetGroups();
            //How many questions?
            questionCount = triviaGroups[categoryIndex].Items.Count;

            //Get "Random" Category
            int groupCount = triviaGroups.Count;
            categoryIndex = 0;
            if (groupCount > 0)
            {
                Random r = new Random();
                categoryIndex = r.Next(0, groupCount);
            }

            TextBlockCategory.Text = triviaGroups[categoryIndex].Title;
        }

        private void ConfigureQuestion()
        {
            questionItems = triviaGroups[categoryIndex].Items.ToList();
            //make sure only NON-Answered questions are shown
            GetNonAnsweredQuestion();
        }

        private async void GetNonAnsweredQuestion()
        {
            if (currentQuestionCount >= questionCount)
            {
                //Load questions again for new game
                await TriviaQuestionsDataSource.LoadQuestions(true);
                //Send to Over Screen
                Frame.Navigate(typeof(OverView));
                return;
            }

            Random r = new Random();

            int index = questionItems.Where(x => !x.Answered).Count() == 0 ? -1 : r.Next(questionItems.Where(x => !x.Answered).Count());
            questionItem = index == -1 ? null : questionItems.Where(x => !x.Answered).ToList()[index];
            if (questionItem != null)
                questionItem.Answered = true;

            int questionToSpeak = currentQuestionCount+1;

            string ssmlText = "Question " + questionToSpeak.ToString() + " <break time=\"500ms\" /> True or False <break time=\"500ms\" />" + questionItem.QuestionText.ToString();

            TextToSpeech(ssmlText);

            TextBlockQuestion.Text = questionItem.QuestionText.ToString();

            currentQuestionCount++;
        }

        private async void ValidateAnswer(string userAnswer)
        {

            string correctAnswer = questionItem.AnswerText.ToString().ToLower();
            string speechText = string.Empty;

            if (correctAnswer == userAnswer)
            {
                //Correct - increment score
                score += SCORE_INCREMENT;
                //speech text
                speechText = "Nice Job";
                //light LED
                SetDuinoLED("GREEN");
            }
            else
            {
                //Incorrect - decrement score
                score -= SCORE_DECREMENT;
                //speech text
                speechText += "I'm sorry <break time=\"500ms\" /> that is incorrect";
                //light LED
                SetDuinoLED("RED");
            }

            //alert user
            TextToSpeech(speechText);

            //update score
            TextBlockScore.Text = score.ToString();

            //pause
            await Task.Delay(TimeSpan.FromMilliseconds(1000));
            //get next question
            GetNonAnsweredQuestion();

        }
        
        #endregion

        #region Speech
        private async void TextToSpeech(string textToSpeak)
        {
            string ssmlText = "<speak version=\"1.0\" ";
            ssmlText += "xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\">";
            ssmlText += textToSpeak;
            ssmlText += "</speak>";

            SpeechSynthesisStream synthesisStream;
            try
            {
                //creating a stream from the text which can be played using media element. This new API converts text input into a stream.
                synthesisStream = await this.synthesizer.SynthesizeSsmlToStreamAsync(ssmlText);

            }
            catch (Exception)
            {
                synthesisStream = null;
            }

            // start this audio stream playing
            this.media.AutoPlay = true;
            this.media.SetSource(synthesisStream, synthesisStream.ContentType);
            this.media.Play();
        }
        #endregion

        #region Arduino Messaging
        private async void ResetDuinoLED()
        {
            string cmd = "TURN_OFF_GREEN";
            var res = await App.BluetoothManager.SendMessageAsync(cmd);
            cmd = "TURN_OFF_RED";
            res = await App.BluetoothManager.SendMessageAsync(cmd);
        }

        private async void SetDuinoLED(string color)
        {
            //send ON commands
            string cmd = string.Format("TURN_ON_{0}", color);
            //try to send this message
            var res = await App.BluetoothManager.SendMessageAsync(cmd);
        }
        #endregion

        #region Countdown Timer
        private void UpdateTimer()
        {
            //Show how much time is left in the game
            int minute = (timeLeft / 60);
            int seconds = (timeLeft % 60);
            TextBlockTimer.Text = minute + ":" + (seconds < 10 ? "0" : "") + seconds;
            TextBlockTimer.Foreground = flashRed ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 200, 0, 0)) : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255));
        }

        private async void TimerTick(object state)
        {
            //Ticks every seconds
            timeLeft--;
            skipTimeLeft--;

            //Manages the lifespan of the game
            if (timeLeft >= 0)
            {
                if (timeLeft < 10)
                    flashRed = true;
                else if (timeLeft <= 20)
                    flashRed = !flashRed;

                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, UpdateTimer);
            }
            else
            {
                timer.Dispose();
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    Frame.Navigate(typeof(MainPage), score);
                });
            }


        }
        #endregion

        #region Dispose Cleanups
        /// <summary>
        /// Performs any finalization method needed
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// cleans up all local objects disposable objects
        /// </summary>
        /// <param name="disposing"></param>
        private void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.synthesizer.Dispose();
                }
            }

            this.disposed = true;
        }
        #endregion

    }
}
