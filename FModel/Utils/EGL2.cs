namespace FModel.Utils
{
    /// <summary>
    /// https://github.com/WorkingRobot/EGL2/blob/60c06ed0fb2a5e7c0798c401d7de57b6151716d8/gui/settings.cpp#L81
    /// </summary>
    static class EGL2
    {
        const uint FILE_CONFIG_MAGIC = 0xE6219B27;
        const ushort FILE_CONFIG_VERSION = (ushort)ESettingsVersion.Latest;
    }

    public enum ESettingsVersion : ushort
    {
        // Initial Version
        Initial,

        // Removes GameDir and MountDrive
        // Adds CommandArgs
        SimplifyPathsAndCmdLine,

        LatestPlusOne,
        Latest = LatestPlusOne - 1
    }
}
