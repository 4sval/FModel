using FModel.Framework;

namespace FModel.ViewModels.Commands
{
    public class AudioCommand : ViewModelCommand<AudioPlayerViewModel>
    {
        public AudioCommand(AudioPlayerViewModel contextViewModel) : base(contextViewModel)
        {
        }

        public override void Execute(AudioPlayerViewModel contextViewModel, object parameter)
        {
            if (parameter is not string s)
                return;

            switch (s)
            {
                case "Previous":
                    contextViewModel.Previous();
                    break;
                case "PlayPause":
                    contextViewModel.PlayPauseOnStart();
                    break;
                case "ForcePlayPause":
                    contextViewModel.PlayPauseOnForce();
                    break;
                case "Stop":
                    contextViewModel.Stop();
                    break;
                case "Next":
                    contextViewModel.Next();
                    break;
                case "Remove":
                    contextViewModel.Remove();
                    break;
                case "Save":
                    contextViewModel.Save();
                    break;
                case "Save_Playlist":
                    contextViewModel.SavePlaylist();
                    break;
            }
        }
    }
}