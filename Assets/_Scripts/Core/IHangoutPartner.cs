// IHangoutPartner.cs

/// <summary>
/// A contract for any game entity that can participate in a hangout session.
/// This allows the HangoutManager to be generic and not know about NPCs specifically.
/// </summary>
public interface IHangoutPartner
{
    // A method for the HangoutManager to tell the participant to enter its "Hangout" state.
    void BeginHangoutState();

    // A method for the HangoutManager to tell the participant to return to its normal state.
    void EndHangoutState();
}