namespace Pretzel.Audio
{
    /// <summary>Volume groups. Values are serialized; append new ones at the end.</summary>
    public enum AudioChannel
    {
        Master = 0,
        Music = 1,
        Sfx = 2,
        UI = 3,
        Voice = 4,
        Ambience = 5
    }
}
