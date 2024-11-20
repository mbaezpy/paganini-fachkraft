public interface IServeContextualHelp
{
    // This method will return a key (could be a string, enum, or any other identifier) that determines which help panel to show.
    string GetContextualHelpKey();
}