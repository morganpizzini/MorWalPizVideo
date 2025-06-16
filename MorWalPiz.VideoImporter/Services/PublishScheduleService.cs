using System;
using System.Collections.Generic;
using System.Linq;
using MorWalPiz.VideoImporter.Data;
using MorWalPiz.VideoImporter.Models;

namespace MorWalPiz.VideoImporter.Services
{
    public class PublishScheduleService
    {
        private readonly DatabaseService _databaseService;

        public PublishScheduleService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        /// <summary>
        /// Gets the next publishing date and time for a video based on the current schedules
        /// </summary>
        public (DateTime date, TimeSpan time) GetNextPublishDateTime(DateTime startDate)
        {
            using var context = _databaseService.CreateContext();
            var activeSchedules = context.PublishSchedules
                .Where(s => s.IsActive)
                .ToList()
                .OrderBy(s => s.DaysOfWeek)
                .ThenBy(s => s.PublishTime)
                .ToList();

            if (!activeSchedules.Any())
            {
                // Fallback to default behavior if no schedules exist
                return (startDate, new TimeSpan(12, 0, 0));
            }

            return FindNextPublishSlot(startDate, activeSchedules);
        }

        /// <summary>
        /// Gets publishing dates and times for multiple videos starting from a base date
        /// </summary>
        public List<(DateTime date, TimeSpan time)> GetPublishDateTimesForVideos(DateTime startDate, int videoCount)
        {
            var results = new List<(DateTime date, TimeSpan time)>();
            var currentDate = startDate;

            for (int i = 0; i < videoCount; i++)
            {
                var (date, time) = GetNextPublishDateTime(currentDate);
                results.Add((date, time));
                
                // Move to next day for the next video
                currentDate = date.AddDays(1);
            }

            return results;
        }

        private (DateTime date, TimeSpan time) FindNextPublishSlot(DateTime startDate, List<PublishSchedule> schedules)
        {
            var searchDate = startDate;
            var maxSearchDays = 14; // Prevent infinite loops
            var searchDays = 0;

            while (searchDays < maxSearchDays)
            {
                var dayOfWeek = searchDate.DayOfWeek;

                // Find the first schedule that matches this day
                var matchingSchedule = schedules
                    .Where(s => WeekdayHelper.HasDay(s.DaysOfWeek, dayOfWeek))
                    .FirstOrDefault();

                if (matchingSchedule != null)
                {
                    return (searchDate, matchingSchedule.PublishTime);
                }

                // Move to next day
                searchDate = searchDate.AddDays(1);
                searchDays++;
            }

            // Fallback if no matching schedule found
            return (startDate, new TimeSpan(12, 0, 0));
        }

        /// <summary>
        /// Gets all active publish schedules
        /// </summary>
        public List<PublishSchedule> GetActiveSchedules()
        {
            using var context = _databaseService.CreateContext();
            return context.PublishSchedules
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToList();
        }

        /// <summary>
        /// Gets all publish schedules (active and inactive)
        /// </summary>
        public List<PublishSchedule> GetAllSchedules()
        {
            using var context = _databaseService.CreateContext();
            return context.PublishSchedules
                .OrderBy(s => s.Name)
                .ToList();
        }

        /// <summary>
        /// Saves a publish schedule
        /// </summary>
        public void SaveSchedule(PublishSchedule schedule)
        {
            using var context = _databaseService.CreateContext();
            
            if (schedule.Id == 0)
            {
                context.PublishSchedules.Add(schedule);
            }
            else
            {
                context.PublishSchedules.Update(schedule);
            }

            context.SaveChanges();
        }

        /// <summary>
        /// Deletes a publish schedule
        /// </summary>
        public void DeleteSchedule(int scheduleId)
        {
            using var context = _databaseService.CreateContext();
            var schedule = context.PublishSchedules.Find(scheduleId);
            if (schedule != null)
            {
                context.PublishSchedules.Remove(schedule);
                context.SaveChanges();
            }
        }

        /// <summary>
        /// Validates if a schedule configuration is valid
        /// </summary>
        public (bool isValid, string errorMessage) ValidateSchedule(PublishSchedule schedule)
        {
            if (string.IsNullOrWhiteSpace(schedule.Name))
            {
                return (false, "Il nome della pianificazione è obbligatorio.");
            }

            if (schedule.DaysOfWeek == 0)
            {
                return (false, "Deve essere selezionato almeno un giorno della settimana.");
            }

            if (schedule.PublishTime < TimeSpan.Zero || schedule.PublishTime >= TimeSpan.FromDays(1))
            {
                return (false, "L'orario di pubblicazione non è valido.");
            }

            using var context = _databaseService.CreateContext();
            var existingSchedule = context.PublishSchedules
                .FirstOrDefault(s => s.Name == schedule.Name && s.Id != schedule.Id);

            if (existingSchedule != null)
            {
                return (false, "Esiste già una pianificazione con questo nome.");
            }

            return (true, string.Empty);
        }
    }
}
