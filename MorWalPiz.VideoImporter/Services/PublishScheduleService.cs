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
            return FindNextPublishSlot(startDate);
        }

        /// <summary>
        /// Gets publishing dates and times for multiple videos starting from a base date
        /// </summary>
        public List<(DateTime date, TimeSpan time)> GetPublishDateTimesForVideos(DateTime startDate, int videoCount)
        {
            var results = new List<(DateTime date, TimeSpan time)>();
            var currentDateTime = startDate;
            var usedSlots = new HashSet<(DateTime date, TimeSpan time)>();

            for (int i = 0; i < videoCount; i++)
            {
                var (date, time) = FindNextPublishSlot(currentDateTime, usedSlots);
                results.Add((date, time));
                usedSlots.Add((date, time));
                
                // Move to the next available slot (could be same day or next day)
                currentDateTime = date.Add(time).AddMinutes(1);
            }

            return results;
        }

        private (DateTime date, TimeSpan time) FindNextPublishSlot(DateTime startDateTime, HashSet<(DateTime date, TimeSpan time)> usedSlots = null)
        {
            using var context = _databaseService.CreateContext();
            var activeSchedules = context.PublishSchedules
                .Where(s => s.IsActive)
                .ToList()
                .OrderBy(s => s.PublishTime)
                .ToList();

            if (!activeSchedules.Any())
            {
                // Fallback to default behavior if no schedules exist
                return (startDateTime.Date, new TimeSpan(12, 0, 0));
            }

            var searchDate = startDateTime.Date;
            var searchTime = startDateTime.TimeOfDay;
            var maxSearchDays = 14; // Prevent infinite loops
            var searchDays = 0;
            
            usedSlots ??= new HashSet<(DateTime date, TimeSpan time)>();

            while (searchDays < maxSearchDays)
            {
                var dayOfWeek = searchDate.DayOfWeek;

                // Find all schedules that match this day, ordered by time
                var matchingSchedules = activeSchedules
                    .Where(s => WeekdayHelper.HasDay(s.DaysOfWeek, dayOfWeek))
                    .OrderBy(s => s.PublishTime)
                    .ToList();

                foreach (var schedule in matchingSchedules)
                {
                    // For the start date, only consider times that haven't passed yet
                    if (searchDate == startDateTime.Date && schedule.PublishTime <= searchTime)
                        continue;

                    var slot = (searchDate, schedule.PublishTime);
                    
                    // Check if this slot is already used
                    if (!usedSlots.Contains(slot))
                    {
                        return slot;
                    }
                }

                // Move to next day and reset time constraint
                searchDate = searchDate.AddDays(1);
                searchTime = TimeSpan.Zero;
                searchDays++;
            }

            // Fallback if no matching schedule found
            return (startDateTime.Date, new TimeSpan(12, 0, 0));
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
