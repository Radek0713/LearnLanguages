using System;
using System.Collections.Generic;
using LearnLanguages.Models;

namespace LearnLanguages.Services
{
    public interface IDatabaseService
    {
        ProfileModel CurrentProfile { get; set; }
        bool TestConnection(out string errorMessage);

        List<ProfileModel> GetProfiles();
        void CreateProfile(string name);
        void DeleteProfile(int profileId);

        List<Department> GetDepartments();
        List<Topic> GetTopicsByDepartment(int departmentId);
        void MarkTopicAsSelected(int topicId);
        void UnmarkTopicAsSelected(int topicId);
        List<SelectedTopicItem> GetSelectedTopics();

        Dictionary<DateTime, int> GetReviewCountsForMonth(int year, int month);
        int GetOverdueReviewCount();
        int GetReviewCountForToday();
        int GetUpcomingReviewsCount(int days = 3);
        bool HasUnselectedTopics();

        List<TopicReviewSummary> GetTopicReviewSummariesForToday();
        List<FlashcardModel> GetFlashcardsForToday(int? topicId = null, bool onlyOverdue = false);
        void UpdateFlashcardProgress(FlashcardModel card, bool isCorrect);

        List<string> GetVocabularyWordsByTopic(int topicId);
        string GetGrammarTopicName(int grammarId);
        List<GrammarModel> GetGrammarTopics();
    }
}