// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
// PARTICULAR PURPOSE. 
// 
// Copyright (c) Microsoft Corporation. All rights reserved 
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

using Windows.Data.Json;
using Windows.Storage;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

// The data model defined by this file serves as a representative example of a strongly-typed
// model. The property names chosen coincide with data bindings in the standard item templates.
//
// Applications may use this model as a starting point and build on it, or discard it entirely and
// replace it with something appropriate to their needs. If using this model, you might improve app 
// responsiveness by initiating the data loading task in the code behind for App.xaml when the app 
// is first launched.

namespace TriviaGame.DataModel
{
    /// <summary>
    /// Generic item data model.
    /// </summary>
    public class TriviaQuestionItem
    {
        public TriviaQuestionItem(String uniqueId, String questionType, String questionText, String answerText, bool answered)
        {
            this.UniqueId = uniqueId;
            this.QuestionType = questionType;
            this.QuestionText = questionText;
            this.AnswerText = answerText;
            this.Answered = answered;
        }

        public string UniqueId { get; private set; }
        public string QuestionType { get; private set; }
        public string QuestionText { get; private set; }
        public string AnswerText { get; private set; }
        public bool Answered { get; set; }

        public override string ToString()
        {
            return this.QuestionText;
        }
    }

    /// <summary>
    /// Generic group data model.
    /// </summary>
    public class TriviaQuestionGroup
    {
        public TriviaQuestionGroup(String uniqueId, String title, String subtitle)
        {
            this.UniqueId = uniqueId;
            this.Title = title;
            this.Subtitle = subtitle;
            this.Items = new ObservableCollection<TriviaQuestionItem>();
        }

        public string UniqueId { get; private set; }
        public string Title { get; private set; }
        public string Subtitle { get; private set; }
        public ObservableCollection<TriviaQuestionItem> Items { get; private set; }

        public override string ToString()
        {
            return this.Title;
        }
    }

    /// <summary>
    /// Creates a collection of groups and items with content read from a static json file.
    /// 
    /// SampleDataSource initializes with data read from a static json file included in the 
    /// project.  This provides sample data at both design-time and run-time.
    /// </summary>
    public sealed class TriviaQuestionsDataSource
    {
        private static TriviaQuestionsDataSource _triviaDataSource = new TriviaQuestionsDataSource();
       
        private ObservableCollection<TriviaQuestionGroup> _groups = new ObservableCollection<TriviaQuestionGroup>();
        public ObservableCollection<TriviaQuestionGroup> Groups
        {
            get { return this._groups; }
        }

        public static async Task LoadQuestions(bool clear)
        {
            if (clear)
                _triviaDataSource._groups.Clear();

            await _triviaDataSource.LoadDataAsync();
        }

        public static List<TriviaQuestionGroup> GetGroups()
        {
            return _triviaDataSource.Groups.ToList();
        }

        public static async Task<IEnumerable<TriviaQuestionGroup>> GetGroupsAsync()
        {
            await _triviaDataSource.LoadDataAsync();

            return _triviaDataSource.Groups;
        }

        public static async Task<TriviaQuestionGroup> GetGroupAsync(string uniqueId)
        {
            await _triviaDataSource.LoadDataAsync();
            // Simple linear search is acceptable for small data sets
            var matches = _triviaDataSource.Groups.Where((group) => group.UniqueId.Equals(uniqueId));
            if (matches.Count() == 1) return matches.First();
            return null;
        }

        public static async Task<TriviaQuestionItem> GetItemAsync(string uniqueId)
        {
            await _triviaDataSource.LoadDataAsync();
            // Simple linear search is acceptable for small data sets
            var matches = _triviaDataSource.Groups.SelectMany(group => group.Items).Where((item) => item.UniqueId.Equals(uniqueId));
            if (matches.Count() == 1) return matches.First();
            return null;
        }

        private async Task LoadDataAsync()
        {
            if (this._groups.Count != 0)
                return;

            Uri dataUri = new Uri("ms-appx:///DataModel/TriviaQuestions.json");

            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(dataUri);
            string jsonText = await FileIO.ReadTextAsync(file);
            JsonObject jsonObject = JsonObject.Parse(jsonText);
            JsonArray jsonArray = jsonObject["Groups"].GetArray();

            foreach (JsonValue groupValue in jsonArray)
            {
                JsonObject groupObject = groupValue.GetObject();
                TriviaQuestionGroup group = new TriviaQuestionGroup(groupObject["UniqueId"].GetString(),
                                                            groupObject["Title"].GetString(),
                                                            groupObject["Subtitle"].GetString());

                foreach (JsonValue itemValue in groupObject["Items"].GetArray())
                {
                    JsonObject itemObject = itemValue.GetObject();
                    group.Items.Add(new TriviaQuestionItem(itemObject["UniqueId"].GetString(),
                                                       itemObject["QuestionType"].GetString(),
                                                       itemObject["QuestionText"].GetString(),
                                                       itemObject["AnswerText"].GetString(),
                                                       false));
                }
                this.Groups.Add(group);
            }
        }
    }
}