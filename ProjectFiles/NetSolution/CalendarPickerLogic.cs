#region Using directives
using System;
using System.Globalization;
using FTOptix.NetLogic;
using UAManagedCore;
#endregion

public class CalendarPickerLogic : BaseNetLogic
{
    public override void Start()
    {
        // Init all global variables
        selectedDayVariable = LogicObject.GetVariable("SelDay");
        actualDayVariable = LogicObject.GetVariable("ActDay");
        selectedMonthYearVariable = LogicObject.GetVariable("SelMonthYear");
        selectedActMonthVariable = LogicObject.GetVariable("SelActMonth");
        isTodayVariable = LogicObject.GetVariable("IsToday");
        var outputDateVariable = Owner.GetAlias("OutputDate");
        inputDateVariable = outputDateVariable.GetVariable("SelectedDate");
        // Draw the calendar
        DrawCalendar();
    }

    public override void Stop()
    {
        // Nothing to do here
    }

    [ExportMethod]
    public void ChangeMonth(int monthsToAdd)
    {
        // Get the active month from SelMonthYear
        var currentlySelectedDate = GetSelectedDate();
        // Moving to next or previous month
        currentlySelectedDate = currentlySelectedDate.AddMonths(monthsToAdd);
        // Replace new month, year on variable
        selectedMonthYearVariable.Value = currentlySelectedDate.Date.ToString("MMMM yyyy");

        DateTime selectedDateTime = inputDateVariable.Value;
        selectedActMonthVariable.Value = currentlySelectedDate.Month == selectedDateTime.Month &&
                                         currentlySelectedDate.Year == selectedDateTime.Year;

        var dateTimeNow = DateTime.Now;

        isTodayVariable.Value = currentlySelectedDate.Month == dateTimeNow.Month &&
                                currentlySelectedDate.Year == dateTimeNow.Year &&
                                currentlySelectedDate.Day == dateTimeNow.Day;

        SundayToSaturday();
    }

    private void SundayToSaturday()
    {
        // Here I want to create an array of days where data starts from the first day of the week
        var currentlySelectedDate = GetSelectedDate();
        int daysInMonth = DateTime.DaysInMonth(currentlySelectedDate.Year, currentlySelectedDate.Month);
        string[] daysArray = new string[37];

        int j = 1;
        for (int i = 0; j <= daysInMonth; i++)
        {
            if (i >= (int)currentlySelectedDate.DayOfWeek)
            {
                daysArray[i] = j.ToString();
                j++;
            }
        }

        LogicObject.GetVariable("MonthDays").Value = daysArray;
    }

    [ExportMethod]
    public void SetDateTime()
    {
        string selectedDay = selectedMonthYearVariable.Value;
        int dayNumber = selectedDayVariable.Value;

        if (selectedDay != "")
        {
            var currentlySelectedDate = GetSelectedDate();
            currentlySelectedDate = currentlySelectedDate.AddDays(dayNumber - 1);
            inputDateVariable.Value = currentlySelectedDate.Date;

        }
    }

    [ExportMethod]
    public void SetToday()
    {
        inputDateVariable.Value = DateTime.Now.Date;
        DrawCalendar();
    }

    private void DrawCalendar()
    {
        DateTime selectedDateTime = inputDateVariable.Value;
        selectedDayVariable.Value = selectedDateTime.Day.ToString();
        actualDayVariable.Value = DateTime.Now.Day.ToString();
        selectedMonthYearVariable.Value = selectedDateTime.Date.ToString("MMMM yyyy");
        selectedActMonthVariable.Value = true;

        isTodayVariable.Value = selectedDateTime.Month == DateTime.Now.Month &&
                                selectedDateTime.Year == DateTime.Now.Year;

        SundayToSaturday();
    }

    private DateTime GetSelectedDate() => DateTime.ParseExact(selectedMonthYearVariable.Value, "MMMM yyyy", CultureInfo.CurrentCulture);

    private IUAVariable selectedDayVariable;
    private IUAVariable actualDayVariable;
    private IUAVariable selectedMonthYearVariable;
    private IUAVariable selectedActMonthVariable;
    private IUAVariable isTodayVariable;
    private IUAVariable inputDateVariable;
}
