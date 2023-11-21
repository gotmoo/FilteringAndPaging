using System.ComponentModel.DataAnnotations;

namespace EucRepo.Models;

public class UtilityCalendarDay
{
    [Key] public DateTime Date { get; set; }
}