using System;
using FModel.Framework;
using FModel.Settings;

namespace FModel.ViewModels.Commands;

public class RemindMeCommand : ViewModelCommand<UpdateViewModel>
{
    public RemindMeCommand(UpdateViewModel contextViewModel) : base(contextViewModel)
    {
    }

    public override void Execute(UpdateViewModel contextViewModel, object parameter)
    {
        switch (parameter)
        {
            case "Days":
                // check for update in 3 days
                UserSettings.Default.NextUpdateCheck = DateTime.Now.AddDays(3);
                break;
            case "Week":
                // check for update next week (a week starts on Monday)
                var delay = DayOfWeek.Monday - DateTime.Now.DayOfWeek;
                UserSettings.Default.NextUpdateCheck = DateTime.Now.AddDays(delay);
                break;
            case "Month":
                // check for update next month (if today is 31st, it will be 1st of next month)
                UserSettings.Default.NextUpdateCheck = DateTime.Now.AddDays(1 - DateTime.Now.Day).AddMonths(1);
                break;
            case "Never":
                // never check for updates
                UserSettings.Default.NextUpdateCheck = DateTime.MaxValue;
                break;
            default:
                // reset
                UserSettings.Default.NextUpdateCheck = DateTime.Now;
                break;
        }
    }
}
